// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;

namespace UnityEditor
{
    [Serializable]
    [FullscreenSettingsFor(typeof(GameView))]
    internal class GameViewFullscreenSettings : IPlayModeViewFullscreenSettings
    {
        [SerializeField] private int m_displayNumber;
        [SerializeField] private bool m_showToolbar;
        [SerializeField] private bool m_vsyncEnabled;
        [SerializeField] private bool m_showStats;
        [SerializeField] private bool m_showGizmos;
        [SerializeField] private int m_selectedSizeIndex;

        public int DisplayNumber
        {
            get => m_displayNumber;
            set => m_displayNumber = value;
        }

        public bool ShowToolbar
        {
            get => m_showToolbar;
            set => m_showToolbar = value;
        }

        public bool VsyncEnabled
        {
            get => m_vsyncEnabled;
            set => m_vsyncEnabled = value;
        }

        public bool ShowStats
        {
            get => m_showStats;
            set => m_showStats = value;
        }

        public bool ShowGizmos
        {
            get => m_showGizmos;
            set => m_showGizmos = value;
        }

        public int SelectedSizeIndex
        {
            get => m_selectedSizeIndex;
            set => m_selectedSizeIndex = value;
        }

        private class Styles
        {
            public static readonly GUIContent vsyncContent = EditorGUIUtility.TrTextContent("VSync");
            public static readonly GUIContent gizmosContent = EditorGUIUtility.TrTextContent("Gizmos");
            public static readonly GUIContent toolbarContent = EditorGUIUtility.TrTextContent("Toolbar");
            public static readonly GUIContent statsContent = EditorGUIUtility.TrTextContent("Stats");
        }

        public void OnPreferenceGUI(BuildTarget target)
        {
            using (new GUILayout.HorizontalScope())
            {
                m_displayNumber = EditorGUILayout.Popup(m_displayNumber, EditorFullscreenController.GetDisplayNamesForBuildTarget(target), GUILayout.Width(80));
                GUILayout.Space(12);
                m_vsyncEnabled = EditorGUILayout.ToggleLeft(Styles.vsyncContent, m_vsyncEnabled, GUILayout.Width(60));
                m_showToolbar = EditorGUILayout.ToggleLeft(Styles.toolbarContent, m_showToolbar, GUILayout.Width(70));
                m_showGizmos = EditorGUILayout.ToggleLeft(Styles.gizmosContent, m_showGizmos, GUILayout.Width(60));
                m_showStats = EditorGUILayout.ToggleLeft(Styles.statsContent, m_showStats, GUILayout.Width(60));
            }
        }
    }
} // namespace
