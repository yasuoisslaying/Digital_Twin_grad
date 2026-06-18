namespace SmartGuardTwin.Config
{
    /// <summary>
    /// The paper's three recording scenarios (Bouchabou 2023, §3.2.2), each an ordered
    /// list of ADL labels. Morning ends by leaving for work; noon and evening start by
    /// entering the home.
    /// </summary>
    public static class ScenarioConfig
    {
        public static readonly string[] Morning =
        { "Sleep in Bed", "Take medication", "Go to toilet", "Dress", "Cook breakfast", "Eat breakfast", "Wash dishes", "Bathe", "Leave home" };

        public static readonly string[] Noon =
        { "Enter home", "Cook lunch", "Eat lunch", "Wash dishes", "Go to toilet", "Leave home" };

        public static readonly string[] Evening =
        { "Enter home", "Cook dinner", "Eat dinner", "Wash dishes", "Watch TV", "Read", "Bathe", "Go to toilet", "Take medication", "Sleep" };
    }
}
