using UnityEngine;
using SmartGuardTwin.Objects;

namespace SmartGuardTwin.Core
{
    /// <summary>
    /// Phase 2 manual test harness: hotkeys to toggle a few objects so their state
    /// changes are visible in Play, plus an on-screen state readout. The avatar will
    /// drive these objects automatically from Phase 3 onward.
    /// </summary>
    public class DebugInput : MonoBehaviour
    {
        readonly string[] _watch =
            { "fridge", "front_door", "wardrobe", "stove", "tv", "microwave", "light_kitchen", "light_livingroom", "bed", "sofa" };

        void Update()
        {
            Bind(KeyCode.F, "fridge");
            Bind(KeyCode.H, "front_door");
            Bind(KeyCode.N, "wardrobe");
            Bind(KeyCode.G, "stove");
            Bind(KeyCode.J, "tv");
            Bind(KeyCode.M, "microwave");
            Bind(KeyCode.L, "light_kitchen");
            Bind(KeyCode.K, "light_livingroom");
            Bind(KeyCode.B, "bed");
            Bind(KeyCode.U, "sofa");
        }

        void Bind(KeyCode key, string id)
        {
            if (!Input.GetKeyDown(key)) return;
            var o = ObjectRegistry.Get(id);
            if (o == null) return;
            switch (o)
            {
                case Openable op:  op.Toggle(); break;
                case Powerable pw: pw.Toggle(); break;
                case RoomLight rl: rl.Toggle(); break;
                case Occupiable oc: oc.SetOccupied(!oc.IsOccupied); break;
            }
            Debug.Log($"[Debug] {id} -> {o.StateText}");
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(8, 8, 300, 330), GUI.skin.box);
            GUILayout.Label("Phase 2 - interactive objects");
            GUILayout.Label("Toggle keys:");
            GUILayout.Label("F fridge    H door    N wardrobe");
            GUILayout.Label("G stove    J tv    M microwave");
            GUILayout.Label("L kitchen-light    K living-light");
            GUILayout.Label("B bed    U sofa");
            GUILayout.Space(8);
            foreach (var id in _watch)
            {
                var o = ObjectRegistry.Get(id);
                if (o != null) GUILayout.Label($"   {id}: {o.StateText}");
            }
            GUILayout.EndArea();
        }
    }
}
