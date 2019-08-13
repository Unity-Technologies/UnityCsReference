// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Profiling
{
    internal class ProfilerUserSettings
    {
        // User setting keys. Don't localize!
        const string k_SettingsPrefix = "Profiler.";
        public const string k_FrameCountSettingKey = k_SettingsPrefix + "FrameCount";
        public const string k_ProfilerOutOfProcessSettingKey = k_SettingsPrefix + "OutOfProcess";

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
                    m_FrameCount = EditorPrefs.GetInt(k_FrameCountSettingKey, kMinFrameCount);

                return m_FrameCount;
            }
            set
            {
                if (value != m_FrameCount)
                {
                    m_FrameCount = value;
                    EditorPrefs.SetInt(k_FrameCountSettingKey, value);
                    ProfilerDriver.maxHistoryLength = value;

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

        public static void Refresh()
        {
            // Reset all cached values to force fetching it from the settings registry.
            m_FrameCount = 0;
        }
    }
}
