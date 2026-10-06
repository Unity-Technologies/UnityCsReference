// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        // The lazy guard in GetRecompileTimes() is `if (s_CachedRecompileTimes != null) return ...`, so it
        // only reloads from EditorPrefs when the field is null. Automatic cleanup of a List<T> empties the
        // list instead of nulling it, which makes the guard pass with an EMPTY list: the EditorPrefs reload
        // is skipped and the next SaveRecompileTimes() overwrites the stored history with the truncated one,
        // permanently losing the tracked recompile times. The entries are plain DateTime values, so keeping
        // the list across a reload pins nothing.
        [NoAutoStaticsCleanup]
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
