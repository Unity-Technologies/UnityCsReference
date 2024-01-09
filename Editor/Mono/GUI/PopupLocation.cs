// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum PopupLocation
    {
        Below,
        BelowAlignLeft = Below,
        Above,
        AboveAlignLeft = Above,
        Left,
        Right,
        Overlay,
        BelowAlignRight,
        AboveAlignRight
    }
}
