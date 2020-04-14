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
        private const string k_StatusText = "Status:";
        private const string k_EndpointText = "Endpoint:";
        private const string k_NamespaceText = "Namespace:";
        private const string k_StatusConnectedText = "Connected";
        private const string k_StatusDisconnectedText = "Disconnected";
        private const string k_StatusDisabledText = "Disabled";

        private readonly GUIContent m_WindowTitleContent;
        private readonly GUIContent m_WindowOverflowMenuProjectSettingsButtonContent;
        private readonly GUIContent m_WindowOverflowMenuPreferencesButtonContent;
        private readonly GUIContent m_CacheDisabledButtonContent;
        private readonly GUIContent m_CacheCanReconnectButtonContent;
        private readonly GUIContent m_CacheCannotConnectButtonContent;

        private readonly GUIStyle m_WindowStyle;
        private readonly GUIStyle m_CacheServerOverflowMenuButtonStyle;

        private const int k_WindowWidth = 320;
        private const int k_ConnectionButtonHeight = 24; // "Large Button" height
        private const int k_ConnectionButtonWidth = 190;
        private const int k_Header_footer_vertical_padding = 10;
        private static Vector2 s_IconSize = new Vector2(16, 16);

        public CacheServerWindow()
        {
            m_WindowTitleContent = EditorGUIUtility.TrTextContent("Cache Server");
            m_WindowOverflowMenuProjectSettingsButtonContent = EditorGUIUtility.TrTextContent("Project Settings");
            m_WindowOverflowMenuPreferencesButtonContent = EditorGUIUtility.TrTextContent("Preferences");
            m_CacheCanReconnectButtonContent = EditorGUIUtility.TrTextContent("Reconnect");
            m_CacheCannotConnectButtonContent = EditorGUIUtility.TrTextContentWithIcon("Check connection settings", MessageType.Warning);
            m_CacheDisabledButtonContent = EditorGUIUtility.TrTextContentWithIcon("Enable in settings", MessageType.Info);

            m_WindowStyle = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
            m_CacheServerOverflowMenuButtonStyle = "PaneOptions";
        }

        public override void OnGUI(Rect rect)
        {
            var exit = false;
            GUILayout.BeginArea(rect, m_WindowStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(m_WindowTitleContent, EditorStyles.boldLabel);
            if (GUILayout.Button(GUIContent.none, m_CacheServerOverflowMenuButtonStyle))
            {
                var menu = new GenericMenu();
                menu.AddItem(m_WindowOverflowMenuPreferencesButtonContent, false, () =>
                {
                    OpenPreferences();
                });
                menu.AddItem(m_WindowOverflowMenuProjectSettingsButtonContent, false, () =>
                {
                    OpenProjectSettings();
                });
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(k_Header_footer_vertical_padding);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(k_StatusText, ConnectionStatusText());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(k_EndpointText, AssetDatabaseExperimental.GetCacheServerAddress());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(k_NamespaceText, AssetDatabaseExperimental.GetCacheServerNamespacePrefix());
            GUILayout.EndHorizontal();

            GUILayout.Space(k_Header_footer_vertical_padding);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            OnGUIFooter();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            exit |= Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

            if (exit)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private string ConnectionStatusText()
        {
            string status = k_StatusConnectedText;
            if (!AssetDatabaseExperimental.IsCacheServerEnabled())
            {
                status = k_StatusDisabledText;
            }
            else if (!AssetDatabaseExperimental.IsConnectedToCacheServer())
            {
                status = k_StatusDisconnectedText;
            }
            return status;
        }

        private void OnGUIFooter()
        {
            using (new EditorGUIUtility.IconSizeScope(s_IconSize))
            {
                if (AssetDatabaseExperimental.IsCacheServerEnabled())
                {
                    // Bad IP:Port. Direct the user to fix in Project Preferences.
                    if (!AssetDatabaseExperimental.CanConnectToCacheServer(AssetDatabaseExperimental.GetCacheServerAddress(), AssetDatabaseExperimental.GetCacheServerPort()))
                    {
                        if (GUILayout.Button(m_CacheCannotConnectButtonContent, GUILayout.Width(k_ConnectionButtonWidth), GUILayout.Height(k_ConnectionButtonHeight)))
                        {
                            OpenProjectSettings();
                        }
                    }
                    // Give the option to 'reconnect' as the user may have changed the IP or Port within Project Preferences.
                    else
                    {
                        if (GUILayout.Button(m_CacheCanReconnectButtonContent, GUILayout.Width(k_ConnectionButtonWidth), GUILayout.Height(k_ConnectionButtonHeight)))
                        {
                            AssetDatabaseExperimental.RefreshSettings();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(m_CacheDisabledButtonContent, GUILayout.Width(k_ConnectionButtonWidth), GUILayout.Height(k_ConnectionButtonHeight)))
                    {
                        OpenProjectSettings();
                    }
                }
            }
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
            const int fieldCount = 4; // Header, 3x content rows (excludes button)
            const int fieldPadding = 2;
            int heightOfFields = (int)Math.Ceiling(EditorGUI.kSingleLineHeight * fieldCount);
            int heightOfFieldsPadding = fieldCount * fieldPadding;
            const int heightOfPaddingForHeaderAndFooter =  2 * k_Header_footer_vertical_padding;
            int heightOfWindowPadding = m_WindowStyle.padding.top + m_WindowStyle.padding.bottom;
            int totalHeight = heightOfFields + heightOfFieldsPadding + heightOfPaddingForHeaderAndFooter + heightOfWindowPadding + k_ConnectionButtonHeight;

            return new Vector2(k_WindowWidth, totalHeight);
        }
    }
}
