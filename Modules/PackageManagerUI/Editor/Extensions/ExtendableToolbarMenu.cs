// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExtendableToolbarMenu : ToolbarWindowMenu, IToolbarMenuElement, IMenu
    {
        public new class UxmlFactory : UxmlFactory<ExtendableToolbarMenu, UxmlTraits> {}

        private bool m_NeedRefresh;
        private List<MenuDropdownItem> m_BuiltInItems;
        private List<MenuDropdownItem> m_DropdownItems;
        public DropdownMenu menu { get; private set; }

        private ResourceLoader m_ResourceLoader;
        private void ResolveDependencies()
        {
            m_ResourceLoader = ServicesContainer.instance.Resolve<ResourceLoader>();
        }

        public ExtendableToolbarMenu()
        {
            ResolveDependencies();

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
            if (m_NeedRefresh || m_DropdownItems.Any(item => item.needRefresh))
            {
                m_BuiltInItems.Sort(ExtensionManager.CompareExtensions);
                m_DropdownItems.Sort(ExtensionManager.CompareExtensions);

                var newDropdownMenu = new DropdownMenu();
                foreach (var item in m_BuiltInItems.Concat(m_DropdownItems))
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
            m_BuiltInItems.Add(new MenuDropdownItem());
            m_NeedRefresh = true;
            return m_BuiltInItems.Last();
        }

        public IMenuDropdownItem AddDropdownItem()
        {
            m_DropdownItems.Add(new MenuDropdownItem());
            m_NeedRefresh = true;
            return m_DropdownItems.Last();
        }

        public void Remove(MenuDropdownItem item)
        {
            m_DropdownItems.Remove(item);
            m_NeedRefresh = true;
        }

        public void ShowInputDropdown(InputDropdownArgs args)
        {
            var rect = GUIUtility.GUIToScreenRect(worldBound);
            var dropdown = new GenericInputDropdown(m_ResourceLoader, PackageManagerWindow.instance, args) { position = rect };
            DropdownContainer.ShowDropdown(dropdown);
        }
    }
}
