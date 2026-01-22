// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class IssueLayout
    {
        public IssueCategory Category { get; set; }
        public PropertyDefinition[] Properties { get; set; }
        public bool IsHierarchy { get; set; } = false;

        public int DefaultGroupPropertyIndex
        {
            get
            {
                if (IsHierarchy)
                    return -1;
                return Array.FindIndex(Properties, p => p.IsDefaultGroup);
            }
        }

        /// <summary>
        /// Get the layout for a category
        /// </summary>
        /// <param name="category">The category to get the layout for</param>
        /// <returns>The IssueLayout for the specified category</returns>
        public static IssueLayout GetLayout(IssueCategory category)
        {
            if (category == IssueCategory.Metadata)
                return new IssueLayout {Category = IssueCategory.Metadata, Properties = Array.Empty<PropertyDefinition>() };

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Module)))
            {
                if (type.IsAbstract)
                    continue;
                var module = Activator.CreateInstance(type) as Module;

                foreach (var layout in module.SupportedLayouts)
                {
                    if (layout.Category == category)
                        return layout;
                }
            }
            return null;
        }
    }
}
