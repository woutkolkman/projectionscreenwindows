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
        public static Configurable<bool> overrideAllGames, ignoreOrigPos, moveRandomly;
        public static Configurable<string> windowName, processName, openProgram, openProgramArguments;
        public static Configurable<int> framerate, cropLeft, cropBottom, cropRight, cropTop;
        public static OpSimpleButton testButton;
        public static OpImage testFrame, backgroundImg;
        public int curTab;


        public Options()
        {
            overrideAllGames = config.Bind("overrideAllGames", defaultValue: true, new ConfigurableInfo("When unchecked, RM & SL Pong are accessible and all original games can be played.", null, "", "Replace all games"));
            ignoreOrigPos = config.Bind("ignoreOrigPos", defaultValue: false, new ConfigurableInfo("When unchecked, RM screen has a glitch animation.", null, "", "Ignore original position"));
            moveRandomly = config.Bind("moveRandomly", defaultValue: true, new ConfigurableInfo("When checked, the window position is constantly adjusted.", null, "", "Move randomly"));
            framerate = config.Bind("framerate", defaultValue: 40, new ConfigurableInfo("Setpoint for maximum capture framerate.", new ConfigAcceptableRange<int>(1, 60), "", "Framerate setpoint"));
            windowName = config.Bind("windowName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for process name.\nExamples:    Notepad    Command Prompt    VLC media player", null, "", "Window name"));
            processName = config.Bind("processName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for window name.\nExamples:    notepad    cmd    vlc", null, "", "Process name"));
            openProgram = config.Bind("openProgram", defaultValue: "", new ConfigurableInfo("If no window was found, the program will try to open a program once. It will also be closed afterwards. Leaving this empty will not start or stop any program.\nExamples:    notepad    CMD.exe    C:\\Program Files\\VideoLAN\\VLC\\vlc.exe", null, "", "Open program"));
            openProgramArguments = config.Bind("openProgramArguments", defaultValue: "", new ConfigurableInfo("Arguments to pass when opening a program.\nExamples:    /?    \\\"\\\"C:\\vids folder\\pebbsi.mp4\\\"\\\"", null, "", "Arguments"));

            cropLeft = config.Bind("cropLeft", defaultValue: 0, new ConfigurableInfo("Crop left of frames (pixels). No performance loss.\nExamples:    (Notepad) 1    (VLC) 1    (CMD) 1", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Crop left"));
            cropBottom = config.Bind("cropBottom", defaultValue: 0, new ConfigurableInfo("Crop bottom of frames (pixels). No performance loss.\nExamples:    (Notepad) 1    (VLC) 53    (CMD) 1", new ConfigAcceptableRange<int>(0, int.MaxValue), "", "Crop bottom"));
            cropRight = config.Bind("cropRight", defaultValue: 0, new ConfigurableInfo("Crop right of frames (pixels). No performance loss.\nExamples:    (Notepad) -17    (VLC) -1    (CMD) -18", new ConfigAcceptableRange<int>(int.MinValue + 1, 0), "", "Crop right"));
            cropTop = config.Bind("cropTop", defaultValue: 0, new ConfigurableInfo("Crop top of frames (pixels). No performance loss.\nExamples:    (Notepad) -51    (VLC) -52    (CMD) -31", new ConfigAcceptableRange<int>(int.MinValue + 1, 0), "", "Crop top"));
        }


        public override void Initialize()
        {
            base.Initialize();

            curTab = 0;
            Tabs = new OpTab[]
            {
                new OpTab(this, "General"),
                new OpTab(this, "Position"),
                new OpTab(this, "Test")
            };
            AddTitle();
            float mid(float size) { return 300f - (size / 2); } //both X and Y
            float height = 540f;
            AddCheckbox(overrideAllGames, new Vector2(20f, height -= 40f));
            AddCheckbox(ignoreOrigPos, new Vector2(320f, height));
            AddDragger(framerate, new Vector2(20f, height -= 40f));
            AddCheckbox(moveRandomly, new Vector2(320f, height));
            AddTextbox(windowName, new Vector2(20f, height -= 40f));
            AddTextbox(processName, new Vector2(20f, height -= 40f));
            AddTextbox(openProgram, new Vector2(20f, height -= 40f), 460f);
            AddTextbox(openProgramArguments, new Vector2(20f, height -= 40f), 460f);

            height = 540f;
            curTab++;
            AddUpDown(cropTop, new Vector2(mid(60f), height -= 40f));
            AddUpDown(cropLeft, new Vector2(mid(60f) - 140f, height -= 40f));
            AddUpDown(cropRight, new Vector2(mid(60f) + 140f, height));
            AddUpDown(cropBottom, new Vector2(mid(60f), height -= 40f));

            curTab++;
            string str = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + "pebbles_can_centered_600x600.png");
            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            try {
                AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + str, false, true);
            } catch (Exception e) {
                Plugin.ME.Logger_p.LogError(e.ToString());
            }
            backgroundImg = new OpImage(new Vector2(), tex);
            testFrame = new OpImage(new Vector2(mid(1f), mid(1f)), Texture2D.whiteTexture);
            testButton = new OpSimpleButton(new Vector2(mid(60f), 20f), new Vector2(60f, 40f), "Test");
            testButton.OnClick += TestButtonOnClickHandler;
            testButton.OnReactivate += TestButtonUpdate;
            Tabs[curTab].AddItems(new UIelement[] { backgroundImg, testFrame, testButton });

            this.OnDeactivate += () => { testActive = false; };
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


        private void AddTextbox(Configurable<string> option, Vector2 pos, float width = 150f)
        {
            OpTextBox textbox = new OpTextBox(option, pos, width)
            {
                allowSpace = true,
                maxLength = 8191, //max total length cmd commands
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


        private void AddUpDown(Configurable<int> option, Vector2 pos)
        {
            OpUpdown updown = new OpUpdown(option, pos, 60f)
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(pos.x + 60f + 20f, pos.y + 6f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[curTab].AddItems(new UIelement[]
            {
                updown,
                label
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
