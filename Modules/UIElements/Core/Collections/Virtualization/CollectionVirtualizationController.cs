// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    abstract class CollectionVirtualizationController
    {
        protected readonly ScrollView m_ScrollView;

        public abstract int firstVisibleIndex { get; protected set; }
        public abstract int visibleItemCount { get; }

        protected CollectionVirtualizationController(ScrollView scrollView)
        {
            m_ScrollView = scrollView;
        }

        public abstract void Refresh(bool rebuild);
        public abstract void ScrollToItem(int id);
        public abstract void Resize(Vector2 size);
        public abstract void OnScroll(Vector2 offset);
        public abstract int GetIndexFromPosition(Vector2 position);
        public abstract float GetExpectedItemHeight(int index);
        public abstract float GetExpectedContentHeight();
        public abstract void OnFocusIn(VisualElement leafTarget);
        public abstract void OnFocusOut(VisualElement willFocus);
        public abstract void UpdateBackground();

        public abstract IEnumerable<ReusableCollectionItem> activeItems { get; }

        internal abstract void StartDragItem(ReusableCollectionItem item);
        internal abstract void EndDrag(int dropIndex);

        public abstract void UnbindAll();
    }
}
