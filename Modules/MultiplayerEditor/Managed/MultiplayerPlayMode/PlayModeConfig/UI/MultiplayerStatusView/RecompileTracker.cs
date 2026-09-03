// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: HeadlessRuntime not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static partial class RecompileTracker
    {
        private const int k_MaxTrackedRecompiles = 100;
        private const string k_EditorPrefsKey = "RecompileTracker_Times";

        [AutoStaticsCleanupOnCodeReload] // lazy cache; must re-load from EditorPrefs after reload
        private static List<DateTime> s_CachedRecompileTimes;

        [OnCodeLoaded]
        static void InitializeOnLoad()
        {
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private static List<DateTime> GetRecompileTimes()
        {
            if (s_CachedRecompileTimes != null)
                return s_CachedRecompileTimes;

            s_CachedRecompileTimes = new List<DateTime>();
            var json = EditorPrefs.GetString(k_EditorPrefsKey, "[]");
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<DateTimeListWrapper>(json);
                if (wrapper?.ticks != null)
                {
                    foreach (var tick in wrapper.ticks)
                        s_CachedRecompileTimes.Add(new DateTime(tick, DateTimeKind.Local));
                }
            }
            catch
            {
            }
            return s_CachedRecompileTimes;
        }

        private static void SaveRecompileTimes()
        {
            var times = GetRecompileTimes();
            var ticks = new long[times.Count];
            for (int i = 0; i < times.Count; i++)
                ticks[i] = times[i].Ticks;

            var wrapper = new DateTimeListWrapper { ticks = ticks };
            EditorPrefs.SetString(k_EditorPrefsKey, UnityEngine.JsonUtility.ToJson(wrapper));
        }

        private static void OnCompilationFinished(object context)
        {
            var recompileTimes = GetRecompileTimes();
            recompileTimes.Add(DateTime.Now);

            if (recompileTimes.Count > k_MaxTrackedRecompiles)
                recompileTimes.RemoveAt(0);

            SaveRecompileTimes();
        }

        public static int GetRecompileCountSince(DateTime since)
        {
            var count = 0;
            foreach (var recompileTime in GetRecompileTimes())
            {
                if (recompileTime > since)
                    count++;
            }
            return count;
        }

        internal static void ClearCache()
        {
            s_CachedRecompileTimes = null;
        }

        [Serializable]
        private class DateTimeListWrapper
        {
            public long[] ticks;
        }
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
