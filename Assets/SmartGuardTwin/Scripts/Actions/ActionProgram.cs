using System.Collections.Generic;

namespace SmartGuardTwin.Actions
{
    public enum ActionType
    {
        GoToRoom, GoToObject, Open, Close, PowerOn, PowerOff,
        LightOn, LightOff, Sit, Lie, StandUp, Collapse, Wait, Say
    }

    /// <summary>One step of an activity. Built via the static factory helpers.</summary>
    public struct ActionStep
    {
        public ActionType type;
        public string target;   // object id, room name, or message
        public float value;     // duration (sim seconds) for Wait

        public static ActionStep GoRoom(string room) => new ActionStep { type = ActionType.GoToRoom, target = room };
        public static ActionStep Go(string objId)    => new ActionStep { type = ActionType.GoToObject, target = objId };
        public static ActionStep Open(string id)      => new ActionStep { type = ActionType.Open, target = id };
        public static ActionStep Close(string id)     => new ActionStep { type = ActionType.Close, target = id };
        public static ActionStep On(string id)        => new ActionStep { type = ActionType.PowerOn, target = id };
        public static ActionStep Off(string id)       => new ActionStep { type = ActionType.PowerOff, target = id };
        public static ActionStep LightOn(string room) => new ActionStep { type = ActionType.LightOn, target = room };
        public static ActionStep LightOff(string room)=> new ActionStep { type = ActionType.LightOff, target = room };
        public static ActionStep Sit(string id)       => new ActionStep { type = ActionType.Sit, target = id };
        public static ActionStep Lie(string id)       => new ActionStep { type = ActionType.Lie, target = id };
        public static ActionStep Stand()              => new ActionStep { type = ActionType.StandUp };
        public static ActionStep Collapse()           => new ActionStep { type = ActionType.Collapse };
        public static ActionStep Wait(float simSecs)  => new ActionStep { type = ActionType.Wait, value = simSecs };
        public static ActionStep Say(string msg)      => new ActionStep { type = ActionType.Say, target = msg };
    }

    /// <summary>
    /// An activity = an ordered list of steps with a label and a class. Normal ADLs use
    /// the default class "normal"; anomalies set <see cref="isAnomaly"/> and a class of
    /// "fall" or "prolonged_inactivity" for the SmartGuard 3-class twin.
    /// </summary>
    public class ActionProgram
    {
        public readonly string label;
        public readonly List<ActionStep> steps = new List<ActionStep>();
        public bool isAnomaly = false;
        public string klass = "normal";

        public ActionProgram(string label) { this.label = label; }
        public ActionProgram Add(ActionStep s) { steps.Add(s); return this; }
    }
}
