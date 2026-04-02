// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class TreeViewSelection
    {
        public List<string> groups;
        public List<string> selection;

        public TreeViewSelection()
        {
            groups = new List<string>();
            selection = new List<string>();
        }

        public TreeViewSelection(TreeViewSelection selection)
        {
            groups = new List<string>();
            this.selection = new List<string>();

            Set(selection);
        }

        public void SetAll(string[] names)
        {
            groups.Clear();
            var allIdentifier = new TreeItemIdentifier("All", TreeItemIdentifier.kAll);
            groups.Add(allIdentifier.nameWithIndex);

            selection.Clear();
            foreach (var nameWithIndex in names)
                if (nameWithIndex != allIdentifier.nameWithIndex)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (identifier.index != TreeItemIdentifier.kAll)
                        selection.Add(nameWithIndex);
                }
        }

        public void Set(string name)
        {
            groups.Clear();
            selection.Clear();
            selection.Add(name);
        }

        public void SetGroup(string groupName)
        {
            groups.Clear();
            selection.Clear();

            var allTreeViewSelection = new TreeItemIdentifier(groupName, TreeItemIdentifier.kAll);
            groups.Add(allTreeViewSelection.nameWithIndex);
        }

        public void Set(TreeViewSelection selection)
        {
            groups.Clear();
            this.selection.Clear();

            if (selection.groups != null)
                groups.AddRange(selection.groups);
            if (selection.selection != null)
                this.selection.AddRange(selection.selection);
        }

        public bool Contains(string name)
        {
            return selection.Contains(name);
        }

        public bool ContainsAny(string[] names)
        {
            foreach (string name in names)
            {
                if (selection.Contains(name))
                    return true;
            }
            return false;
        }

        public bool ContainsGroup(string groupName)
        {
            return groups.Contains(groupName);
        }

        // stephenm TODO - This seems wildly more complex than it needs to be... UNLESS assemblies can have sub-assemblies?
        // If that's the case, we need to test for that. Otherwise we need to strip a bunch of this complexity out.
        public string[] GetSelectedStrings(string[] names, bool summarize, bool removeWhitespace)
        {
            if (selection == null || selection.Count == 0)
            {
                if (summarize)
                {
                    return new[] { "None" };
                }

                return null;
            }

            // Count all items in a group
            var dict = new Dictionary<string, int>();
            var selectionDict = new Dictionary<string, int>();
            foreach (var nameWithIndex in names)
            {
                var identifier = new TreeItemIdentifier(nameWithIndex);
                if (identifier.index == TreeItemIdentifier.kAll)
                    continue;

                int count;
                if (dict.TryGetValue(identifier.name, out count))
                    dict[identifier.name] = count + 1;
                else
                    dict[identifier.name] = 1;

                selectionDict[identifier.name] = 0;
            }

            // Count all the items we have 'selected' in a group
            foreach (var name in selection)
            {
                var nameWithIndex = removeWhitespace ? name.Replace(" ", "") : name;
                var identifier = new TreeItemIdentifier(nameWithIndex);

                if (dict.ContainsKey(identifier.name) &&
                    selectionDict.ContainsKey(identifier.name) &&
                    identifier.index <= dict[identifier.name])
                    // Selected assembly valid and in the assembly list
                    // and also within the range of valid assemblies for this data set
                    selectionDict[identifier.name]++;
            }

            // Count all groups where we have 'selected all the items'
            var selectedCount = 0;
            foreach (var name in dict.Keys)
            {
                if (selectionDict[name] != dict[name])
                    continue;

                selectedCount++;
            }

            // If we've just added all the item names we have everything selected
            // Note we don't compare against the names array directly as this contains the 'all' versions
            if (summarize && selectedCount == dict.Keys.Count)
                return new[] { "All" };

            // Add all the individual items were we haven't already added the group
            var individualItems = new List<string>();
            foreach (var name in selectionDict.Keys)
            {
                var selectionCount = selectionDict[name];
                if (selectionCount <= 0)
                    continue;
                var itemCount = dict[name];
                if (itemCount == 1)
                    individualItems.Add(name);
                else if (selectionCount != itemCount)
                    individualItems.Add(string.Format("{0} ({1} of {2})", name, selectionCount, itemCount));
                else
                    individualItems.Add(string.Format("{0} (All)", name));
            }

            // Maintain alphabetical order
            individualItems.Sort(CompareUINames);

            return individualItems.ToArray();
        }

        int CompareUINames(string a, string b)
        {
            var aTokens = a.Split(':');
            var bTokens = b.Split(':');

            if (aTokens.Length > 1 && bTokens.Length > 1)
            {
                var firstName = aTokens[0].Trim();
                var secondName = bTokens[0].Trim();

                if (firstName == secondName)
                {
                    var firstNameIndex = aTokens[1].Trim();
                    var secondNameIndex = bTokens[1].Trim();

                    if (firstNameIndex == "All" && secondNameIndex != "All")
                        return -1;
                    if (firstNameIndex != "All" && secondNameIndex == "All")
                        return 1;

                    int aGroupIndex;
                    if (int.TryParse(firstNameIndex, out aGroupIndex))
                    {
                        int bGroupIndex;
                        if (int.TryParse(secondNameIndex, out bGroupIndex)) return aGroupIndex.CompareTo(bGroupIndex);
                    }
                }
            }

            return a.CompareTo(b);
        }
    }
}
