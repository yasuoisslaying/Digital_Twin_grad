namespace SmartGuardTwin.Config
{
    /// <summary>
    /// Run-level settings for dataset generation. Edit these to change how much data is
    /// produced and how it plays.
    /// </summary>
    public static class SimConfig
    {
        /// <summary>Number of days to generate in one run.</summary>
        public static int Days = 60;

        /// <summary>Fast mode teleports the avatar and skips real-time waits, so a full
        /// multi-day dataset generates in seconds. Set false to watch a day play out.</summary>
        public static bool FastMode = false;

        /// <summary>Seed for the (reproducible) anomaly scheduling.</summary>
        public static int Seed = 1234;

        /// <summary>Probability a given day contains an injected fall/inactivity anomaly.</summary>
        public static double AnomalyProbabilityPerDay = 0.45;

        /// <summary>Probability that a medication dose (wake-up / bedtime) is forgotten.</summary>
        public static double MedicationMissProb = 0.12;

        /// <summary>Probability that a medication dose is taken late (~2 h delay).</summary>
        public static double MedicationLateProb = 0.15;

        /// <summary>In watch mode (FastMode=false), real seconds to hold during an
        /// anomaly's motionless period so it is visible in a demo.</summary>
        public static float DemoAnomalyPauseSeconds = 5f;

        // --- behavioural variability (so days are not near-duplicates) ---

        /// <summary>Random +/- fraction applied to every activity's duration (0 = none).</summary>
        public static float DurationJitter = 0.20f;

        /// <summary>Pick a random resident profile each day (see ResidentProfiles).
        /// If false, FixedProfileIndex is used for every day.</summary>
        public static bool RandomizeProfilePerDay = true;

        /// <summary>Profile index used when RandomizeProfilePerDay is false.</summary>
        public static int FixedProfileIndex = 0;
    }
}
