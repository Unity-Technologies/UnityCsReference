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
        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;

        public virtual void OnEnable()
        {
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
