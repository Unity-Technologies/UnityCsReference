// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(UnityEngine.Rendering.SortingGroup))]
    [CanEditMultipleObjects]
    internal class SortingGroupEditor : Editor
    {
        private static class Styles
        {
            public static GUIContent m_SortAtRootStyle = EditorGUIUtility.TrTextContent("Sort At Root"
                , "Ignores all parent Sorting Groups and sorts at the root level against other Sorting Groups and Renderers");
        }


        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;
        private SerializedProperty m_SortAtRoot;

        public virtual void OnEnable()
        {
            alwaysAllowExpansion = true;
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_SortAtRoot = serializedObject.FindProperty("m_SortAtRoot");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);
            EditorGUILayout.PropertyField(m_SortAtRoot, Styles.m_SortAtRootStyle);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
