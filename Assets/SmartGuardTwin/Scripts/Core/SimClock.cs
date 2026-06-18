using System;
using UnityEngine;

namespace SmartGuardTwin.Core
{
    /// <summary>
    /// Owned simulated clock — fixes the paper's lack of time-of-day (§4.3.2). Unlike a
    /// wall-clock, it is advanced explicitly by the action system (by each action's
    /// intended duration), so exported timestamps reflect real activity lengths
    /// regardless of how fast the demo plays.
    /// </summary>
    public class SimClock : MonoBehaviour
    {
        public static SimClock I { get; private set; }
        public DateTime Now { get; private set; }

        void Awake() { I = this; Now = new DateTime(2025, 1, 1, 7, 0, 0); }

        public void Advance(float seconds) { Now = Now.AddSeconds(seconds); }
        public void SetTimeOfDay(int hour, int minute = 0)
            => Now = new DateTime(Now.Year, Now.Month, Now.Day, hour, minute, 0);
        public void SetDay(int index) => Now = new DateTime(2025, 1, 1).AddDays(index);
    }
}
