// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using uei = UnityEngine.Internal;

namespace UnityEngine.Analytics
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityAnalytics/Public/UnityAnalytics.h")]
    [NativeHeader("Modules/UnityAnalytics/ContinuousEvent/Manager.h")]
    [uei.ExcludeFromDocs]
    public class ContinuousEvent
    {
        public static AnalyticsResult RegisterCollector<T>(string metricName, System.Func<T> del) where T : struct, IComparable<T>, IEquatable<T>
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Cannot set metric name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return InternalRegisterCollector(typeof(T).ToString(), metricName, del);
        }

        public static AnalyticsResult SetEventHistogramThresholds<T>(string eventName, int count, T[] data, int ver = 1, string prefix = "") where T : struct, IComparable<T>, IEquatable<T>
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return InternalSetEventHistogramThresholds(typeof(T).ToString(), eventName, count, data, ver, prefix);
        }

        public static AnalyticsResult SetCustomEventHistogramThresholds<T>(string eventName, int count, T[] data) where T : struct, IComparable<T>, IEquatable<T>
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return InternalSetCustomEventHistogramThresholds(typeof(T).ToString(), eventName, count, data);
        }

        public static AnalyticsResult ConfigureCustomEvent(string customEventName, string metricName, float interval, float period, bool enabled = true)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return InternalConfigureCustomEvent(customEventName, metricName, interval, period, enabled);
        }

        public static AnalyticsResult ConfigureEvent(string eventName, string metricName, float interval, float period, bool enabled = true, int ver = 1, string prefix = "")
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return InternalConfigureEvent(eventName, metricName, interval, period, enabled, ver, prefix);
        }

        [StaticAccessor("::GetUnityAnalytics().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static AnalyticsResult InternalRegisterCollector(string type, string metricName, object collector);

        [StaticAccessor("::GetUnityAnalytics().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static AnalyticsResult InternalSetEventHistogramThresholds(string type, string eventName, int count, object data, int ver, string prefix);

        [StaticAccessor("::GetUnityAnalytics().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static AnalyticsResult InternalSetCustomEventHistogramThresholds(string type, string eventName, int count, object data);

        [StaticAccessor("::GetUnityAnalytics().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static AnalyticsResult InternalConfigureCustomEvent(string customEventName, string metricName, float interval, float period, bool enabled);

        [StaticAccessor("::GetUnityAnalytics().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static AnalyticsResult InternalConfigureEvent(string eventName, string metricName, float interval, float period, bool enabled, int ver, string prefix);

        internal static bool IsInitialized()
        {
            return Analytics.IsInitialized();
        }
    }
}
