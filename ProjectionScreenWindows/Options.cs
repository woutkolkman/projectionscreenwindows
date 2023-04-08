using Menu.Remix.MixedUI;
using UnityEngine;

namespace ProjectionScreenWindows
{
    //based on: https://github.com/SabreML/MusicAnnouncements/blob/master/src/MusicAnnouncementsConfig.cs
    //and: https://github.com/SchuhBaum/SBCameraScroll/blob/Rain-World-v1.9/SourceCode/MainModOptions.cs
    public class Options : OptionInterface
    {
        public static Configurable<bool> overrideAllGames;
        public static Configurable<string> windowName, processName, openProgram, openProgramArguments;
        public static Configurable<int> framerate;


        public Options()
        {
            overrideAllGames = config.Bind("overrideAllGames", defaultValue: true, new ConfigurableInfo("When unchecked, RM & SL Pong are accessible and all original games can be played.", null, "", "Replace all games"));
            framerate = config.Bind("framerate", defaultValue: 40, new ConfigurableInfo("Setpoint for maximum capture framerate.", new ConfigAcceptableRange<int>(1, 60), "", "Framerate setpoint"));
            windowName = config.Bind("windowName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for process name.\nExamples:    Notepad    Command Prompt    VLC media player", null, "", "Window name"));
            processName = config.Bind("processName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for window name.\nExamples:    notepad    cmd    vlc", null, "", "Process name"));
            openProgram = config.Bind("openProgram", defaultValue: "", new ConfigurableInfo("If no window was found, the program will try to open a program once. Leaving this empty will not start any program.\nExamples:    notepad    CMD.exe    C:\\Program Files\\VideoLAN\\VLC\\vlc.exe", null, "", "Open program"));
            openProgramArguments = config.Bind("openProgramArguments", defaultValue: "", new ConfigurableInfo("Arguments to pass when opening a program.\nExamples:    /?    \\\"\\\"C:\\vids folder\\pebbsi.mp4\\\"\\\"", null, "", "Arguments"));
        }


        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[]
            {
                new OpTab(this, "Options")
            };
            AddTitle();
            float height = 540f;
            AddCheckbox(overrideAllGames, new Vector2(20f, height -= 40f));
            AddDragger(framerate, new Vector2(20f, height -= 40f));
            AddTextbox(windowName, new Vector2(20f, height -= 40f));
            AddTextbox(processName, new Vector2(20f, height -= 40f));
            AddTextbox(openProgram, new Vector2(20f, height -= 40f), 460f);
            AddTextbox(openProgramArguments, new Vector2(20f, height -= 40f), 460f);
        }


        private void AddTitle()
        {
            OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), Plugin.ME.Name, bigText: true);
            OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {Plugin.ME.Version}");

            Tabs[0].AddItems(new UIelement[]
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

            Tabs[0].AddItems(new UIelement[]
            {
                checkbox,
                label
            });
        }


        private void AddTextbox(Configurable<string> option, Vector2 pos, float width = 100f)
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

            Tabs[0].AddItems(new UIelement[]
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

            Tabs[0].AddItems(new UIelement[]
            {
                dragger,
                label
            });
        }
    }
}
