// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioDistortionFilter))]
    class AudioDistortionFilterEditor : Editor
    {
        private SerializedProperty m_DistortionLevel;

        private static class Styles
        {
            public static readonly GUIContent DistortionLevelTooltip = EditorGUIUtility.TrTextContent("Distortion Level", "Distortion value");
        }

        private void OnEnable()
        {
            m_DistortionLevel = serializedObject.FindProperty("m_DistortionLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_DistortionLevel, Styles.DistortionLevelTooltip);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
