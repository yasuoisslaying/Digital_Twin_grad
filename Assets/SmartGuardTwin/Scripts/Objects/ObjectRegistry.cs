using System.Collections.Generic;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// Global lookup of interactive objects by id. Cleared at the start of each build;
    /// the Editor's domain reload also resets it between Play sessions.
    /// </summary>
    public static class ObjectRegistry
    {
        static readonly Dictionary<string, InteractiveObject> Map = new Dictionary<string, InteractiveObject>();

        public static void Clear() => Map.Clear();
        public static void Register(InteractiveObject io) { if (io != null) Map[io.Id] = io; }
        public static InteractiveObject Get(string id) => Map.TryGetValue(id, out var v) ? v : null;
        public static T Get<T>(string id) where T : InteractiveObject => Get(id) as T;
        public static IEnumerable<InteractiveObject> All => Map.Values;
        public static int Count => Map.Count;
    }
}
