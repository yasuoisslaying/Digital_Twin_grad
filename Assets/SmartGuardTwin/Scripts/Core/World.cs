using UnityEngine;

namespace SmartGuardTwin.Core
{
    /// <summary>
    /// Lightweight global access to the running simulation's main objects, so the action
    /// system and sensors can reach the scene, navigation graph and resident without
    /// heavy plumbing. Reset each Play session (Editor domain reload).
    /// </summary>
    public static class World
    {
        public static SmartGuardTwin.Home.SceneBuilder Scene;
        public static SmartGuardTwin.Home.NavGraph Nav;
        public static SmartGuardTwin.Avatar.Resident Resident;

        public static Vector3 PositionOf(string id)
            => (Scene != null && Scene.Furniture.TryGetValue(id, out var go)) ? go.transform.position : Vector3.zero;
    }
}
