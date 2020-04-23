using System;

namespace UnityEngine.UIElements
{
    public class RepeatButton : TextElement
    {
        public new class UxmlFactory : UxmlFactory<RepeatButton, UxmlTraits> {}

        public new class UxmlTraits : TextElement.UxmlTraits
        {
            UxmlLongAttributeDescription m_Delay = new UxmlLongAttributeDescription { name = "delay" };
            UxmlLongAttributeDescription m_Interval = new UxmlLongAttributeDescription { name = "interval" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                RepeatButton repeatButton = (RepeatButton)ve;
                repeatButton.SetAction(null, m_Delay.GetValueFromBag(bag, cc), m_Interval.GetValueFromBag(bag, cc));
            }
        }

        private PointerClickable m_Clickable;

        public new static readonly string ussClassName = "unity-repeat-button";

        public RepeatButton()
        {
            AddToClassList(ussClassName);
        }

        public RepeatButton(System.Action clickEvent, long delay, long interval) : this()
        {
            SetAction(clickEvent, delay, interval);
        }

        public void SetAction(System.Action clickEvent, long delay, long interval)
        {
            this.RemoveManipulator(m_Clickable);
            m_Clickable = new PointerClickable(clickEvent, delay, interval);
            this.AddManipulator(m_Clickable);
        }
    }
}
