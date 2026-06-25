// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEngine.AdaptivePerformance
{
    internal static partial class AdaptivePerformanceInitializer
    {
        [AutoStaticsCleanup]
        static AdaptivePerformanceManagerSpawner s_Spawner;

        [RequiredByNativeCode(optional: false)]
        public static void AutoInitializeAdaptivePerformanceManaged()
        {
            InitializeSpawner(isAuto: true);
        }

        public static void Initialize()
        {
            InitializeSpawner(isAuto: false);
        }

        public static void Deinitialize()
        {
            if (s_Spawner == null)
                return;

            s_Spawner.Deinitialize();
            UnityEngine.Object.Destroy(s_Spawner);
            s_Spawner = null;
        }

        static void InitializeSpawner(bool isAuto)
        {
            if (s_Spawner == null)
                s_Spawner = ScriptableObject.CreateInstance<AdaptivePerformanceManagerSpawner>();

            if (s_Spawner != null && s_Spawner.ManagerGameObject != null)
                return;

            // isAuto translates to isCheckingProvider:
            //    - IsAuto=True, then isCheckingProvider=true; DO check if provider is present
            //    - IsAuto=False, then isCheckingProvider=false; DON'T check if provider is present
            // the reason for this is the auto startup process initializes providers, and should be available at the
            // time of the check.  During a manual startup, the providers have not yet been initialized, so skipping
            // the check so that the AP Manager is created and available to be initialized and the provider is then
            // made available.
            s_Spawner.Initialize(isCheckingProvider: isAuto);
        }
    }
}
