using UnityEngine;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Core
{
    /// <summary>
    /// Live playback-speed control for demos (uses Time.timeScale, so it scales both
    /// movement and waits). Clickable buttons (bottom-left) work without keyboard focus;
    /// keys also work when the Game view is focused: '[' slower, ']' faster, Space
    /// pause/resume, Backspace reset. Only has a visible effect in watch mode
    /// (SimConfig.FastMode = false) — in fast mode the run finishes near-instantly.
    /// </summary>
    public class DemoControls : MonoBehaviour
    {
        float _saved = 1f;

        void Awake() { Time.timeScale = 1f; }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightBracket)) Faster();
            else if (Input.GetKeyDown(KeyCode.LeftBracket)) Slower();
            else if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            else if (Input.GetKeyDown(KeyCode.Backspace)) Time.timeScale = 1f;
        }

        void Faster() => Time.timeScale = Mathf.Min((Time.timeScale <= 0f ? 1f : Time.timeScale) * 1.5f, 16f);
        void Slower() => Time.timeScale = Mathf.Max((Time.timeScale <= 0f ? 1f : Time.timeScale) / 1.5f, 0.1f);
        void TogglePause()
        {
            if (Time.timeScale > 0f) { _saved = Time.timeScale; Time.timeScale = 0f; }
            else Time.timeScale = _saved;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(8, Screen.height - 104, 470, 96), GUI.skin.box);
            string state = Time.timeScale <= 0f ? "PAUSED" : $"x{Time.timeScale:0.0}";
            GUILayout.Label($"Playback speed: {state}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<< Slower", GUILayout.Height(28))) Slower();
            if (GUILayout.Button(Time.timeScale <= 0f ? "  Resume  " : "  Pause  ", GUILayout.Height(28))) TogglePause();
            if (GUILayout.Button("Faster >>", GUILayout.Height(28))) Faster();
            if (GUILayout.Button("Reset", GUILayout.Height(28))) Time.timeScale = 1f;
            GUILayout.EndHorizontal();

            if (SimConfig.FastMode)
                GUILayout.Label("Fast mode is ON — set SimConfig.FastMode = false to watch & control it.");
            else
                GUILayout.Label("Keys: [ slower   ] faster   Space pause   Backspace reset");
            GUILayout.EndArea();
        }

        void OnDisable() { Time.timeScale = 1f; } // restore when Play stops / on recompile
    }
}
