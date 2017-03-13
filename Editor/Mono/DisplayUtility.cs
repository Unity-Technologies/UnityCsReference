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
        private static GUIContent[] s_GenericDisplayNames =
        {
            EditorGUIUtility.TextContent("Display 1"), EditorGUIUtility.TextContent("Display 2"),
            EditorGUIUtility.TextContent("Display 3"), EditorGUIUtility.TextContent("Display 4"),
            EditorGUIUtility.TextContent("Display 5"), EditorGUIUtility.TextContent("Display 6"),
            EditorGUIUtility.TextContent("Display 7"), EditorGUIUtility.TextContent("Display 8")
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
