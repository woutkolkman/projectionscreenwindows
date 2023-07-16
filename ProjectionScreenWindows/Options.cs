using Menu.Remix.MixedUI;
using System;
using System.IO;
using UnityEngine;

namespace ProjectionScreenWindows
{
    //based on: https://github.com/SabreML/MusicAnnouncements/blob/master/src/MusicAnnouncementsConfig.cs
    //and: https://github.com/SchuhBaum/SBCameraScroll/blob/Rain-World-v1.9/SourceCode/MainModOptions.cs
    public class Options : OptionInterface
    {
        public static Configurable<bool> overrideAllGames, ignoreOrigPos, moveRandomly, altOpenProgram, reduceStartupTime;
        public static Configurable<string> windowName, processName, openProgram, openProgramArguments;
        public static Configurable<string> positionType, startTrigger, stopTrigger, startDialog, stopDialog;
        public static Configurable<int> framerate, cropLeft, cropBottom, cropRight, cropTop, chromaKeyError;
        public static Configurable<int> startDialogDelay, stopDialogDelay, startDelay, stopDelay;
        public static Configurable<float> offsetPosX, offsetPosY;
        public static Configurable<bool> puppetLooksAtWindow, pauseConversation, chromaKeying;
        public static Configurable<Color> chromaKeyColor;
        public static OpSimpleButton testButton;
        public static OpImage testFrame;
        public int curTab;

        public enum PositionTypes
        {
            Middle,
            Puppet,
            Player,
            Mouse
        }
        public enum TriggerTypes
        {
            None,
            OnConstructor,
            OnPlayerEntersRoom,
            OnPlayerLeavesRoom,
            OnPlayerNoticed,
            OnConversationStart,
            OnConversationEnd,
            OnPlayerDeath,
            OnThrowOut,
            OnKillOnSight
        }


        public Options()
        {
            /**************** General ****************/
            overrideAllGames = config.Bind("overrideAllGames", defaultValue: true, new ConfigurableInfo("When unchecked, RM & SL Pong are accessible and all original games can be played.", null, "", "Replace all games"));
            puppetLooksAtWindow = config.Bind("puppetLooksAtWindow", defaultValue: true, new ConfigurableInfo("If there is a window, puppet looks at it.", null, "", "Look at window"));
            pauseConversation = config.Bind("pauseConversation", defaultValue: false, new ConfigurableInfo("Softlock warning! If there is a window, conversation is paused.", null, "", "Pause conversation"));
            framerate = config.Bind("framerate", defaultValue: 40, new ConfigurableInfo("Setpoint for maximum capture framerate.", new ConfigAcceptableRange<int>(1, 99), "", "Framerate setpoint"));
            windowName = config.Bind("windowName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for process name.\nExamples:    Notepad    Command Prompt    VLC media player", null, "", "Window name"));
            processName = config.Bind("processName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for window name.\nExamples:    notepad    cmd    vlc", null, "", "Process name"));
            openProgram = config.Bind("openProgram", defaultValue: "", new ConfigurableInfo("If no window was found, the program will try to open a program once. It will also be closed afterwards. Leaving this empty will not start or stop any program.\nExamples:    notepad    CMD.exe    C:\\Program Files\\VideoLAN\\VLC\\vlc.exe", null, "", "Open program"));
            openProgramArguments = config.Bind("openProgramArguments", defaultValue: "", new ConfigurableInfo("Arguments to pass when opening a program.\nExamples:    /?    \\\"\\\"C:\\vids folder\\pebbsi.mp4\\\"\\\"", null, "", "Arguments"));
            altOpenProgram = config.Bind("altOpenProgram", defaultValue: false, new ConfigurableInfo("Program is opened with another method, so the game doesn't lose focus. Programs also won't close automatically afterwards.", null, "", "Don't lose focus"));
            reduceStartupTime = config.Bind("reduceStartupTime", defaultValue: false, new ConfigurableInfo("Skip first search for window and immediately start program.", null, "", "Reduce startup time"));
            chromaKeyColor = config.Bind("chromaKeyColor", defaultValue: Color.green, new ConfigurableInfo("Configured color for chroma keying option.", null, "", ""));
            chromaKeying = config.Bind("chromaKeying", defaultValue: false, new ConfigurableInfo("Pixels with the configured value become transparent. Computation intensive task.", null, "", "Chroma keying"));
            chromaKeyError = config.Bind("chromaKeyError", defaultValue: 5000, new ConfigurableInfo("Higher value will replace more colors.", new ConfigAcceptableRange<int>(1, int.MaxValue), "", "Chroma key error"));
            /*****************************************/

