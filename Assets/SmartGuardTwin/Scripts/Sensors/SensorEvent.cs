using System;

namespace SmartGuardTwin.Sensors
{
    /// <summary>The paper's 5 implemented virtual sensor types (Bouchabou 2023, §3.1).</summary>
    public enum SensorType { Magnetic, Pressure, Light, Power, Zone }

    /// <summary>A single sensor state-change record: the unit of the generated dataset.</summary>
    public struct SensorEvent
    {
        public DateTime time;
        public SensorType type;
        public string sensorId;
        public string room;
        public string value;     // OPEN/CLOSED, ON/OFF
    }
}
