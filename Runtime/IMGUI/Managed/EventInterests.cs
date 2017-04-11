// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    internal struct EventInterests
    {
        public bool wantsMouseMove { get; set; }
        public bool wantsMouseEnterLeaveWindow { get; set; }

        public bool WantsEvent(EventType type)
        {
            switch (type)
            {
                case EventType.MouseMove:
                    return wantsMouseMove;
                case EventType.MouseEnterWindow:
                case EventType.MouseLeaveWindow:
                    return wantsMouseEnterLeaveWindow;
                default:
                    return true;
            }
        }
    }
}
