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
            private static GUIStyle m_BoldPopupStyle;

            public static GUIContent m_SortingLayerStyle = EditorGUIUtility.TrTextContent("Sorting Layer", "Name of the Renderer's sorting layer");
            public static GUIContent m_SortingOrderStyle = EditorGUIUtility.TrTextContent("Order in Layer", "Renderer's order within a sorting layer");

            public static GUIStyle boldPopupStyle
            {
                get
                {
                    if (m_BoldPopupStyle == null)
                    {
                        m_BoldPopupStyle = new GUIStyle(EditorStyles.popup);
                        m_BoldPopupStyle.fontStyle = FontStyle.Bold;
                    }
                    return m_BoldPopupStyle;
                }
            }
        }

        internal static bool HasPrefabOverride(SerializedProperty property)
        {
            return property != null && property.serializedObject.targetObjectsCount == 1 && property.isInstantiatedPrefab && property.prefabOverride;
        }

        public static void RenderSortingLayerFields(SerializedProperty sortingOrder, SerializedProperty sortingLayer)
        {
            var hasPrefabOverride = HasPrefabOverride(sortingLayer);
            EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, sortingLayer, hasPrefabOverride ? Styles.boldPopupStyle : EditorStyles.popup, hasPrefabOverride ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.PropertyField(sortingOrder, Styles.m_SortingOrderStyle);
        }

        public static void RenderSortingLayerFields(Rect r, SerializedProperty sortingOrder, SerializedProperty sortingLayer)
        {
            var hasPrefabOverride = HasPrefabOverride(sortingLayer);
            EditorGUI.SortingLayerField(r, Styles.m_SortingLayerStyle, sortingLayer, hasPrefabOverride ? Styles.boldPopupStyle : EditorStyles.popup, hasPrefabOverride ? EditorStyles.boldLabel : EditorStyles.label);
            r.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(r, sortingOrder, Styles.m_SortingOrderStyle);
        }
    }
}
