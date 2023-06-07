// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageExtensionAction : IPackageActionButton, IPackageActionMenu
    {
        private IWindow m_Window;
        private bool m_NeedRefresh;
        public IEnumerable<PackageActionDropdownItem> visibleDropdownItems => m_DropdownItems.Where(item => item.visible);
        private List<PackageActionDropdownItem> m_DropdownItems;
        public DropdownButton dropdownButton { get; }

        private Action<PackageSelectionArgs> m_Action;
        public Action<PackageSelectionArgs> action
        {
            get => m_Action;
            set
            {
                m_Action = value;
                if (value != null)
                    dropdownButton.clicked += OnAction;
                else
                    dropdownButton.clicked -= OnAction;
            }
        }
        public string text
        {
            get => dropdownButton.text;
            set => dropdownButton.text = value;
        }

        public string tooltip
        {
            get => dropdownButton.tooltip;
            set => dropdownButton.tooltip = value;
        }

        private int m_Priority;
        public int priority
        {
            get => m_Priority;
            set
            {
                if (m_Priority == value)
                    return;
                m_Priority = value;
                onPriorityChanged?.Invoke();
            }
        }

        public Texture2D icon { set => dropdownButton.SetIcon(value); }

        private bool m_Visible;
        public bool visible
        {
            get => m_Visible;
            set
            {
                if (m_Visible == value)
                    return;
                m_Visible = value;
                UIUtils.SetElementDisplay(dropdownButton, value);
                onVisibleChanged?.Invoke();
            }
        }
        public bool enabled { get => dropdownButton.enabledSelf; set => dropdownButton.SetEnabled(value); }

        public event Action onVisibleChanged;
        public event Action onPriorityChanged;

        public PackageExtensionAction(IWindow window)
        {
            m_Window = window;
            m_DropdownItems = new List<PackageActionDropdownItem>();

            dropdownButton = new DropdownButton();
            dropdownButton.onBeforeShowDropdown += OnBeforeShowDropdown;

            m_Visible = true;
            m_NeedRefresh = true;
        }

        private void OnBeforeShowDropdown()
        {
            if (m_NeedRefresh || m_DropdownItems.Any(d => d.needRefresh))
            {
                m_DropdownItems.Sort(ExtensionManager.CompareExtensions);
                var newDropdownMenu = new DropdownMenu();
                foreach (var item in visibleDropdownItems)
                {
                    if (item.insertSeparatorBefore)
                        newDropdownMenu.AppendSeparator();
                    newDropdownMenu.AppendAction(item.text, a => { item.action?.Invoke(m_Window.activeSelection); }, item.statusCallback);
                    item.needRefresh = false;
                }
                dropdownButton.menu = newDropdownMenu;
                m_NeedRefresh = false;
            }
        }

        public IPackageActionDropdownItem AddDropdownItem()
        {
            var result = new PackageActionDropdownItem();
            result.onVisibleChanged += RefreshDropdownIcon;

            m_DropdownItems.Add(result);
            RefreshDropdownIcon();
            m_NeedRefresh = true;
            return result;
        }

        private void RefreshDropdownIcon()
        {
            var anyDropdownItemsVisible = visibleDropdownItems.Any();
            if (anyDropdownItemsVisible && dropdownButton.menu == null)
                dropdownButton.menu = new DropdownMenu();
            else if (!anyDropdownItemsVisible && dropdownButton.menu != null)
                dropdownButton.menu = null;
        }

        private void OnAction()
        {
            m_Action?.Invoke(m_Window.activeSelection);
        }
    }
}
