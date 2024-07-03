// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

namespace UnityEditor.ShaderKeywordFilter
{
    // An internal constraint state. This is used for tracking which constraints affect which data field/attribute.
    // Provides the functionality to check whether the constraint state of the current pass makes a filter rule active or inactive.
    // Note: Don't mix up with struct ConstraintState which is the interop struct for getting the state from C++.
    internal class Constraints
    {
        internal Constraints(Constraints other)
        {
            m_TagAttributes = new List<TagConstraintAttribute>(other.m_TagAttributes);
            m_GraphicsAPIAttributes = new List<GraphicsAPIConstraintAttribute>(other.m_GraphicsAPIAttributes);
        }

        internal Constraints(TagConstraintAttribute[] tagConstraints, GraphicsAPIConstraintAttribute[] graphicsAPIConstraints)
        {
            m_TagAttributes = new List<TagConstraintAttribute>(tagConstraints);
            m_GraphicsAPIAttributes = new List<GraphicsAPIConstraintAttribute>(graphicsAPIConstraints);
        }

        internal void AddParentConstraints(Constraints parent)
        {
            m_TagAttributes.InsertRange(0, parent.m_TagAttributes);
            m_GraphicsAPIAttributes.InsertRange(0, parent.m_GraphicsAPIAttributes);
        }

        internal List<TagConstraintAttribute> TagConstraints
        {
            get => m_TagAttributes;
        }

        internal List<GraphicsAPIConstraintAttribute> GraphicsAPIConstraints
        {
            get => m_GraphicsAPIAttributes;
        }

        internal bool ShouldApplyFilterRules(ConstraintState state)
        {
            if (state.graphicsAPIs != null)
            {
                bool gfxAPIMismatch = false;
                foreach (var gfxAPIConstraint in m_GraphicsAPIAttributes)
                {
                    foreach (var api in state.graphicsAPIs)
                    {
                        gfxAPIMismatch |= !gfxAPIConstraint.ShouldApplyRules(api); // can't filter if any of the APIs must be included (DX11/DX12 shared compiler platform)
                    }
                }
                if (gfxAPIMismatch)
                    return false;
            }

            if (state.tags != null)
            {
                bool tagMismatch = false;
                foreach (var tagConstraint in m_TagAttributes)
                {
                    tagMismatch |= !tagConstraint.ShouldApplyRules(state.tags);
                }

                if (tagMismatch)
                    return false;
            }

            return true;
        }

        List<TagConstraintAttribute> m_TagAttributes;
        List<GraphicsAPIConstraintAttribute> m_GraphicsAPIAttributes;
    }

    // Represents a single filter attribute in the settings tree structure.
    // Tracks the field it's attached to and all the accumulated constraints that affect it.
    internal struct FilterData
    {
        internal FilterData(string fieldName, FilterAttribute attribute, Constraints constraints, object value)
        {
            this.fieldName = fieldName;
            this.attribute = attribute;
            this.constraints = constraints;
            this.value = value;
        }

        internal string fieldName;
        internal FilterAttribute attribute;
        internal Constraints constraints;
        internal object value;
    }

    internal struct FilterStats
    {
        internal void CountActionUnresolved(FilterAction a)
        {
            switch (a)
            {
                case FilterAction.Select:
                    ++m_NumberOfSelectKeywords;
                    break;
                case FilterAction.Remove:
                    ++m_NumberOfRemoveKeywords;
                    break;
                case FilterAction.SelectOrRemove:
                    ++m_NumberOfSelectKeywords;
                    ++m_NumberOfRemoveKeywords;
                    break;
            }

            // Always count against the total number of filter rules
            ++m_NumberOfRules;
        }

        internal void CountActionResolved(FilterAction a)
        {
            switch (a)
            {
                case FilterAction.Select:
                    ++m_NumberOfSelectKeywordsResolved;
                    break;
                case FilterAction.Remove:
                    ++m_NumberOfRemoveKeywordsResolved;
                    break;
                case FilterAction.SelectOrRemove:
                    // Resolved actions cannot be both
                    break;
            }
        }

        internal UInt32 m_NumberOfRules;                  // Total number of filter rules for a settings node
        internal UInt32 m_NumberOfRemoveKeywords;         // Total number of remove actions (includes select or remove)
        internal UInt32 m_NumberOfSelectKeywords;         // Total number of select actions (includes select or remove)
        internal UInt32 m_NumberOfRemoveKeywordsResolved; // Number of remove actions based on current setting values
        internal UInt32 m_NumberOfSelectKeywordsResolved; // Number of select actions based on current setting values
    }

