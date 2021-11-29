// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    internal class EditorDisplayFullscreenSetting
    {
        public enum Mode
        {
            DoNothing,
            FullscreenOnPlaymode,
            AlwaysFullscreen
        }

        public EditorDisplayFullscreenSetting(int id, string name)
        {
            displayId = id;
            displayName = name;
            mode = Mode.DoNothing;
            enabled = false;
            viewWindowTitle = string.Empty;
            playModeViewSettings = null;
        }

        public string displayName;
        public int displayId;

        public bool enabled;

        public Mode mode;

        public string viewWindowTitle;

        [SerializeReference]
        public IPlayModeViewFullscreenSettings playModeViewSettings;
    }
}
