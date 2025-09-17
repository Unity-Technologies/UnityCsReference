// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
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
        const string k_DefaultPath = "./ProfilerCaptures";
        public const string k_CapturePathSettingKey = k_SettingsPrefix + "CaptureStoragePath";
        public const string k_FrameCountSettingKey = k_SettingsPrefix + "FrameCount";
        public const string k_DropFramesOnMemoryPressureSettingKey = k_SettingsPrefix + "DropFramesOnMemoryPressure";
        public const string k_ProfilerOutOfProcessSettingKey = k_SettingsPrefix + "OutOfProcess";
        public const string k_RememberLastRecordStateSettingKey = k_SettingsPrefix + "RememberLastRecordState";
        public const string k_DefaultRecordStateSettingKey = k_SettingsPrefix + "DefaultRecordState";
        public const string k_DefaultTargetModeSettingKey = k_SettingsPrefix + "DefaultTargetMode";
        public const string k_ShowStatsLabelsOnCurrentFrameSettingKey = k_SettingsPrefix + "ShowStatsLabelsOnCurrentFrame";
        public const string k_CustomConnectionID = k_SettingsPrefix + "CustomConnectionID";
        public const string k_TargetFramesPerSecond = k_SettingsPrefix + "TargetFramesPerSecond";
        public const string k_LastImportPathPrefKey = k_SettingsPrefix + "LastImportPath";

        public const int kMinFrameCount = 600;
        public const int kDefaultFrameCount = 2000;
        public const int kMaxFrameCount = 4000;
        public const int k_MinimumTargetFramesPerSecond = 1;
        public const int k_MaximumTargetFramesPerSecond = 1000;

        private const int kMaxCustomIDLength = 26;

        [SerializeField]
        private static int m_FrameCount = 0;

        public static Action settingsChanged;
        public static event Action CaptureStoragePathChanged;
        public static event Action targetFramesPerSecondChanged;

        public static string ProfilerCaptureStoragePath
        {
            get
            {
                return EditorPrefs.GetString(k_CapturePathSettingKey, k_DefaultPath);
            }
            set
            {
                var notify = ProfilerCaptureStoragePath != value;
                EditorPrefs.SetString(k_CapturePathSettingKey, value);
                if (notify)
                    CaptureStoragePathChanged?.Invoke();
            }
        }

        public static bool UsingDefaultProfilerCaptureStoragePath() => ProfilerCaptureStoragePath.Equals(k_DefaultPath, StringComparison.Ordinal);
        public static void ResetProfilerCaptureStoragePathToDefault()
        {
            EditorPrefs.SetString(k_CapturePathSettingKey, k_DefaultPath);
        }

        public static string AbsoluteProfilerCaptureStoragePath
        {
            get
            {
                string folderPath = ProfilerCaptureStoragePath;
                //split the string
                var pathTokens = folderPath.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                if (pathTokens.Length == 0)
                    return null;

                StringBuilder pathSb = new StringBuilder();
                if (!pathTokens[0].StartsWith(".")) //ensure that we are a relative path
                {
                    Debug.LogError(folderPath + " Is not a valid relative path, as it doesn't start with './'. Please change the path for profiler captures in the Preferences.");
                    return null;
                }

                if (!pathTokens[0].StartsWith("..")) //relative path first set to start in ./
                {
                    pathSb.Append(Application.dataPath.Replace("/Assets", ""));
                }

                for (int i = 1; i < pathTokens.Length; ++i)
                {
                    pathSb.Append(Path.DirectorySeparatorChar);
                    pathSb.Append(pathTokens[i]);
                }

                var res = pathSb.ToString();
                try
                {
                    //will throw for invalid paths
                    res = Path.GetFullPath(res);
                }
                catch (Exception)
                {
                    Debug.LogError(folderPath + " Is not a valid relative path, it has more instances of '../' than folders above the project folder. Please change the path for profiler captures in the Preferences.");
                    return null;
                }

                return res;
            }
        }

        public static int frameCount
        {
            get
            {
                if (m_FrameCount == 0)
                {
                    var value = EditorPrefs.GetInt(k_FrameCountSettingKey, kDefaultFrameCount);
                    if (value == 300)
                    {
                        // 300 is the legacy default value - update it to the new default value of 2000.
                        value = kDefaultFrameCount;
                    }
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
                    if (m_FrameCount == kDefaultFrameCount)
                    {
                        // Delete the key if the value is the default value.
                        EditorPrefs.DeleteKey(k_FrameCountSettingKey);
                    }
                    else
                    {
                        EditorPrefs.SetInt(k_FrameCountSettingKey, value);
                    }
                    ProfilerDriver.SetMaxFrameHistoryLength(value);

                    settingsChanged?.Invoke();
                }
            }
        }

        public static bool dropFramesOnMemoryPressure
        {
            get { return EditorPrefs.GetBool(k_DropFramesOnMemoryPressureSettingKey, true); }
            set
            {
                if (!value)
                    EditorPrefs.SetBool(k_DropFramesOnMemoryPressureSettingKey, value);
                else
                    EditorPrefs.DeleteKey(k_DropFramesOnMemoryPressureSettingKey);
                ProfilerDriver.SetAutomaticMemoryManagement(value);
            }
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

        public static string customConnectionID
        {
            get => EditorPrefs.GetString(k_CustomConnectionID, "");
            set
            {
                if (value.Length > kMaxCustomIDLength)
                {
                    value = value.Substring(0, kMaxCustomIDLength);
                    Debug.LogWarning($"Custom Connection ID is capped at {kMaxCustomIDLength} characters in length.");
                }
                EditorPrefs.SetString(k_CustomConnectionID, value);
            }
        }

        public static int targetFramesPerSecond
        {
            get => EditorPrefs.GetInt(k_TargetFramesPerSecond, 60);
            set
            {
                if (value < k_MinimumTargetFramesPerSecond || value > k_MaximumTargetFramesPerSecond)
                    throw new ArgumentOutOfRangeException(nameof(targetFramesPerSecond));

                EditorPrefs.SetInt(k_TargetFramesPerSecond, value);
                targetFramesPerSecondChanged?.Invoke();
            }
        }

        public static void Refresh()
        {
            // Reset all cached values to force fetching it from the settings registry.
            m_FrameCount = 0;
        }

        public static bool ValidCustomConnectionID(string id)
        {
            return !id.Contains("\"") && !id.Contains("*") && id.Length <= kMaxCustomIDLength;
        }

        public static string LastImportPath
        {
            get
            {
                return SessionState.GetString(k_LastImportPathPrefKey, Application.dataPath);
            }
            set
            {
                SessionState.SetString(k_LastImportPathPrefKey, value);
            }
        }
    }
}
