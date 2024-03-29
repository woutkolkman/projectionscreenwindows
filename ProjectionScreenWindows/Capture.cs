﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static ProjectionScreenWindows.Options;

namespace ProjectionScreenWindows
{
    public class Capture : FivePebblesPong.FPGame
    {
        public Process captureProcess;
        public int frame = 0;
        public DateTime measureFps = DateTime.Now;
        public FivePebblesPong.ShowMediaMovementBehavior adjusting = new FivePebblesPong.ShowMediaMovementBehavior();
        public int[] cropFrames = new int[] { Options.cropLeft.Value, Options.cropBottom.Value, Options.cropRight.Value, Options.cropTop.Value }; //left-bottom-right-top

        Queue<byte[]> imgLoad = new Queue<byte[]>();
        Mutex imgLoadMtx = new Mutex(); //prevents queue from being used twice at the same time

        Queue<ProjectedImage> imgLoiter = new Queue<ProjectedImage>(); //prevents flashing images
        Queue<Texture2D> texLoiter = new Queue<Texture2D>(); //prevents memory leak
        public const int IMG_LOITER_COUNT = 3; //prevents flashing images

        Queue<string> imgUnload = new Queue<string>();
        public const int IMG_UNLOAD_AT_COUNT = 4; //delay atlas unload so game doesn't throw exceptions

        Options.PositionTypes posType = Options.PositionTypes.Middle;
        public Vector2 offsetPos = new Vector2();
        public Vector2 targetPos, actualPos;


        //constructor starts background process, every newline received will be handled by DataReceivedEvent()
        public Capture(OracleBehavior self) : base(self)
        {
            string args = "";
            if (!String.IsNullOrEmpty(Options.windowName?.Value))
                args += " -c \"" + Options.windowName.Value + "\"";
            if (!String.IsNullOrEmpty(Options.processName?.Value))
                args += " -p \"" + Options.processName.Value + "\"";
            if (!String.IsNullOrEmpty(Options.openProgram?.Value))
                args += " -o \"" + Options.openProgram.Value + "\"";
            if (!String.IsNullOrEmpty(Options.openProgramArguments?.Value))
                args += " -a \"" + Options.openProgramArguments.Value + "\"";
            if (Options.framerate != null)
                args += " -f \"" + Options.framerate.Value.ToString() + "\"";
            if (Options.altOpenProgram?.Value != null && Options.altOpenProgram.Value)
                args += " --alt";
            if (Options.reduceStartupTime?.Value != null && Options.reduceStartupTime.Value)
                args += " -s";

            Plugin.ME.Logger_p.LogInfo("Capture, Arguments: \"" + args + "\"");

            targetPos = new Vector2(midX, midY); //default target position center of screen
            adjusting.showMediaPos = targetPos; //start random move position center of screen
            actualPos = targetPos + offsetPos; //actual pos is final position of image

            //check position type
            if (Options.positionType?.Value != null)
                foreach (Options.PositionTypes val in Enum.GetValues(typeof(PositionTypes)))
                    if (String.Equals(Options.positionType.Value, val.ToString()))
                        posType = val;
            Plugin.ME.Logger_p.LogInfo("Capture, posType: " + posType.ToString());

            //set offset position
            if (Options.offsetPosX?.Value != null && Options.offsetPosY?.Value != null)
                offsetPos = new Vector2(Options.offsetPosX.Value, Options.offsetPosY.Value);

            //create OracleProjectionScreen in case of no projectionscreen
            if (self?.oracle != null && self.oracle.myScreen == null)
                self.oracle.myScreen = new OracleProjectionScreen(self.oracle.room, self);

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ProcessStartInfo info = new ProcessStartInfo(assemblyFolder + "\\CaptureAPI.exe");
            info.Arguments = args;
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;

            Plugin.ME.Logger_p.LogInfo("Capture, Starting CaptureAPI");
            try {
                captureProcess = Process.Start(info);
                captureProcess.OutputDataReceived += new DataReceivedEventHandler(DataReceivedEvent);
                captureProcess.BeginOutputReadLine();
            } catch (Exception ex) {
                Plugin.ME.Logger_p.LogError("Capture, Start exception: " + ex.ToString());
            }
        }


        ~Capture() //destructor
        {
            this.Destroy(); //if not done already
        }


