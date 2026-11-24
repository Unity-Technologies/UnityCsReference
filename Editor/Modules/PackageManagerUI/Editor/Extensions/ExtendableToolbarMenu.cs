// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class ExtendableToolbarMenu : ToolbarWindowMenu, IToolbarMenuElement, IMenu
    {
        private bool m_NeedRefresh;
        private readonly List<MenuDropdownItem> m_BuiltInItems;
        private readonly List<MenuDropdownItem> m_DropdownItems;
        public DropdownMenu menu { get; private set; }

        public ExtendableToolbarMenu() : this(ServicesContainer.instance.Resolve<IDropdownHandler>())
        {
        }

        private readonly IDropdownHandler m_DropdownHandler;
        public ExtendableToolbarMenu(IDropdownHandler dropdownHandler)
        {
            m_DropdownHandler = dropdownHandler;

            m_BuiltInItems = new List<MenuDropdownItem>();
            m_DropdownItems = new List<MenuDropdownItem>();

            menu = new DropdownMenu();
            clicked += OnClicked;

            m_NeedRefresh = true;
        }

        private void OnClicked()
        {
            RefreshDropdown();
            this.ShowMenu();
        }

        public void RefreshDropdown()
        {
            if (m_NeedRefresh || m_DropdownItems.Exists(item => item.needRefresh))
            {
                m_BuiltInItems.Sort(ExtensionManager.CompareExtensions);
                m_DropdownItems.Sort(ExtensionManager.CompareExtensions);

                var newDropdownMenu = new DropdownMenu();
                foreach (var item in m_BuiltInItems.Join(m_DropdownItems))
                {
                    if (item.insertSeparatorBefore)
                        newDropdownMenu.AppendSeparator();

                    newDropdownMenu.AppendAction(item.text, a => { item.action?.Invoke(); }, item.statusCallback, item.userData);
                    item.needRefresh = false;
                }
                menu = newDropdownMenu;
                m_NeedRefresh = false;
            }
        }

        public MenuDropdownItem AddBuiltInDropdownItem()
        {
            var newItem = new MenuDropdownItem();
            m_BuiltInItems.Add(newItem);
            m_NeedRefresh = true;
            return newItem;
        }

        public IMenuDropdownItem AddDropdownItem()
        {
            var newItem = new MenuDropdownItem();
            m_DropdownItems.Add(newItem);
            m_NeedRefresh = true;
            return newItem;
        }

        public void Remove(MenuDropdownItem item)
        {
            m_DropdownItems.Remove(item);
            m_NeedRefresh = true;
        }

        public void ShowInputDropdown(InputDropdownArgs args)
        {
            m_DropdownHandler.ShowGenericInputDropdown(this, args);
        }
    }
}
