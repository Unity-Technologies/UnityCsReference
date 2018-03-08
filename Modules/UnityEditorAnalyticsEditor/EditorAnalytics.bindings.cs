// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Analytics;

namespace UnityEditor
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/UnityEditorAnalytics.h")]
    public static class EditorAnalytics
    {
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
            return EditorAnalytics.SendEvent("buildTargetDevice", parameters);
        }

        internal static bool SendEventSceneViewInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("sceneViewInfo", parameters);
        }

        internal static bool SendCollabUserAction(object parameters)
        {
            return EditorAnalytics.SendEvent("collabUserAction", parameters);
        }

        public extern static bool enabled
        {
            get;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey)
        {
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, 1, "", Assembly.GetCallingAssembly().FullName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver)
        {
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, ver, "", Assembly.GetCallingAssembly().FullName);
        }

        public static AnalyticsResult SendEventWithLimit(string eventName, object parameters)
        {
            return SendEventWithLimit(eventName, parameters, 1, "");
        }

        public static AnalyticsResult SendEventWithLimit(string eventName, object parameters, int ver)
        {
            return SendEventWithLimit(eventName, parameters, ver, "");
        }

        private extern static AnalyticsResult RegisterEventWithLimit(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, string assemblyInfo);

        private extern static AnalyticsResult SendEventWithLimit(string eventName, object parameters, int ver, string prefix);

        extern private static bool SendEvent(string eventName, object parameters);
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
