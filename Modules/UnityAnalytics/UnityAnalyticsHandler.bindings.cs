// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEngine.Analytics
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityAnalytics/UnityAnalytics.h")]
    [NativeHeader("Modules/UnityConnect/UnityConnectClient.h")]
    [NativeHeader("Modules/UnityAnalytics/Events/UserCustomEvent.h")]
    internal class UnityAnalyticsHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        public UnityAnalyticsHandler()
        {
            m_Ptr = Internal_Create(this);
        }

        ~UnityAnalyticsHandler()
        {
            Destroy();
        }

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public bool IsInitialized() { return (m_Ptr != IntPtr.Zero); }

        internal static extern IntPtr Internal_Create(UnityAnalyticsHandler u);
        [ThreadSafe]
        internal static extern void Internal_Destroy(IntPtr ptr);

        [StaticAccessor("GetUnityConnectClient()", StaticAccessorType.Dot)]
        public extern static bool limitUserTracking { get; set; }

        [StaticAccessor("GetUnityConnectClient()", StaticAccessorType.Dot)]
        public extern static bool deviceStatsEnabled { get; set; }

        public extern bool enabled { get; set; }

        public extern bool FlushEvents();

        public extern AnalyticsResult SetUserId(string userId);

        public extern AnalyticsResult SetUserGender(Gender gender);

        public extern AnalyticsResult SetUserBirthYear(int birthYear);

        public extern AnalyticsResult Transaction(string productId, double amount, string currency, string receiptPurchaseData, string signature, bool usingIAPService);

        public extern AnalyticsResult SendCustomEventName(string customEventName);

        public extern AnalyticsResult SendCustomEvent(CustomEventData eventData);

        public extern AnalyticsResult RegisterEvent(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, string assemblyInfo);

        public extern AnalyticsResult SendEvent(string eventName, object parameters, int ver, string prefix);
    }
}
