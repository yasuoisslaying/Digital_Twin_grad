namespace SmartGuardTwin.Config
{
    /// <summary>
    /// A behavioural profile for the resident on a given day: when they get up, how fast
    /// they do things, and how likely they are to skip or add activities. Picking a
    /// different profile per day injects the kind of day-to-day variability the paper got
    /// from having multiple human volunteers.
    /// </summary>
    public class ResidentProfile
    {
        public string Name;
        public int WakeHour, NoonHour, EveningHour;
        public float DurationScale;   // multiplies every activity duration
        public double SkipProb;        // chance to skip an optional activity
        public double ExtraProb;       // chance to insert a filler activity after one

        public ResidentProfile(string name, int wake, int noon, int evening, float scale, double skip, double extra)
        { Name = name; WakeHour = wake; NoonHour = noon; EveningHour = evening; DurationScale = scale; SkipProb = skip; ExtraProb = extra; }
    }

    /// <summary>The set of profiles, and the one active for the current day.</summary>
    public static class ResidentProfiles
    {
        public static readonly ResidentProfile[] All =
        {
            //                 name        wake noon eve  dur   skip  extra
            new ResidentProfile("Typical",   7,  12,  18, 1.00f, 0.05, 0.10),
            new ResidentProfile("Active",    6,  12,  19, 0.85f, 0.03, 0.18),
            new ResidentProfile("Sluggish",  8,  13,  18, 1.30f, 0.12, 0.06),
        };

        public static ResidentProfile Current = All[0];
    }
}
