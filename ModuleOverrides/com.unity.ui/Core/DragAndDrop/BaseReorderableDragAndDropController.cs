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

        protected List<int> m_SelectedIndices;

        public BaseReorderableDragAndDropController(BaseVerticalCollectionView view)
        {
            m_View = view;
            enableReordering = true;
        }

        public bool enableReordering { get; set; }

        public virtual bool CanStartDrag(IEnumerable<int> itemIndices)
        {
            return enableReordering;
        }

        public virtual StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false)
        {
            m_SelectedIndices ??= new List<int>();
            m_SelectedIndices.Clear();

            var title = string.Empty;
            if (itemIndices != null)
            {
                foreach (var index in itemIndices)
                {
                    m_SelectedIndices.Add(index);

                    if (skipText)
                        continue;

                    if (string.IsNullOrEmpty(title))
                    {
                        var label = m_View.GetRecycledItemFromIndex(index)?.rootElement.Q<Label>();
                        title = label != null ? label.text : $"Item {index}";
                    }
                    else
                    {
                        title = "<Multiple>";
                        skipText = true;
                    }
                }
            }

            m_SelectedIndices.Sort();

            return new StartDragArgs(title, m_View);
        }

        public abstract DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args);
        public abstract void OnDrop(IListDragAndDropArgs args);
    }
}
