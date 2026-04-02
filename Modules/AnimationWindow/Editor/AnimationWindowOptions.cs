// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Time;
using UnityEditor;

namespace UnityEditorInternal
{
    static class AnimationWindowOptions
    {
        const string k_TimeFormat = "AnimationWindow.TimelineFoundation.TimeFormat";
        const string k_FilterBySelection = "AnimationWindow.FilterBySelection";
        const string k_ShowReadOnly = "AnimationWindow.ShowReadOnly";
        const string k_ShowFrameRate = "AnimationWindow.ShowFrameRate";

        private static TimeFormat m_TimeFormat;
        private static bool m_FilterBySelection;
        private static bool m_ShowReadOnly;
        private static bool m_ShowFrameRate;

        static AnimationWindowOptions()
        {
            m_TimeFormat = (TimeFormat)EditorPrefs.GetInt(k_TimeFormat, (int)TimeFormat.Frames);
            m_FilterBySelection = EditorPrefs.GetBool(k_FilterBySelection, false);
            m_ShowReadOnly = EditorPrefs.GetBool(k_ShowReadOnly, false);
            m_ShowFrameRate = EditorPrefs.GetBool(k_ShowFrameRate, false);
        }

        public static TimeFormat timeFormat
        {
            get => m_TimeFormat;
            set
            {
                m_TimeFormat = value;
                EditorPrefs.SetInt(k_TimeFormat, (int)value);
            }
        }

        public static bool filterBySelection
        {
            get => m_FilterBySelection;
            set
            {
                m_FilterBySelection = value;
                EditorPrefs.SetBool(k_FilterBySelection, value);
            }
        }

        public static bool showReadOnly
        {
            get => m_ShowReadOnly;
            set
            {
                m_ShowReadOnly = value;
                EditorPrefs.SetBool(k_ShowReadOnly, value);
            }
        }

        public static bool showFrameRate
        {
            get => m_ShowFrameRate;
            set
            {
                m_ShowFrameRate = value;
                EditorPrefs.SetBool(k_ShowFrameRate, value);
            }
        }
    }
}
