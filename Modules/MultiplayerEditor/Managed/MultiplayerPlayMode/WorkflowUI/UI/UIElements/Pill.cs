// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [UxmlElement]
    internal partial class Pill : VisualElement
    {

        readonly Button m_DeleteButton;
        readonly TextElement m_Text;

        [UxmlAttribute]
        public string Text
        {
            get => m_Text.text;
            set => m_Text.text = value;
        }

        public event Action<Pill> CloseEvent;

        public Pill()
        {
            m_DeleteButton = new Button();
            m_Text = new Label();

            Add(m_DeleteButton);
            m_DeleteButton.Add(m_Text);

            this.AddEventLifecycle(OnAttach, OnDetach);
        }

        void OnAttach(AttachToPanelEvent _)
        {
            m_DeleteButton.clickable.clickedWithEventInfo += ClickableOnClickedWithEventInfo;
        }

        void OnDetach(DetachFromPanelEvent _)
        {
            m_DeleteButton.clickable.clickedWithEventInfo -= ClickableOnClickedWithEventInfo;
        }

        void ClickableOnClickedWithEventInfo(EventBase evt)
        {
            CloseEvent?.Invoke(this);

            evt.StopPropagation();
        }
    }
}
