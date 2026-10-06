// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal class SortingLayerEditorUtility
    {
        private static class Styles
        {
            [NoAutoStaticsCleanup] // lazily built from EditorStyles.popup (a GUIStyle); rebuilt on demand and safe to persist
            private static GUIStyle m_BoldPopupStyle;

            public static readonly GUIContent m_SortingLayerStyle = L10n.TextContent("Sorting Layer", "Name of the Renderer's sorting layer", null, null);
            public static readonly GUIContent m_SortingOrderStyle = L10n.TextContent("Order in Layer", "Renderer's order within a sorting layer", null, null);

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

        public static void RenderSortingLayerFields(SerializedProperty sortingLayer)
        {
            var hasPrefabOverride = HasPrefabOverride(sortingLayer);
            EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, sortingLayer, hasPrefabOverride ? Styles.boldPopupStyle : EditorStyles.popup, hasPrefabOverride ? EditorStyles.boldLabel : EditorStyles.label);
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
