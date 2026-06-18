using UnityEngine;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// A smart-plug powered appliance (stove, oven, kettle, microwave, dishwasher, TV)
    /// — the paper's power-consumption sensor reads <see cref="IsOn"/> as ON/OFF.
    /// Visual: emissive glow when ON.
    /// </summary>
    public class Powerable : InteractiveObject
    {
        public bool IsOn { get; private set; }
        Color _onEmission = new Color(1f, 0.6f, 0.15f);

        public void SetEmissionColor(Color c) => _onEmission = c;

        public override void Init(string id)
        {
            base.Init(id);
            if (Rend != null) Rend.material.EnableKeyword("_EMISSION");
            Apply();
        }

        public void SetPower(bool on) { if (on == IsOn) return; IsOn = on; Apply(); NotifyChanged(); }
        public void Toggle() => SetPower(!IsOn);
        public override string StateText => IsOn ? "ON" : "OFF";

        void Apply()
        {
            if (Rend != null) Rend.material.SetColor("_EmissionColor", IsOn ? _onEmission : Color.black);
        }
    }
}
