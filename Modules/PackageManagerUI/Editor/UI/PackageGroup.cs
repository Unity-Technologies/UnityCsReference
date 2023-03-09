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

        public bool isHidden { get; }

        public IEnumerable<PackageItem> packageItems => groupContainer.Children().Cast<PackageItem>();

        private readonly PageManager m_PageManager;
        private readonly PackageDatabase m_PackageDatabase;

        public PackageGroup(ResourceLoader resourceLoader, PageManager pageManager, PackageDatabase packageDatabase, string groupName, bool expanded = true, bool hidden = false)
        {
            m_PageManager = pageManager;
            m_PackageDatabase = packageDatabase;

            name = groupName;
            var root = resourceLoader.GetTemplate("PackageGroup.uxml");
            Add(root);
            m_Cache = new VisualElementCache(root);

            headerCaret.SetValueWithoutNotify(expanded);
            EnableInClassList("collapsed", !expanded);
            headerCaret.RegisterValueChangedCallback((evt) =>
            {
                m_PageManager.activePage.SetGroupExpanded(groupName, evt.newValue);
                EnableInClassList("collapsed", !evt.newValue);
                EditorApplication.delayCall += () => onGroupToggle?.Invoke(evt.newValue);
            });

            headerTag.pickingMode = PickingMode.Ignore;
            headerCaret.text = groupName;

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
            var packageItem = new PackageItem(m_PageManager, m_PackageDatabase) {packageGroup = this};
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
        private Toggle headerCaret => m_Cache.Get<Toggle>("headerCaret");
        private Label headerTag => m_Cache.Get<Label>("headerTag");
    }
}
