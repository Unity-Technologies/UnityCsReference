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
            public static readonly GUIContent DelayTooltip = L10n.TextContent("Delay", "Echo delay in ms", null, null);
            public static readonly GUIContent DecayRatioTooltip = L10n.TextContent("Decay Ratio", "Echo decay per delay", null, null);
            public static readonly GUIContent DryMixTooltip = L10n.TextContent("Dry Mix", "Volume of original signal to pass to output", null, null);
            public static readonly GUIContent WetMixTooltip = L10n.TextContent("Wet Mix", "Volume of echo signal to pass to output", null, null);
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
