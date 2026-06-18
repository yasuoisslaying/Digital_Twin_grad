using System.Collections.Generic;
using UnityEngine;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Draws room-name labels over the top-down view via OnGUI (uses Unity's default
    /// GUI font, so no font asset is required). Purely a verification aid.
    /// </summary>
    public class HomeLabels : MonoBehaviour
    {
        struct Label { public string text; public Vector3 world; }
        readonly List<Label> _labels = new List<Label>();

        public void Add(string text, Vector3 world) => _labels.Add(new Label { text = text, world = world });

        void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.black;

            foreach (var l in _labels)
            {
                var sp = cam.WorldToScreenPoint(l.world);
                if (sp.z < 0f) continue; // behind the camera
                GUI.Label(new Rect(sp.x - 60f, Screen.height - sp.y - 10f, 120f, 20f), l.text, style);
            }
        }
    }
}
