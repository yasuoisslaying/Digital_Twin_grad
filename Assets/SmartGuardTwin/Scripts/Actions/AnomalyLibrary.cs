using static SmartGuardTwin.Actions.ActionStep;

namespace SmartGuardTwin.Actions
{
    /// <summary>
    /// Anomalous behaviours for the SmartGuard 3-class twin (normal / fall /
    /// prolonged_inactivity). Both produce the tell-tale signature of a long quiet gap
    /// (no sensor activity while the resident is motionless), labelled accordingly.
    /// </summary>
    public static class AnomalyLibrary
    {
        /// <summary>Resident collapses on the floor (e.g. bathroom) and stays down.</summary>
        public static ActionProgram Fall()
        {
            var p = new ActionProgram("Fall") { isAnomaly = true, klass = "fall" };
            return p.Add(GoRoom("bathroom"))
                    .Add(Collapse())
                    .Add(Wait(3000))   // ~50 min motionless on the floor
                    .Add(Stand());
        }

        /// <summary>Resident sits/stays in one place far longer than normal, no activity.</summary>
        public static ActionProgram ProlongedInactivity()
        {
            var p = new ActionProgram("Prolonged inactivity") { isAnomaly = true, klass = "prolonged_inactivity" };
            return p.Add(Sit("armchair"))
                    .Add(Wait(7200))   // 2 h with no sensor activity
                    .Add(Stand());
        }
    }
}
