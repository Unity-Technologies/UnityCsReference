// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental;
using System;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class CacheServerWindow : PopupWindowContent
    {
        private readonly GUIContent m_StatusMessageDisabled;
        private readonly GUIContent m_StatusMessageConnected;
        private readonly GUIContent m_StatusMessageError;
        private readonly GUIContent m_OpenProjectSettings;

        private readonly GUIContent m_RefreshIcon;

        private readonly GUIStyle m_WindowStyle;

        public CacheServerWindow()
        {
            m_StatusMessageDisabled = EditorGUIUtility.TrTextContent("No cache server connected");
            m_StatusMessageConnected = EditorGUIUtility.TrTextContent("Connected");
            m_StatusMessageError = EditorGUIUtility.TrTextContent("Attempting to reconnect");
            m_OpenProjectSettings = EditorGUIUtility.TrTextContent("Open Project Settings...");

            m_RefreshIcon = EditorGUIUtility.TrIconContent("Refresh", "Refresh connection");

            m_WindowStyle = new GUIStyle { padding = new RectOffset(6, 6, 6, 6) };
        }

        public override void OnGUI(Rect rect)
        {
            var exit = false;

            GUILayout.BeginArea(rect, m_WindowStyle);
            if (AssetDatabaseExperimental.IsCacheServerEnabled())
            {
                var iconPosition = new Rect();
                iconPosition.x = rect.width - m_RefreshIcon.image.width - m_WindowStyle.padding.right;
                iconPosition.y = m_WindowStyle.padding.top;
                iconPosition.width = m_RefreshIcon.image.width;
                iconPosition.height = m_RefreshIcon.image.height;
                GUIStyle helpIconStyle = EditorStyles.iconButton;
                if (GUI.Button(iconPosition, m_RefreshIcon, helpIconStyle))
                {
                    AssetDatabaseExperimental.RefreshSettings();
                }

                GUILayout.BeginHorizontal();
                var style = new GUIStyle();
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = EditorStyles.boldLabel.normal.textColor;
                if (!AssetDatabaseExperimental.IsConnectedToCacheServer())
                {
                    style.normal.textColor = new Color(0.97f, 0.32f, 0.31f);
                }
                EditorGUILayout.LabelField(AssetDatabaseExperimental.GetCacheServerAddress(), style);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            var statusTextStyle = new GUIStyle()
            {
                normal = { textColor = Color.grey }
            };
            EditorGUILayout.LabelField(ConnectionStatusText(), statusTextStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(m_OpenProjectSettings, "ControlLabel"))
            {
                OpenProjectSettings();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            exit |= Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
            if (exit)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private GUIContent ConnectionStatusText()
        {
            GUIContent status = m_StatusMessageConnected;
            if (!AssetDatabaseExperimental.IsCacheServerEnabled())
            {
                status = m_StatusMessageDisabled;
            }
            else if (!AssetDatabaseExperimental.IsConnectedToCacheServer())
            {
                status = m_StatusMessageError;
            }
            return status;
        }

        private void OpenProjectSettings()
        {
            var settings = SettingsWindow.Show(SettingsScope.Project, "Project/Editor");
            if (settings == null)
            {
                Debug.LogError("Could not find Preferences for 'Project/Editor'");
            }
        }

        private void OpenPreferences()
        {
            var settings = SettingsWindow.Show(SettingsScope.User, "Preferences/Cache Server (global)");
            if (settings == null)
            {
                Debug.LogError("Could not find Preferences for 'Preferences/Cache Server (global)'");
            }
        }

        public override Vector2 GetWindowSize()
        {
            int lines = AssetDatabaseExperimental.IsCacheServerEnabled() ? 3 : 2;
            int heightOfLines = (int)Math.Ceiling(EditorGUI.kSingleLineHeight * lines);
            int heightOfWindowPadding = m_WindowStyle.padding.top + m_WindowStyle.padding.bottom;
            return new Vector2(250, heightOfLines + heightOfWindowPadding);
        }
    }
}
