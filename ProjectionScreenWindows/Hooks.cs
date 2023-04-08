using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;

namespace ProjectionScreenWindows
{
    class Hooks
    {
        public static void Apply()
        {
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
        }


        public static void Unapply()
        {
            //TODO
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SSGetNewFPGame_RuntimeDetour(Func<SSOracleBehavior, int, FivePebblesPong.FPGame> orig, SSOracleBehavior ob, int nr)
        {
            return new Capture(ob);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour(Func<MoreSlugcats.SSOracleRotBehavior, FivePebblesPong.FPGame> orig, MoreSlugcats.SSOracleRotBehavior ob)
        {
            return new Capture(ob);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour(Func<SLOracleBehavior, FivePebblesPong.FPGame> orig, SLOracleBehavior ob)
        {
            if (ob.oracle.room.game.IsMoonHeartActive()) {
                return new Capture(ob);
            } else {
                return orig(ob);
            }
        }
    }
}
