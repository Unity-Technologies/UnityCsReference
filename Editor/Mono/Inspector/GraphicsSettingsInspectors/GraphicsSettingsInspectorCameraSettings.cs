// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorCameraSettings : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorCameraSettings, UxmlTraits> { }

        internal class Styles
        {
            public static readonly GUIContent cameraSettings = EditorGUIUtility.TrTextContent("Camera Settings");
        }

        public override bool BuiltinOnly => true;

        SerializedProperty m_TransparencySortMode;
        SerializedProperty m_TransparencySortAxis;

        protected override void Initialize()
        {
            m_TransparencySortMode = m_SerializedObject.FindProperty("m_TransparencySortMode");
            m_TransparencySortAxis = m_SerializedObject.FindProperty("m_TransparencySortAxis");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            GUILayout.Label(Styles.cameraSettings, EditorStyles.boldLabel);

            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_TransparencySortMode);
            if ((TransparencySortMode)m_TransparencySortMode.intValue == TransparencySortMode.CustomAxis)
                EditorGUILayout.PropertyField(m_TransparencySortAxis);
            if (changeScope.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
