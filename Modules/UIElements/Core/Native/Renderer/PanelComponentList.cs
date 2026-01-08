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

        bool IsHierarchyOrderGreater(IPanelComponent a, IPanelComponent b)
        {
            if (a is PanelRenderer aRenderer && b is PanelRenderer bRenderer)
                return aRenderer.hierarchyOrder > bRenderer.hierarchyOrder;

            // UIDocuments are not compared by hierarchy
            return false;
        }

        internal void RemoveFromListAndFromVisualTree(IPanelComponent panelComponent)
        {
            m_AttachedPanelComponents.Remove(panelComponent);
            PanelComponentUtils.GetRootVisualElement(panelComponent)?.RemoveFromHierarchy();
        }

        internal void AddToListAndToVisualTree(IPanelComponent panelComponent, VisualElement visualTree, bool ignoreContentContainer, int firstChildIndex = 0)
        {
            int index = 0;
            foreach (var sibling in m_AttachedPanelComponents)
            {
                if ((panelComponent.sortingOrder > sibling.sortingOrder) ||
                    ((panelComponent.sortingOrder == sibling.sortingOrder) && IsHierarchyOrderGreater(panelComponent, sibling)))
                {
                    index++;
                }
                else
                    break;
            }

            if (index < m_AttachedPanelComponents.Count)
            {
                m_AttachedPanelComponents.Insert(index, panelComponent);

                if (visualTree == null || PanelComponentUtils.GetRootVisualElement(panelComponent) == null)
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
                visualTree.Insert(insertionIndex, PanelComponentUtils.GetRootVisualElement(panelComponent), ignoreContentContainer);
            }
            else
            {
                visualTree.Add(PanelComponentUtils.GetRootVisualElement(panelComponent), ignoreContentContainer);
            }
        }
    }
}
