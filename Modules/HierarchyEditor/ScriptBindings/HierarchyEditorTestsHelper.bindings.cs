// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace Unity.Hierarchy.Editor
{
    [NativeHeader("Modules/HierarchyEditor/HierarchyEditorTestsHelper.h")]
    internal static class HierarchyEditorTestsHelper
    {
        [NativeMethod(IsThreadSafe = true)]
        internal static extern HierarchyNodeType GetNodeType_HierarchyGameObjectHandler();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern HierarchyNodeType GetNodeType_HierarchySceneHandler();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern HierarchyNodeType GetNodeType_HierarchySubSceneAuthoringHandler();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void SearchBeginNative(HierarchyGameObjectHandler handler, HierarchySearchQueryDescriptor query);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool SearchMatchNative(HierarchyGameObjectHandler handler, HierarchyNode node);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void SearchEndNative(HierarchyGameObjectHandler handler);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool SearchMatchGameObjectNative(HierarchyGameObjectHandler handler, GameObject obj);

        internal static bool IsScenePickingEnable(Scene scene) => SceneVisibilityManager.instance.GetScenePickingState(scene) == SceneVisibilityManager.ScenePickingState.PickingEnabledAll;

        internal static string GetSearchableWindowQuery(SearchableEditorWindow window) => window.m_SearchFilter;

        internal static string[] GetNodeNames(Hierarchy hierarchy, HierarchyNode[] nodes)
        {
            if (nodes.Length == 0)
                return Array.Empty<string>();

            var names = new string[nodes.Length];
            for (var i = 0; i < names.Length; ++i)
            {
                if (nodes[i] == HierarchyNode.Null)
                    names[i] = "Null";
                else
                    names[i] = hierarchy.GetName(nodes[i]);
            }
            return names;
        }
    }
}
