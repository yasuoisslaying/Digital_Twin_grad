using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SmartGuardTwin.Sensors;
using SmartGuardTwin.Scenarios;

namespace SmartGuardTwin.Logging
{
    /// <summary>
    /// Writes the generated dataset to the project's Output/ folder:
    ///  - casas_log.txt        CASAS-style "timestamp \t sensor \t value"
    ///  - sensor_events.csv    full records incl. type, room, activity, class
    ///  - activity_labels.csv  ground truth: day, scenario, activity, class, is_anomaly, start/end/dur
    ///  - summary.csv          per-activity counts/durations
    ///  - class_summary.csv    per-class (normal/fall/prolonged_inactivity) totals for the 3-class twin
    /// </summary>
    public static class DatasetLogger
    {
        public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Output"));

        public static void Export()
        {
            var bank = SensorBank.I;
            if (bank == null) { Debug.LogWarning("[Export] No SensorBank; nothing to export."); return; }

            var events = bank.Log;
            var acts = ScenarioRunner.I != null ? ScenarioRunner.I.Activities : new List<ScenarioRunner.Activity>();

            Directory.CreateDirectory(OutputDir);
            WriteCasas(events);
            WriteEventsCsv(events, acts);
            WriteLabelsCsv(acts);
            WriteSummary(events, acts);
            WriteClassSummary(events, acts);

            int anomalies = 0;
            foreach (var a in acts) if (a.isAnomaly) anomalies++;
            Debug.Log($"[Export] Dataset written to:\n  {OutputDir}\n  {events.Count} events, "
                      + $"{acts.Count} activities, {anomalies} anomalies.");
        }

        static void Find(List<ScenarioRunner.Activity> acts, DateTime t, out string label, out string klass)
        {
            // Half-open [start, end): an event at an activity boundary belongs to the
            // activity that is starting, not the one that just ended.
            foreach (var a in acts) if (t >= a.start && t < a.end) { label = a.label; klass = a.klass; return; }
            // Fall back to the closest containing/last activity (handles the final event).
            foreach (var a in acts) if (t >= a.start && t <= a.end) { label = a.label; klass = a.klass; return; }
            label = "Other"; klass = "normal";
        }

        static void WriteCasas(List<SensorEvent> events)
        {
            var sb = new StringBuilder();
            foreach (var e in events)
                sb.Append(e.time.ToString("yyyy-MM-dd HH:mm:ss.ffffff")).Append('\t')
                  .Append(e.sensorId).Append('\t').Append(e.value).Append('\n');
            File.WriteAllText(Path.Combine(OutputDir, "casas_log.txt"), sb.ToString());
        }

        static void WriteEventsCsv(List<SensorEvent> events, List<ScenarioRunner.Activity> acts)
        {
            var sb = new StringBuilder();
            sb.Append("timestamp,sensor_id,type,room,value,activity,class\n");
            foreach (var e in events)
            {
                Find(acts, e.time, out var label, out var klass);
                sb.Append(e.time.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
                  .Append(e.sensorId).Append(',').Append(e.type).Append(',')
                  .Append(e.room ?? "").Append(',').Append(e.value).Append(',')
                  .Append(label).Append(',').Append(klass).Append('\n');
            }
            File.WriteAllText(Path.Combine(OutputDir, "sensor_events.csv"), sb.ToString());
        }

        static void WriteLabelsCsv(List<ScenarioRunner.Activity> acts)
        {
            var sb = new StringBuilder();
            sb.Append("day,scenario,activity,class,is_anomaly,start,end,duration_sec\n");
            foreach (var a in acts)
                sb.Append(a.start.ToString("yyyy-MM-dd")).Append(',')
                  .Append(a.scenario).Append(',').Append(a.label).Append(',')
                  .Append(a.klass).Append(',').Append(a.isAnomaly ? "1" : "0").Append(',')
                  .Append(a.start.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
                  .Append(a.end.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
                  .Append((int)(a.end - a.start).TotalSeconds).Append('\n');
            File.WriteAllText(Path.Combine(OutputDir, "activity_labels.csv"), sb.ToString());
        }

        static void WriteSummary(List<SensorEvent> events, List<ScenarioRunner.Activity> acts)
        {
            var occ = new Dictionary<string, int>();
            var dur = new Dictionary<string, double>();
            var cls = new Dictionary<string, string>();
            foreach (var a in acts)
            {
                Inc(occ, a.label);
                dur[a.label] = (dur.TryGetValue(a.label, out var d) ? d : 0) + (a.end - a.start).TotalSeconds;
                cls[a.label] = a.klass;
            }
            var ev = new Dictionary<string, int>();
            foreach (var e in events) { Find(acts, e.time, out var l, out _); Inc(ev, l); }

            var sb = new StringBuilder();
            sb.Append("activity,class,occurrences,total_duration_sec,avg_duration_sec,sensor_events\n");
            foreach (var kv in occ)
            {
                string l = kv.Key; int o = kv.Value;
                double tot = dur.TryGetValue(l, out var dd) ? dd : 0;
                int e = ev.TryGetValue(l, out var ee) ? ee : 0;
                string k = cls.TryGetValue(l, out var kk) ? kk : "normal";
                sb.Append(l).Append(',').Append(k).Append(',').Append(o).Append(',')
                  .Append((int)tot).Append(',').Append((int)(tot / Math.Max(1, o))).Append(',').Append(e).Append('\n');
            }
            File.WriteAllText(Path.Combine(OutputDir, "summary.csv"), sb.ToString());
        }

        static void WriteClassSummary(List<SensorEvent> events, List<ScenarioRunner.Activity> acts)
        {
            var occ = new Dictionary<string, int>();
            var dur = new Dictionary<string, double>();
            foreach (var a in acts)
            {
                Inc(occ, a.klass);
                dur[a.klass] = (dur.TryGetValue(a.klass, out var d) ? d : 0) + (a.end - a.start).TotalSeconds;
            }
            var ev = new Dictionary<string, int>();
            foreach (var e in events) { Find(acts, e.time, out _, out var k); Inc(ev, k); }

            var sb = new StringBuilder();
            sb.Append("class,activities,total_duration_sec,sensor_events\n");
            foreach (var kv in occ)
            {
                string k = kv.Key;
                sb.Append(k).Append(',').Append(kv.Value).Append(',')
                  .Append((int)(dur.TryGetValue(k, out var dd) ? dd : 0)).Append(',')
                  .Append(ev.TryGetValue(k, out var ee) ? ee : 0).Append('\n');
            }
            File.WriteAllText(Path.Combine(OutputDir, "class_summary.csv"), sb.ToString());
        }

        static void Inc(Dictionary<string, int> d, string k) => d[k] = (d.TryGetValue(k, out var v) ? v : 0) + 1;
    }
}
