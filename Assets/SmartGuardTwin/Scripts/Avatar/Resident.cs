using UnityEngine;
using SmartGuardTwin.Core;

namespace SmartGuardTwin.Avatar
{
    /// <summary>
    /// The single elderly resident. Tracks the room it is in (for the HUD; sensors
    /// determine room independently via raycast) and its posture, and renders posture
    /// on the primitive body/head.
    /// </summary>
    public class Resident : MonoBehaviour
    {
        public enum Posture { Standing, Sitting, Lying }
        public Posture CurrentPosture { get; private set; } = Posture.Standing;
        public string CurrentRoom { get; private set; }

        Transform _body, _head;

        public void Bind(Transform body, Transform head) { _body = body; _head = head; }

        void Update() { if (World.Nav != null) CurrentRoom = World.Nav.RoomAt(transform.position); }

        public void SetPosture(Posture p)
        {
            CurrentPosture = p;
            if (_body == null) return;
            switch (p)
            {
                case Posture.Standing:
                    _body.localRotation = Quaternion.identity;
                    _body.localScale = new Vector3(0.4f, 0.9f, 0.4f);
                    _body.localPosition = new Vector3(0f, 0.9f, 0f);
                    if (_head) _head.localPosition = new Vector3(0f, 1.75f, 0f);
                    break;
                case Posture.Sitting:
                    _body.localRotation = Quaternion.identity;
                    _body.localScale = new Vector3(0.4f, 0.6f, 0.4f);
                    _body.localPosition = new Vector3(0f, 0.6f, 0f);
                    if (_head) _head.localPosition = new Vector3(0f, 1.15f, 0f);
                    break;
                case Posture.Lying:
                    _body.localRotation = Quaternion.Euler(90f, 0f, 0f); // capsule horizontal
                    _body.localScale = new Vector3(0.4f, 0.9f, 0.4f);
                    _body.localPosition = new Vector3(0f, 0.25f, 0f);
                    if (_head) _head.localPosition = new Vector3(0f, 0.25f, -0.85f);
                    break;
            }
        }
    }
}
