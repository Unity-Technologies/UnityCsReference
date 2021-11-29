// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;

namespace UnityEditor
{
    internal interface IPlayModeViewFullscreenSettings
    {
        int DisplayNumber { get; }

        bool VsyncEnabled { get; }

        void OnPreferenceGUI(BuildTarget target);
    }
} // namespace
