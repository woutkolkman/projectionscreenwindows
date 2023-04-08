using System;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using FivePebblesPong;

namespace ProjectionScreenWindows
{
    class Hooks
    {
        public static void Apply()
        {
            //replace RM Pong with Capture
            IDetour detourRMGetNewFPGame = new Detour<Func<MoreSlugcats.SSOracleRotBehavior, FivePebblesPong.FPGame>>(
                FivePebblesPong.Plugin.RMGetNewFPGame,
                FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour
            );

            //replace SL Pong with Capture
            IDetour detourSLGetNewFPGame = new Detour<Func<SLOracleBehavior, FivePebblesPong.FPGame>>(
                FivePebblesPong.Plugin.SLGetNewFPGame,
                FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour
            );
        }


        public static void Unapply()
        {
            //TODO
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_RMGetNewFPGame_RuntimeDetour(MoreSlugcats.SSOracleRotBehavior self)
        {
            return new Capture(self);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour(SLOracleBehavior self)
        {
            Plugin.ME.Logger_p.LogInfo("FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour");
            if (self.oracle.room.game.IsMoonHeartActive()) {
                return new Capture(self);
            } else {
                return new Dino(self);
            }
        }
    }
}
