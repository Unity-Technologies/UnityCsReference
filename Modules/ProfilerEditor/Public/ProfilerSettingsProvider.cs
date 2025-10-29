// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine;
using System.IO;

namespace UnityEditor.Profiling
{
    class ProfilerSettingsProvider : SettingsProvider
    {
        [UsedImplicitly]
        class Content
        {
            public static readonly GUIContent k_CapturePathText = EditorGUIUtility.TrTextContent("Profiler Capture Storage Path", "Where Profiler Capture files are saved to.");
            public static readonly GUIContent k_FrameCountText = EditorGUIUtility.TrTextContent("Frame Count", "Maximum of visible frames in the Profiler Window.");
            public static readonly GUIContent k_FrameCountWarningText = EditorGUIUtility.TrTextContent("Profiler overhead and memory usage can increase significantly the more frames are kept visible in the Profiler Window through the 'Frame Count' setting.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent k_DropFramesOnMemoryPressureText = EditorGUIUtility.TrTextContent("Automatic memory management", "Automatically drop frame data when system memory usage is at critical level and Unity Profiler uses more than 75% of the Editor memory.");
            public static readonly GUIContent k_DropFramesOnMemoryPressureWarningText = EditorGUIUtility.TrTextContent("Disabled automatic memory management may lead to Unity Editor crashes, system performance degradation and instabilities.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent k_DefaultRecordState = EditorGUIUtility.TrTextContent("Default recording state", "Recording state in which the profiler should start the first time, or when not remembering state.");
            public static readonly GUIContent k_DefaultTargetMode = EditorGUIUtility.TrTextContent("Default editor target mode on start", "Default profiler recording target mode, which is set on editor start.");
            public static readonly GUIContent k_TargetFps = EditorGUIUtility.TrTextContent("Target Frames Per Second (Highlights Module)", "The target frames per second used by the Highlights module.");
            public static readonly string OnlyRelativePaths = L10n.Tr("Only relative paths are allowed");
            public static readonly string OKButton = L10n.Tr("OK");
            public static readonly string InvalidPathWindow = L10n.Tr("Invalid Path");

            public static readonly GUIContent[] k_RecordStates =
            {
                EditorGUIUtility.TrTextContent("Remember", "The profiler will remember the previous recording state."),
                EditorGUIUtility.TrTextContent("Enabled", "The profiler starts with recording enabled."),
                EditorGUIUtility.TrTextContent("Disabled", "The profiler starts with recording disabled.")
            };

            public static readonly GUIContent[] k_TargetModes =
            {
                EditorGUIUtility.TrTextContent("Play Mode", "The editor starts with play mode as a recording target."),
                EditorGUIUtility.TrTextContent("Edit Mode", "The editor starts with the editor as a recording target.")
            };
        }

        public ProfilerSettingsProvider()
            : base("Preferences/Analysis/Profiler", SettingsScope.User)
        {
            connectionID = ProfilerUserSettings.customConnectionID;
        }

        const string k_RootPathSignifier = "./";
        const string k_PathOneUpSignifier = "../";
        private string connectionID;
        int m_TargetFramesPerSecond;

        public override void OnGUI(string searchContext)
        {
            using var _ = new SettingsWindow.GUIScope();

            EditorGUIUtility.labelWidth = 300;

            EditorGUI.BeginChangeCheck();
            var prevControl = GUI.GetNameOfFocusedControl();
            var val = EditorGUILayout.DelayedTextField(Content.k_CapturePathText, ProfilerUserSettings.ProfilerCaptureStoragePath);

            if (EditorGUI.EndChangeCheck())
            {
                if (!(val.StartsWith(k_RootPathSignifier) || val.StartsWith(k_PathOneUpSignifier)))
                {
                    if (EditorUtility.DisplayDialog(Content.InvalidPathWindow, Content.OnlyRelativePaths, Content.OKButton))
                    {
                        GUI.FocusControl(prevControl);
                        var currentlySavedPath = ProfilerUserSettings.ProfilerCaptureStoragePath;
                        // in case this faulty path has actually been saved, fix it back to default
                        if (!(currentlySavedPath.StartsWith(k_RootPathSignifier) || currentlySavedPath.StartsWith(k_PathOneUpSignifier)))
                            ProfilerUserSettings.ResetProfilerCaptureStoragePathToDefault();
                    }
                }
                else
                {
                    ProfilerUserSettings.ProfilerCaptureStoragePath = val;
                    var collectionPath = ProfilerUserSettings.AbsoluteProfilerCaptureStoragePath;
                    var info = new DirectoryInfo(collectionPath);
                    if (!info.Exists)
                    {
                        info = Directory.CreateDirectory(collectionPath);
                        if (!info.Exists)
                            throw new UnityException("Failed to create directory, with provided preferences path: " + collectionPath);
                    }
                }
            }


            ProfilerUserSettings.frameCount = EditorGUILayout.IntSlider(Content.k_FrameCountText, ProfilerUserSettings.frameCount, ProfilerUserSettings.kMinFrameCount, ProfilerUserSettings.kMaxFrameCount);
            if (ProfilerUserSettings.frameCount > ProfilerUserSettings.kDefaultFrameCount)
                EditorGUILayout.HelpBox(Content.k_FrameCountWarningText);

            ProfilerUserSettings.dropFramesOnMemoryPressure = EditorGUILayout.Toggle(Content.k_DropFramesOnMemoryPressureText, ProfilerUserSettings.dropFramesOnMemoryPressure);
            if (!ProfilerUserSettings.dropFramesOnMemoryPressure)
                EditorGUILayout.HelpBox(Content.k_DropFramesOnMemoryPressureWarningText);

            ProfilerUserSettings.showStatsLabelsOnCurrentFrame = EditorGUILayout.Toggle(ProfilerWindow.Styles.showStatsLabelsOnCurrentFrameLabel, ProfilerUserSettings.showStatsLabelsOnCurrentFrame);

            var defaultRecordStateIndex = EditorGUILayout.Popup(Content.k_DefaultRecordState,
                ProfilerUserSettings.rememberLastRecordState ? 0 : (ProfilerUserSettings.defaultRecordState ? 1 : 2), Content.k_RecordStates);
            if (defaultRecordStateIndex == 0)
                ProfilerUserSettings.rememberLastRecordState = true;
            else
            {
                ProfilerUserSettings.rememberLastRecordState = false;
                ProfilerUserSettings.defaultRecordState = defaultRecordStateIndex == 1;
            }

            var defaultModeIndexIndex = EditorGUILayout.Popup(Content.k_DefaultTargetMode, (int)ProfilerUserSettings.defaultTargetMode, Content.k_TargetModes);
            ProfilerUserSettings.defaultTargetMode = (ProfilerEditorTargetMode)defaultModeIndexIndex;

            GUI.SetNextControlName("connectionID");
            connectionID = EditorGUILayout.TextField("Custom connection ID", connectionID);

            if ((Event.current.isKey && Event.current.keyCode == KeyCode.Space &&
                 GUI.GetNameOfFocusedControl() == "connectionID"))
            {
                connectionID = connectionID.Replace(" ", "_");
                Repaint();
            }

            if ((Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "connectionID") || GUI.GetNameOfFocusedControl() != "connectionID")
            {
                ValidateConnectionId();
            }

            if (ProfilerUserSettings.ValidCustomConnectionID(connectionID))
            {
                EditorGUILayout.HelpBox($"Connection ID cannot contain \", * or be more than 26 characters in length. Spaces will be converted to underscores.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Connection ID contains \", * or is more than 26 characters in length and will be reverted", MessageType.Error);
            }

            EditorGUI.BeginChangeCheck();
            m_TargetFramesPerSecond = EditorGUILayout.IntSlider(Content.k_TargetFps, ProfilerUserSettings.targetFramesPerSecond, ProfilerUserSettings.k_MinimumTargetFramesPerSecond, ProfilerUserSettings.k_MaximumTargetFramesPerSecond);
            if (EditorGUI.EndChangeCheck())
            {
                ProfilerUserSettings.targetFramesPerSecond = m_TargetFramesPerSecond;
            }
        }

        private void ValidateConnectionId()
        {
            if (ProfilerUserSettings.ValidCustomConnectionID(connectionID))
            {
                ProfilerUserSettings.customConnectionID = connectionID;
            }
            else
            {
                connectionID = ProfilerUserSettings.customConnectionID;
                Repaint();
            }
        }

        protected override void FocusLost()
        {
            ValidateConnectionId();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var p = new ProfilerSettingsProvider
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Content>()
            };
            return p;
        }
    }
}
