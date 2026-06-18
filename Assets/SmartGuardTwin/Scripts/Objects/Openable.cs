using UnityEngine;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Objects
{
    /// <summary>
    /// A magnetic open/close object (door, fridge, cabinet, wardrobe, drawer) — the
    /// paper's open/close sensor reads <see cref="IsOpen"/>.
    /// Visual: a door panel hinged on the face that points into the room, swinging ~80°.
    /// </summary>
    public class Openable : InteractiveObject
    {
        public bool IsOpen { get; private set; }

        Transform _hinge;
        Quaternion _openRot = Quaternion.identity;

        public override void Init(string id)
        {
            base.Init(id);
            BuildDoor();
        }

        public void SetOpen(bool open) { if (open == IsOpen) return; IsOpen = open; NotifyChanged(); }
        public void Toggle() => SetOpen(!IsOpen);
        public override string StateText => IsOpen ? "OPEN" : "CLOSED";

        void BuildDoor()
        {
            Vector3 p = transform.position;
            Vector3 s = transform.localScale;
            float W = HomeLayout.Width, D = HomeLayout.Depth;

            // Hinge the door on the face that points away from the nearest wall (into the room).
            float dxW = W - p.x, dx0 = p.x, dzD = D - p.z, dz0 = p.z;
            float min = Mathf.Min(Mathf.Min(dxW, dx0), Mathf.Min(dzD, dz0));
            bool faceX; float sgn;
            if (min == dxW)      { faceX = true;  sgn = -1f; }
            else if (min == dx0) { faceX = true;  sgn =  1f; }
            else if (min == dzD) { faceX = false; sgn = -1f; }
            else                 { faceX = false; sgn =  1f; }

            float doorW = (faceX ? s.z : s.x) * 0.96f;
            float doorH = s.y * 0.92f;
            const float thick = 0.05f;

            Vector3 faceCenter = p;
            if (faceX) faceCenter.x = p.x + sgn * s.x * 0.5f;
            else       faceCenter.z = p.z + sgn * s.z * 0.5f;

            Vector3 tangent = faceX ? Vector3.forward : Vector3.right;
            Vector3 hingePos = faceCenter - tangent * (doorW * 0.5f);

            // Parent to the (unscaled) Furniture container so the door isn't distorted
            // by the object's own non-uniform scale.
            _hinge = new GameObject(Id + "_hinge").transform;
            _hinge.SetParent(transform.parent, false);
            _hinge.position = new Vector3(hingePos.x, p.y, hingePos.z);
            _hinge.rotation = Quaternion.identity;

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = door.GetComponent<Collider>(); if (col != null) Destroy(col);
            door.name = Id + "_door";
            door.transform.SetParent(_hinge, false);
            door.transform.localPosition = tangent * (doorW * 0.5f);
            door.transform.localScale = faceX ? new Vector3(thick, doorH, doorW)
                                              : new Vector3(doorW, doorH, thick);
            var baseCol = Rend != null ? Rend.material.color : Color.gray;
            door.GetComponent<Renderer>().material.color = Color.Lerp(baseCol, Color.black, 0.3f);

            _openRot = Quaternion.Euler(0f, (faceX ? sgn : -sgn) * 80f, 0f);
        }

        void Update()
        {
            if (_hinge == null) return;
            var target = IsOpen ? _openRot : Quaternion.identity;
            _hinge.localRotation = Quaternion.RotateTowards(_hinge.localRotation, target, 300f * Time.deltaTime);
        }
    }
}
