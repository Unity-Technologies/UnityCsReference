// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class CanvasManipulatorMessageEvent : EventBase<CanvasManipulatorMessageEvent>
{
    public string Message { get; private set; }

    public static CanvasManipulatorMessageEvent GetPooled(string message)
    {
        var e = GetPooled();
        e.Message = message;
        return e;
    }

    protected override void Init()
    {
        base.Init();
        bubbles = true;
        tricklesDown = false;
    }
}
