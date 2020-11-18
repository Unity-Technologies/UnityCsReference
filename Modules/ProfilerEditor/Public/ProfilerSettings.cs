// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental.Networking.PlayerConnection;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Profiling
{
    internal enum ProfilerEditorTargetMode
    {
        Playmode,
        Editmode
    }

    internal class ProfilerUserSettings
    {
        // User setting keys. Don't localize!
        const string k_SettingsPrefix = "Profiler.";
        public const string k_FrameCountSettingKey = k_SettingsPrefix + "FrameCount";
        public const string k_ProfilerOutOfProcessSettingKey = k_SettingsPrefix + "OutOfProcess";
        public const string k_RememberLastRecordStateSettingKey = k_SettingsPrefix + "RememberLastRecordState";
        public const string k_DefaultRecordStateSettingKey = k_SettingsPrefix + "DefaultRecordState";
        public const string k_DefaultTargetModeSettingKey = k_SettingsPrefix + "DefaultTargetMode";
        public const string k_ShowStatsLabelsOnCurrentFrameSettingKey = k_SettingsPrefix + "ShowStatsLabelsOnCurrentFrame";

        public const int kMinFrameCount = 300;
        public const int kMaxFrameCount = 2000;

        [SerializeField]
        private static int m_FrameCount = 0;

        public static Action settingsChanged;

        public static int frameCount
        {
            get
            {
                if (m_FrameCount == 0)
                {
                    var value = EditorPrefs.GetInt(k_FrameCountSettingKey, kMinFrameCount);
                    m_FrameCount = Mathf.Clamp(value, kMinFrameCount, kMaxFrameCount);
                    ProfilerDriver.SetMaxFrameHistoryLength(m_FrameCount);
                }

                return m_FrameCount;
            }
            set
            {
                if (value < 0 || value > kMaxFrameCount)
                    throw new ArgumentOutOfRangeException(nameof(frameCount), value, $"must be between {0} and {kMaxFrameCount}");

                if (value != m_FrameCount)
                {
                    m_FrameCount = value;
                    EditorPrefs.SetInt(k_FrameCountSettingKey, value);
                    ProfilerDriver.SetMaxFrameHistoryLength(value);

                    if (settingsChanged != null)
                        settingsChanged.Invoke();
                }
            }
        }

        public static bool useOutOfProcessProfiler
        {
            get { return EditorPrefs.GetBool(k_ProfilerOutOfProcessSettingKey, false); }
            set { EditorPrefs.SetBool(k_ProfilerOutOfProcessSettingKey, value); }
        }

        public static bool rememberLastRecordState
        {
            get { return EditorPrefs.GetBool(k_RememberLastRecordStateSettingKey, false); }
            set { EditorPrefs.SetBool(k_RememberLastRecordStateSettingKey, value); }
        }

        public static bool defaultRecordState
        {
            get { return EditorPrefs.GetBool(k_DefaultRecordStateSettingKey, true); }
            set { EditorPrefs.SetBool(k_DefaultRecordStateSettingKey, value); }
        }

        public static bool showStatsLabelsOnCurrentFrame
        {
            get { return EditorPrefs.GetBool(k_ShowStatsLabelsOnCurrentFrameSettingKey, false); }
            set { EditorPrefs.SetBool(k_ShowStatsLabelsOnCurrentFrameSettingKey, value); }
        }

        public static ProfilerEditorTargetMode defaultTargetMode
        {
            get { return (ProfilerEditorTargetMode)EditorPrefs.GetInt(k_DefaultTargetModeSettingKey, (int)ProfilerEditorTargetMode.Playmode); }
            set { EditorPrefs.SetInt(k_DefaultTargetModeSettingKey, (int)value); }
        }

        public static void Refresh()
        {
            // Reset all cached values to force fetching it from the settings registry.
            m_FrameCount = 0;
        }
    }
}
