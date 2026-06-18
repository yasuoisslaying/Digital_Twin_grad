using System.Collections;
using UnityEngine;
using SmartGuardTwin.Core;
using SmartGuardTwin.Objects;
using SmartGuardTwin.Avatar;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Actions
{
    /// <summary>
    /// Runs an <see cref="ActionProgram"/> as a coroutine: walks the resident to targets
    /// (the Mover advances the sim clock + emits zone events), toggles object state, and
    /// changes posture. In fast mode the real-time pauses are skipped. A small HUD shows
    /// the current activity, room, posture and sim time.
    /// </summary>
    public class ActionExecutor : MonoBehaviour
    {
        const float InteractSecs = 6f;   // sim seconds a discrete interaction "takes"

        Resident _res;
        Mover _mover;
        string _current = "(idle)";
        bool _currentIsAnomaly;
        readonly System.Random _rng = new System.Random(SimConfig.Seed + 7);

        public void Bind(Resident res) { _res = res; _mover = res.GetComponent<Mover>(); }
        public void Begin(ActionProgram program) { StartCoroutine(Run(program)); }

        public IEnumerator Run(ActionProgram prog)
        {
            _current = prog.label;
            _currentIsAnomaly = prog.isAnomaly;
            if (!SimConfig.FastMode) Debug.Log($"[ADL] >>> {prog.label}");
            foreach (var step in prog.steps) yield return RunStep(step);
            _current = "(idle)";
            _currentIsAnomaly = false;
        }

        IEnumerator RunStep(ActionStep s)
        {
            switch (s.type)
            {
                case ActionType.GoToRoom:   yield return _mover.MoveTo(World.Nav.RoomCenterWorld(s.target), s.target); break;
                case ActionType.GoToObject: yield return GoTo(s.target); break;
                case ActionType.Open:     SetOpen(s.target, true);   Adv(InteractSecs); yield return Pause(); break;
                case ActionType.Close:    SetOpen(s.target, false);  Adv(InteractSecs); yield return Pause(); break;
                case ActionType.PowerOn:  SetPower(s.target, true);  Adv(InteractSecs); yield return Pause(); break;
                case ActionType.PowerOff: SetPower(s.target, false); Adv(InteractSecs); yield return Pause(); break;
                case ActionType.LightOn:  SetLight(s.target, true);  Adv(3f); yield return Pause(); break;
                case ActionType.LightOff: SetLight(s.target, false); Adv(3f); yield return Pause(); break;
                case ActionType.Sit: yield return SitLie(s.target, Resident.Posture.Sitting); break;
                case ActionType.Lie: yield return SitLie(s.target, Resident.Posture.Lying); break;
                case ActionType.Collapse: _res.SetPosture(Resident.Posture.Lying); Adv(InteractSecs); yield return Pause(); break;
                case ActionType.StandUp: StandUp(); Adv(InteractSecs); yield return Pause(); break;
                case ActionType.Wait:
                {
                    // Scale by the day's profile pace and apply +/- jitter so durations vary.
                    float dur = s.value * ResidentProfiles.Current.DurationScale
                                * (1f + (float)(_rng.NextDouble() * 2.0 - 1.0) * SimConfig.DurationJitter);
                    Adv(dur);
                    yield return WaitReal(dur);
                    break;
                }
                case ActionType.Say: if (!SimConfig.FastMode) Debug.Log($"[ADL]   \"{s.target}\""); break;
            }
        }

        IEnumerator GoTo(string id)
        {
            Vector3 p = World.PositionOf(id);
            string room = World.Nav.RoomAt(p);
            Vector3 center = World.Nav.RoomCenterWorld(room);
            Vector3 dir = center - p; dir.y = 0f;
            Vector3 stand = p + (dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward) * 0.8f;
            stand.y = 0f;
            yield return _mover.MoveTo(stand, room);
        }

        IEnumerator SitLie(string id, Resident.Posture posture)
        {
            yield return GoTo(id);
            Vector3 p = World.PositionOf(id);
            float height = (World.Scene != null && World.Scene.Furniture.TryGetValue(id, out var go))
                ? go.transform.localScale.y : 0.5f;
            var t = _res.transform;
            t.position = new Vector3(p.x, posture == Resident.Posture.Lying ? height : 0f, p.z);
            _res.SetPosture(posture);
            var occ = ObjectRegistry.Get<Occupiable>(id);
            if (occ != null) occ.SetOccupied(true);
            Adv(InteractSecs);
            yield return Pause();
        }

        void StandUp()
        {
            foreach (var io in ObjectRegistry.All)
                if (io is Occupiable oc && oc.IsOccupied) oc.SetOccupied(false);
            var t = _res.transform;
            t.position = new Vector3(t.position.x, 0f, t.position.z);
            _res.SetPosture(Resident.Posture.Standing);
        }

        void SetOpen(string id, bool open) { var o = ObjectRegistry.Get<Openable>(id); if (o != null) o.SetOpen(open); else Warn(id); }
        void SetPower(string id, bool on)  { var o = ObjectRegistry.Get<Powerable>(id); if (o != null) o.SetPower(on); else Warn(id); }
        void SetLight(string room, bool on){ var o = ObjectRegistry.Get<RoomLight>("light_" + room); if (o != null) o.SetOn(on); else Warn("light_" + room); }
        static void Warn(string id) => Debug.LogWarning($"[ADL] missing object: {id}");

        static void Adv(float seconds) { if (SimClock.I != null) SimClock.I.Advance(seconds); }
        static IEnumerator Pause() { if (SimConfig.FastMode) yield break; yield return new WaitForSeconds(0.15f); }
        IEnumerator WaitReal(float simSeconds)
        {
            if (SimConfig.FastMode) yield break;
            // Anomalies (fall / inactivity / missed-med) hold visibly so a demo viewer sees the freeze.
            float real = _currentIsAnomaly
                ? Mathf.Max(SimConfig.DemoAnomalyPauseSeconds, 0.1f)
                : Mathf.Clamp(simSeconds / 120f, 0.1f, 1.0f);
            yield return new WaitForSeconds(real);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(8, 8, 320, 132), GUI.skin.box);
            GUILayout.Label("Resident");
            GUILayout.Label($"Activity: {_current}");
            if (_currentIsAnomaly)
            {
                var warn = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                warn.normal.textColor = Color.red;
                GUILayout.Label(">> ANOMALY <<", warn);
            }
            if (_res != null) GUILayout.Label($"Room: {_res.CurrentRoom}    Posture: {_res.CurrentPosture}");
            if (SimClock.I != null) GUILayout.Label($"Sim time: {SimClock.I.Now:yyyy-MM-dd HH:mm}");
            GUILayout.EndArea();
        }
    }
}
