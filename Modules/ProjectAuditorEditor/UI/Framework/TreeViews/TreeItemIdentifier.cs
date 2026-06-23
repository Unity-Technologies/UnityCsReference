// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal struct TreeItemIdentifier
    {
        public string nameWithIndex { get; private set; }

        public string name { get; private set; }

        // stephenm TODO - Pretty sure this can go. Assemblies don't have indeces. I think the most we'll need is a flag
        // to say whether this is the "All" TreeItemIdentifier (i.e. (nameWithIndex == "All"))
        public int index { get; private set; }

        public static readonly int kAll = -1;
        public static readonly int kSingle = 0;

        public TreeItemIdentifier(string _name, int _index)
        {
            name = _name;
            index = _index;
            if (index == kAll)
                nameWithIndex = string.Format("All:{1}", index, name);
            else
                nameWithIndex = string.Format("{0}:{1}", index, name);
        }

        public TreeItemIdentifier(TreeItemIdentifier treeItemIdentifier)
        {
            name = treeItemIdentifier.name;
            index = treeItemIdentifier.index;
            nameWithIndex = treeItemIdentifier.nameWithIndex;
        }

        public TreeItemIdentifier(string _nameWithIndex)
        {
            // stephenm TODO - Pretty sure this can go. Assembly names don't have a foo:N (or N:foo?) naming convention like threads do.
            // So index should probably always be treated as 0 (sorry, "kSingle")
            nameWithIndex = _nameWithIndex;

            var tokens = nameWithIndex.Split(':');
            if (tokens.Length >= 2)
            {
                name = tokens[1];
                var indexString = tokens[0];
                if (indexString == "All")
                {
                    index = kAll;
                }
                else
                {
                    int intValue;
                    if (int.TryParse(tokens[0], out intValue))
                        index = intValue;
                    else
                        index = kSingle;
                }
            }
            else
            {
                index = kSingle;
                name = nameWithIndex;
            }
        }

        void UpdateAssemblyNameWithIndex()
        {
            if (index == kAll)
                nameWithIndex = string.Format("All:{1}", index, name);
            else
                nameWithIndex = string.Format("{0}:{1}", index, name);
        }

        public void SetName(string newName)
        {
            name = newName;
            UpdateAssemblyNameWithIndex();
        }

        public void SetIndex(int newIndex)
        {
            index = newIndex;
            UpdateAssemblyNameWithIndex();
        }

        public void SetAll()
        {
            SetIndex(kAll);
        }
    }

    class SelectionWindowTreeViewItem : TreeViewItem
    {
        public readonly TreeItemIdentifier TreeItemIdentifier;

        public SelectionWindowTreeViewItem(int id, int depth, string displayName, TreeItemIdentifier treeItemIdentifier)
            : base(id, depth, displayName)
        {
            TreeItemIdentifier = treeItemIdentifier;
        }
    }
}
