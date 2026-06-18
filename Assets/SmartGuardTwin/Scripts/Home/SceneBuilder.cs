using System.Collections.Generic;
using UnityEngine;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Builds the apartment twin from <see cref="HomeLayout"/> — colour-coded floor
    /// zones, perimeter walls (with a front-door gap), and furniture — entirely from
    /// runtime primitives, plus a top-down orthographic camera and a sun light.
    ///
    /// Created GameObjects are kept in <see cref="Furniture"/> (by id) and
    /// <see cref="Zones"/> for the interaction and sensor layers in later phases.
    /// </summary>
    public class SceneBuilder
    {
        public readonly Dictionary<string, GameObject> Furniture = new Dictionary<string, GameObject>();
        public readonly List<RoomZone> Zones = new List<RoomZone>();
        public Transform Root { get; private set; }

        public void Build()
        {
            Root = new GameObject("Home").transform;

            var floors = NewChild("Floors");
            BuildFloor(HomeLayout.Outside, floors, isOutside: true);
            foreach (var r in HomeLayout.Rooms) BuildFloor(r, floors, isOutside: false);

            var walls = NewChild("Walls");
            BuildWalls(walls);

            var furn = NewChild("Furniture");
            foreach (var it in HomeLayout.Furniture) BuildItem(it, furn);

            SetupLight();
            SetupCamera();
            AttachLabels();

            Debug.Log($"[SceneBuilder] Built {HomeLayout.Rooms.Length} rooms + outside, "
                      + $"{Furniture.Count} objects, {Zones.Count} floor zones.");
        }

        Transform NewChild(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(Root);
            return t;
        }

        // Flip paper-z to world-z so north (z=0) maps to +Z (top of the top-down view).
        static float WZ(float paperZ) => HomeLayout.Depth - paperZ;

        void BuildFloor(HomeLayout.Room r, Transform parent, bool isOutside)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "floor_" + r.name;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(r.Cx, isOutside ? -0.02f : 0f, WZ(r.Cz));
            go.transform.localScale = new Vector3(r.x1 - r.x0, 0.04f, r.z1 - r.z0);
            Paint(go, r.color);

            var zone = go.AddComponent<RoomZone>();
            zone.RoomName = r.name;
            Zones.Add(zone);
        }

        void BuildWalls(Transform parent)
        {
            int idx = 0;
            foreach (var wl in HomeLayout.Walls) BuildWallLine(wl, parent, ref idx);
        }

        // Builds one wall line as cube segments, leaving a gap at each doorway that lies
        // on the line (perimeter and interior walls are handled the same way).
        void BuildWallLine(HomeLayout.WallLine wl, Transform parent, ref int idx)
        {
            float D = HomeLayout.Depth, t = HomeLayout.WallThick, hw = HomeLayout.DoorWidth * 0.5f;

            var doors = new List<float>();
            foreach (var dl in HomeLayout.Doorways)
            {
                if (wl.vertical) { if (Mathf.Abs(dl.x - wl.line) < 0.05f && dl.z >= wl.a - 0.05f && dl.z <= wl.b + 0.05f) doors.Add(dl.z); }
                else             { if (Mathf.Abs(dl.z - wl.line) < 0.05f && dl.x >= wl.a - 0.05f && dl.x <= wl.b + 0.05f) doors.Add(dl.x); }
            }
            doors.Sort();

            float cursor = wl.a;
            var segs = new List<Vector2>();                 // (start, end) along the span
            foreach (var dc in doors)
            {
                float gs = dc - hw, ge = dc + hw;
                if (gs > cursor) segs.Add(new Vector2(cursor, gs));
                cursor = Mathf.Max(cursor, ge);
            }
            if (cursor < wl.b) segs.Add(new Vector2(cursor, wl.b));

            foreach (var s in segs)
            {
                float len = s.y - s.x;
                if (len < 0.05f) continue;
                float mid = (s.x + s.y) * 0.5f;
                if (wl.vertical) MakeWall(parent, $"wall_{idx++}", wl.line, D - mid, t, len);
                else             MakeWall(parent, $"wall_{idx++}", mid, D - wl.line, len, t);
            }
        }

        void MakeWall(Transform parent, string name, float cx, float cz, float sx, float sz)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(cx, HomeLayout.WallHeight * 0.5f, cz);
            go.transform.localScale = new Vector3(sx, HomeLayout.WallHeight, sz);
            Paint(go, new Color(0.30f, 0.30f, 0.34f));
        }

        void BuildItem(HomeLayout.Item it, Transform parent)
        {
            var prim = it.shape == HomeLayout.Shape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(prim);
            go.name = it.id;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(it.x, it.h * 0.5f, WZ(it.z));
            // Unity's cylinder is 2 units tall by default, so y-scale is half-height.
            go.transform.localScale = it.shape == HomeLayout.Shape.Cylinder
                ? new Vector3(it.w, it.h * 0.5f, it.d)
                : new Vector3(it.w, it.h, it.d);
            Paint(go, it.color);
            Furniture[it.id] = go;
        }

        static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }

        void SetupLight()
        {
            // Flat ambient keeps the dollhouse evenly lit (less blotchy shadowing).
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.60f);

            if (Object.FindObjectOfType<Light>() != null) return;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, 35f, 0f);
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);

            // CameraController gives a 3D orbit view by default; press V for top-down.
            var ctrl = cam.GetComponent<CameraController>();
            if (ctrl == null) ctrl = cam.gameObject.AddComponent<CameraController>();
            ctrl.target = new Vector3(HomeLayout.Width * 0.5f, 0.4f, HomeLayout.Depth * 0.5f);
        }

        void AttachLabels()
        {
            var labels = Root.gameObject.AddComponent<HomeLabels>();
            foreach (var r in HomeLayout.Rooms)
                labels.Add(r.name, new Vector3(r.Cx, 0.2f, WZ(r.Cz)));
            labels.Add("outside", new Vector3(HomeLayout.Outside.Cx, 0.2f, WZ(HomeLayout.Outside.Cz)));
        }
    }
}
