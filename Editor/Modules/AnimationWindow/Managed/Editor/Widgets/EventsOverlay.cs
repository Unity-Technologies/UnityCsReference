// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class EventsOverlay : TooltipOverlay
    {
        const string k_Style = "eventsOverlay";
        const string k_Name = "eventsOverlay";

        static readonly CustomStyleProperty<int> k_HorizontalOffsetStyleProperty = new("--tooltip-horizontal-offset");
        static readonly CustomStyleProperty<int> k_VerticalOffsetStyleProperty = new("--tooltip-vertical-offset");

        private int m_HorizontalOffset;
        private int m_VerticalOffset;

        public EventsOverlay()
        {
            AddToClassList(k_Style);
            name = k_Name;

            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolved);
        }

        public void Set(string text, Vector2 position)
        {
            labelText = text;
            if (string.IsNullOrEmpty(text))
            {
                Hide();
            }
            else
            {
                Show();

                style.translate = position + new Vector2(m_HorizontalOffset, m_VerticalOffset);
            }
        }

        void CustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            customStyle.TryGetValue(k_HorizontalOffsetStyleProperty, out m_HorizontalOffset);
            customStyle.TryGetValue(k_VerticalOffsetStyleProperty, out m_VerticalOffset);
        }
    }
}
