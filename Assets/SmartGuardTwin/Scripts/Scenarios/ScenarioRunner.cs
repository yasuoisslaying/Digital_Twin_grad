using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartGuardTwin.Core;
using SmartGuardTwin.Actions;
using SmartGuardTwin.Config;

namespace SmartGuardTwin.Scenarios
{
    /// <summary>
    /// Runs scenarios across multiple days with behavioural variability: each day gets a
    /// resident profile (wake time, pace, skip/extra tendencies), activities are randomly
    /// skipped or have fillers inserted, durations are jittered (in the executor), and
    /// fall / inactivity / medication anomalies are injected. Records every activity's
    /// label, class, anomaly flag and start/end time for export.
    /// </summary>
    public class ScenarioRunner : MonoBehaviour
    {
        public static ScenarioRunner I { get; private set; }

        public struct Activity
        {
            public string scenario, label, klass;
            public bool isAnomaly;
            public DateTime start, end;
        }

        public readonly List<Activity> Activities = new List<Activity>();
        ActionExecutor _exec;
        System.Random _rng;

        // Activities that may be skipped on a given day (structural ones never are).
        static readonly HashSet<string> Skippable = new HashSet<string>
        {
            "Bathe", "Read", "Watch TV", "Dress", "Wash dishes",
            "Cook breakfast", "Eat breakfast", "Cook lunch", "Eat lunch", "Cook dinner", "Eat dinner"
        };
        static readonly string[] FillersEvening = { "Go to toilet", "Watch TV", "Read" };
        static readonly string[] FillersDay = { "Go to toilet" };

        void Awake() { I = this; _rng = new System.Random(SimConfig.Seed); }
        public void Init(ActionExecutor exec) { _exec = exec; }

        DateTime Now() => SimClock.I != null ? SimClock.I.Now : DateTime.Now;

        IEnumerator RunProgram(string scenario, ActionProgram prog)
        {
            DateTime start = Now();
            yield return _exec.Run(prog);
            DateTime end = Now();
            Activities.Add(new Activity
            {
                scenario = scenario, label = prog.label, klass = prog.klass,
                isAnomaly = prog.isAnomaly, start = start, end = end
            });
            if (!SimConfig.FastMode || prog.isAnomaly)
                Debug.Log($"[SCENARIO] {(prog.isAnomaly ? "* " : "  ")}{prog.label,-20} {start:MM-dd HH:mm} -> {end:HH:mm}");
        }

        public IEnumerator RunScenario(string scenario, string[] labels)
        {
            var prof = ResidentProfiles.Current;
            foreach (var label in labels)
            {
                if (Skippable.Contains(label) && _rng.NextDouble() < prof.SkipProb)
                {
                    if (!SimConfig.FastMode) Debug.Log($"[SCENARIO]   (skipped {label})");
                    continue;
                }

                if (label == "Take medication") yield return RunMedicationDose(scenario);
                else
                {
                    var prog = AdlLibrary.Build(label);
                    if (prog == null) { Debug.LogWarning($"[SCENARIO] unknown ADL: {label}"); continue; }
                    yield return RunProgram(scenario, prog);
                }

                // Occasionally insert a plausible filler activity.
                if (_rng.NextDouble() < prof.ExtraProb)
                {
                    var pool = scenario == "evening" ? FillersEvening : FillersDay;
                    var filler = AdlLibrary.Build(pool[_rng.Next(pool.Length)]);
                    if (filler != null) yield return RunProgram(scenario, filler);
                }
            }
        }

        // A medication dose becomes: missed (at the bedside but never opens the pill box,
        // so no contact_medication event), late (~2 h delayed), or normal.
        IEnumerator RunMedicationDose(string scenario)
        {
            double r = _rng.NextDouble();
            double miss = SimConfig.MedicationMissProb;
            double late = SimConfig.MedicationLateProb;

            if (r < miss)
            {
                var p = new ActionProgram("Missed medication") { isAnomaly = true, klass = "missed_medication" };
                p.Add(ActionStep.Go("nightstand")).Add(ActionStep.Wait(60));
                yield return RunProgram(scenario, p);
            }
            else if (r < miss + late)
            {
                if (SimClock.I != null) SimClock.I.Advance(7200f); // ~2 h late
                var p = new ActionProgram("Late medication") { isAnomaly = true, klass = "late_medication" };
                p.Add(ActionStep.Go("medication")).Add(ActionStep.Open("medication"))
                 .Add(ActionStep.Wait(30)).Add(ActionStep.Close("medication"));
                yield return RunProgram(scenario, p);
            }
            else
            {
                yield return RunProgram(scenario, AdlLibrary.Build("Take medication"));
            }
        }

        public IEnumerator RunDays(int n)
        {
            _rng = new System.Random(SimConfig.Seed);
            for (int d = 0; d < n; d++)
            {
                // Pick the day's resident profile.
                ResidentProfiles.Current = SimConfig.RandomizeProfilePerDay
                    ? ResidentProfiles.All[_rng.Next(ResidentProfiles.All.Length)]
                    : ResidentProfiles.All[Mathf.Clamp(SimConfig.FixedProfileIndex, 0, ResidentProfiles.All.Length - 1)];
                var prof = ResidentProfiles.Current;

                if (SimClock.I != null) SimClock.I.SetDay(d);
                string date = SimClock.I != null ? SimClock.I.Now.ToString("yyyy-MM-dd") : "";
                Debug.Log($"[SCENARIO] ===== DAY {d + 1}/{n}  {date}  profile={prof.Name} =====");

                if (SimClock.I != null) SimClock.I.SetTimeOfDay(prof.WakeHour);
                yield return RunScenario("morning", ScenarioConfig.Morning);
                if (SimClock.I != null) SimClock.I.SetTimeOfDay(prof.NoonHour);
                yield return RunScenario("noon", ScenarioConfig.Noon);
                if (SimClock.I != null) SimClock.I.SetTimeOfDay(prof.EveningHour);
                yield return RunScenario("evening", ScenarioConfig.Evening);

                // Inject a fall / prolonged-inactivity anomaly on a fraction of evenings.
                if (_rng.NextDouble() < SimConfig.AnomalyProbabilityPerDay)
                {
                    if (SimClock.I != null) SimClock.I.SetTimeOfDay(22);
                    var anomaly = _rng.Next(2) == 0 ? AnomalyLibrary.Fall() : AnomalyLibrary.ProlongedInactivity();
                    yield return RunProgram("evening", anomaly);
                }

                yield return null; // let a frame pass between days
            }

            int anomalies = 0;
            foreach (var a in Activities) if (a.isAnomaly) anomalies++;
            Debug.Log($"[SCENARIO] All {n} days complete: {Activities.Count} activities, {anomalies} anomalies.");
        }
    }
}
