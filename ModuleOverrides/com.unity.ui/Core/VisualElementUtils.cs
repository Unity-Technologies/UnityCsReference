// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class VisualElementUtils
    {
        private static readonly HashSet<string> s_usedNames = new HashSet<string>();
        private static readonly Type s_FoldoutType = typeof(Foldout);

        public static string GetUniqueName(string nameBase)
        {
            string name = nameBase;
            int counter = 2;
            while (s_usedNames.Contains(name))
            {
                name = nameBase + counter;
                counter++;
            }
            s_usedNames.Add(name);
            return name;
        }

        /// <summary>
        /// Computes the depth of the visual element, i.e. the number of parenting foldouts.
        /// </summary>
        /// <param name="element">The visual element to check.</param>
        /// <returns>The foldout depth.</returns>
        internal static int GetFoldoutDepth(this VisualElement element)
        {
            var depth = 0;
            if (element.parent != null)
            {
                var currentParent = element.parent;
                while (currentParent != null)
                {
                    if (s_FoldoutType.IsAssignableFrom(currentParent.GetType()))
                    {
                        depth++;
                    }
                    currentParent = currentParent.parent;
                }
            }

            return depth;
        }

        /// <summary>
        /// Computes the depth of the visual element including list views, i.e. the number of parenting foldouts and list views.
        /// </summary>
        /// <param name="element">The visual element to check.</param>
        /// <returns>The list and foldout depth.</returns>
        internal static int GetListAndFoldoutDepth(this VisualElement element)
        {
            var depth = 0;
            if (element.hierarchy.parent != null)
            {
                var currentParent = element.hierarchy.parent;
                while (currentParent != null)
                {
                    if (currentParent is Foldout || currentParent is ListView)
                    {
                        depth++;
                    }

                    currentParent = currentParent.hierarchy.parent;
                }
            }
            return depth;
        }
    }
}
