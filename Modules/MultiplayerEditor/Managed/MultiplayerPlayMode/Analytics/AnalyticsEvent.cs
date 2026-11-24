// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Text.Json;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static partial class AnalyticsEvent
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_DebugEnabled = MigrationUtility.ShouldEnableMultiplayerPlayMode() && DebugUtils.IsDebugFlagEnabled(DebugUtils.DebugFlags.MppmAnalyticsDebug);
        }

        [AutoStaticsCleanupOnCodeReload] // set from runtime state; must re-evaluate after reload
        private static bool s_DebugEnabled;
        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        public static event Action<IAnalytic.IData> AnalyticSent;

        // Kept out of this class's static initializer, which Initialize triggers on every
        // domain reload, so a reload doesn't load System.Text.Json through this class.
        static class DebugSerializerOptionsHolder
        {
            // Analytics data types typically expose their payload as public fields, which
            // System.Text.Json skips by default (Newtonsoft included them).
            [NoAutoStaticsCleanup] // immutable serializer options; safe to persist across reload
            internal static readonly JsonSerializerOptions k_Options = new JsonSerializerOptions { IncludeFields = true };
        }

        internal static JsonSerializerOptions DebugSerializerOptions => DebugSerializerOptionsHolder.k_Options;

        internal static bool IsDebugEnabled() => s_DebugEnabled;
        internal static void InvokeAnalyticSent<E, T>(AnalyticsEvent<E, T> evt)
            where T : IAnalytic.IData
            where E : AnalyticsEvent<E, T>, new()
        {
            evt.TryGatherData(out IAnalytic.IData data, out Exception error);

            AnalyticSent?.Invoke(data);
        }
    }

    internal abstract class AnalyticsEvent<E, T> : IAnalytic
        where T : IAnalytic.IData
        where E : AnalyticsEvent<E, T>, new()
    {
        private T m_Data;
        public static void Send(T data)
        {
            var analytic = new E { m_Data = data };
            EditorAnalytics.SendAnalytic(analytic);

            AnalyticsEvent.InvokeAnalyticSent(analytic);
            if (AnalyticsEvent.IsDebugEnabled())
                UnityEngine.Debug.Log($"Data Name: {data.GetType()} - Data: {JsonSerializer.Serialize<object>(data, AnalyticsEvent.DebugSerializerOptions)}");
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return true;
        }
    }
}
