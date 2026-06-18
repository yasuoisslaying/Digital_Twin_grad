#!/usr/bin/env python3
"""
SmartGuard Twin - dataset validation (multi-day, 3-class).

Reads ../Output and reports: overview (events/activities/days), sensor events by
TYPE and ROOM, activities by CLASS (normal/fall/prolonged_inactivity) + the injected
anomalies, a per-activity summary with sensor signatures, and activities per day.
Pure standard library; writes PNG plots too if matplotlib is installed.

Usage:  python validate.py
"""
import csv
import os
import sys
import statistics
from collections import Counter, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "Output"))
PAPER_TYPES = ["Zone", "Magnetic", "Power", "Light", "Pressure"]
CLASSES = ["normal", "fall", "prolonged_inactivity", "late_medication", "missed_medication"]


def load(name):
    path = os.path.join(OUT, name)
    if not os.path.exists(path):
        sys.exit(f"Missing {path}\n  -> run the Unity sim (press Play) to generate the dataset first.")
    with open(path, newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def bar(label, n, total, width=34):
    fill = int(round(width * n / total)) if total else 0
    return f"  {label:<20}{n:>6}  {'#' * fill}"


def main():
    events = load("sensor_events.csv")
    acts = load("activity_labels.csv")
    days = sorted({a.get("day", "?") for a in acts})

    print("=" * 72)
    print(" SmartGuard Twin - dataset validation")
    print(f" {OUT}")
    print("=" * 72)
    print(f" sensor events : {len(events)}")
    print(f" activities    : {len(acts)}")
    print(f" days          : {len(days)}" + (f"  ({days[0]} .. {days[-1]})" if days else ""))
    if events:
        print(f" time span     : {events[0]['timestamp']}  ->  {events[-1]['timestamp']}")
    print()

    by_type = Counter(e["type"] for e in events)
    print("Sensor events by TYPE (the paper's 5)")
    for t in PAPER_TYPES:
        print(bar(t, by_type.get(t, 0), len(events)))
    print(f"  -> all 5 present: {'YES' if all(by_type.get(t, 0) for t in PAPER_TYPES) else 'NO'}")
    print()

    by_room = Counter(e["room"] for e in events)
    print("Sensor events by ROOM")
    for r, n in by_room.most_common():
        print(bar(r or "(none)", n, len(events)))
    print()

    cls = Counter(a.get("class", "normal") for a in acts)
    print("Activities by CLASS (SmartGuard 3-class)")
    for c in CLASSES:
        print(bar(c, cls.get(c, 0), len(acts)))
    anoms = [a for a in acts if str(a.get("is_anomaly", "0")) in ("1", "True", "true")]
    print(f"\nInjected anomalies: {len(anoms)}")
    for a in anoms:
        print(f"   {a.get('day','?')}  {a['activity']:<22}{a.get('class',''):<22}{a.get('duration_sec','?')}s")
    print()

    durations = defaultdict(list)
    for a in acts:
        try:
            durations[a["activity"]].append(int(a["duration_sec"]))
        except (ValueError, KeyError):
            pass
    ev_by_act = defaultdict(Counter)
    for e in events:
        ev_by_act[e["activity"]][e["sensor_id"]] += 1
    occ = Counter(a["activity"] for a in acts)

    print("Per-activity summary")
    print(f"  {'activity':<22}{'n':>3}{'avg_dur_s':>10}{'events':>8}   top sensors")
    for act in sorted(occ, key=lambda k: -occ[k]):
        ds = durations.get(act, [])
        avg = int(statistics.mean(ds)) if ds else 0
        evs = sum(ev_by_act[act].values())
        top = ", ".join(f"{s}({c})" for s, c in ev_by_act[act].most_common(3))
        print(f"  {act:<22}{occ[act]:>3}{avg:>10}{evs:>8}   {top}")
    print()

    per_day = Counter(a.get("day", "?") for a in acts)
    print("Activities per day")
    for d in days:
        print(f"   {d}: {per_day[d]}")
    print()

    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt

        fig, ax = plt.subplots(figsize=(6, 3))
        ax.bar(PAPER_TYPES, [by_type.get(t, 0) for t in PAPER_TYPES], color="#4C8BF5")
        ax.set_title("Sensor events by type")
        fig.tight_layout()
        fig.savefig(os.path.join(OUT, "plot_events_by_type.png"), dpi=120)

        fig2, ax2 = plt.subplots(figsize=(5.5, 3))
        ax2.bar(CLASSES, [cls.get(c, 0) for c in CLASSES],
                color=["#34A853" if c == "normal" else "#EA4335" for c in CLASSES])
        ax2.set_title("Activities by class")
        import matplotlib.pyplot as _p
        _p.xticks(rotation=20, ha="right")
        fig2.tight_layout()
        fig2.savefig(os.path.join(OUT, "plot_classes.png"), dpi=120)
        print("[plots] wrote plot_events_by_type.png and plot_classes.png to Output/")
    except ImportError:
        print("[plots] matplotlib not installed - skipping PNGs (pip install matplotlib to enable).")

    print()
    print("Dataset is ready for HAR / 3-class training:")
    print("  sensor_events.csv (features) + activity_labels.csv (labels incl. class).")


if __name__ == "__main__":
    main()
