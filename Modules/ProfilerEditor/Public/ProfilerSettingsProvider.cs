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
        }

        public ProfilerSettingsProvider()
            : base("Preferences/Analysis/Profiler", SettingsScope.User)
        {
        }

        public override void OnGUI(string searchContext)
        {
            ProfilerUserSettings.frameCount = EditorGUILayout.IntSlider(Content.k_FrameCountText, ProfilerUserSettings.frameCount, ProfilerUserSettings.kMinFrameCount, ProfilerUserSettings.kMaxFrameCount);
            if (ProfilerUserSettings.frameCount > 600)
                EditorGUILayout.HelpBox(Content.k_FrameCountWarningText);
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
