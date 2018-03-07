// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class Toggle : BaseControl<bool>
    {
        Action clickEvent;
        private Label m_Label;

        [Obsolete("Use value instead", false)]
        public bool on
        {
            get { return value; }
            set { this.value = value; }
        }

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
