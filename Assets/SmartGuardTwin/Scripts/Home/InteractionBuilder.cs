using UnityEngine;
using SmartGuardTwin.Config;
using SmartGuardTwin.Objects;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Attaches interactive-state components to the furniture built by SceneBuilder,
    /// creates a smart light per room, and registers everything in ObjectRegistry so the
    /// action system (Phase 3) and sensors (Phase 4) can find objects by id.
    /// </summary>
    public static class InteractionBuilder
    {
        public static void Setup(SceneBuilder scene)
        {
            ObjectRegistry.Clear();

            // Magnetic open/close objects.
            string[] openables = { "front_door", "fridge", "wardrobe", "kitchen_cabinet", "nightstand", "medication" };
            foreach (var id in openables) AddOpenable(scene, id);

            // Smart-plug powered appliances (emission colour just for visual flavour).
            AddPowerable(scene, "stove",      new Color(1.0f, 0.30f, 0.10f));
            AddPowerable(scene, "oven",       new Color(1.0f, 0.40f, 0.10f));
            AddPowerable(scene, "kettle",     new Color(1.0f, 0.70f, 0.30f));
            AddPowerable(scene, "microwave",  new Color(1.0f, 0.85f, 0.20f));
            AddPowerable(scene, "dishwasher", new Color(0.3f, 0.80f, 1.00f));
            AddPowerable(scene, "tv",         new Color(0.3f, 0.60f, 1.00f));

            // Occupiable furniture (pressure sensors get attached to bed + sofa in Phase 4).
            AddOccupiable(scene, "bed", Occupiable.Kind.Bed);
            string[] seats = { "sofa", "armchair", "chair_1", "chair_2", "chair_3", "chair_4" };
            foreach (var id in seats) AddOccupiable(scene, id, Occupiable.Kind.Seat);

            // Per-room smart lights.
            var lights = new GameObject("RoomLights").transform;
            lights.SetParent(scene.Root);
            foreach (var r in HomeLayout.Rooms) AddRoomLight(r, lights);

            Debug.Log($"[InteractionBuilder] Registered {ObjectRegistry.Count} interactive objects "
                      + "(open/close, power, occupancy, room lights).");
        }

        static void AddOpenable(SceneBuilder scene, string id)
        {
            if (!scene.Furniture.TryGetValue(id, out var go)) { Warn(id); return; }
            var c = go.AddComponent<Openable>(); c.Init(id); ObjectRegistry.Register(c);
        }

        static void AddPowerable(SceneBuilder scene, string id, Color emission)
        {
            if (!scene.Furniture.TryGetValue(id, out var go)) { Warn(id); return; }
            var c = go.AddComponent<Powerable>(); c.SetEmissionColor(emission); c.Init(id);
            ObjectRegistry.Register(c);
        }

        static void AddOccupiable(SceneBuilder scene, string id, Occupiable.Kind kind)
        {
            if (!scene.Furniture.TryGetValue(id, out var go)) { Warn(id); return; }
            var c = go.AddComponent<Occupiable>(); c.kind = kind; c.Init(id); ObjectRegistry.Register(c);
        }

        static void AddRoomLight(HomeLayout.Room r, Transform parent)
        {
            var go = new GameObject("light_" + r.name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(r.Cx, 2.2f, HomeLayout.Depth - r.Cz);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(r.x1 - r.x0, r.z1 - r.z0) + 2f;
            light.intensity = 2.4f;
            light.color = new Color(1f, 0.96f, 0.85f);
            var rl = go.AddComponent<RoomLight>(); rl.Init("light_" + r.name);
            ObjectRegistry.Register(rl);
        }

        static void Warn(string id) => Debug.LogWarning($"[InteractionBuilder] furniture id not found: {id}");
    }
}
