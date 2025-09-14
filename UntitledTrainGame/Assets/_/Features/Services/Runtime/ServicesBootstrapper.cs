using System.Collections;
using Foundation.Runtime;
using Game.Runtime;
using Manager.Runtime;
using ServiceInterfaces.Runtime;
using ToServiceInterfacesols.Runtime;
using UnityEngine;

namespace Services.Runtime
{
    // Run early so our Start executes before most others' Start,
    // but after all Awake calls (where singletons assign Instance).
    [DefaultExecutionOrder(-1000)]
    public class ServicesBootstrapper : FMono
    {
        private void Start()
        {
            // Start a single coroutine that waits for all Instances, then registers once.
            StartCoroutine(RegisterServicesWhenReady());
        }

        private IEnumerator RegisterServicesWhenReady()
        {
            const int maxFramesToWait = 120; // ~2 seconds at 60 FPS
            int frames = 0;

            // Wait until all singleton Instances are assigned in their Awake,
            // or until timeout.
            while (!AreAllSingletonsReady() && frames < maxFramesToWait)
            {
                frames++;
                yield return null;
            }

            if (!AreAllSingletonsReady())
            {
                Error("Service registration timed out. One or more singletons are still null. " +
                      "Ensure ClockManager, RouteManager, SceneManager, UIManager, and CustomInputManager " +
                      "exist in the first scene and assign Instance in Awake.");
                yield break;
            }

            // Clear previous registrations (e.g., when restarting domain) and register.
            ServiceRegistry.Clear();

            ServiceRegistry.Register<IClockService>(ClockManager.Instance);
            ServiceRegistry.Register<IRouteService>(RouteManager.Instance);
            ServiceRegistry.Register<ISceneService>(SceneManager.Instance);
            ServiceRegistry.Register<IUiService>(UIManager.Instance);
            ServiceRegistry.Register<IInputService>(CustomInputManager.Instance);

            Info("Managers registered in ServiceRegistry");
        }

        private static bool AreAllSingletonsReady()
        {
            return ClockManager.Instance != null &&
                   RouteManager.Instance != null &&
                   SceneManager.Instance != null &&
                   UIManager.Instance != null &&
                   CustomInputManager.Instance != null;
        }
    }
}