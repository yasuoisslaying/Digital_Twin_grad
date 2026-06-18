using System;
using System.Collections.Generic;
using UnityEngine;
using SmartGuardTwin.Core;
using SmartGuardTwin.Objects;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Sensors
{
    /// <summary>
    /// Central sensor hub. The four graph-based sensor types (magnetic, power, light,
    /// pressure) subscribe to interactive objects' StateChanged. Zone-occupancy is fed
    /// by the mover via <see cref="EnterZone"/> (the avatar's room path), so it is
    /// deterministic and works at any playback speed. Every emission is appended to
    /// <see cref="Log"/> (the dataset) and raised on <see cref="Emitted"/>.
    /// </summary>
    public class SensorBank : MonoBehaviour
    {
        public static SensorBank I { get; private set; }
        public readonly List<SensorEvent> Log = new List<SensorEvent>();
        public event Action<SensorEvent> Emitted;

        string _zone;

        void Awake() { I = this; }

        public void Record(SensorType type, string id, string room, string value)
        {
            var ev = new SensorEvent
            {
                time = SimClock.I != null ? SimClock.I.Now : DateTime.Now,
                type = type, sensorId = id, room = room, value = value
            };
            Log.Add(ev);
            Emitted?.Invoke(ev);
            if (!SimConfig.FastMode)
                Debug.Log($"[SENSOR] {ev.time:HH:mm:ss}  {id,-18} {value,-7} ({type}, {room})");
        }

        /// <summary>Zone-occupancy ("sensitive floor"): emit floor_&lt;room&gt; ON/OFF as the
        /// resident moves between rooms.</summary>
        public void EnterZone(string room)
        {
            if (room == _zone) return;
            if (!string.IsNullOrEmpty(_zone)) Record(SensorType.Zone, "floor_" + _zone, _zone, "OFF");
            if (!string.IsNullOrEmpty(room)) Record(SensorType.Zone, "floor_" + room, room, "ON");
            _zone = room;
        }

        public void SetupObjectSensors()
        {
            foreach (var io in ObjectRegistry.All)
            {
                switch (io)
                {
                    case Openable op:  Hook(op, SensorType.Magnetic, "contact_" + op.Id); break;
                    case Powerable pw: Hook(pw, SensorType.Power,    "power_"   + pw.Id); break;
                    case RoomLight rl: Hook(rl, SensorType.Light,    rl.Id);              break;
                    case Occupiable oc when oc.Id == "bed" || oc.Id == "sofa":
                                       Hook(oc, SensorType.Pressure, "pressure_" + oc.Id); break;
                }
            }
            Debug.Log("[SensorBank] Object sensors wired (magnetic, power, light, pressure); "
                      + "zone-occupancy driven by the mover.");
        }

        void Hook(InteractiveObject io, SensorType type, string sensorId)
        {
            string room = World.Nav != null ? World.Nav.RoomAt(io.transform.position) : null;
            io.StateChanged += changed => Record(type, sensorId, room, ValueOf(changed));
        }

        static string ValueOf(InteractiveObject io)
        {
            switch (io)
            {
                case Openable o:   return o.IsOpen ? "OPEN" : "CLOSED";
                case Powerable p:  return p.IsOn ? "ON" : "OFF";
                case RoomLight l:  return l.IsOn ? "ON" : "OFF";
                case Occupiable c: return c.IsOccupied ? "ON" : "OFF";
            }
            return "?";
        }

        void OnGUI()
        {
            const float w = 360f, h = 250f;
            GUILayout.BeginArea(new Rect(Screen.width - w - 8f, 8f, w, h), GUI.skin.box);
            GUILayout.Label($"Sensors  ({Log.Count} events)");
            int start = Mathf.Max(0, Log.Count - 12);
            for (int i = start; i < Log.Count; i++)
            {
                var e = Log[i];
                GUILayout.Label($"{e.time:MM-dd HH:mm:ss}  {e.sensorId} = {e.value}");
            }
            GUILayout.EndArea();
        }
    }
}
