using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.UIElements
{
    internal static class UIDocumentHierarchyUtil
    {
        internal static UIDocumentHierarchicalIndexComparer indexComparer = new UIDocumentHierarchicalIndexComparer();

        internal static int FindHierarchicalSortedIndex(SortedDictionary<UIDocumentHierarchicalIndex, UIDocument> children,
            UIDocument child)
        {
            int index = 0;
            foreach (var sibling in children.Values)
            {
                if (sibling == child)
                {
                    return index;
                }

                if (sibling.rootVisualElement != null && sibling.rootVisualElement.parent != null)
                {
                    index++;
                }
            }

            return index;
        }

        internal static void SetHierarchicalIndex(Transform childTransform, Transform directParentTransform, Transform mainParentTransform, out UIDocumentHierarchicalIndex hierarchicalIndex)
        {
            if (mainParentTransform == null || childTransform == null)
            {
                hierarchicalIndex.pathToParent = null;
                return;
            }

            if (directParentTransform == mainParentTransform)
            {
                hierarchicalIndex.pathToParent = new int[] { childTransform.GetSiblingIndex() };
            }
            else
            {
                List<int> pathToParent = new List<int>();
                while (mainParentTransform != childTransform && childTransform != null)
                {
                    pathToParent.Add(childTransform.GetSiblingIndex());
                    childTransform = childTransform.parent;
                }

                pathToParent.Reverse();
                hierarchicalIndex.pathToParent = pathToParent.ToArray();
            }
        }

        internal static void SetGlobalIndex(Transform objectTransform, Transform directParentTransform,  out UIDocumentHierarchicalIndex globalIndex)
        {
            if (objectTransform == null)
            {
                globalIndex.pathToParent = null;
                return;
            }

            if (directParentTransform == null)
            {
                globalIndex.pathToParent = new int[] { objectTransform.GetSiblingIndex() };
            }
            else
            {
                List<int> pathToParent = new List<int>() { objectTransform.GetSiblingIndex() };
                while (directParentTransform != null)
                {
                    pathToParent.Add(directParentTransform.GetSiblingIndex());
                    directParentTransform = directParentTransform.parent;
                }

                pathToParent.Reverse();
                globalIndex.pathToParent = pathToParent.ToArray();
            }
        }
    }

    internal class UIDocumentHierarchicalIndexComparer : IComparer<UIDocumentHierarchicalIndex>
    {
        public int Compare(UIDocumentHierarchicalIndex x, UIDocumentHierarchicalIndex y)
        {
            return x.CompareTo(y);
        }
    }

    internal struct UIDocumentHierarchicalIndex : IComparable<UIDocumentHierarchicalIndex>
    {
        internal int[] pathToParent;

        public int CompareTo(UIDocumentHierarchicalIndex other)
        {
            // Safety checks
            if (pathToParent == null)
            {
                if (other.pathToParent == null)
                {
                    return 0;
                }

                return 1;
            }
            if (other.pathToParent == null) // we know pathToParent != null
            {
                return -1;
            }

            int myLength = pathToParent.Length;
            int otherLength = other.pathToParent.Length;
            for (int i = 0; i < myLength && i < otherLength; ++i)
            {
                if (pathToParent[i] < other.pathToParent[i])
                {
                    return -1;
                }

                if (pathToParent[i] > other.pathToParent[i])
                {
                    return 1;
                }
            }

            if (myLength > otherLength)
            {
                return 1;
            }
            else if (myLength < otherLength)
            {
                return -1;
            }

            return 0;
        }

        public override string ToString()
        {
            StringBuilder toString = new StringBuilder("pathToParent = [");

            if (pathToParent != null)
            {
                int count = pathToParent.Length;
                for (int i = 0; i < count; ++i)
                {
                    toString.Append(pathToParent[i]);

                    if (i < count - 1)
                    {
                        toString.Append(", ");
                    }
                }
            }

            toString.Append("]");

            return toString.ToString();
        }
    }
}
