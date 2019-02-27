// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(BillboardRenderer))]
    [CanEditMultipleObjects]
    internal class BillboardRendererInspector : RendererEditorBase
    {
        class Styles
        {
            public static readonly GUIContent billboard = EditorGUIUtility.TrTextContent("Billboard");
        }

        private SerializedProperty m_Billboard;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Billboard = serializedObject.FindProperty("m_Billboard");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Billboard, Styles.billboard);

            LightingSettingsGUI(false);
            OtherSettingsGUI(true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
