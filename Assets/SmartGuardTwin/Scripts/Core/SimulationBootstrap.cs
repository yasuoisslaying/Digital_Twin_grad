using UnityEngine;
using SmartGuardTwin.Home;
using SmartGuardTwin.Avatar;
using SmartGuardTwin.Actions;
using SmartGuardTwin.Sensors;
using SmartGuardTwin.Scenarios;
using SmartGuardTwin.Config;
using SmartGuardTwin.Logging;

namespace SmartGuardTwin.Core
{
    /// <summary>
    /// Entry point for the SmartGuard digital-twin simulation
    /// (replication of Bouchabou et al., "A Smart Home Digital Twin...", Sensors 2023).
    ///
    /// Auto-spawns on Play via <see cref="RuntimeInitializeOnLoadMethodAttribute"/> — just
    /// press Play in any scene and the simulation builds itself.
    ///
    /// PHASE 3 - builds the home + interactive objects, spawns the resident, and runs a
    /// test action routine through the doorways. Phase 4 adds the virtual sensors that
    /// watch the object/room state the avatar changes here.
    /// </summary>
    public class SimulationBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindObjectOfType<SimulationBootstrap>() != null) return;
            new GameObject("SmartGuardTwin").AddComponent<SimulationBootstrap>();
        }

        private void Awake()
        {
            Debug.Log("====================================================");
            Debug.Log("  SmartGuard Digital Twin  -  Bouchabou 2023 replica");
            Debug.Log("  Phase 8: multi-day generation + anomalies");
            Debug.Log("====================================================");

            var builder = new SceneBuilder();
            builder.Build();
            InteractionBuilder.Setup(builder);

            World.Scene = builder;
            World.Nav = new NavGraph();

            gameObject.AddComponent<SimClock>();
            gameObject.AddComponent<DemoControls>();

            var resident = ResidentBuilder.Build(builder.Root, World.Nav.RoomCenterWorld("bedroom"));
            World.Resident = resident;

            var bank = gameObject.AddComponent<SensorBank>();
            bank.SetupObjectSensors();

            var exec = gameObject.AddComponent<ActionExecutor>();
            exec.Bind(resident);

            var runner = gameObject.AddComponent<ScenarioRunner>();
            runner.Init(exec);
            StartCoroutine(RunDaysThenExport(runner));

            Debug.Log($"[SmartGuardTwin] Phase 8: generating {SimConfig.Days} day(s) "
                      + $"(fast mode = {SimConfig.FastMode}) with injected anomalies, then exporting to Output/.");
        }

        private System.Collections.IEnumerator RunDaysThenExport(ScenarioRunner runner)
        {
            yield return runner.RunDays(SimConfig.Days);
            DatasetLogger.Export();
        }

        // Safety net: also export whatever has been collected if Play is stopped early.
        private void OnApplicationQuit()
        {
            DatasetLogger.Export();
        }
    }
}
