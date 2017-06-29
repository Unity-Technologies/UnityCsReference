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
    internal class EditorAnalytics
    {
        public static bool SendEventServiceInfo(object parameters)
        {
            return EditorAnalytics.SendEvent("serviceInfo", parameters);
        }

        public static bool SendEventShowService(object parameters)
        {
            return EditorAnalytics.SendEvent("showService", parameters);
        }

        extern private static bool SendEvent(string eventName, object parameters);
    }
}
