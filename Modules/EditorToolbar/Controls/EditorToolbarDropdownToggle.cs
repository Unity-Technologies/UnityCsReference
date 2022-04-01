// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [MovedFrom("UnityEditor.Overlays")]
    public class EditorToolbarDropdownToggle : BaseField<bool>
    {
        internal new static readonly string ussClassName = "unity-dropdown-toggle";
        internal static readonly string dropdownClassName = ussClassName + "__dropdown";
        internal static readonly string toggleClassName = ussClassName + "__toggle";
        internal static readonly string toggleIconClassName = ussClassName + "__icon";
        internal static readonly string toggleTextClassName = ussClassName + "__text";
        internal static readonly string toggleNoIconClassName = ussClassName + "-noicon";
        internal static readonly string toggleNoDropdownClassName = toggleClassName + "-no-dropdown";

        readonly Image m_IconElement;
        readonly Button m_Toggle;
        readonly Button m_DropdownButton;
        TextElement m_TextElement;

        public event Action dropdownClicked
        {
            add => m_DropdownButton.clicked += value;
            remove => m_DropdownButton.clicked -= value;
        }

        public Clickable dropdownClickable => m_DropdownButton.clickable;

        public Texture2D icon
        {
            get => m_IconElement.image as Texture2D;
            set
            {
                m_IconElement.image = value;
                UpdateIconState();
            }
        }

        public string text
        {
            get => m_TextElement?.text;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_TextElement?.RemoveFromHierarchy();
                    m_TextElement = null;
                    return;
                }

                if (m_TextElement == null)
                    m_Toggle.Add(m_TextElement = new TextElement());

                m_TextElement.AddToClassList(toggleTextClassName);
                m_TextElement.AddToClassList(EditorToolbar.elementLabelClassName);
                m_TextElement.text = value;
                UpdateIconState();
            }
        }

        public EditorToolbarDropdownToggle() : this("", null) {}
        public EditorToolbarDropdownToggle(Action dropdownClickEvent) : this("", null, dropdownClickEvent) {}
        public EditorToolbarDropdownToggle(string text, Action dropdownClickEvent) : this(text, null, dropdownClickEvent) {}
        public EditorToolbarDropdownToggle(Texture2D icon, Action dropdownClickEvent) : this("", icon, dropdownClickEvent) {}

        public EditorToolbarDropdownToggle(string text, Texture2D icon, Action dropdownClickEvent) : base(null)
        {
            AddToClassList(ussClassName);

            focusable = false;

            m_Toggle = new Button(ToggleValue);
            m_Toggle.AddToClassList(toggleClassName);

            m_IconElement = new Image {scaleMode = ScaleMode.ScaleToFit, image = icon};
            m_IconElement.AddToClassList(EditorToolbar.elementIconClassName);
            m_IconElement.AddToClassList(toggleIconClassName);
            if (icon == null && !(text == null || text == string.Empty))
                m_IconElement.AddToClassList(toggleNoIconClassName);
            m_IconElement.pickingMode = PickingMode.Ignore;
            m_Toggle.Add(m_IconElement);

            m_DropdownButton = new Button(dropdownClickEvent);
            m_DropdownButton.AddToClassList(dropdownClassName);

            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            arrow.pickingMode = PickingMode.Ignore;
            m_DropdownButton.Add(arrow);

            Add(m_Toggle);
            Add(m_DropdownButton);

            this.text = text;
        }

        void UpdateIconState()
        {
            if (icon == null && !string.IsNullOrEmpty(text))
                m_IconElement.AddToClassList(toggleNoIconClassName);
            else if (icon)
                m_IconElement.RemoveFromClassList(toggleNoIconClassName);
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

        public void ShowDropDown(bool show)
        {
            m_DropdownButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (!show)
            {
                if(!m_Toggle.ClassListContains(toggleNoDropdownClassName))
                    m_Toggle.AddToClassList(toggleNoDropdownClassName);

                if(!m_Toggle.ClassListContains(toggleClassName))
                    m_Toggle.RemoveFromClassList(toggleClassName);
            }
            else if(show)
            {
                if(m_Toggle.ClassListContains(toggleNoDropdownClassName))
                    m_Toggle.RemoveFromClassList(toggleNoDropdownClassName);

                if(!m_Toggle.ClassListContains(toggleClassName))
                    m_Toggle.AddToClassList(toggleClassName);
            }
        }
    }
}
