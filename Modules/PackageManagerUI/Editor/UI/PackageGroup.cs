// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageGroup : VisualElement
    {
        public event Action<bool> onGroupToggle = delegate {};

        public bool expanded => headerCaret.value;

        public bool isHidden { get; }

        public IEnumerable<PackageItem> packageItems => groupContainer.Children().Cast<PackageItem>();

        private ResourceLoader m_ResourceLoader;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies(ResourceLoader resourceLoader, PageManager pageManager, PackageManagerProjectSettingsProxy settingsProxy, PackageDatabase packageDatabase)
        {
            m_ResourceLoader = resourceLoader;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_PackageDatabase = packageDatabase;
        }

        public PackageGroup(ResourceLoader resourceLoader, PageManager pageManager, PackageManagerProjectSettingsProxy settingsProxy, PackageDatabase packageDatabase, string groupName, string displayName, bool expanded = true, bool hidden = false)
        {
            ResolveDependencies(resourceLoader, pageManager, settingsProxy, packageDatabase);

            name = groupName;
            var root = m_ResourceLoader.GetTemplate("PackageGroup.uxml");
            Add(root);
            m_Cache = new VisualElementCache(root);

            headerCaret.SetValueWithoutNotify(expanded);
            EnableInClassList("collapsed", !expanded);
            headerCaret.RegisterValueChangedCallback((evt) =>
            {
                m_PageManager.SetGroupExpanded(groupName, evt.newValue);
                EnableInClassList("collapsed", !evt.newValue);
                EditorApplication.delayCall += () => onGroupToggle?.Invoke(evt.newValue);
            });

            headerTag.pickingMode = PickingMode.Ignore;
            headerCaret.text = displayName;

            isHidden = hidden;
            if (isHidden)
                AddToClassList("hidden");
        }

        public bool Contains(ISelectableItem item)
        {
            return groupContainer.Contains(item.element);
        }

        public void RefreshHeaderVisibility()
        {
            EnableInClassList("empty", !packageItems.Any(item => item.visualState.visible));
        }

        internal PackageItem AddPackageItem(IPackage package, VisualState state)
        {
            var packageItem = new PackageItem(m_PageManager, m_SettingsProxy, m_PackageDatabase) {packageGroup = this};
            packageItem.SetPackageAndVisualState(package, state);
            groupContainer.Add(packageItem);
            return packageItem;
        }

        internal void AddPackageItem(PackageItem item)
        {
            groupContainer.Add(item);
        }

        internal void ClearPackageItems()
        {
            groupContainer.Clear();
        }

        internal void RemovePackageItem(PackageItem item)
        {
            if (groupContainer.Contains(item))
                groupContainer.Remove(item);
        }

        private readonly VisualElementCache m_Cache;

        public VisualElement groupContainer => m_Cache.Get<VisualElement>("groupContainer");
        public VisualElement headerContainer => m_Cache.Get<VisualElement>("headerContainer");
        private Toggle headerCaret => m_Cache.Get<Toggle>("headerCaret");
        private Label headerTag => m_Cache.Get<Label>("headerTag");
    }
}
