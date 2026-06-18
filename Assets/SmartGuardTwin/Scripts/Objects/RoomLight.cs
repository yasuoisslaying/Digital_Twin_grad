using UnityEngine;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// A room's smart light — the paper's light sensor reads <see cref="IsOn"/>.
    /// Wraps a point Light that toggles on/off.
    /// </summary>
    public class RoomLight : InteractiveObject
    {
        public bool IsOn { get; private set; }
        Light _light;

        public override void Init(string id)
        {
            base.Init(id);
            _light = GetComponent<Light>();
            if (_light != null) _light.enabled = false;
        }

        public void SetOn(bool on)
        {
            if (on == IsOn) return;
            IsOn = on;
            if (_light != null) _light.enabled = on;
            NotifyChanged();
        }
        public void Toggle() => SetOn(!IsOn);
        public override string StateText => IsOn ? "ON" : "OFF";
    }
}
