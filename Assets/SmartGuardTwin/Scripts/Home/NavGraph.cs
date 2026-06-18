using System.Collections.Generic;
using UnityEngine;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Room-adjacency graph built from <see cref="HomeLayout.Doorways"/>. Provides the
    /// BFS room path and the doorway waypoints between two rooms, so the avatar walks
    /// through doorways and zone-occupancy can be emitted deterministically per room.
    /// </summary>
    public class NavGraph
    {
        readonly Dictionary<string, List<KeyValuePair<string, Vector3>>> _adj
            = new Dictionary<string, List<KeyValuePair<string, Vector3>>>();

        public NavGraph()
        {
            foreach (var d in HomeLayout.Doorways)
            {
                Vector3 w = ToWorld(d.x, d.z);
                Link(d.a, d.b, w);
                Link(d.b, d.a, w);
            }
        }

        void Link(string a, string b, Vector3 w)
        {
            if (!_adj.TryGetValue(a, out var list)) { list = new List<KeyValuePair<string, Vector3>>(); _adj[a] = list; }
            list.Add(new KeyValuePair<string, Vector3>(b, w));
        }

        static Vector3 ToWorld(float px, float pz) => new Vector3(px, 0f, HomeLayout.Depth - pz);

        public string RoomAt(Vector3 worldPos)
        {
            float px = worldPos.x, pz = HomeLayout.Depth - worldPos.z;
            foreach (var r in HomeLayout.Rooms) if (Inside(r, px, pz)) return r.name;
            if (Inside(HomeLayout.Outside, px, pz)) return "outside";
            return null;
        }

        static bool Inside(HomeLayout.Room r, float px, float pz)
            => px >= r.x0 - 0.01f && px <= r.x1 + 0.01f && pz >= r.z0 - 0.01f && pz <= r.z1 + 0.01f;

        public Vector3 RoomCenterWorld(string name)
        {
            foreach (var r in HomeLayout.Rooms) if (r.name == name) return new Vector3(r.Cx, 0f, HomeLayout.Depth - r.Cz);
            if (name == "outside") return new Vector3(HomeLayout.Outside.Cx, 0f, HomeLayout.Depth - HomeLayout.Outside.Cz);
            return Vector3.zero;
        }

        /// <summary>BFS shortest room path from start to goal, inclusive of both ends.</summary>
        public List<string> RoomPathTo(string start, string goal)
        {
            var path = new List<string>();
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(goal)) { if (!string.IsNullOrEmpty(goal)) path.Add(goal); return path; }
            if (start == goal) { path.Add(start); return path; }

            var prev = new Dictionary<string, string>();
            var q = new Queue<string>();
            q.Enqueue(start); prev[start] = null;
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == goal) break;
                if (!_adj.TryGetValue(cur, out var nbrs)) continue;
                foreach (var kv in nbrs) if (!prev.ContainsKey(kv.Key)) { prev[kv.Key] = cur; q.Enqueue(kv.Key); }
            }
            if (!prev.ContainsKey(goal)) { path.Add(goal); return path; }

            for (var n = goal; n != null; n = prev[n]) path.Add(n);
            path.Reverse();
            return path;
        }

        /// <summary>Doorway waypoints (then goalPos) for the room path start -> goal.</summary>
        public List<Vector3> WaypointsTo(string startRoom, string goalRoom, Vector3 goalPos)
        {
            var wps = new List<Vector3>();
            var rooms = RoomPathTo(startRoom, goalRoom);
            if (rooms.Count <= 1) { wps.Add(goalPos); return wps; }
            for (int i = 0; i < rooms.Count - 1; i++) wps.Add(DoorBetween(rooms[i], rooms[i + 1]));
            wps.Add(goalPos);
            return wps;
        }

        Vector3 DoorBetween(string a, string b)
        {
            if (_adj.TryGetValue(a, out var l)) foreach (var kv in l) if (kv.Key == b) return kv.Value;
            return RoomCenterWorld(b);
        }
    }
}
