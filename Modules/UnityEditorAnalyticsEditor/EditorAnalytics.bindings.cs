// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Analytics;
using UnityEditor.PackageManager;

namespace UnityEditor
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalytics.h")]
    [NativeHeader("Modules/UnityAnalytics/ContinuousEvent/Manager.h")]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalyticsManager.h")]
    public static class EditorAnalytics
    {
        [Flags]
        internal enum SendEventOptions
        {
            kAppendNone = 0,
            kAppendBuildGuid = 1 << 0,
            kAppendBuildTarget = 1 << 1
        }

        internal static bool SendEventScriptableBuildPipelineInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("scriptableBuildPipeline", parameters);
        }

        internal static bool SendEventBuildFrameworkList(object parameters)
        {
            return EditorAnalytics.SendEvent("buildFrameworkList", parameters, SendEventOptions.kAppendBuildGuid | SendEventOptions.kAppendBuildTarget);
        }

        internal static bool SendEventServiceInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("serviceInfo", parameters);
        }

        internal static bool SendEventShowService(object parameters)
        {
            return EditorAnalytics.SendEvent("showService", parameters);
        }

        internal static bool SendEventTimelineInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("timelineInfo", parameters);
        }

        internal static bool SendEventBuildTargetDevice(object parameters)
        {
            return EditorAnalytics.SendEvent("buildTargetDevice", parameters, SendEventOptions.kAppendBuildGuid);
        }

        internal static bool SendEventSceneViewInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("sceneViewInfo", parameters);
        }

        internal static bool SendEventBuildPackageList(object parameters)
        {
            return EditorAnalytics.SendEvent("buildPackageList", parameters,
                SendEventOptions.kAppendBuildGuid | SendEventOptions.kAppendBuildTarget);
        }

        internal static bool SendEventBuildTargetPermissions(object parameters)
        {
            return EditorAnalytics.SendEvent("buildTargetPermissions", parameters,
                SendEventOptions.kAppendBuildGuid | SendEventOptions.kAppendBuildTarget);
        }

        internal static bool SendCollabUserAction(object parameters)
        {
            return EditorAnalytics.SendEvent("collabUserAction", parameters);
        }

        internal static bool SendCollabOperation(object parameters)
        {
            return EditorAnalytics.SendEvent("collabOperation", parameters);
        }

        internal extern static bool SendAssetDownloadEvent(object parameters);

        public extern static bool enabled
        {
            get;
        }

        extern private static bool SendEvent(string eventName, object parameters, SendEventOptions sendEventOptions = SendEventOptions.kAppendNone);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey)
        {
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, 1, "", Assembly.GetCallingAssembly());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver)
        {
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, ver, "", Assembly.GetCallingAssembly());
        }

        public static AnalyticsResult SendEventWithLimit(string eventName, object parameters)
        {
            return SendEventWithLimit(eventName, parameters, 1, "");
        }

        public static AnalyticsResult SendEventWithLimit(string eventName, object parameters, int ver)
        {
            return SendEventWithLimit(eventName, parameters, ver, "");
        }

        private static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, Assembly assembly)
        {
            string assemblyInfo = null;
            string packageName = null;
            string packageVersion = null;
            if (assembly != null)
            {
                assemblyInfo = assembly.FullName;
                UnityEditor.PackageManager.PackageInfo packageInfo = (UnityEditor.PackageManager.PackageInfo)Packages.GetForAssembly(assembly);
                if (packageInfo != null)
                {
                    packageName = packageInfo.name;
                    packageVersion = packageInfo.version;
                }
            }
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, ver, prefix, assemblyInfo, packageName, packageVersion, true);
        }

        private extern static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, string assemblyInfo, string packageName, string packageVersion, bool notifyServer);

        private extern static AnalyticsResult SendEventWithLimit(string eventName, object parameters, int ver, string prefix);

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

            [StaticAccessor("GetUnityEditorAnalyticsManager().GetUnityEditorAnalytics()->GetContinuousEventManager()", StaticAccessorType.Dot)]
            extern private static void RegisterCollector_Internal(string type, string collectorName, object collector);

            [StaticAccessor("GetUnityEditorAnalyticsManager().GetUnityEditorAnalytics()->GetContinuousEventManager()", StaticAccessorType.Dot)]
            extern private static void SetEventHistogramThresholds_Internal(string type, string eventName, int count, object data);

            [StaticAccessor("GetUnityEditorAnalyticsManager().GetUnityEditorAnalytics()->GetContinuousEventManager()", StaticAccessorType.Dot)]
            extern public static void EnableEvent(string eventName, bool enabled);

            [StaticAccessor("GetUnityEditorAnalyticsManager().GetUnityEditorAnalytics()->GetContinuousEventManager()", StaticAccessorType.Dot)]
            extern public static void ConfigureEvent(string eventName, string collectorName, float interval, float period, bool enabled, bool custom = false);
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalytics.h")]
    public static class EditorAnalyticsSessionInfo
    {
        public extern static long id
        {
            [NativeMethod("GetSessionId")]
            get;
        }

        public extern static long sessionCount
        {
            get;
        }

        public extern static long elapsedTime
        {
            [NativeMethod("GetSessionElapsedTime")]
            get;
        }

        public extern static long focusedElapsedTime
        {
            [NativeMethod("GetSessionFocusedElapsedTime")]
            get;
        }

        public extern static long playbackElapsedTime
        {
            [NativeMethod("GetSessionPlaybackElapsedTime")]
            get;
        }

        public extern static long activeElapsedTime
        {
            [NativeMethod("GetSessionUserActiveElapsedTime")]
            get;
        }

        public extern static string userId
        {
            get;
        }
    }
}
