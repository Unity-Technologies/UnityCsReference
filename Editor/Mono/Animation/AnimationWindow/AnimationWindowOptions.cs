// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal static class AnimationWindowOptions
    {
        static string kTimeFormat = "AnimationWindow.TimeFormat";
        static string kFilterBySelection = "AnimationWindow.FilterBySelection";
        static string kShowReadOnly = "AnimationWindow.ShowReadOnly";
        static string kShowFrameRate = "AnimationWindow.ShowFrameRate";

        private static TimeArea.TimeFormat m_TimeFormat;
        private static bool m_FilterBySelection;
        private static bool m_ShowReadOnly;
        private static bool m_ShowFrameRate;

        static AnimationWindowOptions()
        {
            m_TimeFormat = (TimeArea.TimeFormat)EditorPrefs.GetInt(kTimeFormat, (int)TimeArea.TimeFormat.TimeFrame);
            m_FilterBySelection = EditorPrefs.GetBool(kFilterBySelection, false);
            m_ShowReadOnly = EditorPrefs.GetBool(kShowReadOnly, false);
            m_ShowFrameRate = EditorPrefs.GetBool(kShowFrameRate, false);
        }

        public static TimeArea.TimeFormat timeFormat
        {
            get
            {
                return m_TimeFormat;
            }
            set
            {
                m_TimeFormat = value;
                EditorPrefs.SetInt(kTimeFormat, (int)value);
            }
        }

        public static bool filterBySelection
        {
            get
            {
                return m_FilterBySelection;
            }
            set
            {
                m_FilterBySelection = value;
                EditorPrefs.SetBool(kFilterBySelection, value);
            }
        }

        public static bool showReadOnly
        {
            get
            {
                return m_ShowReadOnly;
            }
            set
            {
                m_ShowReadOnly = value;
                EditorPrefs.SetBool(kShowReadOnly, value);
            }
        }

        public static bool showFrameRate
        {
            get
            {
                return m_ShowFrameRate;
            }
            set
            {
                m_ShowFrameRate = value;
                EditorPrefs.SetBool(kShowFrameRate, value);
            }
        }
    }
}