    // A single instance of keyword name/FilterAction pair. Used as the output format when flattening the settings tree
    // to an array form with GetVariantArray().
    internal struct FilterRule
    {
        internal FilterRule(string keywordName, bool withEmptyKeyword, FilterAction action, string resolutionMessage)
        {
            this.keywordName = keywordName;
            this.withEmptyKeyword = withEmptyKeyword;
            this.action = action;
            this.resolutionMessage = resolutionMessage;
        }

        internal string keywordName;
        internal bool withEmptyKeyword;
        internal FilterAction action;
        internal string resolutionMessage;
    }

    // SettingsNode represents a node in the settings tree, containing filter data and child nodes.
    // Static method GatherFilterData() is used for constructing SettingsNode tree from a type tree.
    internal class SettingsNode
    {
        internal SettingsNode(string name)
        {
            m_Name = name;
            m_Children = new List<SettingsNode>();
            m_FilterData = new List<FilterData>();
        }

        private static Constraints GetConstraintAttributes(ICustomAttributeProvider ap)
        {
            var tagConstraints = ap.GetCustomAttributes(typeof(TagConstraintAttribute), false);
            var graphicsAPIConstraints = ap.GetCustomAttributes(typeof(GraphicsAPIConstraintAttribute), false);
            if (tagConstraints.Length > 0 || graphicsAPIConstraints.Length > 0)
            {
                return new Constraints((TagConstraintAttribute[])tagConstraints, (GraphicsAPIConstraintAttribute[])graphicsAPIConstraints);
            }

            return null;
        }

