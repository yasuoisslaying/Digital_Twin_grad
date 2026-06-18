# CLAUDE.md — SmartGuard Unity Twin: AI/agent handoff & architecture

This file is the **single source of truth for an AI continuing this project cold.**
It describes what the system is, how it runs, every file, the conventions/invariants you
must preserve, and recipes for extending it. Read it fully before changing code.

---

## 1. What this project is

A **custom Unity 3D digital twin** of a 6-room apartment with one elderly resident. On
Play it builds the home, drives the resident through Activities of Daily Living (ADLs)
across multiple simulated days, reads **virtual ambient sensors**, and **exports a
labeled dataset** to `UnityTwin/Output/`.

It is a faithful reproduction of the data-generation method of **Bouchabou et al.,
"A Smart Home Digital Twin…", Sensors 2023, 23, 7586** (PDF + extracted `paper_text.txt`
are in the repo root), **extended** for the user's **SmartGuard** graduation project with:
multi-day generation, behavioural variability, and **anomaly classes** (fall, prolonged
inactivity, medication late/missed) for a behavioural-monitoring twin.

**This is "trial 2": a deliberate pivot to a hand-built Unity scene** (NOT VirtualHome /
evolving_graph, which an earlier attempt used). Keep it engine-faithful but custom.

## 2. Layout & where things live

```
Grad_dataset_trial2/                 <- working directory
  CLAUDE.md                          <- this file
  sensors-23-paper3.pdf              <- the paper
  paper_text.txt                     <- extracted paper text (grep this for paper details)
  UnityTwin/                         <- the Unity project
    README.md                        <- human quickstart/tuning
    Assets/SmartGuardTwin/Scripts/   <- ALL code (one Assembly-CSharp, no asmdef)
    Output/                          <- generated dataset (created at runtime; gitignored)
    tools/validate.py                <- pure-Python dataset validator
    ml/                              <- Python HAR training (BiLSTM) + REPORT.md + figures (§13)
    ProjectSettings/ProjectVersion.txt  <- pins Unity 2021.3.10f1
    Packages/manifest.json           <- minimal built-in modules only
```

## 3. Environment & how it runs (critical)

- **Unity 2021.3.10f1** (LTS), Windows, **Built-in render pipeline**, legacy Input
  Manager. No extra packages, no asset imports, **no NavMesh**.
- **Auto-boot, no scene setup:** `SimulationBootstrap.AutoStart()` is a
  `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` that spawns a GameObject and adds the
  bootstrap; its `Awake()` builds *everything in code*. **Just press Play in any scene.**
  Do not rely on hand-edited `.unity` scenes.
- **You (an AI) generally cannot drive the Editor headlessly** — if the user has the
  project open, a second `Unity -batchmode` instance fails on the project lock. **Verify
  changes by asking the user to press Play**, then inspect `Output/` with
  `python UnityTwin/tools/validate.py` or by reading the CSVs directly. Compile errors
  surface in Unity's Console / "Safe Mode" dialog — ask the user to paste them.
- **C# edits hot-reload** when the user focuses Unity (it recompiles).

## 4. Architecture & data flow

`SimulationBootstrap.Awake()` wires the whole pipeline, in this order:

1. **`SceneBuilder.Build()`** — builds colour-coded floor zones (`RoomZone` + collider),
   perimeter + interior walls (with doorway gaps), furniture primitives, a sun light, the
   `CameraController`, and `HomeLabels`. Created furniture GameObjects are kept in
   `SceneBuilder.Furniture` (id→GameObject).
2. **`InteractionBuilder.Setup(scene)`** — attaches state components
   (`Openable`/`Powerable`/`RoomLight`/`Occupiable`) to furniture by id, creates a point
   light per room, and registers everything in the static **`ObjectRegistry`** (id→component).
3. **`World`** statics set: `World.Scene`, `World.Nav = new NavGraph()`, `World.Resident`.
4. **`SimClock`** + **`DemoControls`** components added.
5. **`ResidentBuilder.Build()`** — primitive avatar (capsule+head+facing nub) with
   `Resident` + `Mover`.
