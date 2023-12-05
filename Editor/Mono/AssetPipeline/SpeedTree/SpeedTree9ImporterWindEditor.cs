// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using System;
using UnityEngine;

namespace UnityEditor.SpeedTree.Importer
{
    class SpeedTree9ImporterWindEditor : BaseSpeedTree9ImporterTabUI
    {
        private class Styles
        {
            public static GUIContent EnableWind = EditorGUIUtility.TrTextContent("Enable Wind");
            public static GUIContent StrengthResponse = EditorGUIUtility.TrTextContent("Strength Response", "The strength response of the wind.");
            public static GUIContent DirectionResponse = EditorGUIUtility.TrTextContent("Direction Response", "The direction response of the wind.");
            public static GUIContent WindRandomness = EditorGUIUtility.TrTextContent("Randomness", "Amount of world position based noise applied to each tree.");
        }

        private SerializedProperty m_EnableWind;
        private SerializedProperty m_StrengthResponse;
        private SerializedProperty m_DirectionResponse;
        private SerializedProperty m_WindRandomness;

        private SpeedTree9Importer m_StEditor;

        public SpeedTree9ImporterWindEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        { }

        internal override void OnEnable()
        {
            m_StEditor = target as SpeedTree9Importer;

            WindSettings windSettings = m_StEditor.m_WindSettings;
            string windSettingsStr = nameof(m_StEditor.m_WindSettings);

            m_EnableWind = FindPropertyFromName(windSettingsStr, nameof(windSettings.enableWind));
            m_StrengthResponse = FindPropertyFromName(windSettingsStr, nameof(windSettings.strenghResponse));
            m_DirectionResponse = FindPropertyFromName(windSettingsStr, nameof(windSettings.directionResponse));
            m_WindRandomness = FindPropertyFromName(windSettingsStr, nameof(windSettings.randomness));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_EnableWind, Styles.EnableWind);

            using (new EditorGUI.DisabledScope(!m_EnableWind.boolValue))
            {
                EditorGUILayout.PropertyField(m_StrengthResponse, Styles.StrengthResponse);
                EditorGUILayout.PropertyField(m_DirectionResponse, Styles.DirectionResponse);
                EditorGUILayout.PropertyField(m_WindRandomness, Styles.WindRandomness);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty FindPropertyFromName(string parentProperty, string childProperty)
        {
            const string dotStr = ".";

            string finalName = String.Concat(parentProperty, dotStr, childProperty);

            return serializedObject.FindProperty(finalName);
        }
    }
}
