// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Analytics
{

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityAnalytics/Public/UnityAnalytics.h")]
    [NativeHeader("Modules/UnityAnalytics/Public/Events/UserCustomEvent.h")]
    public static partial class Analytics
    {
        [ThreadSafe]
        private static extern bool IsInitialized();

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private extern static bool enabledInternal
        {
            [NativeMethod("GetEnabled")]
            get;
            [NativeMethod("SetEnabled")]
            set;
        }

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private extern static bool limitUserTrackingInternal
        {
            [NativeMethod("GetLimitUserTracking")]
            get;
            [NativeMethod("SetLimitUserTracking")]
            set;
        }

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private extern static bool deviceStatsEnabledInternal
        {
            [NativeMethod("GetDeviceStatsEnabled")]
            get;
            [NativeMethod("SetDeviceStatsEnabled")]
            set;
        }

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        [NativeMethod("FlushEvents")]
        private static extern bool FlushArchivedEvents();

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private static extern AnalyticsResult Transaction(string productId, double amount, string currency, string receiptPurchaseData, string signature, bool usingIAPService);

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private static extern AnalyticsResult SendCustomEventName(string customEventName);

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private static extern AnalyticsResult SendCustomEvent(CustomEventData eventData);

        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private static extern AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, string assemblyInfo);

        [ThreadSafe]
        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        private static extern AnalyticsResult SendEventWithLimit(string eventName, object parameters, int ver, string prefix);

        [ThreadSafe]
        [StaticAccessor("GetUnityAnalytics()", StaticAccessorType.Dot)]
        internal static extern bool QueueEvent(string eventName, object parameters, int ver, string prefix);
    }
}
