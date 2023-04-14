using System;
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
        Process captureProcess;
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
            if (Options.altOpenProgram != null && Options.altOpenProgram.Value)
                args += " --alt";

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
                foreach (string name in img.imageNames) {
//                    Plugin.ME.Logger_p.LogInfo("Capture.Destroy, RemoveFromRoom: \"" + name + "\"");
                    imgUnload.Enqueue(name);
                }
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
//                    Plugin.ME.Logger_p.LogInfo("Capture.Destroy task, Unload: \"" + name + "\"");
                    Futile.atlasManager.ActuallyUnloadAtlasOrImage(name);
                }
            });
        }


        //read queues filled by DataReceivedEvent() and create/delete images
        public override void Update(OracleBehavior self)
        {
            base.Update(self);

            //behavior of puppets
            if (self is SSOracleBehavior)
                (self as SSOracleBehavior).movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            if (self is SLOracleBehavior) {
                (self as SLOracleBehavior).movementBehavior = SLOracleBehavior.MovementBehavior.KeepDistance;
                if ((self as SLOracleBehavior).holdingObject is FivePebblesPong.GameController)
                    self.lookPoint = actualPos;
            }
            if (self is MoreSlugcats.SSOracleRotBehavior && (self as MoreSlugcats.SSOracleRotBehavior).holdingObject is FivePebblesPong.GameController)
                self.lookPoint = actualPos;

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
//            Plugin.ME.Logger_p.LogInfo("Capture.Update, Creating: \"" + imgName + "\"");

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

            if (newFrame?.width <= 0 || newFrame.height <= 0) {
                Texture2D.Destroy(newFrame);
                return null;
            }
            texLoiter.Enqueue(newFrame); //prevents memory leak

            newFrame = CreateGamePNGs.AddTransparentBorder(ref newFrame, cropFrames);
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
            if (String.IsNullOrEmpty(e?.Data)) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Data null or empty");
                return;
            }

            if (imgLoad.Count > 5) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Byte[] queue too large, dropping frame... [" + droppedFrames++ + "]");
                return;
            }

            //every newline is a frame
            byte[] imageBase64 = new byte[0];
            try {
                imageBase64 = Convert.FromBase64String(e.Data);
//                File.WriteAllBytes("C:\\test\\test.png", bytes);
            } catch (FormatException) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, \"" + e.Data + "\"");
                return;
            } catch (ArgumentNullException ex) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Error parsing data: " + ex.ToString());
                return;
            }

            if (firstEventMsg)
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Received first valid frame");
            firstEventMsg = false;

            //the byte array MUST be queued, calling the Texture2D ctor here crashes the game apparently
            if (!imgLoadMtx.WaitOne(50)) {
                Plugin.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Mutex timeout");
                return;
            }
            imgLoad.Enqueue(imageBase64);
            imgLoadMtx.ReleaseMutex();
        }
    }
}
