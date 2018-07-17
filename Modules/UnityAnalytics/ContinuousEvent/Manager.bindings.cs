// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;

namespace UnityEngine.Analytics
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityAnalytics/CoreStats/AnalyticsCoreStats.h")]
    [NativeHeader("Modules/UnityAnalytics/ContinuousEvent/Manager.h")]
    internal class ContinuousEvent
    {
        public static void RegisterCollector<T>(string collectorName, System.Func<T> del) where T : struct, IComparable<T>, IEquatable<T>
        {
            RegisterCollector_Internal(typeof(T).ToString(), collectorName, del);
        }

        public static void SetEventHistogramThresholds<T>(string eventName, int count, T[] data) where T : struct, IComparable<T>, IEquatable<T>
        {
            SetEventHistogramThresholds_Internal(typeof(T).ToString(), eventName, count, data);
        }

        [StaticAccessor("::GetAnalyticsCoreStats().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static void RegisterCollector_Internal(string type, string collectorName, object collector);

        [StaticAccessor("::GetAnalyticsCoreStats().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern private static void SetEventHistogramThresholds_Internal(string type, string eventName, int count, object data);

        [StaticAccessor("::GetAnalyticsCoreStats().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern public static void EnableEvent(string eventName, bool enabled);

        [StaticAccessor("::GetAnalyticsCoreStats().GetContinuousEventManager()", StaticAccessorType.Dot)]
        extern public static void ConfigureEvent(string eventName, string collectorName, float interval, float period, bool enabled, bool custom = false);
    }
}
