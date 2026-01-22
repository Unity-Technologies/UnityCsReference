// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor
{
    // A wrapper around the existing singleton API for storing global Editor preferences. This exists to make it easier going forward to migrate away from the singleton approach, as well as to provide a single API to the multiple places where persistent preferences are currently stored.
    class LegacyGlobalProfilerPersistentSettingsService : IProfilerPersistentSettingsService
    {
        const string k_PersistentSettingKey_BottlenecksViewVisible = "bottlenecks-view-visible";
        const string k_PersistentSettingKey_BottleneckDetailsViewSelectedSummaryType = "bottlenecks-details-view__selected-summary-type";

        public LegacyGlobalProfilerPersistentSettingsService()
        {
            ProfilerUserSettings.settingsChanged += OnSettingsChanged;
            ProfilerUserSettings.targetFramesPerSecondChanged += OnTargetFpsChanged;
        }

        public event Action TargetFrameDurationChanged;

        public event Action MaximumFrameCountChanged;

        public bool IsBottleneckViewVisible
        {
            get => EditorPrefs.GetBool(k_PersistentSettingKey_BottlenecksViewVisible, true);
            set => EditorPrefs.SetBool(k_PersistentSettingKey_BottlenecksViewVisible, value);
        }

        public ulong TargetFrameDurationNs
        {
            get => Convert.ToUInt64((1f / ProfilerUserSettings.targetFramesPerSecond) * 1e9f);
            set => ProfilerUserSettings.targetFramesPerSecond = UnityEngine.Mathf.RoundToInt(1e9f / value);
        }

        public int MaximumFrameCount => ProfilerUserSettings.frameCount;

        public int BottleneckDetailsViewSelectedSummaryType
        {
            get => EditorPrefs.GetInt(k_PersistentSettingKey_BottleneckDetailsViewSelectedSummaryType, 0);
            set => EditorPrefs.SetInt(k_PersistentSettingKey_BottleneckDetailsViewSelectedSummaryType, value);
        }

        public readonly struct ValuePref : IProfilerPersistentSettingsService.IValue
        {
            readonly string m_PreferenceKey;
            readonly string m_Suffix;

            readonly string FullPreferenceKey => m_PreferenceKey + m_Suffix;

            public ValuePref(string key, string suffix)
            {
                m_PreferenceKey = key;
                m_Suffix = suffix;
            }

            public string Get()
            {
                if (string.IsNullOrEmpty(m_PreferenceKey))
                    return string.Empty;

                return EditorPrefs.GetString(FullPreferenceKey);
            }

            public void Set(string value)
            {
                if (string.IsNullOrEmpty(m_PreferenceKey))
                    return;

                EditorPrefs.SetString(FullPreferenceKey, value);
            }

            public void Delete()
            {
                if (string.IsNullOrEmpty(m_PreferenceKey))
                    return;

                EditorPrefs.DeleteKey(FullPreferenceKey);
            }

            public IProfilerPersistentSettingsService.IValue Rename(string newKey)
            {
                string value = string.Empty;
                if (!string.IsNullOrEmpty(m_PreferenceKey))
                {
                    value = Get();
                    Delete();
                }

                var ret = new ValuePref(newKey, m_Suffix);
                ret.Set(value);
                return ret;
            }
        }

        public IProfilerPersistentSettingsService.IValue ChartCountersOrder(string chartNameKey)
        {
            return new ValuePref(chartNameKey, "Order");
        }

        public IProfilerPersistentSettingsService.IValue ChartCountersVisible(string chartNameKey)
        {
            return new ValuePref(chartNameKey, "Visible");
        }

        public void Dispose()
        {
            ProfilerUserSettings.targetFramesPerSecondChanged -= OnTargetFpsChanged;
            ProfilerUserSettings.settingsChanged -= OnSettingsChanged;
        }

        void OnSettingsChanged()
        {
            // Today this method is only invoked when maximum frame count is changed.
            MaximumFrameCountChanged?.Invoke();
        }

        void OnTargetFpsChanged()
        {
            TargetFrameDurationChanged?.Invoke();
        }
    }
}
