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
            windowName = config.Bind("windowName", defaultValue: "", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for process name.\nExamples:    Notepad    Command Prompt", null, "", "Window name"));
            processName = config.Bind("processName", defaultValue: "notepad", new ConfigurableInfo("Program will match a window name with a process name. Leave empty to only search for window name.\nExamples:    notepad    cmd", null, "", "Process name"));
            openProgram = config.Bind("openProgram", defaultValue: "notepad", new ConfigurableInfo("If no window was found, the program will try to open a program once. Leave empty to ignore.\nExamples:    notepad    CMD.exe", null, "", "Open program"));
            openProgramArguments = config.Bind("openProgramArguments", defaultValue: "", new ConfigurableInfo("Arguments to pass when opening a program.", null, "", "Arguments"));
            framerate = config.Bind("framerate", defaultValue: 40, new ConfigurableInfo("Setpoint for maximum capture framerate.", new ConfigAcceptableRange<int>(1, 60), "", "Framerate setpoint"));
        }


        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[]
            {
                new OpTab(this, "Options")
            };
            AddTitle();
            AddCheckbox(overrideAllGames, 500f);
            AddTextbox(windowName, 460f);
            AddTextbox(processName, 420f);
            AddTextbox(openProgram, 380f);
            AddTextbox(openProgramArguments, 340f);
            AddDragger(framerate, 300f);
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


        private void AddCheckbox(Configurable<bool> option, float y)
        {
            OpCheckBox checkbox = new OpCheckBox(option, new Vector2(210f, y))
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(210f + 40f, y + 2f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[0].AddItems(new UIelement[]
            {
                checkbox,
                label
            });
        }


        private void AddTextbox(Configurable<string> option, float y, float width = 100f)
        {
            OpTextBox textbox = new OpTextBox(option, new Vector2(210f, y), width)
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(210f + width + 20f, y + 2f, option.info.Tags[0] as string)
            {
                description = option.info.description
            };

            Tabs[0].AddItems(new UIelement[]
            {
                textbox,
                label
            });
        }


        private void AddDragger(Configurable<int> option, float y, float width = 100f)
        {
            OpDragger dragger = new OpDragger(option, 210f, y)
            {
                description = option.info.description
            };

            OpLabel label = new OpLabel(210f + 40f, y + 2f, option.info.Tags[0] as string)
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
