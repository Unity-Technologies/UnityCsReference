// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal class ReorderableDragAndDropController : ICollectionDragAndDropController
    {
        readonly CollectionView m_CollectionView;
        readonly List<int> m_SortedSelectedIndices = new();

        public IEnumerable<int> GetSortedSelectedIndices() => m_SortedSelectedIndices;

        public ReorderableDragAndDropController(CollectionView view)
        {
            m_CollectionView = view;
        }

        public DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.OverItem)
                return DragVisualMode.Rejected;

            return args.dragAndDropData.source == m_CollectionView ?
                DragVisualMode.Move :
                DragVisualMode.Rejected;
        }

        public void OnDrop(IListDragAndDropArgs args)
        {
            var insertIndex = args.insertAtIndex;
            var insertIndexShift = 0;
            var srcIndexShift = 0;

            for (var i = m_SortedSelectedIndices.Count - 1; i >= 0; --i)
            {
                var index = m_SortedSelectedIndices[i];

                if (index < 0)
                    continue;

                var newIndex = insertIndex - insertIndexShift;

                if (index >= insertIndex)
                {
                    index += srcIndexShift;
                    srcIndexShift++;
                }
                else if (index < newIndex)
                {
                    insertIndexShift++;
                    newIndex--;
                }

                m_CollectionView.Move(index, newIndex);
            }

            if (m_CollectionView.selectionType != SelectionType.None)
            {
                var newSelection = new List<int>();

                for (var i = 0; i < m_SortedSelectedIndices.Count; ++i)
                {
                    newSelection.Add(insertIndex - insertIndexShift + i);
                }

                m_CollectionView.SetSelectionWithoutNotify(newSelection);
            }
            else
            {
                m_CollectionView.ClearSelection();
            }

            m_CollectionView.RefreshItems();
        }

        public bool enableReordering { get; set; } = true;

        public bool CanStartDrag(IEnumerable<int> itemIndices)
        {
            return enableReordering;
        }

        public StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false)
        {
            m_SortedSelectedIndices.Clear();

            var title = string.Empty;
            if (itemIndices != null)
            {
                foreach (var index in itemIndices)
                {
                    m_SortedSelectedIndices.Add(index);

                    if (skipText)
                        continue;

                    if (string.IsNullOrEmpty(title))
                    {
                        var label = m_CollectionView.GetRootElementForIndex(index)?.Q<Label>();
                        title = label != null ? label.text : $"Item {index}";
                    }
                    else
                    {
                        title = "<Multiple>";
                        skipText = true;
                    }
                }
            }

            m_SortedSelectedIndices.Sort(CompareIndex);

            return new StartDragArgs(title, DragVisualMode.Move);
        }

        int CompareIndex(int index1, int index2) => index1.CompareTo(index2);
    }
}
