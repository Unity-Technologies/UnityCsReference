// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine.UIElements;

namespace Unity.UI.Builder;

internal static class VersionChangeTypeUtility
{
    public static VersionChangeType StylingChanged()
    {
        return VersionChangeType.Layout |
               VersionChangeType.StyleSheet |
               VersionChangeType.Styles |
               VersionChangeType.Overflow |
               VersionChangeType.BorderRadius |
               VersionChangeType.BorderWidth |
               VersionChangeType.Transform |
               VersionChangeType.Size |
               VersionChangeType.Repaint |
               VersionChangeType.Color |
               VersionChangeType.Picking |
               VersionChangeType.RenderHints;
    }
}
