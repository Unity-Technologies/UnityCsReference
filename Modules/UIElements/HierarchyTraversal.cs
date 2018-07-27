// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
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

            while (i < element.shadow.childCount)
            {
                var child = element.shadow[i];
                TraverseRecursive(child, depth + 1);

                // if the child has been moved to another parent, which happens when its parent has changed, then do not increment the iterator
                if (child.shadow.parent != element)
                    continue;
                i++;
            }
        }
    }
}
