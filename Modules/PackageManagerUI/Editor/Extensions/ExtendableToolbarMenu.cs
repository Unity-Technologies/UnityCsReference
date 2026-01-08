// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExtendableToolbarMenu : ToolbarWindowMenu, IToolbarMenuElement, IMenu
    {
        [Serializable]
        public new class UxmlSerializedData : ToolbarWindowMenu.UxmlSerializedData
        {
            public override object CreateInstance() => new ExtendableToolbarMenu();
        }

        private bool m_NeedRefresh;
        private List<MenuDropdownItem> m_BuiltInItems;
        private List<MenuDropdownItem> m_DropdownItems;
        public DropdownMenu menu { get; private set; }

        private IDropdownHandler m_DropdownHandler;
        private void ResolveDependencies()
        {
            m_DropdownHandler = ServicesContainer.instance.Resolve<IDropdownHandler>();
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
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var item in m_BuiltInItems.Concat(m_DropdownItems))
#pragma warning restore RS0030
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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_BuiltInItems.Last();
#pragma warning restore RS0030
        }

        public IMenuDropdownItem AddDropdownItem()
        {
            m_DropdownItems.Add(new MenuDropdownItem());
            m_NeedRefresh = true;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_DropdownItems.Last();
#pragma warning restore RS0030
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