6. **`SensorBank`** added + `SetupObjectSensors()` — subscribes the 4 graph-based sensor
   types to each object's `StateChanged`. (Zone-occupancy is emitted separately, see §6.)
7. **`ActionExecutor`** (bound to the resident) + **`ScenarioRunner`**.
8. `StartCoroutine(RunDaysThenExport)` → `runner.RunDays(SimConfig.Days)` →
   **`DatasetLogger.Export()`**. Also `OnApplicationQuit` → `Export()` (so stopping Play
   early still saves).

**Runtime loop (`ScenarioRunner.RunDays`)** per day: pick a `ResidentProfile`, `SetDay`,
then for morning/noon/evening: `SetTimeOfDay(profile hour)` → `RunScenario(labels)`. Each
`RunScenario` may **skip** an optional activity or insert a **filler**; `Take medication`
is routed to `RunMedicationDose` (normal/late/missed). After the evening, a
fall/inactivity anomaly may be injected. Every activity is recorded as a
`ScenarioRunner.Activity {scenario,label,klass,isAnomaly,start,end}`.

**Each activity** = an `ActionProgram` of `ActionStep`s run by `ActionExecutor.Run`
(coroutine). `Mover.MoveTo` walks doorway waypoints (teleports in FastMode), **advances
`SimClock`** by walk time, and emits **zone** events via `SensorBank.EnterZone`. Object
interactions (`Open/SetPower/SetOn/SetOccupied`) fire `StateChanged` → `SensorBank.Record`.

**Dataset** = `SensorBank.Log` (events, each stamped with `SimClock.Now`) joined with
`ScenarioRunner.Activities` at export.

## 5. Full file map (namespace `SmartGuardTwin.*`, one assembly)

