// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class BaseListItem : VisualElement, IListItem
    {
        public bool selected { get; private set; }
        public VisualState visualState { get; private set; }
        public VisualElement element => this;

        private readonly IPageManager m_PageManager;
        protected BaseListItem(IPageManager pageManager)
        {
            m_PageManager = pageManager;

            AddToClassList("list-item");
        }

        public virtual void BindVisualState(VisualState newVisualState)
        {
            visualState = newVisualState;
            EnableInClassList("invisible", newVisualState is not { visible: true });
            RefreshSelection();
        }

        public void RefreshSelection()
        {
            selected = !string.IsNullOrEmpty(visualState?.itemUniqueId) && m_PageManager.activePage.GetSelection().Contains(visualState.itemUniqueId);
            EnableInClassList("selected", selected);
        }
    }
}
