using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ProjectionScreenWindows
{
    class Hooks
    {
        public static void Apply()
        {
            //initialize options
            On.RainWorld.OnModsInit += RainWorldOnModsInitHook;

            //test in options menu
            On.RainWorld.Update += RainWorldUpdateHook;

            //replace all SS games with Capture
            IDetour detourSSGetNewFPGame = new Hook(
                typeof(FivePebblesPong.Plugin).GetMethod("SSGetNewFPGame", BindingFlags.Static | BindingFlags.Public),
                typeof(Hooks).GetMethod("FivePebblesPongPlugin_SSGetNewFPGame_RuntimeDetour", BindingFlags.Static | BindingFlags.Public)
            );

            //replace RM Pong with Capture
            IDetour detourRMGetNewFPGame = new Hook(
                typeof(FivePebblesPong.Plugin).GetMethod("RMGetNewFPGame", BindingFlags.Static | BindingFlags.Public),
                typeof(Hooks).GetMethod("FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour", BindingFlags.Static | BindingFlags.Public)
            );

            //replace SL Pong with Capture
            IDetour detourSLGetNewFPGame = new Hook(
                typeof(FivePebblesPong.Plugin).GetMethod("SLGetNewFPGame", BindingFlags.Static | BindingFlags.Public),
                typeof(Hooks).GetMethod("FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour", BindingFlags.Static | BindingFlags.Public)
            );

            //add game to gamelist
            gameNrCapture = FivePebblesPong.Plugin.amountOfGames;
            FivePebblesPong.Plugin.amountOfGames++;

            /********************************* Hooks for all triggers *********************************/
            //five pebbles constructor
            On.SSOracleBehavior.ctor += SSOracleBehaviorCtorHook;

            //moon constructor
            On.SLOracleBehavior.ctor += SLOracleBehaviorCtorHook;

            //five pebbles (rot) constructor
            On.MoreSlugcats.SSOracleRotBehavior.ctor += MoreSlugcatsSSOracleRotBehaviorCtorHook;

            //five pebbles update function
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;

            //moon update function
            On.SLOracleBehavior.Update += SLOracleBehaviorUpdateHook;

            //five pebbles (rot) update function
            On.MoreSlugcats.SSOracleRotBehavior.Update += MoreSlugcatsSSOracleRotBehaviorUpdateHook;
            /******************************************************************************************/
        }


        public static void Unapply()
        {
            //TODO

            //remove from gamelist
            FivePebblesPong.Plugin.amountOfGames--;
            gameNrCapture = -1;
        }


        //initialize options
        static void RainWorldOnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(Plugin.ME.GUID, new Options());
        }


        //test in options menu
        static Capture testCapture = null;
        static void RainWorldUpdateHook(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig(self);

            if (Options.testActive && Options.testFrame != null) {
                if (testCapture == null)
                    testCapture = new Capture(null);

                Texture2D newFrame = testCapture?.GetNewFrame();
                if (newFrame != null) {
                    Options.testFrame.ChangeImage(newFrame);
                    Options.testFrame.pos = new Vector2(300f - newFrame.width/2, 300f - newFrame.height/2) + testCapture.offsetPos;
                }

                //memory leak "fix" (probably FSprite after ChangeImage call)
                if (testCapture.frame > 0 && testCapture.frame % 100 == 0)
                    FTexture.GarbageCollect(); //memory is also freed when exiting Remix menu

            } else {
                testCapture?.Destroy();
                testCapture = null;
            }
        }


        //replace all SS games with Capture
        static int gameNrCapture = -1;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SSGetNewFPGame_RuntimeDetour(Func<SSOracleBehavior, int, FivePebblesPong.FPGame> orig, SSOracleBehavior ob, int nr)
        {
            if (Options.overrideAllGames.Value)
                return new Capture(ob);

            if (FivePebblesPong.Plugin.amountOfGames != 0 &&                //divide by 0 safety
                gameNrCapture >= 0 &&                                       //game is added to list
                nr % FivePebblesPong.Plugin.amountOfGames == gameNrCapture) //correct pearl is grabbed
                return new Capture(ob);

            return orig(ob, nr);
        }


        //replace RM Pong with Capture
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour(Func<MoreSlugcats.SSOracleRotBehavior, FivePebblesPong.FPGame> orig, MoreSlugcats.SSOracleRotBehavior ob)
        {
            if (Options.overrideAllGames.Value)
                return new Capture(ob);
            return orig(ob);
        }


        //replace SL Pong with Capture
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour(Func<SLOracleBehavior, FivePebblesPong.FPGame> orig, SLOracleBehavior ob)
        {
            if (Options.overrideAllGames.Value)
                return new Capture(ob);
            return orig(ob);
        }


        /********************************* Hooks for all triggers *********************************/
        //five pebbles constructor
        static void SSOracleBehaviorCtorHook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("SSOracleBehaviorCtorHook");
            orig(self, oracle);
            Triggers.CheckCtorTriggers(self, FivePebblesPong.SSGameStarter.starter?.game == null);
        }


        //moon constructor
        static void SLOracleBehaviorCtorHook(On.SLOracleBehavior.orig_ctor orig, SLOracleBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("SLOracleBehaviorCtorHook");
            orig(self, oracle);
            Triggers.CheckCtorTriggers(self, FivePebblesPong.SLGameStarter.starter?.game == null);
        }


        //five pebbles (rot) constructor
        static void MoreSlugcatsSSOracleRotBehaviorCtorHook(On.MoreSlugcats.SSOracleRotBehavior.orig_ctor orig, MoreSlugcats.SSOracleRotBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("MoreSlugcatsSSOracleRotBehaviorCtorHook");
            orig(self, oracle);
            Triggers.CheckCtorTriggers(self, FivePebblesPong.RMGameStarter.starter?.game == null);
        }


        //five pebbles update function
        static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);
            Triggers.CheckUpdateTriggers(self, FivePebblesPong.SSGameStarter.starter?.game == null);
        }


        //moon update function
        static void SLOracleBehaviorUpdateHook(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            orig(self, eu);
            Triggers.CheckUpdateTriggers(self, FivePebblesPong.SLGameStarter.starter?.game == null);
        }


        //five pebbles (rot) update function
        static void MoreSlugcatsSSOracleRotBehaviorUpdateHook(On.MoreSlugcats.SSOracleRotBehavior.orig_Update orig, MoreSlugcats.SSOracleRotBehavior self, bool eu)
        {
            orig(self, eu);
            Triggers.CheckUpdateTriggers(self, FivePebblesPong.RMGameStarter.starter?.game == null);
        }
        /******************************************************************************************/
    }
}
