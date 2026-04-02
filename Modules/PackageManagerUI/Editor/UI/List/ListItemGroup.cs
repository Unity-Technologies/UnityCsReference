// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ListItemGroup : VisualElement
    {
        public bool isHidden { get; private set; }
        public string groupName { get; private set; }

        public IEnumerable<IListItem> items => Children().FilterByType<IListItem>();

        private readonly IPageManager m_PageManager;
        public ListItemGroup(IResourceLoader resourceLoader, IPageManager pageManager, string groupName)
        {
            m_PageManager = pageManager;

            var root = resourceLoader.GetTemplate("ListItemGroup.uxml");
            hierarchy.Add(root);
            m_Cache = new VisualElementCache(root);

            headerCaret.RegisterValueChangedCallback(OnHeaderClicked);
            headerTag.pickingMode = PickingMode.Ignore;

            SetGroupName(groupName);
        }

        private void OnHeaderClicked(ChangeEvent<bool> evt)
        {
            var expanded = evt.newValue;
            m_PageManager.activePage.SetGroupExpanded(groupName, expanded);
            EnableInClassList("collapsed", !expanded);

            if (!expanded)
                return;

            foreach (var item in items)
            {
                if (!item.selected)
                    continue;
                EditorApplication.delayCall += () => UIUtils.ScrollIfNeeded(this, item.element);
                return;
            }
        }

        public void SetGroupName(string value)
        {
            groupName = value;

            name = groupName;

            var expanded = m_PageManager.activePage.IsGroupExpanded(groupName);
            headerCaret.text = groupName;
            headerCaret.SetValueWithoutNotify(expanded);
            EnableInClassList("collapsed", !expanded);

            isHidden = string.IsNullOrEmpty(groupName);
            EnableInClassList("hidden", isHidden);
        }

        public void RefreshHeaderVisibility()
        {
            EnableInClassList("empty", !items.AnyMatches(item => item.visualState.visible));
        }

        private readonly VisualElementCache m_Cache;
        public override VisualElement contentContainer => m_Cache.Get<VisualElement>("groupContainer");
        private Toggle headerCaret => m_Cache.Get<Toggle>("headerCaret");
        private Label headerTag => m_Cache.Get<Label>("headerTag");
    }
}
