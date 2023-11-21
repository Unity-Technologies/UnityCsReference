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
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new DropdownButton();
        }

        private const string k_HasSeparateDropdownClass = "separate-dropdown";
        private const string k_HasIconClass = "has-icon";

        private const float k_MarginsWidth = 6.0f;
        private const float k_PaddingWidth = 14.0f;
        private const float k_SideElementWidth = 18.0f;
        private const float k_DropdownWidth = 12.0f;

        private VisualElement m_MainButton;
        public VisualElement mainButton => m_MainButton;

        private TextElement m_Label;
        private VisualElement m_ImageIcon;
        private Background? m_IconBackground;

        private VisualElement m_SeparateDropdownArea;
        private VisualElement m_DropdownIcon;

        private DropdownMenu m_DropdownMenu;
        /// <summary>
        /// Sets a dropdown menu for this button. The dropdown menu icon will only show if there is a non-null menu set.
        /// </summary>
        public DropdownMenu menu
        {
            get => m_DropdownMenu;
            set
            {
                m_DropdownMenu = value;
                RefreshDropdownIcon();
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
                RefreshDropdownIcon();
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
                RefreshDropdownIcon();
            }
            remove
            {
                m_Clicked -= value;
                RefreshDropdownIcon();
            }
        }

        public string text
        {
            get => m_Label.text;
            set
            {
                m_Label.text = value;
                UIUtils.SetElementDisplay(m_Label, !string.IsNullOrEmpty(value));
            }
        }

        public event Action onBeforeShowDropdown = delegate {};

        private bool showDropdownIcon => m_AlwaysShowDropdown || m_DropdownMenu?.Count > 0;
        private bool isDropdownIconSeparated => m_Clicked != null;

        private Clickable m_MainButtonClickable;
        public Clickable clickable => m_MainButtonClickable;

        public float estimatedWidth
        {
            get
            {
                var width = string.IsNullOrEmpty(text) ? 0.0f :  m_Label.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
                if (m_ImageIcon != null && (m_IconBackground != null || m_ImageIcon.classList.Any()))
                    width += k_SideElementWidth;
                if (showDropdownIcon)
                    width += isDropdownIconSeparated ? k_SideElementWidth : k_DropdownWidth;
                return width + k_MarginsWidth + k_PaddingWidth;
            }
        }

        public DropdownButton()
        {
            m_MainButton = new VisualElement { name = "mainButton" };
            m_MainButton.AddToClassList(Button.ussClassName);
            m_MainButtonClickable = new Clickable(OnMainButtonClicked);
            m_MainButton.AddManipulator(m_MainButtonClickable);
            Add(m_MainButton);

            m_Label = new TextElement { name = "label" };
            m_MainButton.Add(m_Label);
            text = string.Empty;
        }

        public DropdownButton(Action clickEvent) : this()
        {
            clicked += clickEvent;
        }

        private void ShowImageIcon()
        {
            if (m_ImageIcon == null)
            {
                m_ImageIcon = new VisualElement { name = "imageIcon" };
                m_MainButton.Insert(0, m_ImageIcon);
            }
            AddToClassList(k_HasIconClass);
            UIUtils.SetElementDisplay(m_ImageIcon, true);
        }

        private void HideImageIcon()
        {
            if (m_ImageIcon == null)
                return;
            m_ImageIcon.ClearClassList();
            RemoveFromClassList(k_HasIconClass);
            UIUtils.SetElementDisplay(m_ImageIcon, false);
        }

        public void SetIcon(Icon icon)
        {
            if (m_IconBackground != null)
                return;

            if (icon == Icon.None)
            {
                HideImageIcon();
            }
            else
            {
                ShowImageIcon();
                m_ImageIcon.ClearClassList();
                m_ImageIcon.AddToClassList(icon.ClassName());
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

        private void RefreshDropdownIcon()
        {
            if (!showDropdownIcon)
            {
                m_DropdownIcon?.RemoveFromHierarchy();
                RemoveFromClassList(k_HasSeparateDropdownClass);
                UIUtils.SetElementDisplay(m_SeparateDropdownArea, false);
                return;
            }

            m_DropdownIcon ??= new VisualElement { name = "dropdownIcon" };
            if (isDropdownIconSeparated)
            {
                if (m_SeparateDropdownArea == null)
                {
                    m_SeparateDropdownArea = new VisualElement { name = "dropdownArea" };
                    m_SeparateDropdownArea.AddToClassList(Button.ussClassName);
                    m_SeparateDropdownArea.AddManipulator(new Clickable(ShowDropdown));
                    Add(m_SeparateDropdownArea);
                }
                m_SeparateDropdownArea.Add(m_DropdownIcon);
                AddToClassList(k_HasSeparateDropdownClass);
                UIUtils.SetElementDisplay(m_SeparateDropdownArea, true);
            }
            else
            {
                m_MainButton.Add(m_DropdownIcon);
                RemoveFromClassList(k_HasSeparateDropdownClass);
                UIUtils.SetElementDisplay(m_SeparateDropdownArea, false);
            }
        }

        public void ClearClickedEvents()
        {
            if (m_Clicked == null)
                return;

            m_Clicked = null;
            RefreshDropdownIcon();
        }

        private void OnMainButtonClicked()
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
    }
}
