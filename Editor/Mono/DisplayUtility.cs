// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    internal class DisplayUtility
    {
        static string s_DisplayStr = "Display {0}";
        private static GUIContent[] s_GenericDisplayNames =
        {
            EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 1)), EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 2)),
            EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 3)), EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 4)),
            EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 5)), EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 6)),
            EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 7)), EditorGUIUtility.TextContent(string.Format(s_DisplayStr, 8))
        };

        private static readonly int[] s_DisplayIndices = { 0, 1, 2, 3, 4, 5, 6, 7 };

        public static GUIContent[] GetGenericDisplayNames()
        {
            return s_GenericDisplayNames;
        }

        public static int[] GetDisplayIndices()
        {
            return s_DisplayIndices;
        }

        public static GUIContent[] GetDisplayNames()
        {
            GUIContent[] platformDisplayNames = Modules.ModuleManager.GetDisplayNames(EditorUserBuildSettings.activeBuildTarget.ToString());
            return platformDisplayNames != null ? platformDisplayNames : s_GenericDisplayNames;
        }
    }
}
