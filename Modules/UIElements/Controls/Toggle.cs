// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public class Toggle : BaseField<bool>
    {
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> {}

        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Toggle)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        public new static readonly string ussClassName = "unity-toggle";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";
        public static readonly string noTextVariantUssClassName = ussClassName + "--no-text";
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        public static readonly string textUssClassName = ussClassName + "__text";

        private Label m_Label;

        public Toggle()
            : this(null) {}

        public Toggle(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            AddToClassList(noTextVariantUssClassName);

            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            // Allocate and add the checkmark to the hierarchy
            var checkMark = new VisualElement() { name = "unity-checkmark", pickingMode = PickingMode.Ignore };
            checkMark.AddToClassList(checkmarkUssClassName);
            visualInput.Add(checkMark);

            // The picking mode needs to be Position in order to have the Pseudostate Hover applied...
            visualInput.pickingMode = PickingMode.Position;

            // Set-up the label and text...
            text = null;
            this.AddManipulator(new Clickable(OnClickEvent));
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
                        m_Label = new Label
                        {
                            pickingMode = PickingMode.Ignore
                        };
                        m_Label.AddToClassList(textUssClassName);
                        RemoveFromClassList(noTextVariantUssClassName);
                        visualInput.Add(m_Label);
                    }

                    m_Label.text = value;
                }
                else if (m_Label != null)
                {
                    Remove(m_Label);
                    AddToClassList(noTextVariantUssClassName);
                    m_Label = null;
                }
            }
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            if (newValue)
            {
                visualInput.pseudoStates |= PseudoStates.Checked;
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;
            }
            base.SetValueWithoutNotify(newValue);
        }

        void OnClickEvent(EventBase evt)
        {
            if ((evt as MouseUpEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mue = (MouseUpEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mue.mousePosition)))
                {
                    OnClick();
                }
            }
        }

        void OnClick()
        {
            value = !value;
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            if (((evt as KeyDownEvent)?.keyCode == KeyCode.KeypadEnter) ||
                ((evt as KeyDownEvent)?.keyCode == KeyCode.Return))
            {
                OnClick();
                evt.StopPropagation();
            }
        }
    }
}
