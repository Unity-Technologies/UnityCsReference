// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal partial class CacheServerToggle
    {
        private readonly GUIContent m_CacheServerNotEnabledContent;
        private readonly GUIContent m_CacheServerDisconnectedContent;
        private readonly GUIContent m_CacheServerConnectedContent;
        private readonly PopupLocation[] m_PopupLocation;

        // cacheServerConnectionChanged is cleared on code reload, so this subscription has to be
        // re-established on every load. A static constructor would only run once per domain, and the status
        // bar would stop repainting on cache server connection changes after the first reload.
        [OnCodeLoaded]
        static void Initialize()
        {
            AssetDatabase.cacheServerConnectionChanged += OnCacherServerConnectionChanged;
        }

        public CacheServerToggle()
        {
            m_CacheServerNotEnabledContent = L10n.IconContent("CacheServerDisabled", "Cache Server disabled", null);
            m_CacheServerDisconnectedContent = L10n.IconContent("CacheServerDisconnected", "Cache Server disconnected", null);
            m_CacheServerConnectedContent = L10n.IconContent("CacheServerConnected", "Cache Server connected", null);
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
            if (!AssetDatabase.IsCacheServerEnabled())
            {
                return m_CacheServerNotEnabledContent;
            }

            if (!AssetDatabase.IsConnectedToCacheServer())
            {
                return m_CacheServerDisconnectedContent;
            }

            return m_CacheServerConnectedContent;
        }

        private static void OnCacherServerConnectionChanged(CacheServerConnectionChangedParameters param)
        {
            AppStatusBar.StatusChanged();
        }
    }
}
