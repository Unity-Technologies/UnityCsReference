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
        private readonly GUIContent m_UploadArtifacts;
        private readonly GUIContent m_UploadShaderCache;
        private readonly GUIContent m_UploadAllRevisions;
        private readonly GUIContent m_RefreshIcon;

        readonly string m_UploadArtifactsDefaultToolip = "Queues the upload of the current revision of all Artifacts present in the project. Only revisions not found on the Accelerator are uploaded.";
        readonly string m_UploadShaderCacheDefaultToolip = "Queues the upload of all Shaders, and their variants, from the Unity Shader Cache. Only shaders and/or variants not found on the Accelerator are uploaded.";
        readonly string m_UploadAllRevisionsDefaultToolip = "Queues upload of all revisions of every Artifact in the project. Only revisions not found on the Accelerator are uploaded.";
        readonly string m_DisabledSettingPrefix = "Uploading is currently disabled in Project Settings.";

        private readonly GUIStyle m_WindowStyle;

        public CacheServerWindow()
        {
            m_StatusMessageDisabled = EditorGUIUtility.TrTextContent("No cache server connected");
            m_StatusMessageConnected = EditorGUIUtility.TrTextContent("Connected");
            m_StatusMessageError = EditorGUIUtility.TrTextContent("Attempting to reconnect");
            m_OpenProjectSettings = EditorGUIUtility.TrTextContent("Open Project Settings...");
            m_UploadArtifacts = EditorGUIUtility.TrTextContent("Upload Artifacts", m_UploadArtifactsDefaultToolip);
            m_UploadShaderCache = EditorGUIUtility.TrTextContent("Upload Shader Cache", m_UploadShaderCacheDefaultToolip);
            m_UploadAllRevisions = EditorGUIUtility.TrTextContent("Upload All Revisions", m_UploadAllRevisionsDefaultToolip);
            m_RefreshIcon = EditorGUIUtility.TrIconContent("Refresh", "Refresh connection");

            m_WindowStyle = new GUIStyle { padding = new RectOffset(6, 6, 6, 6) };
        }

        public override void OnGUI(Rect rect)
        {
            var exit = false;

            bool isCacheConnected = AssetDatabase.IsConnectedToCacheServer();

            bool isCacheEnabled = AssetDatabase.IsCacheServerEnabled();
            bool isUploadEnabled = AssetDatabase.GetCacheServerEnableUpload();

            GUILayout.BeginArea(rect, m_WindowStyle);
            // Cache server connection url
            if (isCacheEnabled)
            {
                var iconPosition = new Rect();
                iconPosition.x = rect.width - (m_RefreshIcon.image.width / (Screen.dpi > 160 ? 2 : 1)) - m_WindowStyle.padding.right;
                iconPosition.y = m_WindowStyle.padding.top;
                iconPosition.width = m_RefreshIcon.image.width;
                iconPosition.height = m_RefreshIcon.image.height;
                GUIStyle helpIconStyle = EditorStyles.iconButton;
                if (GUI.Button(iconPosition, m_RefreshIcon, helpIconStyle))
                {
                    AssetDatabase.RefreshSettings();
                }

                GUILayout.BeginHorizontal();
                var style = new GUIStyle();
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = EditorStyles.boldLabel.normal.textColor;
                if (!isCacheConnected)
                {
                    style.normal.textColor = new Color(0.97f, 0.32f, 0.31f);
                }

                if (GUILayout.Button(AssetDatabase.GetCacheServerAddress(), style))
                {
                    var url = $"http://{AssetDatabase.GetCacheServerAddress()}:{AssetDatabase.GetCacheServerPort()}";
                    Application.OpenURL(url);
                }
                GUILayout.EndHorizontal();
            }

            // Connection status text label
            GUILayout.BeginHorizontal();
            var statusTextStyle = new GUIStyle()
            {
                normal = { textColor = Color.grey },
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField(ConnectionStatusText(), statusTextStyle);
            GUILayout.EndHorizontal();

            if(isCacheConnected && isCacheEnabled)
            {
                // Divider line
                var actionButtonLine = EditorGUILayout.GetControlRect(GUILayout.Height(1));
                actionButtonLine.x -= 6;
                actionButtonLine.width += 12;
                EditorGUI.DrawRect(actionButtonLine, new Color(0.387f, 0.387f, 0.387f));

                GUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(!isUploadEnabled || !isCacheEnabled))
                {
                    // change tooltip based on Project Settings
                    m_UploadArtifacts.tooltip = isUploadEnabled ? m_UploadArtifactsDefaultToolip : $"{m_DisabledSettingPrefix} {m_UploadArtifactsDefaultToolip}";
                    m_UploadShaderCache.tooltip = isUploadEnabled ? m_UploadShaderCacheDefaultToolip : $"{m_DisabledSettingPrefix} {m_UploadShaderCacheDefaultToolip}";
                    m_UploadAllRevisions.tooltip = isUploadEnabled ? m_UploadAllRevisionsDefaultToolip : $"{m_DisabledSettingPrefix} {m_UploadAllRevisionsDefaultToolip}";

                    if (GUILayout.Button(m_UploadArtifacts, GUILayout.Width(110)))
                    {
                        CacheServer.UploadArtifacts();
                    }

                    if (GUILayout.Button(m_UploadShaderCache, GUILayout.Width(140)))
                    {
                        CacheServer.UploadShaderCache();
                    }

                    if (GUILayout.Button(m_UploadAllRevisions, GUILayout.Width(130)))
                    {
                        CacheServer.UploadArtifacts(uploadAllRevisions:true);
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Divider line
            var lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            lineRect.x -= 6;
            lineRect.width += 12;
            EditorGUI.DrawRect(lineRect, new Color(0.387f, 0.387f, 0.387f));

            // Open project settings button/label
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
            if (!AssetDatabase.IsCacheServerEnabled())
            {
                status = m_StatusMessageDisabled;
            }
            else if (!AssetDatabase.IsConnectedToCacheServer())
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
            int lines = AssetDatabase.IsCacheServerEnabled() ? 3 : 2;
            bool isConnected = AssetDatabase.IsConnectedToCacheServer();
            if (isConnected)
                lines++;

            int heightOfLines = (int)Math.Ceiling(EditorGUI.kSingleLineHeight * lines);
            int heightOfWindowPadding = m_WindowStyle.padding.top + m_WindowStyle.padding.bottom;
            int dividerLine = 2;

            if (isConnected)
                dividerLine += 2;

            int width = 250;
            if (isConnected)
                width = 390;

            return new Vector2(width, heightOfLines + heightOfWindowPadding + dividerLine);
        }
    }
}
