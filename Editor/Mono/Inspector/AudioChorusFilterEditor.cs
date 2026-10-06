// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioChorusFilter))]
    class AudioChorusFilterEditor : Editor
    {
        private SerializedProperty m_DryMix;
        private SerializedProperty m_WetMix1;
        private SerializedProperty m_WetMix2;
        private SerializedProperty m_WetMix3;
        private SerializedProperty m_Delay;
        private SerializedProperty m_Rate;
        private SerializedProperty m_Depth;

        private static class Styles
        {
            public static readonly GUIContent DryMixTooltip = L10n.TextContent("Dry Mix", "Volume of original signal to pass to output", null, null);
            public static readonly GUIContent WetMix1Tooltip = L10n.TextContent("Wet Mix 1", "Volume of 1st chorus tap", null, null);
            public static readonly GUIContent WetMix2Tooltip = L10n.TextContent("Wet Mix 2", "Volume of 2nd chorus tap", null, null);
            public static readonly GUIContent WetMix3Tooltip = L10n.TextContent("Wet Mix 3", "Volume of 3rd chorus tap", null, null);
            public static readonly GUIContent DelayTooltip = L10n.TextContent("Delay", "Chorus delay in ms", null, null);
            public static readonly GUIContent RateTooltip = L10n.TextContent("Rate", "Chorus modulation rate in hz", null, null);
            public static readonly GUIContent DepthTooltip = L10n.TextContent("Depth", "Chorus modulation depth", null, null);
        }

        private void OnEnable()
        {
            m_DryMix = serializedObject.FindProperty("m_DryMix");
            m_WetMix1 = serializedObject.FindProperty("m_WetMix1");
            m_WetMix2 = serializedObject.FindProperty("m_WetMix2");
            m_WetMix3 = serializedObject.FindProperty("m_WetMix3");
            m_Delay = serializedObject.FindProperty("m_Delay");
            m_Rate = serializedObject.FindProperty("m_Rate");
            m_Depth = serializedObject.FindProperty("m_Depth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DryMix, Styles.DryMixTooltip);
            EditorGUILayout.PropertyField(m_WetMix1, Styles.WetMix1Tooltip);
            EditorGUILayout.PropertyField(m_WetMix2, Styles.WetMix2Tooltip);
            EditorGUILayout.PropertyField(m_WetMix3, Styles.WetMix3Tooltip);
            EditorGUILayout.PropertyField(m_Delay, Styles.DelayTooltip);
            EditorGUILayout.PropertyField(m_Rate, Styles.RateTooltip);
            EditorGUILayout.PropertyField(m_Depth, Styles.DepthTooltip);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
