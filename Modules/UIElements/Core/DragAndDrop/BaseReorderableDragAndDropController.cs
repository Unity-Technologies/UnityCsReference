// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal abstract class BaseReorderableDragAndDropController : ICollectionDragAndDropController
    {
        protected readonly BaseVerticalCollectionView m_View;

        protected List<int> m_SortedSelectedIds = new ();

        // Sorted by index in the source.
        public IEnumerable<int> GetSortedSelectedIds() => m_SortedSelectedIds;

        protected BaseReorderableDragAndDropController(BaseVerticalCollectionView view)
        {
            m_View = view;
        }

        public virtual bool enableReordering { get; set; } = true;

        public virtual bool CanStartDrag(IEnumerable<int> itemIds)
        {
            return true;
        }

        public virtual StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIds, bool skipText = false)
        {
            m_SortedSelectedIds.Clear();

            var title = string.Empty;
            if (itemIds != null)
            {
                foreach (var id in itemIds)
                {
                    m_SortedSelectedIds.Add(id);

                    if (skipText)
                        continue;

                    if (string.IsNullOrEmpty(title))
                    {
                        var label = m_View.GetRecycledItemFromId(id)?.rootElement.Q<Label>();
                        title = label != null ? label.text : $"Item {id}";
                    }
                    else
                    {
                        title = "<Multiple>";
                        skipText = true;
                    }
                }
            }

            // Sort indices, store ids.
            m_SortedSelectedIds.Sort(CompareId);

            return new StartDragArgs(title, DragVisualMode.Move);
        }

        protected virtual int CompareId(int id1, int id2) => id1.CompareTo(id2);

        public abstract DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args);
        public abstract void OnDrop(IListDragAndDropArgs args);

        public virtual void DragCleanup() { }
        public virtual void HandleAutoExpand(ReusableCollectionItem item, Vector2 pointerPosition) { }
    }
}
