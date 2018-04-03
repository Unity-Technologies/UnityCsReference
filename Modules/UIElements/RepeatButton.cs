// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class RepeatButton : BaseTextElement
    {
        public class RepeatButtonFactory : UxmlFactory<RepeatButton, RepeatButtonUxmlTraits> {}

        public class RepeatButtonUxmlTraits : BaseTextElementUxmlTraits
        {
            UxmlLongAttributeDescription m_Delay;
            UxmlLongAttributeDescription m_Interval;

            public RepeatButtonUxmlTraits()
            {
                m_Delay = new UxmlLongAttributeDescription {name = "delay" };
                m_Interval = new UxmlLongAttributeDescription { name = "interval" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_Delay;
                    yield return m_Interval;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                RepeatButton repeatButton = (RepeatButton)ve;
                repeatButton.SetAction(null, m_Delay.GetValueFromBag(bag), m_Interval.GetValueFromBag(bag));
            }
        }

        Clickable m_Clickable;

        public RepeatButton() {}

        public RepeatButton(System.Action clickEvent, long delay, long interval)
        {
            SetAction(clickEvent, delay, interval);
        }

        public void SetAction(System.Action clickEvent, long delay, long interval)
        {
            this.RemoveManipulator(m_Clickable);
            m_Clickable = new Clickable(clickEvent, delay, interval);
            this.AddManipulator(m_Clickable);
        }
    }
}
