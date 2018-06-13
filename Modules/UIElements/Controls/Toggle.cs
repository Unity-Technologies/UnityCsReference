// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class Toggle : BaseField<bool>
    {
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> {}

        public new class UxmlTraits : BaseField<bool>.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label" };
            UxmlBoolAttributeDescription m_Value = new UxmlBoolAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Toggle)ve).m_Label.text = m_Label.GetValueFromBag(bag);
                ((Toggle)ve).value = m_Value.GetValueFromBag(bag);
            }
        }

        Action clickEvent;
        private Label m_Label;

        public Toggle()
            : this(null) {}

        public Toggle(System.Action clickEvent)
        {
            this.clickEvent = clickEvent;

            m_Label = new Label();
            Add(m_Label);

            // Click-once behaviour
            this.AddManipulator(new Clickable(OnClick));
        }

        public string text
        {
            get { return m_Label.text; }
            set { m_Label.text = value; }
        }

        public override bool value
        {
            get
            {
                return (pseudoStates & PseudoStates.Checked) == PseudoStates.Checked;
            }
            set
            {
                if (value)
                {
                    pseudoStates |= PseudoStates.Checked;
                }
                else
                {
                    pseudoStates &= ~PseudoStates.Checked;
                }
            }
        }

        public void OnToggle(Action clickEvent)
        {
            this.clickEvent = clickEvent;
        }

        private void OnClick()
        {
            value = !value;
            if (clickEvent != null)
                clickEvent();
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if ((evt as KeyDownEvent)?.character == '\n')
                OnClick();
        }
    }
}
