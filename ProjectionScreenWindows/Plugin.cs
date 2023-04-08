using BepInEx;
using System;
using System.Security.Permissions;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ProjectionScreenWindows
{
    //also edit version in "modinfo.json"
    [BepInPlugin("maxi-mol.projectionscreenwindows", "ProjectionScreenWindows", "0.1.0")] //(GUID, mod name, mod version)
    public class Plugin : BaseUnityPlugin
    {
        //for accessing logger https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public Plugin() { __me = new WeakReference(this); }
        public static Plugin ME => __me?.Target as Plugin;
        public BepInEx.Logging.ManualLogSource Logger_p => Logger;

        private static bool IsEnabled = false;


        //called when mod is loaded, subscribe functions to methods of the game
        public void OnEnable()
        {
            if (IsEnabled) return;
            IsEnabled = true;

            Hooks.Apply();

            Plugin.ME.Logger_p.LogInfo("OnEnable called");
        }


        //called when mod is unloaded
        public void OnDisable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;

            Hooks.Unapply();

            Plugin.ME.Logger_p.LogInfo("OnDisable called");
        }
    }
}
