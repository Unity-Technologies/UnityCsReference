// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License



namespace UnityEditor
{
    class SceneViewPickingShortcutContext : SceneViewMotion.SceneViewContext, ShortcutManagement.IHelperBarShortcutContext
    {
        public override bool active => ViewHasFocusAndViewportUnderMouse && Tools.current != Tool.View;
        public bool helperBarActive => base.active && Tools.current != Tool.View;
    }
}
