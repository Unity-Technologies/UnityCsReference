// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    [Serializable]
    internal class EditorDisplaySettingsProfile
    {
        [SerializeField] private string m_name;
        [SerializeField] private List<EditorDisplayFullscreenSetting> m_settings;
        [SerializeField] private BuildTarget m_buildTarget;
        [SerializeField] private DisplayAPIControlMode displayAPIMode;
        [SerializeField] private bool m_displayAPIUseSystemConfiguration;

        public string Name
        {
            get => m_name;
            set => m_name = value;
        }

        public BuildTarget Target
        {
            get => m_buildTarget;
            set => m_buildTarget = value;
        }

        public DisplayAPIControlMode DisplayAPIMode
        {
            get => displayAPIMode;
            set => displayAPIMode = value;
        }

        public bool DisplayAPIUseSystemConfiguration
        {
            get => m_displayAPIUseSystemConfiguration;
            set => m_displayAPIUseSystemConfiguration = value;
        }

        public List<EditorDisplayFullscreenSetting> Settings => m_settings;

        public EditorDisplaySettingsProfile(string name)
        {
            m_name = name;
            m_buildTarget = EditorUserBuildSettings.activeBuildTarget;
            displayAPIMode = DisplayAPIControlMode.FromEditor;
        }

        public EditorDisplayFullscreenSetting GetEditorDisplayFullscreenSetting(int displayId)
        {
            if (m_settings == null)
            {
                m_settings = new List<EditorDisplayFullscreenSetting>();
            }

            return m_settings.FirstOrDefault(setting => setting.displayId == displayId);
        }

        public void AddEditorDisplayFullscreenSetting(EditorDisplayFullscreenSetting setting)
        {
            if (m_settings == null)
            {
                m_settings = new List<EditorDisplayFullscreenSetting>();
            }

            m_settings.Add(setting);
        }

        public void RemoveEditorDisplayFullscreenSetting(EditorDisplayFullscreenSetting setting)
        {
            m_settings.Remove(setting);
        }
    }
} // namespace
