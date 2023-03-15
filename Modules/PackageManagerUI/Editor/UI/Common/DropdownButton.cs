// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DropdownButton : VisualElement, ITextElement, IToolbarMenuElement
    {
        internal new class UxmlFactory : UxmlFactory<DropdownButton> {}

        private const string k_HasDropDownClass = "hasDropDown";
        private const string k_HasCustomAction = "customAction";

        private const float k_MarginsWidth = 6.0f;
        private const float k_PaddingWidth = 14.0f;
        private const float k_SideElementWidth = 18.0f;
        private const float k_DropdownWidth = 12.0f;

        public event Action onBeforeShowDropdown = delegate {};
        private TextElement m_Label;

        public float estimatedWidth
        {
            get
            {
                var width = string.IsNullOrEmpty(text) ? 0.0f :  m_Label.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
                if (hasIcon)
                    width += k_SideElementWidth;
                if (m_AlwaysShowDropdown || m_DropdownMenu != null)
                    width += ClassListContains(k_HasCustomAction) ? k_SideElementWidth : k_DropdownWidth;
                return width + k_MarginsWidth + k_PaddingWidth;
            }
        }

        private Background? m_IconBackground;
        private VisualElement m_ImageIcon;
        public bool hasIcon => m_ImageIcon != null && (m_IconBackground != null || m_ImageIcon.classList.Any());

        public string iconTooltip
        {
            get => m_ImageIcon?.tooltip ?? string.Empty;
            set
            {
                if (m_ImageIcon != null)
                    m_ImageIcon.tooltip = value;
            }
        }

        private void ShowImageIcon()
        {
            if (m_ImageIcon == null)
            {
                m_ImageIcon = new VisualElement { name = "imageIcon" };
                Insert(0, m_ImageIcon);
            }
            UIUtils.SetElementDisplay(m_ImageIcon, true);
        }

        private void HideImageIcon()
        {
            if (m_ImageIcon != null)
            {
                m_ImageIcon.ClearClassList();
                UIUtils.SetElementDisplay(m_ImageIcon, false);
            }
        }

        public void SetIcon(string ussClass)
        {
            if (m_IconBackground != null)
                return;

            if (string.IsNullOrEmpty(ussClass))
            {
                HideImageIcon();
            }
            else
            {
                ShowImageIcon();
                m_ImageIcon.ClearClassList();
                m_ImageIcon.AddToClassList(ussClass);
            }
        }

        public void SetIcon(Texture2D icon)
        {
            if (icon == null)
            {
                m_IconBackground = null;
                HideImageIcon();
            }
            else
            {
                ShowImageIcon();
                m_IconBackground = Background.FromTexture2D(icon);
                m_ImageIcon.ClearClassList();
                m_ImageIcon.style.backgroundImage = new StyleBackground((Background)m_IconBackground);
            }
        }

        private Clickable m_Clickable;
        public Clickable clickable => m_Clickable;

        private VisualElement m_DropDownArea;
        private void RefreshDropdownArea()
        {
            var showDropdownArea = m_AlwaysShowDropdown || m_DropdownMenu != null;
            if (showDropdownArea && m_DropDownArea == null)
            {
                m_DropDownArea = new VisualElement { name = "dropDownArea" };
                m_DropDownArea.Add(new VisualElement { name = "dropDown" });
                m_DropDownArea.RegisterCallback<MouseDownEvent>(evt =>
                {
                    // If there's no custom button action meaning the button click event will show the dropdown menu anyways
                    // we'll just let the buttonClick event handle `ShowMenu` so the button `clicked` state show properly
                    if (m_Clicked == null)
                        return;

                    evt.StopImmediatePropagation();

                    ShowDropdown();
                }, TrickleDown.TrickleDown);
                Add(m_DropDownArea);
            }
            UIUtils.SetElementDisplay(m_DropDownArea, showDropdownArea);
            EnableInClassList(k_HasDropDownClass, showDropdownArea);
        }

        private DropdownMenu m_DropdownMenu;
        /// <summary>
        /// Sets a dropdown menu for this button. The dropdown menu icon will only show if there is a non-null menu set.
        /// </summary>
        public DropdownMenu menu
        {
            get { return m_DropdownMenu; }
            set
            {
                m_DropdownMenu = value;
                RefreshDropdownArea();
            }
        }

        private bool m_AlwaysShowDropdown;
        public bool alwaysShowDropdown
        {
            get => m_AlwaysShowDropdown;
            set
            {
                if (m_AlwaysShowDropdown == value)
                    return;
                m_AlwaysShowDropdown = value;
                RefreshDropdownArea();
            }
        }

        private Action m_Clicked;
        /// <summary>
        /// If the clicked event is never set, then the default behaviour of the button click is to open the dropdown menu
        /// </summary>
        public event Action clicked
        {
            add
            {
                m_Clicked += value;
                AddToClassList(k_HasCustomAction);
            }
            remove
            {
                m_Clicked -= value;
            }
        }

        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        private void OnButtonClicked()
        {
            if (m_Clicked != null)
                m_Clicked.Invoke();
            else
                ShowDropdown();
        }

        private void ShowDropdown()
        {
            onBeforeShowDropdown?.Invoke();
            if (menu != null)
                this.ShowMenu();
        }

        public DropdownButton()
        {
            AddToClassList(Button.ussClassName);

            style.flexDirection = FlexDirection.Row;

            m_Clickable = new Clickable(OnButtonClicked);
            this.AddManipulator(m_Clickable);

            m_Label = new TextElement { name = "label" };
            Add(m_Label);
        }
    }
}
