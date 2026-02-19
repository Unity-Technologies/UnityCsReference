// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal partial class Pill : Button
    {
        public event Action<string> CloseEvent;

        public Pill()
        {
            AddToClassList("player-tag-pill");
            this.AddEventLifecycle(OnAttach, OnDetach);
        }

        void OnAttach(AttachToPanelEvent _)
        {
            clickable.clickedWithEventInfo += ClickableOnClickedWithEventInfo;
        }

        void OnDetach(DetachFromPanelEvent _)
        {
            clickable.clickedWithEventInfo -= ClickableOnClickedWithEventInfo;
        }

        void ClickableOnClickedWithEventInfo(EventBase evt)
        {
            CloseEvent?.Invoke(text);
            evt.StopPropagation();
        }
    }
}