        // Traverses through the type tree of the given object to find filter attributes and creates
        // settings tree structure out of it.
        // Returns null if no filter attributes were found.
        // This method is called recursively for the tree traversing purposes.
        internal static SettingsNode GatherFilterData(string nodeName, object containerObject, HashSet<object> visited, Constraints parentConstraints = null)
        {
            SettingsNode node = null; // defer construction to when filter data is actually found
            if (containerObject == null)
                return node;

            if (!visited.Add(containerObject))
                return node;

            var containerConstraints = GetConstraintAttributes(containerObject.GetType());
            if (parentConstraints != null)
            {
                // Merge constraints with the parent
                if (containerConstraints == null)
                    containerConstraints = new Constraints(parentConstraints);
                else
                    containerConstraints.AddParentConstraints(parentConstraints);
            }

            // Go through all fields that could potentially contain filter attributes (directly or through children)
            var fields = containerObject.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
            foreach (var f in fields)
            {
                bool isConst = f.IsLiteral && !f.IsInitOnly;

                // Only public fields, constants and private fields with [SerializeField] attribute are accepted sources
                if (!f.IsPublic && !isConst && f.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                    continue;

                // Drop deprecated fields
                if (f.GetCustomAttributes(typeof(System.ObsoleteAttribute), false).Length != 0)
                    continue;

                var value = f.GetValue(containerObject);
                if (value == null)
                    continue;

                Type type = value.GetType();

                // Enumerable (array/list) is a potential branch in the settings tree
                if (value is IEnumerable)
                {
                    var enumerable = value as IEnumerable;

                    // UUM-72309 - A crash occured due to a third party package throwing when accessing its enumerator.
                    // This try/catch should prevent such issues and log an error.
                    try
                    {
                        bool hasDifferentChildren = (node != null) && (node.m_Children.Count > 0);
                        foreach (var e in enumerable)
                        {
                            var childVisited = new HashSet<object>(visited);
                            SettingsNode newBranch = GatherFilterData(f.Name, e, childVisited, containerConstraints);
                            if (newBranch != null)
                            {
                                if (node == null)
                                    node = new SettingsNode(nodeName);

                                if (hasDifferentChildren)
                                {
                                    Debug.LogError("ShaderKeywordFilter attributes cannot be placed on a settings tree with multiple different branches on the same level.");
                                }
                                else
                                {
                                    node.m_Children.Add(newBranch);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError($"An error occured while processing ShaderKeywordFilter attributes; Reading node: {nodeName} and field: {f.Name}\n {ex.Message}");
                    }
                }
                else if (!type.IsValueType || (type.IsValueType && !type.IsPrimitive && !type.IsEnum)) // class or struct
                {
                    var childVisited = new HashSet<object>(visited);
                    var nestedNode = GatherFilterData(f.Name, value, childVisited, containerConstraints);
                    if (nestedNode != null)
                    {
                        if (node == null)
                            node = new SettingsNode(nodeName);

                        node.m_FilterData.AddRange(nestedNode.m_FilterData);
                        foreach (var fd in nestedNode.m_FilterData)
                        {
                            node.m_FilterStats.CountActionUnresolved(fd.attribute.Action);
                        }

                        if (node.m_Children.Count > 0 && nestedNode.m_Children.Count > 0)
                        {
                            Debug.LogError("ShaderKeywordFilter attributes cannot be placed on a settings tree with multiple different branches on the same level.");
                        }
                        else
                        {
                            node.m_Children.AddRange(nestedNode.m_Children);
                        }
                    }
                }
                else // plain data fields
                {
                    var constraints = GetConstraintAttributes(f);
                    if (containerConstraints != null)
                    {
                        // Merge constraints with parent. Each field sees all the constraints from the root up to it.
                        if (constraints != null)
                            constraints.AddParentConstraints(containerConstraints);
                        else
                            constraints = new Constraints(containerConstraints);
                    }

                    var attributes = f.GetCustomAttributes(typeof(FilterAttribute), false);
                    foreach (var a in attributes)
                    {
                        if (a is FilterAttribute)
                        {
                            if (node == null)
                                node = new SettingsNode(nodeName);

                            var fa = a as FilterAttribute;
                            node.m_FilterData.Add(new FilterData(f.Name, fa, constraints, value));
                            node.m_FilterStats.CountActionUnresolved(fa.Action);
                        }
                    }
                }
            }

            return node;
        }

        // Recursively traversing through the settings tree, resolving the rules on its way from
        // the root to the leaves. When a leaf is encountered, a new SettingsVariant is added to the list.
        // The filter rule resolving happens from root to leaf with this logic:
        //  * Whatever rule for a specific keyword is encountered first takes precedence by default.
        //  * Later encountered rules are ignored, unless the attribute explicitly asks for override.
        //    In that case this override rule will take precedence.
        internal void GetVariantArray(ConstraintState constraintState, List<SettingsVariant> variants, List<FilterRule> parentRules = null)
        {
            // Grab a copy of the rules resolved so far. We don't want to modify the parent data
            // as siblings of this node could resolve things differently.
            var currentRules = parentRules != null ? new List<FilterRule>(parentRules) : new List<FilterRule>();
            foreach(var filter in m_FilterData)
            {
                // Skip the filter if constraint rules require so
                if (filter.constraints != null)
                {
                    if(!filter.constraints.ShouldApplyFilterRules(constraintState))
                        continue;
                }

                FilterAction filterAction;
                bool isActive = filter.attribute.GetActiveFilterAction(filter.value, out filterAction);

                // Early out for inactive filter attributes.
                if (!isActive)
                {
                    filter.attribute.GetFormattedResolutionMessageInactive(filter.value);
                    continue;
                }

                bool overrideRule = filter.attribute.RulePrecedence == FilterAttribute.Precedence.Override;
                bool addRule = true;
                bool affectsEmptyKw = false;

                // check if the attribute affects "empty keyword" case
                foreach (var keywordName in filter.attribute.KeywordNames)
                {
                    if (keywordName.Length == 0)
                        affectsEmptyKw = true;
                }

                foreach (var keywordName in filter.attribute.KeywordNames)
                {
                    if (keywordName.Length == 0)
                        continue;

                    for (int i = 0; i < currentRules.Count; ++i)
                    {
                        if (currentRules[i].keywordName.Equals(keywordName))
                        {
                            if (overrideRule)
                            {
                                var resolutionMessage = filter.attribute.GetFormattedResolutionMessageOverride(filter.value, currentRules[i]);
                                currentRules[i] = new FilterRule(keywordName, affectsEmptyKw, filterAction, resolutionMessage);
                            }

                            addRule = false;
                            break;
                        }
                    }
                    if (addRule)
                    {
                        var resolutionMessage = filter.attribute.GetFormattedResolutionMessageActive(filter.value);
                        currentRules.Add(new FilterRule(keywordName, affectsEmptyKw, filterAction, resolutionMessage));
                        m_FilterStats.CountActionResolved(filterAction);
                    }
                }
            }

            if (m_Children.Count > 0)
            {
                foreach (var child in m_Children)
                {
                    child.GetVariantArray(constraintState, variants, currentRules);
                }
            }
            else
            {
                variants.Add(new SettingsVariant(currentRules));
            }
        }

        internal string Name
        {
            get => m_Name;
        }

        internal List<SettingsNode> Children
        {
            get => m_Children;
        }

        internal List<FilterData> FilterData
        {
            get => m_FilterData;
        }

        internal FilterStats FilterStats
        {
            get => m_FilterStats;
        }

        string m_Name;
        List<SettingsNode> m_Children;
        List<FilterData> m_FilterData;
        FilterStats m_FilterStats;
    }
}
