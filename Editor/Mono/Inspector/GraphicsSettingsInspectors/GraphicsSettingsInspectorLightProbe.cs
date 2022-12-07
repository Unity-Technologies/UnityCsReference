// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorLightProbe : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorLightProbe, UxmlTraits> { }

        internal class Styles
        {
            public static readonly GUIContent lightProbeOutsideHullStrategy = EditorGUIUtility.TrTextContent("Renderer Light Probe Selection",
                "Finding the Light Probes closest to a Renderer positioned outside of the tetrahedral Light Probe hull can be very expensive in terms of CPU cycles. Use this option to configure if Unity should spend time searching the hull to find the closest probe, or if it should use the global Ambient Probe instead.");

            public static readonly int[] lightProbeOutsideHullStrategyValues =
            {
                (int)LightProbeOutsideHullStrategy.kLightProbeSearchTetrahedralHull,
                (int)LightProbeOutsideHullStrategy.kLightProbeUseAmbientProbe
            };

            public static readonly GUIContent[] lightProbeOutsideHullStrategyStrings =
            {
                EditorGUIUtility.TrTextContent("Find closest Light Probe"),
                EditorGUIUtility.TrTextContent("Use Ambient Probe"),
            };
        }

        SerializedProperty m_LightProbeOutsideHullStrategy;

        protected override void Initialize()
        {
            m_LightProbeOutsideHullStrategy = m_SerializedObject.FindProperty("m_LightProbeOutsideHullStrategy");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.IntPopup(m_LightProbeOutsideHullStrategy, Styles.lightProbeOutsideHullStrategyStrings, Styles.lightProbeOutsideHullStrategyValues, Styles.lightProbeOutsideHullStrategy);
            if (changeScope.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
