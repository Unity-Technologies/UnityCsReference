// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class GraphViewSettings
    {
        // Pan settings set by UX as part of GTF-751 - not meant to be set by users
        internal static readonly bool PanUsePercentage_Internal = true;
        internal const EasingFunction panEasingFunction_Internal = EasingFunction.InOutQuad;
        internal const float panAreaSize_Internal = 5f;
        internal const float panMinSpeedFactor_Internal = 0.2f;
        internal const float panMaxSpeedFactor_Internal = 1.75f;

        public enum EasingFunction
        {
            Linear,
            InOutCubic,
            InOutQuad
        };

        internal class UserSettings_Internal
        {
            const string k_SettingsUniqueKey = "UnityEditor.Graph/";

            const string k_EnableSnapToPortKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToPort";
            const string k_EnableSnapToBordersKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToBorders";
            const string k_EnableSnapToGridKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToGrid";
            const string k_EnableSnapToSpacingKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToSpacing";

            static Dictionary<Type, bool> s_SnappingStrategiesStates = new Dictionary<Type, bool>()
            {
                {typeof(SnapToBordersStrategy_Internal), EnableSnapToBorders},
                {typeof(SnapToPortStrategy_Internal), EnableSnapToPort},
                {typeof(SnapToGridStrategy_Internal), EnableSnapToGrid},
                {typeof(SnapToSpacingStrategy_Internal), EnableSnapToSpacing}
            };

            public static bool EnableSnapToPort
            {
                get => EditorPrefs.GetBool(k_EnableSnapToPortKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToPortKey, value);
            }

            public static bool EnableSnapToBorders
            {
                get => EditorPrefs.GetBool(k_EnableSnapToBordersKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToBordersKey, value);
            }

            public static bool EnableSnapToGrid
            {
                get => EditorPrefs.GetBool(k_EnableSnapToGridKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToGridKey, value);
            }

            public static bool EnableSnapToSpacing
            {
                get => EditorPrefs.GetBool(k_EnableSnapToSpacingKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToSpacingKey, value);
            }

            public static Dictionary<Type, bool> SnappingStrategiesStates
            {
                get
                {
                    UpdateSnappingStates();
                    return s_SnappingStrategiesStates;
                }
            }

            static void UpdateSnappingStates()
            {
                s_SnappingStrategiesStates[typeof(SnapToBordersStrategy_Internal)] = EnableSnapToBorders;
                s_SnappingStrategiesStates[typeof(SnapToPortStrategy_Internal)] = EnableSnapToPort;
                s_SnappingStrategiesStates[typeof(SnapToGridStrategy_Internal)] = EnableSnapToGrid;
                s_SnappingStrategiesStates[typeof(SnapToSpacingStrategy_Internal)] = EnableSnapToSpacing;
            }
        }

        class Styles
        {
            public static readonly GUIContent kEnableSnapToPortLabel = EditorGUIUtility.TrTextContent("Connected Port Snapping", "If enabled, nodes align to connected ports.");
            public static readonly GUIContent kEnableSnapToBordersLabel = EditorGUIUtility.TrTextContent("Element Snapping", "If enabled, graph elements align with one another when you move them.");
            public static readonly GUIContent kEnableSnapToGridLabel = EditorGUIUtility.TrTextContent("Grid Snapping", "If enabled, graph elements align with the grid.");
            public static readonly GUIContent kEnableSnapToSpacingLabel = EditorGUIUtility.TrTextContent("Equal Spacing Snapping", "If enabled, graph elements align to keep equal spacing with their neighbors.");
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Graph", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
            provider.guiHandler = _ => OnGUI();
            return provider;
        }

        static void OnGUI()
        {
            // For the moment, the different types of snapping can only be used separately
            EditorGUI.BeginChangeCheck();
            var snappingToBorders = EditorGUILayout.Toggle(Styles.kEnableSnapToBordersLabel, UserSettings_Internal.EnableSnapToBorders);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings_Internal.EnableSnapToBorders = snappingToBorders;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToPort = EditorGUILayout.Toggle(Styles.kEnableSnapToPortLabel, UserSettings_Internal.EnableSnapToPort);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings_Internal.EnableSnapToPort = snappingToPort;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToGrid = EditorGUILayout.Toggle(Styles.kEnableSnapToGridLabel, UserSettings_Internal.EnableSnapToGrid);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings_Internal.EnableSnapToGrid = snappingToGrid;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToSpacing = EditorGUILayout.Toggle(Styles.kEnableSnapToSpacingLabel, UserSettings_Internal.EnableSnapToSpacing);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings_Internal.EnableSnapToSpacing = snappingToSpacing;
            }
        }
    }
}
