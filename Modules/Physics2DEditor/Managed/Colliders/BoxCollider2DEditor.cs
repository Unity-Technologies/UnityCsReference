// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(BoxCollider2D))]
    [CanEditMultipleObjects]
    class BoxCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_Size;
        SerializedProperty m_EdgeRadius;
        readonly AnimBool m_ShowCompositeRedundants = new AnimBool();

        public override void OnEnable()
        {
            base.OnEnable();

            m_Size = serializedObject.FindProperty("m_Size");
            m_EdgeRadius = serializedObject.FindProperty("m_EdgeRadius");
            m_AutoTiling = serializedObject.FindProperty("m_AutoTiling");
            m_ShowCompositeRedundants.value = !isUsedByComposite;
            m_ShowCompositeRedundants.valueChanged.AddListener(Repaint);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowCompositeRedundants.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool disableEditCollider = !CanEditCollider();

            if (disableEditCollider)
            {
                EditorGUILayout.HelpBox(Styles.s_ColliderEditDisableHelp.text, MessageType.Info);

                if (ToolManager.activeToolType == typeof(BoxCollider2DTool))
                    ToolManager.RestorePreviousTool();
            }
            else
                EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);

            GUILayout.Space(5);
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Size);

            m_ShowCompositeRedundants.target = !isUsedByComposite;
            if (EditorGUILayout.BeginFadeGroup(m_ShowCompositeRedundants.faded))
                EditorGUILayout.PropertyField(m_EdgeRadius);
            EditorGUILayout.EndFadeGroup();

            FinalizeInspectorGUI();
        }
    }
}
