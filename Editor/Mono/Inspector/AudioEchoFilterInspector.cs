// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioEchoFilter))]
    class AudioEchoFilterEditor : Editor
    {
        private SerializedProperty m_Delay;
        private SerializedProperty m_DecayRatio;
        private SerializedProperty m_DryMix;
        private SerializedProperty m_WetMix;

        private static class Styles
        {
            public static readonly GUIContent DelayTooltip = EditorGUIUtility.TrTextContent("Delay", "Echo delay in ms");
            public static readonly GUIContent DecayRatioTooltip = EditorGUIUtility.TrTextContent("Decay Ratio", "Echo decay per delay");
            public static readonly GUIContent DryMixTooltip = EditorGUIUtility.TrTextContent("Dry Mix", "Volume of original signal to pass to output");
            public static readonly GUIContent WetMixTooltip = EditorGUIUtility.TrTextContent("Wet Mix", "Volume of echo signal to pass to output");
        }

        private void OnEnable()
        {
            m_Delay = serializedObject.FindProperty("m_Delay");
            m_DecayRatio = serializedObject.FindProperty("m_DecayRatio");
            m_DryMix = serializedObject.FindProperty("m_DryMix");
            m_WetMix = serializedObject.FindProperty("m_WetMix");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Delay, Styles.DelayTooltip);
            EditorGUILayout.PropertyField(m_DecayRatio, Styles.DecayRatioTooltip);
            EditorGUILayout.PropertyField(m_DryMix, Styles.DryMixTooltip);
            EditorGUILayout.PropertyField(m_WetMix, Styles.WetMixTooltip);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
