// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class PanelComponentList
    {
        internal List<IPanelComponent> m_AttachedPanelComponents = new();

        internal void RemoveFromListAndFromVisualTree(IPanelComponent panelComponent)
        {
            m_AttachedPanelComponents.Remove(panelComponent);
            panelComponent.GetRootVisualElement()?.RemoveFromHierarchy();
        }

        internal void AddToListAndToVisualTree(IPanelComponent panelComponent, VisualElement visualTree, bool ignoreContentContainer, int firstChildIndex = 0)
        {
            int index = 0;
            foreach (var sibling in m_AttachedPanelComponents)
            {
                if (panelComponent.sortingOrder > sibling.sortingOrder)
                {
                    index++;
                    continue;
                }

                if (panelComponent.sortingOrder < sibling.sortingOrder)
                    break;

                // They're the same value, compare their count (UIDocuments created first show up first).
                if (panelComponent.creationIndex > sibling.creationIndex)
                {
                    index++;
                    continue;
                }

                break;
            }

            if (index < m_AttachedPanelComponents.Count)
            {
                m_AttachedPanelComponents.Insert(index, panelComponent);

                if (visualTree == null || panelComponent.GetRootVisualElement() == null)
                    return;

                int childCount = visualTree.ChildCount(ignoreContentContainer);
                if (index > childCount)
                    index = childCount;
            }
            else
            {
                // Add in the end.
                m_AttachedPanelComponents.Add(panelComponent);
            }

            if (visualTree == null)
                return;

            int insertionIndex = index + firstChildIndex;
            if (insertionIndex < visualTree.ChildCount(ignoreContentContainer))
            {
                visualTree.Insert(insertionIndex, panelComponent.GetRootVisualElement(), ignoreContentContainer);
            }
            else
            {
                visualTree.Add(panelComponent.GetRootVisualElement(), ignoreContentContainer);
            }
        }
    }
}
