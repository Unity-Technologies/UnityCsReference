// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
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

        internal static bool SendEventRefreshAccess(object parameters)
        {
            return EditorAnalytics.SendEvent("refreshAccess", parameters);
        }

        internal static bool SendEventNewLink(object parameters)
        {
            return EditorAnalytics.SendEvent("newLink", parameters);
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

        internal static bool SendEventCloseServiceWindow(object parameters)
        {
            return EditorAnalytics.SendEvent("closeService", parameters);
        }

        const string k_EventName = "editorgameserviceeditor";
        internal static bool RegisterEventEditorGameService()
        {
            const int maxEventsPerHour = 100;
            const int maxItems = 10;
            const string vendorKey = "unity.services.core.editor";
            const int version = 1;
            var result = EditorAnalytics.RegisterEventWithLimit(k_EventName, maxEventsPerHour, maxItems, vendorKey, version);
            return result == AnalyticsResult.Ok;
        }

        internal static bool SendEventEditorGameService(object parameters)
        {
            return EditorAnalytics.SendEvent(k_EventName, parameters);
        }

        internal static bool SendImportServicePackageEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("importServicePackage", parameters);
        }

        internal static bool SendOpenPackManFromServiceSettings(object parameters)
        {
            return EditorAnalytics.SendEvent("openPackageManager", parameters);
        }

        internal static bool SendOpenDashboardForService(object parameters)
        {
            return EditorAnalytics.SendEvent("openDashboard", parameters);
        }

        internal static bool SendValidatePublicKeyEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("validatePublicKey", parameters);
        }

        internal static bool SendLaunchCloudBuildEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("launchCloudBuild", parameters);
        }

        internal static bool SendClearAnalyticsDataEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("clearAnalyticsData", parameters);
        }

        internal static bool SendProjectServiceBindingEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("projectServiceBinding", parameters);
        }

        internal static bool SendCoppaComplianceEvent(object parameters)
        {
            return EditorAnalytics.SendEvent("coppaSetting", parameters);
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
            return EditorAnalytics.SendEvent("buildPackageList", parameters, SendEventOptions.kAppendBuildGuid | SendEventOptions.kAppendBuildTarget);
        }

        internal static bool SendEventBuildTargetPermissions(object parameters)
        {
            return EditorAnalytics.SendEventWithVersion("buildTargetPermissions", parameters, 2, SendEventOptions.kAppendBuildGuid | SendEventOptions.kAppendBuildTarget);
        }

        internal static bool SendCollabUserAction(object parameters)
        {
            return EditorAnalytics.SendEvent("collabUserAction", parameters);
        }

        internal static bool SendCollabOperation(object parameters)
        {
            return EditorAnalytics.SendEvent("collabOperation", parameters);
        }

        internal static bool SendAssetPostprocessorsUsage(object parameters)
        {
            return SendEventWithVersion("assetPostProcessorsUsage", parameters, 2);
        }

        internal extern static bool SendAssetDownloadEvent(object parameters);

        public extern static bool enabled
        {
            get;
            set;
        }

        internal static AnalyticsResult TryRegisterAnalytic(Analytic analytic, Assembly assembly)
        {
            if (enabled == false) return AnalyticsResult.AnalyticsDisabled;

            AnalyticsResult result = RegisterEventWithLimit(analytic.info.eventName, analytic.info.maxEventsPerHour, analytic.info.maxNumberOfElements, analytic.info.vendorKey, analytic.info.version, "", assembly);
            if (result != AnalyticsResult.Ok)
            {
                Debug.LogWarning($"Unable to register event {analytic.info.eventName} with vendor key: {analytic.info.vendorKey} and error code {result.ToString()}. Editor Analyitics will not be gathered.");
            }

            return result;
        }

        internal static AnalyticsResult TrySendAnalytic(Analytic analytic)
        {
            if (enabled == false) return AnalyticsResult.AnalyticsDisabled;

            AnalyticsResult result = AnalyticsResult.Ok;
            try
            {
                if (!analytic.instance.TryGatherData(out var data, out Exception error))
                {
                    Debug.LogWarning(error);
                    return AnalyticsResult.InvalidData;
                }

                if (data is IEnumerable enumerable)
                {
                    foreach (var subData in enumerable)
                    {
                        result = EditorAnalytics.SendEventWithLimit(analytic.info.eventName, subData, analytic.info.version, "");
                    }
                }
                else
                {
                    result = EditorAnalytics.SendEventWithLimit(analytic.info.eventName, data, analytic.info.version, "");
                }
            }
            catch (Exception ex) // We do not want to break the build if any analytic crashes for any reason, Simply we won't send the data to the server
            {
                Debug.LogWarning($"Exception {ex} found while sending analytic of {analytic.info.eventName}.");
                return AnalyticsResult.InvalidData;
            }

            DebuggerEventListHandler.AddCSharpAnalytic(analytic);

            return result;
        }

        public static AnalyticsResult SendAnalytic(IAnalytic analytic)
        {
            var analyticType = analytic.GetType();

            if (Attribute.IsDefined(analyticType, typeof(AnalyticInfoAttribute)))
            {
                AnalyticInfoAttribute info = (AnalyticInfoAttribute)analyticType.GetCustomAttributes(typeof(AnalyticInfoAttribute), true)[0];

                System.Reflection.Assembly assembly = Assembly.GetCallingAssembly();
                return SendAnalytic(new Analytic(analytic, info), assembly);
            }
            else
            {
                Debug.LogError($"{analyticType} must contain an attribute of {nameof(AnalyticInfoAttribute)}");
                return AnalyticsResult.InvalidData;
            }
        }

        internal static AnalyticsResult SendAnalytic(Analytic analytic, Assembly assembly)
        {
            AnalyticsResult result = AnalyticsResult.Ok;
            {
                result = TryRegisterAnalytic(analytic, assembly);

                if (result != AnalyticsResult.Ok)
                    return result;

                result = TrySendAnalytic(analytic);
            }
            return result;
        }

        extern private static bool SendEvent(string eventName, object parameters, SendEventOptions sendEventOptions = SendEventOptions.kAppendNone);

        extern internal static bool SendEventWithVersion(string eventName, object parameters, int ver, SendEventOptions sendEventOptions = SendEventOptions.kAppendNone);

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

        public static AnalyticsResult SetEventWithLimitEndPoint(string eventName, string endPoint, int ver)
        {
            return SetEventWithLimitEndPoint(eventName, endPoint, ver, "");
        }

        public static AnalyticsResult SetEventWithLimitPriority(string eventName, AnalyticsEventPriority eventPriority, int ver)
        {
            return SetEventWithLimitPriority(eventName, eventPriority, ver, "");
        }

        private static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, Assembly assembly)
        {
            string assemblyInfo = null;
            string packageName = null;
            string packageVersion = null;
            if (assembly != null)
            {
                assemblyInfo = assembly.FullName;
                PackageManager.PackageInfo packageInfo = PackageManager.PackageInfo.FindForAssembly(assembly);
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

        private extern static AnalyticsResult SetEventWithLimitEndPoint(string eventName, string endPoint, int ver, string prefix);

        private extern static AnalyticsResult SetEventWithLimitPriority(string eventName, AnalyticsEventPriority eventPriority, int ver, string prefix);
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
