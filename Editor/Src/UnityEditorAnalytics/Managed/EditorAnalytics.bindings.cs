// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/UnityEditorAnalytics/UnityEditorAnalytics.h")]
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

        public extern static bool enabled
        {
            get;
        }

        extern private static bool SendEvent(string eventName, object parameters);
    }

    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/UnityEditorAnalytics/UnityEditorAnalytics.h")]
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
