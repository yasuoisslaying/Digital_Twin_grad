using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartGuardTwin.Core;
using SmartGuardTwin.Config;
using SmartGuardTwin.Sensors;

namespace SmartGuardTwin.Avatar
{
    /// <summary>
    /// Moves the resident along doorway waypoints. For each segment it advances the sim
    /// clock by the walking time and emits the zone-occupancy transition for the room
    /// being entered. In fast mode it teleports (no animation, no real waits) so large
    /// datasets generate quickly; otherwise it animates and turns to face travel.
    /// </summary>
    public class Mover : MonoBehaviour
    {
        public float speed = 2.6f;     // metres / second (visual)
        const float SimWalk = 1.0f;    // simulated walking speed (m/s) for timestamps

        public IEnumerator MoveTo(Vector3 goalPos, string goalRoom)
        {
            string start = World.Nav != null ? World.Nav.RoomAt(transform.position) : null;
            List<string> rooms = World.Nav != null ? World.Nav.RoomPathTo(start, goalRoom) : new List<string>();
            List<Vector3> wps = World.Nav != null ? World.Nav.WaypointsTo(start, goalRoom, goalPos)
                                                  : new List<Vector3> { goalPos };

            if (rooms.Count > 0 && SensorBank.I != null) SensorBank.I.EnterZone(rooms[0]);

            Vector3 prev = transform.position;
            for (int i = 0; i < wps.Count; i++)
            {
                Vector3 t = wps[i]; t.y = prev.y;
                float seg = Vector3.Distance(prev, t);
                yield return Step(t);
                if (SimClock.I != null) SimClock.I.Advance(seg / SimWalk);
                if (i < rooms.Count - 1 && SensorBank.I != null) SensorBank.I.EnterZone(rooms[i + 1]);
                prev = t;
            }
        }

        IEnumerator Step(Vector3 target)
        {
            if (SimConfig.FastMode) { transform.position = target; yield break; }

            target.y = transform.position.y;
            while ((transform.position - target).sqrMagnitude > 0.0025f)
            {
                Vector3 dir = target - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude > 1e-5f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
