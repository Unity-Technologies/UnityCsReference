// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

 using System;
using UnityEditor;
using UnityEngine.Analytics;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Multiplayer.Center.Editor.Analytics
{
    /// <summary>
    /// Shared constants for Multiplayer Center analytics events.
    /// </summary>
    internal static class MultiplayerCenterAnalyticsConstants
    {
        // Editor Analytics vendorKey
        public const string k_VendorKey = "unity.multiplayer.center";

        // Multiplayer Center event names
        public const string k_WindowChanged = "multiplayer_center_v2_windowChanged";
        public const string k_GenreSelected = "multiplayer_center_v2_genreSelected";
        public const string k_PackageInstalled = "multiplayer_center_v2_packageInstalled";
        public const string k_SampleImported = "multiplayer_center_v2_sampleImported";
    }

    /// <summary>
    /// Static infrastructure for Multiplayer Center analytics events.
    /// </summary>
    internal static partial class AnalyticsEvent
    {
        /// <summary>
        /// Raised after every successful <see cref="AnalyticsEvent{TEvent,TData}.Send"/> call, with the payload that was sent.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<IAnalytic.IData> AnalyticSent;

        internal static void InvokeAnalyticSent<TEvent, TData>(AnalyticsEvent<TEvent, TData> evt)
            where TData : IAnalytic.IData
            where TEvent : AnalyticsEvent<TEvent, TData>, new()
        {
            evt.TryGatherData(out var data, out _);
            AnalyticSent?.Invoke(data);
        }
    }

    /// <summary>
    /// Base class for all Multiplayer Center editor analytics events.
    /// </summary>
    internal abstract class AnalyticsEvent<TEvent, TData> : IAnalytic
        where TData : IAnalytic.IData
        where TEvent : AnalyticsEvent<TEvent, TData>, new()
    {
        TData m_Data;

        public static void Send(TData data)
        {
            var analytic = new TEvent { m_Data = data };
            EditorAnalytics.SendAnalytic(analytic);
            AnalyticsEvent.InvokeAnalyticSent(analytic);
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return true;
        }
    }
}
