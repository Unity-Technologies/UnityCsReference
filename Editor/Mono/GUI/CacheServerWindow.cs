// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class CacheServerWindow : PopupWindowContent
    {
        private readonly GUIContent m_CacheServerTitleContent;
        private readonly GUIContent m_CacheServerStatusTextContent;
        private readonly GUIContent m_CacheServerStatusDisabledTextContent;
        private readonly GUIContent m_CacheServerStatusDisconnectedTextContent;
        private readonly GUIContent m_CacheServerStatusConnectedTextContent;
        private readonly GUIContent m_CacheServerEndpointTextContent;
        private readonly GUIContent m_CacheServerNamespaceTextContent;
        private readonly GUIContent m_CacheServerDisableButtonContent;
        private readonly GUIContent m_CacheServerReconnectButtonContent;

        private readonly GUIStyle m_WindowStyle;

        private const int k_FieldCount = 4;
        private const int k_FrameWidth = 11;
        private const int k_WindowWidth = 320;
        private const int k_WindowHeight = (int)EditorGUI.kSingleLineHeight * k_FieldCount + k_FrameWidth * 2;

        public CacheServerWindow()
        {
            m_CacheServerTitleContent = EditorGUIUtility.TrTextContent("Cache Server");
            m_CacheServerStatusTextContent = EditorGUIUtility.TrTextContent("Status:");
            m_CacheServerStatusDisabledTextContent = EditorGUIUtility.TrTextContent("Disabled");
            m_CacheServerStatusDisconnectedTextContent = EditorGUIUtility.TrTextContent("Disconnected");
            m_CacheServerStatusConnectedTextContent = EditorGUIUtility.TrTextContent("Connected");
            m_CacheServerEndpointTextContent = EditorGUIUtility.TrTextContent("Endpoint:");
            m_CacheServerNamespaceTextContent = EditorGUIUtility.TrTextContent("Namespace:");
            m_CacheServerDisableButtonContent = EditorGUIUtility.TrTextContent("Disable");
            m_CacheServerReconnectButtonContent = EditorGUIUtility.TrTextContent("Reconnect");

            m_WindowStyle = new GUIStyle { padding = new RectOffset(10, 10, 10, 10) };
        }

        public override void OnGUI(Rect rect)
        {
            var exit = false;

            var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };

            GUILayout.BeginArea(rect, m_WindowStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_CacheServerTitleContent, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_CacheServerStatusTextContent);
            if (!AssetDatabaseExperimental.IsCacheServerEnabled())
            {
                GUILayout.Label(m_CacheServerStatusDisabledTextContent, labelStyle);
            }
            else if (!AssetDatabaseExperimental.IsConnectedToCacheServer())
            {
                GUILayout.Label(m_CacheServerStatusDisconnectedTextContent, labelStyle);
            }
            else
            {
                GUILayout.Label(m_CacheServerStatusConnectedTextContent, labelStyle);
            }
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!AssetDatabaseExperimental.IsCacheServerEnabled()))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(m_CacheServerEndpointTextContent);
                GUILayout.Label(AssetDatabaseExperimental.GetCacheServerAddress(), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(m_CacheServerNamespaceTextContent);
                GUILayout.Label(AssetDatabaseExperimental.GetCacheServerNamespacePrefix(), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // GUILayout.BeginHorizontal();
                // if (GUILayout.Button(m_CacheServerDisableButtonContent))
                // {
                //     // TODO - Disable cache server
                // }
                // GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(m_CacheServerReconnectButtonContent))
                {
                    AssetDatabaseExperimental.RefreshSettings();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();

            exit |= Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

            if (exit)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        public override Vector2 GetWindowSize()
        {
            var fieldCount = 5; //AssetDatabaseExperimental.IsCacheServerEnabled() ? 5 : 2;
            var height = (int)EditorGUI.kSingleLineHeight * fieldCount + k_FrameWidth * 2 + 20;

            return new Vector2(k_WindowWidth, height);
        }
    }
}
