// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class DropdownToggle : BaseField<bool>
    {
        public new static readonly string ussClassName = "unity-dropdown-toggle";
        public static readonly string dropdownClassName = ussClassName + "__dropdown";
        public static readonly string toggleClassName = ussClassName + "__toggle";
        public static readonly string toggleIconClassName = ussClassName + "__icon";

        readonly Button m_Toggle;
        readonly Button m_DropdownButton;

        public Button dropdownButton => m_DropdownButton;

        public DropdownToggle() : this(null) {}

        public DropdownToggle(string label) : base(label)
        {
            AddToClassList(ussClassName);

            m_Toggle = new Button(ToggleValue);
            m_Toggle.AddToClassList(toggleClassName);

            var icon = new VisualElement();
            icon.AddToClassList(toggleIconClassName);
            icon.pickingMode = PickingMode.Ignore;
            m_Toggle.Add(icon);

            m_DropdownButton = new Button();
            m_DropdownButton.AddToClassList(dropdownClassName);

            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            arrow.pickingMode = PickingMode.Ignore;
            m_DropdownButton.Add(arrow);

            Add(m_Toggle);
            Add(m_DropdownButton);
        }

        void ToggleValue()
        {
            value = !value;
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            if (newValue)
            {
                m_Toggle.pseudoStates |= PseudoStates.Checked;
                m_DropdownButton.pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                m_Toggle.pseudoStates &= ~PseudoStates.Checked;
                m_DropdownButton.pseudoStates &= ~PseudoStates.Checked;
            }
            base.SetValueWithoutNotify(newValue);
        }
    }
}