        //background process is stopped and all memory and queues are freed
        public override void Destroy()
        {
            base.Destroy(); //empty
            imgLoad.Clear();

            //exit captureProcess
            //Process.CloseMainWindow does not close a console app cleanly, sending "ctrl + c" does
            //the background process listens for "c" newline and then exits, ProcessStartInfo.RedirectStandardInput must be true
            Task closeProcess = Task.Factory.StartNew(() => //prevents short lagspike
            {
                if (captureProcess != null && !captureProcess.HasExited) {
                    try {
                        captureProcess.StandardInput.WriteLine("c"); //close background process
                        if (!captureProcess.WaitForExit(500))
                            Plugin.ME.Logger_p.LogInfo("Capture.StopProgram, Failed communicating close operation");
                    } catch (Exception ex) {
                        Plugin.ME.Logger_p.LogInfo("Capture.StopProgram, Exception: " + ex.ToString());
                    }

                    if (captureProcess != null && !captureProcess.HasExited) {
                        Plugin.ME.Logger_p.LogInfo("Capture.Destroy, Calling CloseMainWindow");
                        if (!captureProcess.CloseMainWindow())
                            Plugin.ME.Logger_p.LogInfo("Capture.Destroy, CloseMainWindow failed");
                        captureProcess.WaitForExit(500);
                    }
                    string msg = captureProcess.HasExited ? "exited" : "did not exit";
                    Plugin.ME.Logger_p.LogInfo("Capture.Destroy, CaptureAPI " + msg);
                    captureProcess?.Close();
                }
                captureProcess = null;
            });

            //TODO program keeps running if exiting RainWorld while Capture was active

            //clear queues
            while (imgLoiter.Count > 0) {
                ProjectedImage img = imgLoiter.Dequeue();
                img.RemoveFromRoom();
                foreach (string name in img.imageNames)
                    imgUnload.Enqueue(name);
                img.Destroy();
            }

            while (texLoiter.Count > 0) { //prevents memory leak
                Texture2D tex = texLoiter.Dequeue();
                if (tex != null)
                    Texture2D.Destroy(tex);
            }

            Task deload = Task.Factory.StartNew(() => //prevents atlasmanager exceptions
            {
                Thread.Sleep(1000);
                if (imgUnload == null) {
                    Plugin.ME.Logger_p.LogWarning("Capture.Destroy task, imgUnload queue is null, result: possible memory leak");
                    return;
                }
                while (imgUnload.Count > 0) {
                    string name = imgUnload.Dequeue();
                    Futile.atlasManager.ActuallyUnloadAtlasOrImage(name);
                }
            });
        }


        //read queues filled by DataReceivedEvent() and create/delete images
        public override void Update(OracleBehavior self)
        {
            base.Update(self);

            PuppetBehavior(self);

            //get target position type for images
            switch (posType)
            {
                case Options.PositionTypes.Player:
                    targetPos = p?.DangerPos ?? self.player?.DangerPos ?? new Vector2(midX, midY);
                    break;

                case Options.PositionTypes.Puppet:
                    targetPos = self.oracle?.bodyChunks[0]?.pos ?? new Vector2(midX, midY);
                    break;

                case Options.PositionTypes.Mouse:
                    targetPos = (Vector2) Futile.mousePosition + ((self.oracle?.room?.game?.cameras?.Length > 0) ? self.oracle.room.game.cameras[0].pos : new Vector2());
                    break;

                case Options.PositionTypes.Middle:
                default:
                    targetPos = new Vector2(midX, midY);
                    break;
            }

            //get setpoint for adjust image randomly
            if (Options.moveRandomly.Value || targetPos != adjusting.showMediaPos)
                adjusting.Update(self, targetPos, !Options.moveRandomly.Value);

            //unload previous frames
            if (imgLoiter.Count >= IMG_LOITER_COUNT) {
                ProjectedImage img = imgLoiter.Dequeue();
                self.oracle.room.RemoveObject(img);
                foreach (string name in img.imageNames)
                    imgUnload.Enqueue(name);
                img.Destroy();
            }

            if (imgUnload.Count >= IMG_UNLOAD_AT_COUNT)
                Futile.atlasManager.ActuallyUnloadAtlasOrImage(imgUnload.Dequeue());

            //get new frame if available
            Texture2D newFrame = GetNewFrame();
            if (newFrame == null)
                return;

            string imgName = "FPP_Window_" + frame;

            //load and display new frame
            ProjectedImage temp;
            if ((self is SLOracleBehavior && !ModManager.MSC) || self is MoreSlugcats.SSOracleRotBehavior) {
                temp = new FivePebblesPong.MoonProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0) { pos = actualPos };
            } else {
                temp = new FivePebblesPong.ProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0) { pos = actualPos };
            }
            imgLoiter.Enqueue(temp);
            self.oracle.myScreen.room.AddObject(temp);

