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

        private const int k_Width = 36;
        private const int k_Height = 19;
        private const int k_MarginX = 4;
        private const int k_MarginY = 0;

        static CacheServerToggle()
        {
            AssetDatabaseExperimental.cacheServerConnectionChanged += OnCacherServerConnectionChanged;
        }

        public CacheServerToggle()
        {
            m_CacheServerNotEnabledContent = EditorGUIUtility.TrIconContent("CacheServerDisabled");
            m_CacheServerDisconnectedContent = EditorGUIUtility.TrIconContent("CacheServerDisconnected");
            m_CacheServerConnectedContent = EditorGUIUtility.TrIconContent("CacheServerConnected");
            m_PopupLocation = new[] { PopupLocation.AboveAlignRight };
        }

        public void OnGUI(float x, float y)
        {
            GUILayout.BeginVertical();
            EditorGUILayout.Space();

            var statusContent = GetStatusContent();
            var buttonArea = new Rect(x + k_MarginX, y + k_MarginY, k_Width, k_Height);

            if (EditorGUI.DropdownButton(buttonArea, statusContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                PopupWindow.Show(buttonArea, new CacheServerWindow(), m_PopupLocation);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
            GUILayout.EndVertical();
        }

        public float GetWidth()
        {
            return k_Width + (k_MarginX << 1);
        }

        public float GetHeight()
        {
            return k_Height + (k_MarginY << 1);
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
