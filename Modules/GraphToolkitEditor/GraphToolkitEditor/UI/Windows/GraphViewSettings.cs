// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal static class GraphViewSettings
    {
        // Pan settings set by UX as part of GTF-751 - not meant to be set by users
        internal static readonly bool k_PanUsePercentage = true;
        internal const EasingFunction k_PanEasingFunction = EasingFunction.InOutQuad;
        internal const float k_PanAreaSize = 5f;
        internal const float k_PanMinSpeedFactor = 0.2f;
        internal const float k_PanMaxSpeedFactor = 1.75f;

        [UnityRestricted]
        internal enum EasingFunction
        {
            Linear,
            InOutCubic,
            InOutQuad
        };

        internal static class UserSettings
        {
            const string k_SettingsUniqueKey = "UnityEditor.Graph/";

            const string k_EnableSnapToPortKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToPort";
            const string k_EnableSnapToBordersKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToBorders";
            const string k_EnableSnapToGridKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToGrid";
            const string k_EnableSnapToSpacingKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToSpacing";

            [NoAutoStaticsCleanup] // dictionary values are always refreshed from EditorPrefs before each read; safe to persist
            static Dictionary<Type, bool> s_SnappingStrategiesStates = new Dictionary<Type, bool>()
            {
                {typeof(SnapToBordersStrategy), EnableSnapToBorders},
                {typeof(SnapToPortStrategy), EnableSnapToPort},
                {typeof(SnapToGridStrategy), EnableSnapToGrid},
                {typeof(SnapToSpacingStrategy), EnableSnapToSpacing}
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
                s_SnappingStrategiesStates[typeof(SnapToBordersStrategy)] = EnableSnapToBorders;
                s_SnappingStrategiesStates[typeof(SnapToPortStrategy)] = EnableSnapToPort;
                s_SnappingStrategiesStates[typeof(SnapToGridStrategy)] = EnableSnapToGrid;
                s_SnappingStrategiesStates[typeof(SnapToSpacingStrategy)] = EnableSnapToSpacing;
            }
        }

        class Styles
        {
            public static readonly GUIContent kEnableSnapToPortLabel = L10n.TextContent("Connected Port Snapping", "If enabled, nodes align to connected ports.", null, null);
            public static readonly GUIContent kEnableSnapToBordersLabel = L10n.TextContent("Element Snapping", "If enabled, graph elements align with one another when you move them.", null, null);
            public static readonly GUIContent kEnableSnapToGridLabel = L10n.TextContent("Grid Snapping", "If enabled, graph elements align with the grid.", null, null);
            public static readonly GUIContent kEnableSnapToSpacingLabel = L10n.TextContent("Equal Spacing Snapping", "If enabled, graph elements align to keep equal spacing with their neighbors.", null, null);
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
            var snappingToBorders = EditorGUILayout.Toggle(Styles.kEnableSnapToBordersLabel, UserSettings.EnableSnapToBorders);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToBorders = snappingToBorders;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToPort = EditorGUILayout.Toggle(Styles.kEnableSnapToPortLabel, UserSettings.EnableSnapToPort);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToPort = snappingToPort;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToGrid = EditorGUILayout.Toggle(Styles.kEnableSnapToGridLabel, UserSettings.EnableSnapToGrid);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToGrid = snappingToGrid;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToSpacing = EditorGUILayout.Toggle(Styles.kEnableSnapToSpacingLabel, UserSettings.EnableSnapToSpacing);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToSpacing = snappingToSpacing;
            }
        }
    }
}
