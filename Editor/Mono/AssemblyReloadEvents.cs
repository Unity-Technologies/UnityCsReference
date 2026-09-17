// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEditor
{
    public static partial class AssemblyReloadEvents
    {
        public delegate void AssemblyReloadCallback();
        public static event AssemblyReloadCallback beforeAssemblyReload
        {
            add => m_BeforeAssemblyReloadEvent.Add(value);
            remove => m_BeforeAssemblyReloadEvent.Remove(value);
        }
        [AutoStaticsCleanupOnCodeReload]
        // Backing list for the beforeAssemblyReload event: subscribers attach through their own lifecycle
        // and re-subscribe after a code reload, so the cleared list refills itself.
        [IgnoreForUAL0015("Event backing store whose subscribers re-register through their own lifecycle")]
        private static EventWithPerformanceTracker<AssemblyReloadCallback> m_BeforeAssemblyReloadEvent =
            new EventWithPerformanceTracker<AssemblyReloadCallback>($"{nameof(AssemblyReloadEvents)}.{nameof(beforeAssemblyReload)}");
        public static event AssemblyReloadCallback afterAssemblyReload
        {
            add => m_AfterAssemblyReloadEvent.Add(value);
            remove => m_AfterAssemblyReloadEvent.Remove(value);
        }
        [AutoStaticsCleanupOnCodeReload]
        // Backing store for the afterAssemblyReload event: subscribers attach through their own lifecycle
        // and re-subscribe after a code reload, so the cleared store refills itself.
        [IgnoreForUAL0015("Event backing store whose subscribers re-register through their own lifecycle")]
        private static EventWithPerformanceTracker<AssemblyReloadCallback> m_AfterAssemblyReloadEvent =
            new EventWithPerformanceTracker<AssemblyReloadCallback>($"{nameof(AssemblyReloadEvents)}.{nameof(afterAssemblyReload)}");

        [RequiredByNativeCode]
        static void OnBeforeAssemblyReload()
        {
            if (!m_BeforeAssemblyReloadEvent.hasSubscribers)
                return;
            using var scope = new ProgressScope("Running managed callbacks", "Executing OnBeforeAssemblyReload Callbacks", forceUpdate: true);
            foreach (var evt in m_BeforeAssemblyReloadEvent)
            {
                evt();
            }
        }

        [RequiredByNativeCode]
        static void OnAfterAssemblyReload()
        {
            if (!m_AfterAssemblyReloadEvent.hasSubscribers)
                return;
            using var scope = new ProgressScope("Running managed callbacks", "Executing OnAfterAssemblyReload Callbacks", forceUpdate: true);
            foreach (var evt in m_AfterAssemblyReloadEvent)
            {
                evt();
            }
        }
    }
}
