// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental;

namespace UnityEditor
{
    internal class CacheServerToggle
    {
        private readonly GUIContent m_CacheServerNotEnabledContent;
        private readonly GUIContent m_CacheServerDisconnectedContent;
        private readonly GUIContent m_CacheServerConnectedContent;
        private readonly PopupLocation[] m_PopupLocation;

        static CacheServerToggle()
        {
            AssetDatabaseExperimental.cacheServerConnectionChanged += OnCacherServerConnectionChanged;
        }

        public CacheServerToggle()
        {
            m_CacheServerNotEnabledContent = EditorGUIUtility.TrIconContent("CacheServerDisabled", "Cache Server disabled");
            m_CacheServerDisconnectedContent = EditorGUIUtility.TrIconContent("CacheServerDisconnected", "Cache Server disconnected");
            m_CacheServerConnectedContent = EditorGUIUtility.TrIconContent("CacheServerConnected", "Cache Server connected");
            m_PopupLocation = new[] { PopupLocation.AboveAlignRight };
        }

        public void OnGUI()
        {
            var content = GetStatusContent();
            var style = AppStatusBar.Styles.statusIcon;
            var rect = GUILayoutUtility.GetRect(content, style);
            if (GUI.Button(rect, content, style))
            {
                PopupWindow.Show(rect, new CacheServerWindow(), m_PopupLocation);
                GUIUtility.ExitGUI();
            }
        }

        private GUIContent GetStatusContent()
        {
            if (!AssetDatabaseExperimental.IsCacheServerEnabled())
            {
                return m_CacheServerNotEnabledContent;
            }

            if (!AssetDatabaseExperimental.IsConnectedToCacheServer())
            {
                return m_CacheServerDisconnectedContent;
            }

            return m_CacheServerConnectedContent;
        }

        private static void OnCacherServerConnectionChanged(AssetDatabaseExperimental.CacheServerConnectionChangedParameters param)
        {
            AppStatusBar.StatusChanged();
        }
    }
}
