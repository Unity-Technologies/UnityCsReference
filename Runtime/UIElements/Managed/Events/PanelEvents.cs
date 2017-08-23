// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IPanelEvent
    {
    }

    public class AttachToPanelEvent : EventBase<AttachToPanelEvent>, IPanelEvent
    {
    }

    public class DetachFromPanelEvent : EventBase<DetachFromPanelEvent>, IPanelEvent
    {
    }
}
