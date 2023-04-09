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
        }


        public static void Unapply()
        {
            //TODO

            //remove from gamelist
            if (FivePebblesPong.Plugin.amountOfGames > gameNrCapture)
                FivePebblesPong.Plugin.amountOfGames--;
            gameNrCapture = -1;
        }


        //initialize options
        static void RainWorldOnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(Plugin.ME.GUID, new Options());
        }


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
                    Options.testFrame.pos = new Vector2(300f - newFrame.width/2, 300f - newFrame.height/2);
                }

                //memory leak "fix" (probably FSprite after ChangeImage call)
                if (testCapture.frame > 0 && testCapture.frame % 100 == 0)
                    FTexture.GarbageCollect(); //memory is also freed when exiting Remix menu

            } else {
                testCapture?.Destroy();
                testCapture = null;
            }
        }


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


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour(Func<MoreSlugcats.SSOracleRotBehavior, FivePebblesPong.FPGame> orig, MoreSlugcats.SSOracleRotBehavior ob)
        {
            if (Options.overrideAllGames.Value)
                return new Capture(ob);
            return orig(ob);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour(Func<SLOracleBehavior, FivePebblesPong.FPGame> orig, SLOracleBehavior ob)
        {
            if (Options.overrideAllGames.Value)
                return new Capture(ob);
            return orig(ob);
        }
    }
}
