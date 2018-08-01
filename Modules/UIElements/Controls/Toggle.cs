// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

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
                ((Toggle)ve).text = m_Label.GetValueFromBag(bag, cc);
                ((Toggle)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        Action m_ClickEvent;

        private Label m_Label;

        public Toggle()
        {
            // Allocate and add the checkmark to the hierarchy
            var checkMark = new VisualElement() { name = "Checkmark", pickingMode = PickingMode.Ignore };
            Add(checkMark);

            // Set-up the label and text...
            text = null;

            this.AddManipulator(new Clickable(OnClick));
        }

        [Obsolete("Use Toggle() with OnValueChanged() instead.", false)]
        public Toggle(System.Action clickEvent) : this()
        {
            OnToggle(clickEvent);
        }

        public string text
        {
            get { return m_Label?.text; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_Label == null)
                    {
                        m_Label = new Label();
                        m_Label.pickingMode = PickingMode.Ignore;
                        Add(m_Label);
                    }

                    m_Label.text = value;
                }
                else if (m_Label != null)
                {
                    Remove(m_Label);
                    m_Label = null;
                }
            }
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            if (newValue)
            {
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                pseudoStates &= ~PseudoStates.Checked;
            }
            base.SetValueWithoutNotify(newValue);
        }

        [Obsolete("Use OnValueChanged() instead.", false)]
        public void OnToggle(Action clickEvent)
        {
            if ((clickEvent != null) && (this.m_ClickEvent == null))
            {
                // Forward the clickEvent by the InternalOnValueChanged notification
                OnValueChanged(InternalOnValueChanged);
            }
            else if ((clickEvent == null) && (this.m_ClickEvent != null))
            {
                // Don't need to keep being notified...
                UnregisterCallback<ChangeEvent<bool>>(InternalOnValueChanged);
            }

            this.m_ClickEvent = clickEvent;
        }

        void InternalOnValueChanged(ChangeEvent<bool> evt)
        {
            m_ClickEvent?.Invoke();
        }

        void OnClick()
        {
            value = !value;
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (((evt as KeyDownEvent)?.character == '\n') || ((evt as KeyDownEvent)?.character == ' '))
            {
                OnClick();
                evt.StopPropagation();
            }
        }
    }
}
