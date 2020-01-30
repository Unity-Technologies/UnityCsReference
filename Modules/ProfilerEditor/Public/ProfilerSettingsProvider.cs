// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.Profiling
{
    class ProfilerSettingsProvider : SettingsProvider
    {
        [UsedImplicitly]
        class Content
        {
            public static readonly GUIContent k_FrameCountText = EditorGUIUtility.TrTextContent("Frame Count", "Maximum of visible frames in the Profiler Window.");
            public static readonly GUIContent k_FrameCountWarningText = EditorGUIUtility.TrTextContent("Profiler overhead and memory usage can increase significantly the more frames are kept visible in the Profiler Window through the 'Frame Count' setting.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public static readonly GUIContent k_DefaultRecordState = EditorGUIUtility.TrTextContent("Default recording state", "Recording state in which the profiler should start the first time, or when not remembering state.");
            public static readonly GUIContent k_DefaultTargetMode = EditorGUIUtility.TrTextContent("Default editor target mode");

            public static readonly GUIContent[] k_RecordStates =
            {
                EditorGUIUtility.TrTextContent("Remember", "The profiler will remember the previous recording state."),
                EditorGUIUtility.TrTextContent("Enabled", "The profiler starts with recording enabled."),
                EditorGUIUtility.TrTextContent("Disabled", "The profiler starts with recording disabled.")
            };

            public static readonly GUIContent[] k_TargetModes =
            {
                EditorGUIUtility.TrTextContent("Playmode"),
                EditorGUIUtility.TrTextContent("Editmode")
            };
        }

        public ProfilerSettingsProvider()
            : base("Preferences/Analysis/Profiler", SettingsScope.User)
        {
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 300;
            ProfilerUserSettings.frameCount = EditorGUILayout.IntSlider(Content.k_FrameCountText, ProfilerUserSettings.frameCount, ProfilerUserSettings.kMinFrameCount, ProfilerUserSettings.kMaxFrameCount);
            if (ProfilerUserSettings.frameCount > 600)
                EditorGUILayout.HelpBox(Content.k_FrameCountWarningText);

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
