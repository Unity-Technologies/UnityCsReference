// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolkitEditor.InternalBridge.Editor;

static class EditorToolbarBridge
{
    public static OverlayToolbar CreateOverlay(IEnumerable<string> toolbarElements, EditorWindow containerWindow)
    {
        return EditorToolbar.CreateOverlay(toolbarElements, containerWindow);
    }
}
