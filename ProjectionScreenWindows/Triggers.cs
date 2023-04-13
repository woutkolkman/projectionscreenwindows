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

            switch (startTrigger) {
                //*******************************************
                case Options.TriggerTypes.OnPlayerEntersRoom:
                    if (prevRoom?.Target != curRoom && curRoom == self?.oracle?.room && trigCapture == null)
                        trigCapture = new Capture(self);
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerLeavesRoom:
                    if (prevRoom?.Target != curRoom && curRoom != self?.oracle?.room && trigCapture == null)
                        trigCapture = new Capture(self);
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerNoticed:
                    if (curNoticed && !prevNoticed && trigCapture == null)
                        trigCapture = new Capture(self);
                    break;

                //*******************************************
                case Options.TriggerTypes.OnConversationStart:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnConversationEnd:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerDeath:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnThrowOut:
                    //TODO
                    break;
            }

            switch (stopTrigger) {
                //*******************************************
                case Options.TriggerTypes.OnPlayerEntersRoom:
                    if (prevRoom?.Target != curRoom && curRoom == self?.oracle?.room)
                    {
                        trigCapture?.Destroy();
                        trigCapture = null;
                    }
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerLeavesRoom:
                    if (prevRoom?.Target != curRoom && curRoom != self?.oracle?.room)
                    {
                        trigCapture?.Destroy();
                        trigCapture = null;
                    }
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerNoticed:
                    if (curNoticed && !prevNoticed)
                    {
                        trigCapture?.Destroy();
                        trigCapture = null;
                    }
                    break;

                //*******************************************
                case Options.TriggerTypes.OnConversationStart:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnConversationEnd:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnPlayerDeath:
                    //TODO
                    break;

                //*******************************************
                case Options.TriggerTypes.OnThrowOut:
                    //TODO
                    break;
            }

            trigCapture?.Update(self);

            if (self?.player?.room != null)
                prevRoom = new WeakReference(self.player.room);
            prevNoticed = curNoticed;
        }
    }
}
