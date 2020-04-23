using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    // Utility class to abstract away the concerne of traversing the visual tree
    internal abstract class HierarchyTraversal
    {
        public virtual void Traverse(VisualElement element)
        {
            TraverseRecursive(element, 0);
        }

        // Subclasses are responsible for calling Recurse() on the element
        public abstract void TraverseRecursive(VisualElement element, int depth);

        protected void Recurse(VisualElement element, int depth)
        {
            int i = 0;

            while (i < element.hierarchy.childCount) //here we can't cache childCount as user code could change the hierarchy
            {
                var child = element.hierarchy[i];
                TraverseRecursive(child, depth + 1);

                // if the child has been moved to another parent, which happens when its parent has changed, then do not increment the iterator
                if (child.hierarchy.parent != element)
                    continue;
                i++;
            }
        }
    }
}
