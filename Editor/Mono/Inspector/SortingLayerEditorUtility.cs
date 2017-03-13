// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class SortingLayerEditorUtility
    {
        private static class Styles
        {
            public static GUIContent m_SortingLayerStyle = EditorGUIUtility.TextContent("Sorting Layer|Name of the Renderer's sorting layer");
            public static GUIContent m_SortingOrderStyle = EditorGUIUtility.TextContent("Order in Layer|Renderer's order within a sorting layer");
        }

        public static void RenderSortingLayerFields(SerializedProperty sortingOrder, SerializedProperty sortingLayer)
        {
            EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, sortingLayer, EditorStyles.popup, EditorStyles.label);
            EditorGUILayout.PropertyField(sortingOrder, Styles.m_SortingOrderStyle);
        }

        public static void RenderSortingLayerFields(Rect r, SerializedProperty sortingOrder, SerializedProperty sortingLayer)
        {
            EditorGUI.SortingLayerField(r, Styles.m_SortingLayerStyle, sortingLayer, EditorStyles.popup, EditorStyles.label);
            r.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(r, sortingOrder, Styles.m_SortingOrderStyle);
        }
    }
}
