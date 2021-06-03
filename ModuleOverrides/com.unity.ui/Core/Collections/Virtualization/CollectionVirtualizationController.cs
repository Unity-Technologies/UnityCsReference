// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    abstract class CollectionVirtualizationController
    {
        protected readonly ScrollView m_ScrollView;

        public abstract int firstVisibleIndex { get; }
        public abstract int lastVisibleIndex { get; }
        public abstract int visibleItemCount { get; }

        protected CollectionVirtualizationController(ScrollView scrollView)
        {
            m_ScrollView = scrollView;
        }

        public abstract void Refresh(bool rebuild);
        public abstract void ScrollToItem(int id);
        public abstract void Resize(Vector2 size, int layoutPass);
        public abstract void OnScroll(Vector2 offset);
        public abstract int GetIndexFromPosition(Vector2 position);
        public abstract float GetItemHeight(int index);
        public abstract void UpdateBackground();

        public abstract IEnumerable<ReusableCollectionItem> activeItems { get; }
        public abstract void ReplaceActiveItem(int index);
    }
}
