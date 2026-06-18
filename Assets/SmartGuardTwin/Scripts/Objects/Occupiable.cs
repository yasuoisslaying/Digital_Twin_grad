using UnityEngine;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// A seat or bed the resident can occupy. The paper's pressure sensors (Phase 4)
    /// read <see cref="IsOccupied"/> on the bed and sofa.
    /// Visual: tints green when occupied.
    /// </summary>
    public class Occupiable : InteractiveObject
    {
        public enum Kind { Seat, Bed }
        public Kind kind = Kind.Seat;

        public bool IsOccupied { get; private set; }
        Color _base;

        public override void Init(string id)
        {
            base.Init(id);
            if (Rend != null) _base = Rend.material.color;
        }

        public void SetOccupied(bool occ)
        {
            if (occ == IsOccupied) return;
            IsOccupied = occ;
            if (Rend != null) Rend.material.color = occ ? Color.Lerp(_base, Color.green, 0.55f) : _base;
            NotifyChanged();
        }
        public override string StateText => IsOccupied ? "OCCUPIED" : "EMPTY";
    }
}
