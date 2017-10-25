// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    class TooltipEvent : EventBase<TooltipEvent>, IPropagatableEvent
    {
        public string tooltip { get; set; }
        public Rect rect { get; set; }
    }
}
