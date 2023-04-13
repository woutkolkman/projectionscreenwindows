using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ProjectionScreenWindows
{
    public static class Triggers
    {
        static Options.TriggerTypes startTrigger = Options.TriggerTypes.None;
        static Options.TriggerTypes stopTrigger = Options.TriggerTypes.None;
        static Capture trigCapture;
        static WeakReference prevRoom = null;
        static bool prevNoticed = false;
        static bool prevConvActive = false;
        static bool prevPlayerAlive = false;
        static SSOracleBehavior.Action prevAction = null;


        public static void CheckCtorTriggers(OracleBehavior self, bool gameIsNull)
        {
            Options.TriggerTypes GetTriggerType(Configurable<string> option) {
                if (option?.Value != null)
                    foreach (Options.TriggerTypes val in Enum.GetValues(typeof(Options.TriggerTypes)))
                        if (String.Equals(option.Value, val.ToString()))
                            return val;
                return Options.TriggerTypes.None;
            }

            startTrigger = GetTriggerType(Options.startTrigger);
            stopTrigger = GetTriggerType(Options.stopTrigger);

            if (stopTrigger != startTrigger && startTrigger == Options.TriggerTypes.OnConstructor && gameIsNull)
                trigCapture = new Capture(self);
            if (stopTrigger == Options.TriggerTypes.OnConstructor) {
                trigCapture?.Destroy();
                trigCapture = null;
            }
        }


        public static void CheckUpdateTriggers(OracleBehavior self, bool gameIsNull)
        {
            //prevent rapid start/stop
            if (stopTrigger == startTrigger)
                return;

            //prevent starting two capture instances at once
            if (!gameIsNull) {
                trigCapture?.Destroy();
                trigCapture = null;
                return;
            }

            Room curRoom = self?.player?.room;

            //player noticed
            bool curNoticed = false;
            if (self is SSOracleBehavior)
                curNoticed |= (self as SSOracleBehavior).timeSinceSeenPlayer > 0;
            if (self is SLOracleBehavior)
                curNoticed |= (self as SLOracleBehavior).hasNoticedPlayer;
            if (self is MoreSlugcats.SSOracleRotBehavior)
                curNoticed |= (self as MoreSlugcats.SSOracleRotBehavior).hasNoticedPlayer;

            //conversation active
            bool curConvActive = false;
            if (self is SSOracleBehavior)
                curConvActive |= (self as SSOracleBehavior).conversation != null;
            if (self is SLOracleBehavior)
                curConvActive |= (self as SLOracleBehavior).conversationAdded;
            if (self is MoreSlugcats.SSOracleRotBehavior)
                curConvActive |= (self as MoreSlugcats.SSOracleRotBehavior).conversation != null;

            bool curPlayerAlive = (self?.player?.abstractCreature?.state != null && self.player.abstractCreature.state.alive);

            //current action
            SSOracleBehavior.Action curAction = null;
            if (self is SSOracleBehavior)
                curAction = (self as SSOracleBehavior).action;

            bool TriggerActive(Options.TriggerTypes trig)
            {
                bool triggered = false;
                switch (trig) {
                    //*******************************************
                    case Options.TriggerTypes.OnPlayerEntersRoom:
                        if (prevRoom?.Target != curRoom && curRoom == self?.oracle?.room)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnPlayerLeavesRoom:
                        if (prevRoom?.Target != curRoom && curRoom != self?.oracle?.room)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnPlayerNoticed:
                        if (curNoticed && !prevNoticed)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnConversationStart:
                        if (curConvActive && !prevConvActive)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnConversationEnd:
                        if (!curConvActive && prevConvActive)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnPlayerDeath:
                        if (!curPlayerAlive && prevPlayerAlive)
                            triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnThrowOut:
                        if (curAction != null && prevAction != null && curAction != prevAction)
                            if (curAction == SSOracleBehavior.Action.ThrowOut_ThrowOut ||
                                curAction == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut ||
                                curAction == SSOracleBehavior.Action.ThrowOut_SecondThrowOut)
                                triggered = true;
                        break;

                    //*******************************************
                    case Options.TriggerTypes.OnKillOnSight:
                        if (curAction != null && prevAction != null && curAction != prevAction)
                            if (curAction == SSOracleBehavior.Action.ThrowOut_KillOnSight)
                                triggered = true;
                        break;
                }
                return triggered;
            }

            if (TriggerActive(startTrigger) && trigCapture == null)
                trigCapture = new Capture(self);

            if (TriggerActive(stopTrigger)) {
                trigCapture?.Destroy();
                trigCapture = null;
            }

            trigCapture?.Update(self);

            if (self?.player?.room != null)
                prevRoom = new WeakReference(self.player.room);
            prevNoticed = curNoticed;
            prevConvActive = curConvActive;
            prevPlayerAlive = curPlayerAlive;
            prevAction = curAction;
        }
    }
}
