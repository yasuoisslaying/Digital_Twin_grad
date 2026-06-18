using System;
using UnityEngine;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// Base class for any object whose state a virtual sensor reads (Phase 4).
    /// Subclasses expose a typed state and raise <see cref="StateChanged"/> when it flips.
    /// </summary>
    public abstract class InteractiveObject : MonoBehaviour
    {
        public string Id { get; protected set; }
        public event Action<InteractiveObject> StateChanged;

        Renderer _rend;
        protected Renderer Rend => _rend != null ? _rend : (_rend = GetComponent<Renderer>());

        /// <summary>
        /// Called once by InteractionBuilder right after AddComponent (we use this rather
        /// than Awake so fields can be configured before any visual setup runs).
        /// </summary>
        public virtual void Init(string id) { Id = id; }

        /// <summary>Short human-readable state, e.g. "OPEN"/"CLOSED", "ON"/"OFF".</summary>
        public abstract string StateText { get; }

        protected void NotifyChanged() => StateChanged?.Invoke(this);
    }
}
