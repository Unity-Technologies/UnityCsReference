// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class TextFilter : IIssueFilter
    {
        public bool ignoreCase = true;
        public bool searchDependencies = false;
        public string searchString = string.Empty;

        readonly int[] searchablePropertyIndices;

        public TextFilter(PropertyDefinition[] propertyDefinitions = null)
        {
            var indices = new List<int>();
            if (propertyDefinitions != null)
            {
                foreach (var propertyDefinition in propertyDefinitions)
                {
                    if (propertyDefinition.Format != PropertyFormat.String)
                        continue;
                    if (!PropertyTypeUtil.IsCustom(propertyDefinition.Type))
                        continue;
                    indices.Add(PropertyTypeUtil.ToCustomIndex(propertyDefinition.Type));
                }
            }
            searchablePropertyIndices = indices.ToArray();
        }

        public bool Match(ReportItem issue)
        {
            if (string.IsNullOrEmpty(searchString))
                return true;

            // return true if the issue matches the any of the following string search criteria
            if (MatchesSearch(issue.Description))
                return true;

            if (MatchesSearch(issue.Filename))
                return true;

            foreach (var customPropertyIndex in searchablePropertyIndices)
            {
                if (MatchesSearch(issue.GetCustomProperty(customPropertyIndex)))
                    return true;
            }

            var dependencies = issue.Dependencies;
            if (dependencies != null)
            {
                if (MatchesSearch(dependencies, searchDependencies))
                    return true;
            }

            // no string match
            return false;
        }

        bool MatchesSearch(string text)
        {
            return text.IndexOf(searchString, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) >= 0;
        }

        bool MatchesSearch(DependencyNode node, bool recursive)
        {
            if (node == null)
                return false;

            var callTreeNode = node as CallTreeNode;
            if (callTreeNode != null)
            {
                if (MatchesSearch(callTreeNode.PrettyTypeName) || MatchesSearch(callTreeNode.PrettyMethodName))
                    return true;
            }
            if (recursive)
                for (var i = 0; i < node.NumChildren; i++)
                {
                    if (MatchesSearch(node.GetChild(i), true))
                        return true;
                }

            return false;
        }
    }
}
