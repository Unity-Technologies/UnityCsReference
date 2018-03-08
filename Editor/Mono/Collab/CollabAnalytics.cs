// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Collaboration
{
    internal static class CollabAnalytics
    {
        [Serializable]
        private struct CollabUserActionAnalyticsEvent
        {
            public string category;
            public string action;
        }

        public static void SendUserAction(string category, string action)
        {
            EditorAnalytics.SendCollabUserAction(new CollabUserActionAnalyticsEvent() { category = category, action = action });
        }

        public static string historyCategoryString = "History";
    };
}
