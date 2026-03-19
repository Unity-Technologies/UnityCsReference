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
        private static Type s_FoldoutType;
        private static readonly string s_InspectorElementUssClassName = "unity-inspector-element";

        // To help with code stripping
        public static void SetFoldoutType(Type foldoutType)
        {
            s_FoldoutType = foldoutType;
        }


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
            if (s_FoldoutType != null && element.parent != null)
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

        internal static void AssignInspectorStyleIfNecessary(this VisualElement element, string classNameToEnable)
        {
            var inspector = element.GetFirstAncestorWhere((i) => i.ClassListContains(s_InspectorElementUssClassName));
            element.EnableInClassList(classNameToEnable, inspector != null);
        }
    }
}
