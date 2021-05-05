// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor
{
    public static class AssemblyReloadEvents
    {
        public delegate void AssemblyReloadCallback();
        public static event AssemblyReloadCallback beforeAssemblyReload
        {
            add => m_BeforeAssemblyReloadEvent.Add(value);
            remove => m_BeforeAssemblyReloadEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<AssemblyReloadCallback> m_BeforeAssemblyReloadEvent =
            new EventWithPerformanceTracker<AssemblyReloadCallback>($"{nameof(AssemblyReloadEvents)}.{nameof(beforeAssemblyReload)}");
        public static event AssemblyReloadCallback afterAssemblyReload
        {
            add => m_AfterAssemblyReloadEvent.Add(value);
            remove => m_AfterAssemblyReloadEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<AssemblyReloadCallback> m_AfterAssemblyReloadEvent =
            new EventWithPerformanceTracker<AssemblyReloadCallback>($"{nameof(AssemblyReloadEvents)}.{nameof(afterAssemblyReload)}");

        [RequiredByNativeCode]
        static void OnBeforeAssemblyReload()
        {
            foreach (var evt in m_BeforeAssemblyReloadEvent)
                evt();
        }

        [RequiredByNativeCode]
        static void OnAfterAssemblyReload()
        {
            foreach (var evt in m_AfterAssemblyReloadEvent)
                evt();
        }
    }
}