            //measure FPS every 100th frame
            if (frame % 100 == 0) {
                TimeSpan diff = DateTime.Now - measureFps;
                Plugin.ME.Logger_p.LogInfo("Capture.Update, Average FPS projection: " + (frame / diff.TotalSeconds).ToString());
                frame = 0;
                measureFps = DateTime.Now;
            }
        }


        //parses new frame from buffer and returns the parsed & cropped image or null
        public Texture2D GetNewFrame()
        {
            if (texLoiter.Count >= IMG_LOITER_COUNT) { //prevents memory leak
                Texture2D tex = texLoiter.Dequeue();
                if (tex != null)
                    Texture2D.Destroy(tex);
            }

            if (imgLoad.Count <= 0)
                return null;

            //get new frame and save
            if (!imgLoadMtx.WaitOne(50)) {
                Plugin.ME.Logger_p.LogInfo("Capture.GetNewFrame, Mutex timeout");
                return null;
            }
            Texture2D newFrame = new Texture2D(0, 0);
            try {
                byte[] imageBase64 = imgLoad.Dequeue();
                newFrame.LoadImage(imageBase64);
            } catch (Exception ex) {
                Plugin.ME.Logger_p.LogInfo("Capture.GetNewFrame, Error storing data: " + ex.ToString());
            }
            imgLoadMtx.ReleaseMutex();

            if (newFrame == null || newFrame.width <= 0 || newFrame.height <= 0) {
                Texture2D.Destroy(newFrame);
                return null;
            }
            texLoiter.Enqueue(newFrame); //prevents memory leak

            //chroma keying option
            if (Options.chromaKeying?.Value == true) {
                Color[] pixels = newFrame.GetPixels(0, 0, newFrame.width, newFrame.height);
                int error = Options.chromaKeyError?.Value ?? 1;
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i] / error == Options.chromaKeyColor?.Value / error)
                        pixels[i] = Color.clear;
                newFrame.SetPixels(pixels);
                newFrame.Apply();
            }

            newFrame = AddTransparentBorder(ref newFrame, cropFrames);
            frame++;
            return newFrame;
        }


        //update all image positions
        public override void Draw(Vector2 offset)
        {
            actualPos = adjusting.showMediaPos - (Options.ignoreOrigPos.Value ? new Vector2() : offset) + offsetPos;
            foreach (ProjectedImage img in imgLoiter)
                img.pos = actualPos;
        }


        //called at every newline received from background process, if it's Base64 --> enqueue
        public bool firstEventMsg = true;
        public int droppedFrames = 0;
        public void DataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
            //empty console queue (for in options menu) if not in use
            while (Options.consoleQueue.Count > 3)
                if (!Options.consoleQueue.TryDequeue(out _))
                    break;

            if (String.IsNullOrEmpty(e?.Data)) {
                const string msg = "Capture.DataReceivedEvent, Data null or empty";
                Plugin.ME.Logger_p.LogInfo(msg);
                Options.consoleQueue.Enqueue(msg);
                return;
            }

            //every newline is a frame
            byte[] imageBase64;
            try {
                imageBase64 = Convert.FromBase64String(e.Data);
                //File.WriteAllBytes("C:\\test\\test.png", bytes);
            } catch (FormatException) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, \"" + e.Data + "\"");
                Options.consoleQueue.Enqueue(String.Copy(e.Data));
                return;
            } catch (ArgumentNullException ex) {
                string msg = "Capture.DataReceivedEvent, Error parsing data: " + ex.ToString();
                Plugin.ME.Logger_p.LogInfo(msg);
                Options.consoleQueue.Enqueue(String.Copy(msg));
                return;
            }

            if (imgLoad.Count > 5) {
                string msg = "Capture.DataReceivedEvent, Byte[] queue too large, dropping frame... [" + droppedFrames++ + "]";
                Plugin.ME.Logger_p.LogInfo(msg);
                Options.consoleQueue.Enqueue(String.Copy(msg));
                return;
            }

            if (firstEventMsg) {
                const string msg = "Capture.DataReceivedEvent, Received first valid frame";
                Plugin.ME.Logger_p.LogInfo(msg);
                Options.consoleQueue.Enqueue(msg);
            }
            firstEventMsg = false;

            //the byte array MUST be queued, calling the Texture2D ctor here crashes the game apparently
            if (!imgLoadMtx.WaitOne(50)) {
                const string msg = "Capture.DataReceivedEvent, Mutex timeout";
                Plugin.ME.Logger_p.LogInfo(msg);
                Options.consoleQueue.Enqueue(msg);
                return;
            }
            imgLoad.Enqueue(imageBase64);
            imgLoadMtx.ReleaseMutex();
        }


        //transparent border is added so projectionshader works correctly
        //crop, left-bottom-right-top, right and top need to be negative to crop
        public static Texture2D AddTransparentBorder(ref Texture2D texIn, int[] crop = null)
        {
            int width(int[] a) { return a[2] - a[0]; }
            int height(int[] a) { return a[3] - a[1]; }

            if (texIn == null || texIn.width <= 0 || texIn.height <= 0)
                return texIn;

            //get new sides of image with crop
            int[] offsets = { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top
            if (crop != null && crop.Length >= 4)
                for (int i = 0; i < crop.Length; i++)
                    offsets[i] += crop[i];

            //check valid crop, if not valid --> ignore crop
            if (width(offsets) <= 0 || height(offsets) <= 0)
                offsets = new int[] { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top

            Texture2D texOut = new Texture2D(
                width(offsets) + (2*FivePebblesPong.CreateGamePNGs.EDGE_DIST), 
                height(offsets) + (2*FivePebblesPong.CreateGamePNGs.EDGE_DIST), 
                TextureFormat.ARGB32, 
                false
            );

            //transparent background
            FivePebblesPong.CreateGamePNGs.FillTransparent(ref texOut);
            texOut.Apply();

            //copies via GPU
            UnityEngine.Graphics.CopyTexture(
                texIn, 0, 0, offsets[0], offsets[1], 
                width(offsets), height(offsets), texOut, 0, 0, 
                FivePebblesPong.CreateGamePNGs.EDGE_DIST, 
                FivePebblesPong.CreateGamePNGs.EDGE_DIST
            );

            return texOut;
        }


        int checkForGrabItem = 0;
        public void PuppetBehavior(OracleBehavior self)
        {
            bool tooClose = Vector2.Distance(self?.oracle?.firstChunk?.pos ?? new Vector2(), (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2())) <= 60;

            //5P & pre-collapse Moon
            if (self is SSOracleBehavior) {
                (self as SSOracleBehavior).movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

                //look at window if player is not too close
                if (!tooClose && (Options.puppetLooksAtWindow?.Value == null || Options.puppetLooksAtWindow.Value))
                    (self as SSOracleBehavior).lookPoint = actualPos;
            }

            //Shoreline Moon
            if (self is SLOracleBehavior) {
                (self as SLOracleBehavior).movementBehavior = SLOracleBehavior.MovementBehavior.KeepDistance;

                //look at window if player is not interfering
                if (Options.puppetLooksAtWindow?.Value == null || Options.puppetLooksAtWindow.Value || 
                    (self as SLOracleBehavior).holdingObject is FivePebblesPong.GameController)
                    if (((self is SLOracleBehaviorNoMark) && !tooClose) || 
                        ((self is SLOracleBehaviorHasMark) && 
                        !(self as SLOracleBehaviorHasMark).playerIsAnnoyingWhenNoConversation && 
                        !(self as SLOracleBehaviorHasMark).playerHoldingNeuronNoConvo && 
                        (self as SLOracleBehaviorHasMark).playerAnnoyingCounter < 20))
                        FivePebblesPong.SLGameStarter.moonLookPoint = actualPos;

                //prevent moon from auto releasing controller if controller was grabbed
                if (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).holdingObject is FivePebblesPong.GameController)
                    (self as SLOracleBehaviorHasMark).describeItemCounter = 0;
            }

            //Rivulet's 5P
            if (self is MoreSlugcats.SSOracleRotBehavior) {
                //look at window if player is not too close or if holding gamecontroller
                if (!tooClose && (Options.puppetLooksAtWindow?.Value == null || Options.puppetLooksAtWindow.Value || 
                    (self as MoreSlugcats.SSOracleRotBehavior).holdingObject is FivePebblesPong.GameController))
                    self.lookPoint = actualPos;

                //grab gamecontroller if close enough
                checkForGrabItem--;
                if (checkForGrabItem > 0)
                    return;
                checkForGrabItem = 240;
                /*//TODO, doesn't work as expected because FivePebblesPong currently does not start RM games if puppet is holding an object
                if ((self as MoreSlugcats.SSOracleRotBehavior).holdingObject != null)
                    return;
                Plugin.ME.Logger_p.LogInfo("Capture.PuppetBehavior, Checking for GrabObject");

                for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                    for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                        if (self.oracle.room.physicalObjects[i][j] is FivePebblesPong.GameController && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                            if (30f >= Vector2.Distance(self.oracle.room.physicalObjects[i][j].firstChunk.pos, self.oracle.bodyChunks[0].pos))
                                (self as MoreSlugcats.SSOracleRotBehavior).GrabObject(self.oracle.room.physicalObjects[i][j]);
                */
            }
        }
    }
}