**Core/**
- `SimulationBootstrap.cs` — auto-boot entry point; wires the pipeline (§4); `BuildTest`
  removed; `OnApplicationQuit` exports.
- `World.cs` — static holder: `Scene`, `Nav`, `Resident`, `PositionOf(id)`.
- `SimClock.cs` — owned sim time; **manual** `Advance(s)`, `SetTimeOfDay(h)`, `SetDay(i)`
  (base date 2025-01-01). Not wall-clock.
- `DemoControls.cs` — live `Time.timeScale` control (buttons + `[`/`]`/Space/Backspace).
- `DebugInput.cs` — **LEGACY/UNUSED** (Phase-2 manual hotkeys); not added by bootstrap.

**Config/** (data, no behaviour)
- `SimConfig.cs` — the main tuning knobs (§8).
- `HomeLayout.cs` — apartment geometry: `Width=11.5`, `Depth=6.3`, `Rooms[]`,
  `Outside`, `Walls[]` (WallLine), `Doorways[]` (DoorLink), `Furniture[]` (Item). Paper
  coordinate convention (§7).
- `ScenarioConfig.cs` — `Morning`/`Noon`/`Evening` arrays of ADL labels.
- `ResidentProfile.cs` — `ResidentProfile` + static `ResidentProfiles.All[]` + `.Current`.

**Home/**
- `SceneBuilder.cs` — builds floors/walls/furniture/camera/light/labels; owns `Furniture`
  dict + `Zones`. `WZ(z)=Depth-z` flip.
- `NavGraph.cs` — room adjacency from `Doorways`; `RoomAt(world)`, `RoomCenterWorld`,
  `RoomPathTo(a,b)` (BFS), `WaypointsTo`.
- `RoomZone.cs` — tags a floor with its room name.
- `InteractionBuilder.cs` — attaches state components + room lights; fills `ObjectRegistry`.
- `HomeLabels.cs` — OnGUI room-name labels.
- `CameraController.cs` — 3D orbit (default) / top-down (`V`); drag + scroll.

**Objects/**
- `InteractiveObject.cs` — base: `Id`, `StateChanged` event, `Init(id)`, `StateText`.
- `Openable.cs` (magnetic; swinging door visual), `Powerable.cs` (power; emissive),
  `RoomLight.cs` (light; point light), `Occupiable.cs` (pressure; green tint; Seat/Bed).
- `ObjectRegistry.cs` — static id→InteractiveObject map (`Get`, `Get<T>`, `All`, `Clear`).

**Avatar/**
- `Resident.cs` — posture (Standing/Sitting/Lying) + `CurrentRoom`.
- `Mover.cs` — waypoint movement; advances clock per segment; emits zone via
  `SensorBank.EnterZone`; **teleports when `SimConfig.FastMode`**.
- `ResidentBuilder.cs` — builds the primitive avatar. **Named ResidentBuilder, not
  AvatarBuilder**, to avoid clashing with `UnityEngine.AvatarBuilder` (see §10).

**Actions/**
- `ActionProgram.cs` — `ActionType` enum, `ActionStep` factories
  (`Go/GoRoom/Open/Close/On/Off/LightOn/LightOff/Sit/Lie/Stand/Collapse/Wait/Say`), and
  `ActionProgram {label, steps, isAnomaly, klass}`.
- `ActionExecutor.cs` — runs a program; applies **duration jitter × profile scale** on
  `Wait`; `FastMode` skips real pauses; HUD (top-left) + red ANOMALY banner.
- `AdlLibrary.cs` — the 16 ADLs + `Take medication` as parameterized programs; `Labels[]`.
- `AnomalyLibrary.cs` — `Fall()`, `ProlongedInactivity()` (set `isAnomaly`,`klass`).

**Scenarios/**
- `ScenarioRunner.cs` — multi-day loop, profile selection, skip/extra, medication
  dose logic, anomaly injection; records `Activities`.

**Sensors/**
- `SensorEvent.cs` — `SensorType {Magnetic,Pressure,Light,Power,Zone}` + `SensorEvent`.
- `SensorBank.cs` — `I` singleton; `Log` (the dataset); `Record(...)`; `EnterZone(room)`
  (path-based zone); `SetupObjectSensors()`; HUD (top-right).

**Logging/**
- `DatasetLogger.cs` — `Export()` writes the 5 output files; `Find()` uses **half-open
  [start,end)** activity windows.

## 6. Conventions & invariants — DO NOT BREAK

- **Coordinates:** `HomeLayout` uses the *paper* convention — origin NW, `x`=east
  `0..Width`, `z`=south-from-north `0..Depth`. `SceneBuilder`/`NavGraph` convert to Unity
  world with **`worldZ = Depth - z`** so top-down north is +Z. Furniture sits on the floor
  (`y = height/2`). If you add geometry, follow this.
- **Auto-boot:** never require manual scene wiring; add components in code from the
  bootstrap. The scene can be empty.
- **Globals:** `World.*`, `ObjectRegistry`, `SensorBank.I`, `SimClock.I`,
  `ScenarioRunner.I`, `ResidentProfiles.Current` are the shared state. Editor domain
  reload resets statics each Play.
- **Sensors = 5 types** (paper): magnetic, pressure (bed+sofa only), light (per room),
  power, zone (floor). Object sensors are **event-driven** (subscribe to `StateChanged`);
  **zone is path-based** via `Mover`→`SensorBank.EnterZone` (the old raycast `ZoneSensor`
  was removed). Sensor ids: `floor_<room>`, `contact_<obj>`, `power_<obj>`,
  `light_<room>`, `pressure_<obj>` (+ `contact_medication`).
- **Clock is manual.** `Mover` advances it by `distance/SimWalk`; `ActionExecutor`
  advances it for interactions/Waits; `ScenarioRunner` sets time-of-day/day. Events are
  stamped with `SimClock.Now`. Quiet gaps (between activities, during anomaly Waits) are
  intentional signal.
- **FastMode** (`SimConfig.FastMode`): teleport + no real waits → seconds for many days.
  `false` = animated/watchable (DemoControls + visible anomaly holds).
- **Reproducibility:** two seeded RNG streams — `ScenarioRunner._rng = Random(Seed)`
  (profiles/skip/extra/anomaly/medication) and `ActionExecutor._rng = Random(Seed+7)`
  (duration jitter). Same `Seed` ⇒ same dataset.
- **5 classes:** `normal`, `fall`, `prolonged_inactivity`, `late_medication`,
  `missed_medication`.
- **Output is at the project root** (`Application.dataPath/../Output`), outside `Assets/`
  so Unity does not import it.

## 7. Output data schema
- `casas_log.txt`: `YYYY-MM-DD HH:MM:SS.ffffff \t sensor_id \t value`
- `sensor_events.csv`: `timestamp, sensor_id, type, room, value, activity, class`
- `activity_labels.csv`: `day, scenario, activity, class, is_anomaly, start, end, duration_sec`
- `summary.csv`: `activity, class, occurrences, total_duration_sec, avg_duration_sec, sensor_events`
- `class_summary.csv`: `class, activities, total_duration_sec, sensor_events`

## 8. Tuning surface (`SimConfig.cs` unless noted)
`Days`, `FastMode`, `Seed`, `AnomalyProbabilityPerDay`, `MedicationMissProb`,
`MedicationLateProb`, `DemoAnomalyPauseSeconds`, `DurationJitter`,
`RandomizeProfilePerDay`, `FixedProfileIndex`.
- `ResidentProfile.cs` → `ResidentProfiles.All[]`: per profile `WakeHour/NoonHour/
  EveningHour/DurationScale/SkipProb/ExtraProb`.
- `ScenarioConfig.cs` → daily ADL lists. `ScenarioRunner` → `Skippable` set + filler pools.
- `AdlLibrary.cs` → each ADL's steps + `Wait` seconds (duration + sensor signature).
- `AnomalyLibrary.cs` → anomaly programs. `HomeLayout.cs`/`InteractionBuilder.cs` → home
  & which objects are sensored. `Mover.speed` (visual) / `SimWalk` (sim walk speed).

## 9. Run / regenerate / validate
- **Generate:** set `FastMode=true`, `Days=N` → Play → wait for `[Export]` → Stop.
- **Validate:** `python UnityTwin/tools/validate.py` (reads `Output/`).
- **Demo:** `FastMode=false`, `Days=1` → Play → use the bottom-left speed buttons.

## 10. Known gotchas
- **Naming:** the avatar builder is `ResidentBuilder`; do **not** create a class named
  `AvatarBuilder` (collides with `UnityEngine.AvatarBuilder` → CS0104). Watch for similar
  clashes (e.g. `UnityEngine.Random` — code uses `System.Random` explicitly).
- `DebugInput.cs` is legacy and unused (not added by bootstrap). `ZoneSensor.cs` was
  deleted (zone is path-based now). Don't resurrect either expecting them to be wired.
- Movement is **straight-line between doorway waypoints** (rooms are convex; no
  pathfinding). Walls have colliders but movement ignores physics — routing through
  doorways is what avoids visual clipping. Adding a wall without a doorway can trap the
  avatar's path visually (data still fine since rooms are detected by position).
- **Class balance depends on `Days`/`Seed`** — a short run can yield 0 of some anomaly
  class. For training use `Days≈30–60`.
- `Packages/manifest.json` is intentionally minimal (built-in modules only);
  `ProjectVersion.txt` pins `2021.3.10f1` (user's may include the full revision hash).
- HUD overlays are `OnGUI`: ActionExecutor (top-left), SensorBank (top-right),
  DemoControls (bottom-left), HomeLabels (room names).
- **No real-world validation is possible here** — the paper's headline result
  (cross-correlation + train-on-synthetic/test-on-real, ~80% F1) needs the real
  Experiment'Haal recordings, which we don't have. We can only claim structural/face validity.

## 11. Extension recipes
- **Add an ADL:** add a `case` in `AdlLibrary.Build` returning an `ActionProgram` of
  steps (use existing object ids from `HomeLayout.Furniture`); add its label to
  `AdlLibrary.Labels` and to the relevant `ScenarioConfig` list. Add to `Skippable` if optional.
- **Add an object + sensor:** add an `Item` to `HomeLayout.Furniture`; register it in
  `InteractionBuilder.Setup` (openables/powerables/occupiables list) → it gets a sensor
  automatically.
- **Add an anomaly class:** add a builder in `AnomalyLibrary` (set `isAnomaly=true`,
  `klass="..."`); inject it in `ScenarioRunner.RunDays` (or `RunMedicationDose`). The
  exporter & validator pick up new class strings automatically.
- **Add a resident profile:** add a `ResidentProfile` to `ResidentProfiles.All[]`.
- **More realism (future):** per-step position jitter (path variation), simulated sensor
  noise/dropouts, a second resident, weekday/weekend routines.
- **ML (implemented — see §13):** the paper's Liciotti BiLSTM trained on the dataset for
  activity-recognition + 5-class behavioral targets, in `UnityTwin/ml/`.

## 12. Status (2026-06-10)
Complete & validated: faithful 6-room twin, 5 paper sensor types, 16 ADLs + medication,
3 daily scenarios, multi-day + fast generation, fall/inactivity + medication late/missed
anomalies (5 classes), per-day resident profiles + duration jitter + skip/extra
variability, watch-mode demo controls, and `validate.py`. A 60-day run (1528 activity
sequences) trained the paper's Liciotti BiLSTM for both activity recognition and 5-class
behavioral targets — see §13 and `UnityTwin/ml/REPORT.md`.

Related: full plan at `~/.claude/plans/frolicking-puzzling-twilight.md`; project memory
note `dataset-trial2-unity-route`.

## 13. ML / training pipeline (`UnityTwin/ml/`)
Replicates the paper's HAR model on the generated dataset — a separate Python project
(venv `ml/.venv`, Python 3.10 + tensorflow/numpy/pandas/scikit-learn/matplotlib). Run order:
`ml/.venv/Scripts/python {preprocess,train,report}.py`.
- `preprocess.py` — segments `Output/sensor_events.csv` by the `activity_labels.csv` windows
  into per-activity integer-token sequences (token = `sensor_id_value`); pads to a fixed
  length; writes `ml/data/` (`X.npy`, `y_activity.npy`, `y_class.npy`, `durations.npy`,
  `vocab.json`, `label_maps.json`, `meta.json`). numpy/pandas only (no TF).
- `model.py` — the Liciotti BiLSTM: `Embedding(64, mask_zero) → Bidirectional(LSTM(64)) →
  Dense(softmax)`; `build_behavior_model` adds a normalized-duration input branch.
- `train.py` — trains TWO models: (1) activity recognition (~21 ADL labels), (2) behavioral
  5-class (duration aux + balanced class weights). Stratified 80/20 + EarlyStopping
  (restore_best). Saves `ml/models/*.keras`, `ml/results/report.txt`, `metrics.json`,
  `history_*.json`, `preds_*.npz`, confusion PNGs.
- `report.py` — writes `ml/REPORT.md` + 11 figures in `ml/results/figures/` (class / activity
  / sensor-type / room distributions, duration boxplots, training curves, confusion matrices,
  per-class F1). Reads train.py's artifacts (no TF needed).

Last run (60-day dataset, 1528 sequences): **activity recognition** acc 0.89 / macro-F1 0.76
— the only confusions are Eat breakfast/lunch/dinner and Sleep vs Sleep-in-Bed (identical
signatures, separable only by time-of-day; exactly why the paper merged those labels).
**Behavioral 5-class** acc 0.86 / balanced-acc 0.97 — near-total anomaly recall, low
precision on the rare classes (the intended safety trade-off; tune with more days). **Faithful**
to the paper (architecture, sequence representation, 80/20 + early-stop); **adapted:**
stratified holdout not leave-one-subject-out (single resident); synthetic-only — the paper's
real-vs-synthetic transfer test needs real recordings we do not have.
