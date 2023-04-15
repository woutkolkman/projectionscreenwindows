using System;
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
        static int startDialogDelay = -1, stopDialogDelay = -1;
        static int startDelay = -1, stopDelay = -1;
        static bool prevGameIsNull = true;


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
            if (stopTrigger == Options.TriggerTypes.OnConstructor || 
                trigCapture?.captureProcess?.HasExited == true) { //when captureprocess stop trigger was not properly triggered
                trigCapture?.Destroy();
                trigCapture = null;
            }
        }


        public static void CheckUpdateTriggers(OracleBehavior self, bool gameIsNull)
        {
            //start dialog
            if (self?.dialogBox != null) {
                if (startDialogDelay == 0)
                    self.dialogBox.Interrupt(self.Translate(Options.startDialog.Value), 10);
                if (startDialogDelay >= 0)
                    startDialogDelay--;
                if (stopDialogDelay == 0)
                    self.dialogBox.Interrupt(self.Translate(Options.stopDialog.Value), 10);
                if (stopDialogDelay >= 0)
                    stopDialogDelay--;
            }

            //start/stop capture
            if (startDelay == 0 && gameIsNull && trigCapture == null)
                trigCapture = new Capture(self);
            if (startDelay >= 0)
                startDelay--;
            if (stopDelay == 0) {
                trigCapture?.Destroy();
                trigCapture = null;
                PauseConversation(self, pause: false);
            }
            if (stopDelay >= 0)
                stopDelay--;

            //prevent starting two capture instances at once
            if (!gameIsNull) {
                trigCapture?.Destroy();
                trigCapture = null;
            }

            //current room
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

            //player alive
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
                        if (self?.oracle?.room != null &&               //oracle room is not unloaded
                            prevRoom?.Target == self?.oracle?.room &&   //previous room was oracle room
                            curRoom != self?.oracle?.room)              //current room is null or not oracle room
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

            if (TriggerActive(startTrigger)) {
                Plugin.ME.Logger_p.LogInfo("CheckUpdateTriggers, Triggered start");

                //dialog
                if (Options.startDialog?.Value?.Length > 0 && Options.startDialogDelay?.Value != null)
                    startDialogDelay = Options.startDialogDelay.Value;

                //capture
                if (Options.startDelay?.Value != null)
                    startDelay = Options.startDelay.Value;
            }

            if (TriggerActive(stopTrigger)) {
                Plugin.ME.Logger_p.LogInfo("CheckUpdateTriggers, Triggered stop");

                //dialog
                if (Options.stopDialog?.Value?.Length > 0 && Options.stopDialogDelay?.Value != null)
                    stopDialogDelay = Options.stopDialogDelay.Value;

                //capture
                if (Options.stopDelay?.Value != null)
                    stopDelay = Options.stopDelay.Value;
            }

            //also trigger dialog when FPGame from Five Pebbles Pong changes
            if (!gameIsNull && prevGameIsNull)
                if (Options.startDialog?.Value?.Length > 0 && Options.startDialogDelay?.Value != null)
                    startDialogDelay = Options.startDialogDelay.Value;
            if (gameIsNull && !prevGameIsNull)
                if (Options.stopDialog?.Value?.Length > 0 && Options.stopDialogDelay?.Value != null)
                    stopDialogDelay = Options.stopDialogDelay.Value;

            trigCapture?.Update(self);
            trigCapture?.Draw(new Vector2());
            if (trigCapture != null && Options.pauseConversation?.Value != null && Options.pauseConversation.Value)
                PauseConversation(self, pause: true);

            //update previous values
            if (curRoom != null) {
                prevRoom = new WeakReference(curRoom);
            } else {
                prevRoom = null; //player entered pipe
            }
            prevNoticed = curNoticed;
            prevConvActive = curConvActive;
            prevPlayerAlive = curPlayerAlive;
            prevAction = curAction;
            prevGameIsNull = gameIsNull;
        }


        public static void PauseConversation(OracleBehavior self, bool pause)
        {
            if (Options.pauseConversation?.Value == null || !Options.pauseConversation.Value)
                return;

            if (self is SSOracleBehavior) {
                if ((self as SSOracleBehavior).conversation != null)
                    (self as SSOracleBehavior).conversation.paused = pause;
                (self as SSOracleBehavior).restartConversationAfterCurrentDialoge = !pause;
            }
            if (self is SLOracleBehaviorHasMark) {
                if ((self as SLOracleBehaviorHasMark).currentConversation != null)
                    (self as SLOracleBehaviorHasMark).currentConversation.paused = pause;
                (self as SLOracleBehaviorHasMark).resumeConversationAfterCurrentDialoge = !pause;
            }
            if (self is MoreSlugcats.SSOracleRotBehavior) {
                if ((self as MoreSlugcats.SSOracleRotBehavior).conversation != null)
                    (self as MoreSlugcats.SSOracleRotBehavior).conversation.paused = pause;
                (self as MoreSlugcats.SSOracleRotBehavior).restartConversationAfterCurrentDialoge = !pause;
            }
        }
    }
}