            /*************** Position ****************/
            positionType = config.Bind("positionType", defaultValue: PositionTypes.Middle.ToString(), new ConfigurableInfo("Target position for the window. Not actual position, because this is influenced by other options.", null, "", "Position type"));
            offsetPosX = config.Bind("offsetPosX", defaultValue: 0f, new ConfigurableInfo("Always offset window position (X) by this ammount. -7.5 makes image align with background. Positive values will move right.", null, "", "Offset position X"));
            offsetPosY = config.Bind("offsetPosY", defaultValue: 0f, new ConfigurableInfo("Always offset window position (Y) by this ammount. 15.5 makes image align with background. Positive values will move up.", null, "", "Offset position Y"));
            ignoreOrigPos = config.Bind("ignoreOrigPos", defaultValue: true, new ConfigurableInfo("When unchecked, RM screen has a glitch animation.", null, "", "Ignore original position"));
            moveRandomly = config.Bind("moveRandomly", defaultValue: true, new ConfigurableInfo("When checked, the window position is constantly adjusted.", null, "", "Move randomly"));
            cropLeft = config.Bind("cropLeft", defaultValue: 0, new ConfigurableInfo("Crop left of frames (pixels). No performance loss.\nExamples:    (Notepad) 1    (VLC) 1    (CMD) 1", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Crop left"));
            cropBottom = config.Bind("cropBottom", defaultValue: 0, new ConfigurableInfo("Crop bottom of frames (pixels). No performance loss.\nExamples:    (Notepad) 1    (VLC) 53    (CMD) 1", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Crop bottom"));
            cropRight = config.Bind("cropRight", defaultValue: 0, new ConfigurableInfo("Crop right of frames (pixels). No performance loss.\nExamples:    (Notepad) -17    (VLC) -1    (CMD) -18", new ConfigAcceptableRange<int>(int.MinValue + 1, 0), "", "Crop right"));
            cropTop = config.Bind("cropTop", defaultValue: 0, new ConfigurableInfo("Crop top of frames (pixels). No performance loss.\nExamples:    (Notepad) -51    (VLC) -52    (CMD) -31", new ConfigAcceptableRange<int>(int.MinValue + 1, 0), "", "Crop top"));
            /*****************************************/

            /*************** Triggers ****************/
            startTrigger = config.Bind("startTrigger", defaultValue: TriggerTypes.None.ToString(), new ConfigurableInfo("When to start the capture program. By default it's only started when holding the controller.", null, "", "Start trigger"));
            stopTrigger = config.Bind("stopTrigger", defaultValue: TriggerTypes.None.ToString(), new ConfigurableInfo("When to stop the capture program. By default it's only stopped when dropping the controller.", null, "", "Stop trigger"));
            startDialog = config.Bind("startDialog", defaultValue: "", new ConfigurableInfo("Dialog at start trigger. When empty, no custom dialog starts.", null, "", "Start dialog"));
            stopDialog = config.Bind("stopDialog", defaultValue: "", new ConfigurableInfo("Dialog at stop trigger. When empty, no custom dialog starts.", null, "", "Stop dialog"));
            startDialogDelay = config.Bind("startDialogDelay", defaultValue: 0, new ConfigurableInfo("Delay before dialog starts after trigger. Bound to tickrate, 40 t/s.", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Start dialog delay"));
            stopDialogDelay = config.Bind("stopDialogDelay", defaultValue: 0, new ConfigurableInfo("Delay before dialog starts after trigger. Bound to tickrate, 40 t/s.", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Stop dialog delay"));
            startDelay = config.Bind("startDelay", defaultValue: 0, new ConfigurableInfo("Delay before capture starts after trigger. Bound to tickrate, 40 t/s.", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Start delay"));
            stopDelay = config.Bind("stopDelay", defaultValue: 0, new ConfigurableInfo("Delay before capture stops after trigger. Bound to tickrate, 40 t/s.", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Stop delay"));
            /*****************************************/
        }


        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[]
            {
                new OpTab(this, "General"),
                new OpTab(this, "Position"),
                new OpTab(this, "Test"),
                new OpTab(this, "Triggers")
            };
            float mid(float size = 0f) { return 300f - (size / 2); } //both X and Y
            this.OnDeactivate += () => { testActive = false; };

            /**************** General ****************/
            float height = 540f;
            curTab = 0;

            AddTitle();
            AddCheckbox(overrideAllGames, new Vector2(20f, height -= 40f));
            AddCheckbox(puppetLooksAtWindow, new Vector2(220f, height));
            AddCheckbox(pauseConversation, new Vector2(420f, height));
            AddDragger(framerate, new Vector2(20f, height -= 40f));
            AddTextbox(windowName, new Vector2(20f, height -= 40f), alH: FLabelAlignment.Right);
            AddTextbox(processName, new Vector2(20f, height -= 40f), alH: FLabelAlignment.Right);
            AddTextbox(openProgram, new Vector2(20f, height -= 40f), 460f, alH: FLabelAlignment.Right);
            AddTextbox(openProgramArguments, new Vector2(20f, height -= 40f), 460f, alH: FLabelAlignment.Right);
            AddCheckbox(altOpenProgram, new Vector2(20f, height -= 40f));
            AddCheckbox(reduceStartupTime, new Vector2(220f, height));

            height -= 40f;
            AddColorPicker(chromaKeyColor, new Vector2(20f, height - 166f));
            AddCheckbox(chromaKeying, new Vector2(220f, height -= 40f));
            AddDragger(chromaKeyError, new Vector2(220f, height -= 40f));
            /*****************************************/

            /*************** Position ****************/
            height = 540f;
            curTab++;

            AddTextboxFloat(offsetPosX, new Vector2(mid() - 180f, height -= 40f), 70);
            AddCheckbox(ignoreOrigPos, new Vector2(mid() + 30f, height));
            AddTextboxFloat(offsetPosY, new Vector2(mid() - 180f, height -= 40f), 70);
            AddCheckbox(moveRandomly, new Vector2(mid() + 30f, height));
            AddComboBox(positionType, new Vector2(mid() - 180f, height -= 40f), Enum.GetNames(typeof(PositionTypes)), alH: FLabelAlignment.Right);
            AddUpDown(cropTop, new Vector2(mid(60f), height = mid() + 40f), alV: OpLabel.LabelVAlignment.Top);
            AddUpDown(cropLeft, new Vector2(mid(60f) - 60f, height -= 40f), alH: FLabelAlignment.Left);
            AddUpDown(cropRight, new Vector2(mid(60f) + 60f, height), alH: FLabelAlignment.Right);
            AddUpDown(cropBottom, new Vector2(mid(60f), height -= 40f), alV: OpLabel.LabelVAlignment.Bottom);
            /*****************************************/

            /***************** Test ******************/
            height = 540f;
            curTab++;

            string str = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + "pebbles_can_centered_600x600.png");
            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            try {
                AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + str, false, true);
            } catch (Exception e) {
                Plugin.ME.Logger_p.LogError(e.ToString());
            }
            OpImage backgroundImg = new OpImage(new Vector2(), tex);
            OpLabel tipsText = new OpLabel(new Vector2(mid(), 500f), new Vector2(), text:
                "Tips:\n- Apply changes before testing.\n- Any issues? " +
                "Check BepInEx logs located in \"Rain World\\BepInEx\\LogOutput.log\", " +
                "\nor enable a console window in \"Rain World\\BepInEx\\config\\BepInEx.cfg\"." +
                "\n- Always enter a window or process name, else any process will match." +
                "\n- Resize windows to be small enough to fit on the screen (and because of performance)."
            );

            testFrame = new OpImage(new Vector2(mid(1f), mid(1f)), Texture2D.blackTexture);
            testButton = new OpSimpleButton(new Vector2(mid(60f), 20f), new Vector2(60f, 40f));
            testButton.OnClick += TestButtonOnClickHandler;
            testButton.OnReactivate += TestButtonUpdate;
            TestButtonUpdate();
            Tabs[curTab].AddItems(new UIelement[] { backgroundImg, tipsText, testFrame, testButton });
            /*****************************************/

            /*************** Triggers ****************/
            height = 540f;
            curTab++;

            AddTextbox(startDialog, new Vector2(20f, height -= 40f), width: 460f, alV: OpLabel.LabelVAlignment.Top);
            AddUpDown(startDialogDelay, new Vector2(500f, height - 2.5f), width: 70f, alV: OpLabel.LabelVAlignment.Top);
            AddComboBox(startTrigger, new Vector2(mid(250f), height -= 70f), Enum.GetNames(typeof(TriggerTypes)), 160f, alV: OpLabel.LabelVAlignment.Top);
            AddUpDown(startDelay, new Vector2(mid(250f) + 180f, height - 2.5f), width: 70f, alV: OpLabel.LabelVAlignment.Top);
            AddTextbox(stopDialog, new Vector2(20f, height -= 180f), width: 460f, alV: OpLabel.LabelVAlignment.Top);
            AddUpDown(stopDialogDelay, new Vector2(500f, height - 2.5f), width: 70f, alV: OpLabel.LabelVAlignment.Top);
            AddComboBox(stopTrigger, new Vector2(mid(250f), height -= 70f), Enum.GetNames(typeof(TriggerTypes)), 160f, alV: OpLabel.LabelVAlignment.Top);
            AddUpDown(stopDelay, new Vector2(mid(250f) + 180f, height - 2.5f), width: 70f, alV: OpLabel.LabelVAlignment.Top);
            /*****************************************/
        }


        private void AddTitle()
        {
            OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), Plugin.ME.Name, bigText: true);
            OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {Plugin.ME.Version}");

            Tabs[curTab].AddItems(new UIelement[]
            {
                title,
                version
            });
        }


        private void AddCheckbox(Configurable<bool> option, Vector2 pos)
        {
            OpCheckBox checkbox = new OpCheckBox(option, pos)
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(pos.x + 40f, pos.y + 2f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[curTab].AddItems(new UIelement[]
            {
                checkbox,
                label
            });
        }


        private void AddTextbox(Configurable<string> option, Vector2 pos, float width = 150f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center)
        {
            OpTextBox textbox = new OpTextBox(option, pos, width)
            {
                allowSpace = true,
                maxLength = 8191, //max total length cmd commands
                description = option.info.description
            };

            Vector2 offset = new Vector2();
            if (alV == OpLabel.LabelVAlignment.Top) {
                offset.y += textbox.size.y + 5f;
            } else if (alV == OpLabel.LabelVAlignment.Bottom) {
                offset.y += -textbox.size.y - 5f;
            } else if (alH == FLabelAlignment.Right) {
                offset.x += textbox.size.x + 20f;
                alH = FLabelAlignment.Left;
            } else if (alH == FLabelAlignment.Left) {
                offset.x += -textbox.size.x - 20f;
                alH = FLabelAlignment.Right;
            }

            OpLabel label = new OpLabel(pos + offset, textbox.size, option.info.Tags[0] as string)
            {
                description = option.info.description
            };
            label.alignment = alH;
            label.verticalAlignment = OpLabel.LabelVAlignment.Center;

            Tabs[curTab].AddItems(new UIelement[]
            {
                textbox,
                label
            });
        }


        private void AddTextboxFloat(Configurable<float> option, Vector2 pos, float width = 150f)
        {
            OpTextBox textbox = new OpTextBox(option, pos, width)
            {
                allowSpace = true,
                description = option.info.description
            };

            OpLabel label = new OpLabel(pos.x + width + 20f, pos.y + 2f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[curTab].AddItems(new UIelement[]
            {
                textbox,
                label
            });
        }


        private void AddDragger(Configurable<int> option, Vector2 pos)
        {
            OpDragger dragger = new OpDragger(option, pos)
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(pos.x + 40f, pos.y + 2f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[curTab].AddItems(new UIelement[]
            {
                dragger,
                label
            });
        }


        private void AddUpDown(Configurable<int> option, Vector2 pos, float width = 60f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center)
        {
            OpUpdown updown = new OpUpdown(option, pos, width)
            {
                description = option.info.description
            };

            Vector2 offset = new Vector2();
            if (alV == OpLabel.LabelVAlignment.Top) {
                offset.y += updown.size.y + 5f;
            } else if (alV == OpLabel.LabelVAlignment.Bottom) {
                offset.y += -updown.size.y - 5f;
            } else if (alH == FLabelAlignment.Right) {
                offset.x += updown.size.x + 18f;
                alH = FLabelAlignment.Left;
            } else if (alH == FLabelAlignment.Left) {
                offset.x += -updown.size.x - 18f;
                alH = FLabelAlignment.Right;
            }

            OpLabel label = new OpLabel(pos + offset, updown.size, option.info.Tags[0] as string)
            {
                description = option.info.description
            };
            label.alignment = alH;
            label.verticalAlignment = OpLabel.LabelVAlignment.Center;

            Tabs[curTab].AddItems(new UIelement[]
            {
                label,
                updown
            });
        }


        private void AddComboBox(Configurable<string> option, Vector2 pos, string[] array, float width = 80f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center)
        {
            OpComboBox box = new OpComboBox(option, pos, width, array)
            {
                description = option.info.description
            };

            Vector2 offset = new Vector2();
            if (alV == OpLabel.LabelVAlignment.Top) {
                offset.y += box.size.y + 5f;
            } else if (alV == OpLabel.LabelVAlignment.Bottom) {
                offset.y += -box.size.y - 5f;
            } else if (alH == FLabelAlignment.Right) {
                offset.x += box.size.x + 20f;
                alH = FLabelAlignment.Left;
            } else if (alH == FLabelAlignment.Left) {
                offset.x += -box.size.x - 20f;
                alH = FLabelAlignment.Right;
            }

            OpLabel label = new OpLabel(pos + offset, box.size, option.info.Tags[0] as string)
            {
                description = option.info.description
            };
            label.alignment = alH;
            label.verticalAlignment = OpLabel.LabelVAlignment.Center;

            Tabs[curTab].AddItems(new UIelement[]
            {
                box,
                label
            });
        }


        private void AddColorPicker(Configurable<Color> option, Vector2 pos)
        {
            OpColorPicker colorPicker = new OpColorPicker(option, pos)
            {
                description = option.info.description
            };

            Tabs[curTab].AddItems(new UIelement[]
            {
                colorPicker
            });
        }


        public static bool testActive = false;
        private void TestButtonOnClickHandler(UIfocusable _)
        {
            testActive = !testActive;
            TestButtonUpdate();
        }
        private void TestButtonUpdate()
        {
            testButton.text = testActive ? "Cancel" : "Test";
            testButton.colorFill = testActive ? Color.red : Color.black;
        }
    }
}
