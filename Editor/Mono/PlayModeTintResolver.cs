// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    static class PlayModeTintResolver
    {
        const string k_PlayModeTintPrefKey = "Playmode tint";
        static readonly PrefColor s_PlayModeTintPref = new PrefColor(k_PlayModeTintPrefKey, .8f, .8f, .8f, 1);

        static Color s_ActiveTint = Color.white;

        public static Color activePlayModeTint
        {
            get => s_ActiveTint;
            private set
            {
                if (s_ActiveTint != value)
                {
                    s_ActiveTint = value;
                    activePlayModeTintChanged?.Invoke(value);
                }
            }
        }

        public static event Action<Color> activePlayModeTintChanged;

        static PlayModeTintResolver()
        {
            s_ActiveTint = ComputeTint();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorPrefs.onValueWasUpdated += OnEditorPrefChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Color newTint;
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    newTint = s_PlayModeTintPref.Color;
                    break;
                default:
                    newTint = Color.white;
                    break;
            }

            activePlayModeTint = newTint;
        }

        static void OnEditorPrefChanged(string key)
        {
            if (key == k_PlayModeTintPrefKey)
                activePlayModeTint = ComputeTint();
        }

        static Color ComputeTint()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode ? s_PlayModeTintPref.Color : Color.white;
        }
    }
}
