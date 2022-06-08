// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CircleCollider2D))]
    [CanEditMultipleObjects]
    class CircleCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_Radius;

        public override void OnEnable()
        {
            base.OnEnable();
            m_Radius = serializedObject.FindProperty("m_Radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);

            GUILayout.Space(5);
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Radius);

            FinalizeInspectorGUI();
        }
    }
}
