using UnityEngine;

namespace SmartGuardTwin.Avatar
{
    /// <summary>
    /// Builds the resident from primitives (capsule body, sphere head, small facing
    /// nub) and attaches the <see cref="Resident"/> + <see cref="Mover"/> components.
    /// A nicer humanoid mesh can be swapped in later without changing the logic.
    ///
    /// (Named ResidentBuilder, not AvatarBuilder, to avoid clashing with the built-in
    /// UnityEngine.AvatarBuilder type.)
    /// </summary>
    public static class ResidentBuilder
    {
        public static Resident Build(Transform parent, Vector3 startWorld)
        {
            var root = new GameObject("Resident");
            root.transform.SetParent(parent);
            root.transform.position = startWorld;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.GetComponent<Renderer>().material.color = new Color(0.85f, 0.30f, 0.20f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.GetComponent<Renderer>().material.color = new Color(0.95f, 0.80f, 0.65f);

            // Small nub so the facing direction is visible.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(nose.GetComponent<Collider>());
            nose.name = "Facing";
            nose.transform.SetParent(head.transform, false);
            nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
            nose.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            nose.GetComponent<Renderer>().material.color = new Color(0.15f, 0.15f, 0.15f);

            var res = root.AddComponent<Resident>();
            root.AddComponent<Mover>();
            res.Bind(body.transform, head.transform);
            res.SetPosture(Resident.Posture.Standing);
            return res;
        }
    }
}
