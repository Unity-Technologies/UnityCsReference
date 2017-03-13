// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // This is a custom nested, double linked list.  This provides flexibility to search quickly up and down
    // the list.  Particularly when parts of the list are expanded or not.
    public class ListItem
    {
        ///@TODO: Violation of unity convention.... m_ on member variables...

        ListItem parent;
        ListItem firstChild;
        ListItem lastChild;
        ListItem prev;
        ListItem next;

        Texture icon;
        string name;
        int indent;
        bool expanded;
        bool exclusive;
        bool dummy;
        bool hidden;
        bool accept;
        object item;
        string[] actions;
        int      identifier;

        public ListItem()
        {
            Clear();
            identifier = (int)(VCSProviderIdentifier.UnsetIdentifier);
        }

        ~ListItem()
        {
            Clear();
        }

        public Texture Icon
        {
            get
            {
                Asset asset = item as Asset;
                if (icon == null && asset != null)
                    return AssetDatabase.GetCachedIcon(asset.path);

                return icon;
            }
            set { icon = value; }
        }

        public int Identifier
        {
            get
            {
                if (identifier == (int)(VCSProviderIdentifier.UnsetIdentifier))
                {
                    identifier = Provider.GenerateID();
                }
                return identifier;
            }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Indent
        {
            get { return indent; }
            set { SetIntent(this, value); }
        }

        public object Item
        {
            get { return item; }
            set { item = value; }
        }

        public Asset Asset
        {
            get { return item as Asset; }
            set { item = value; }
        }

        public bool HasPath()
        {
            Asset a = item as Asset;
            return a != null && a.path != null;
        }

        public ChangeSet Change
        {
            get { return item as ChangeSet; }
            set { item = value; }
        }

        public bool Expanded
        {
            get { return expanded; }
            set { expanded = value; }
        }

        public bool Exclusive
        {
            get { return exclusive; }
            set { exclusive = value; }
        }

        public bool Dummy
        {
            get { return dummy; }
            set { dummy = value; }
        }

        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }

        public bool HasChildren
        {
            get { return FirstChild != null; }
        }

        public bool HasActions
        {
            get { return actions != null && actions.Length != 0; }
        }

        public string[] Actions
        {
            get { return actions; }
            set { actions = value; }
        }

        public bool CanExpand
        {
            get
            {
                // Asset asset = item as Asset;
                ChangeSet change = item as ChangeSet;
                //              return ((asset != null && asset.isFolder) ||
                //                      change != null ||
                //                      HasChildren);
                return (change != null || HasChildren);
            }
        }

        public bool CanAccept
        {
            get { return accept; }
            set { accept = value; }
        }

        public int OpenCount
        {
            get
            {
                if (!Expanded)
                    return 0;

                int count = 0;
                ListItem listItem = firstChild;

                while (listItem != null)
                {
                    if (!listItem.Hidden)
                    {
                        ++count;
                        count += listItem.OpenCount;
                    }

                    listItem = listItem.next;
                }

                return count;
            }
        }

        public int ChildCount
        {
            get
            {
                int count = 0;
                ListItem listItem = firstChild;

                while (listItem != null)
                {
                    ++count;
                    listItem = listItem.next;
                }

                return count;
            }
        }

        public ListItem Parent
        {
            get { return parent; }
        }

        public ListItem FirstChild
        {
            get { return firstChild; }
        }

        public ListItem LastChild
        {
            get { return lastChild; }
        }

        public ListItem Prev
        {
            get { return prev; }
        }

        public ListItem Next
        {
            get { return next; }
        }

        public ListItem PrevOpen
        {
            get
            {
                // Previous sibbling or its last open child
                ListItem enumChild = prev;

                while (enumChild != null)
                {
                    if (enumChild.lastChild == null || !enumChild.Expanded)
                        return enumChild;

                    enumChild = enumChild.lastChild;
                }

                // Move to parent as long as its not the root
                if (parent != null && parent.parent != null)
                    return parent;

                return null;
            }
        }

        public ListItem NextOpen
        {
            get
            {
                // Next child
                if (Expanded && firstChild != null)
                    return firstChild;

                // Next sibbling
                if (next != null)
                    return next;

                // Find a parent with a next
                ListItem enumParent = parent;

                while (enumParent != null)
                {
                    if (enumParent.Next != null)
                        return enumParent.Next;

                    enumParent = enumParent.parent;
                }

                return null;
            }
        }

        public ListItem PrevOpenSkip
        {
            get
            {
                ListItem listItem = PrevOpen;
                while (listItem != null && (listItem.Dummy || listItem.Hidden))
                    listItem = listItem.PrevOpen;

                return listItem;
            }
        }

        public ListItem NextOpenSkip
        {
            get
            {
                ListItem listItem = NextOpen;
                while (listItem != null && (listItem.Dummy || listItem.Hidden))
                    listItem = listItem.NextOpen;

                return listItem;
            }
        }

        public ListItem PrevOpenVisible
        {
            get
            {
                ListItem listItem = PrevOpen;
                while (listItem != null && listItem.Hidden)
                    listItem = listItem.PrevOpen;

                return listItem;
            }
        }

        public ListItem NextOpenVisible
        {
            get
            {
                ListItem listItem = NextOpen;
                while (listItem != null && listItem.Hidden)
                    listItem = listItem.NextOpen;

                return listItem;
            }
        }

        public bool IsChildOf(ListItem listItem)
        {
            ListItem it = Parent;

            while (it != null)
            {
                if (it == listItem)
                    return true;

                it = it.Parent;
            }

            return false;
        }

        public void Clear()
        {
            parent = null;
            firstChild = null;
            lastChild = null;
            prev = null;
            next = null;

            icon = null;
            name = string.Empty;
            indent = 0;
            expanded = false;
            exclusive = false;
            dummy = false;
            accept = false;
            item = null;
        }

        public void Add(ListItem listItem)
        {
            listItem.parent = this;
            listItem.next = null;
            listItem.prev = lastChild;

            // recursively update the indent
            listItem.Indent = indent + 1;

            if (firstChild == null)
                firstChild = listItem;

            if (lastChild != null)
                lastChild.next = listItem;

            lastChild = listItem;
        }

        public bool Remove(ListItem listItem)
        {
            if (listItem == null)
                return false;

            // Can only remove children of this item
            if (listItem.parent != this)
                return false;

            if (listItem == firstChild)
                firstChild = listItem.next;

            if (listItem == lastChild)
                lastChild = listItem.prev;

            if (listItem.prev != null)
                listItem.prev.next = listItem.next;

            if (listItem.next != null)
                listItem.next.prev = listItem.prev;

            listItem.parent = null;
            listItem.prev = null;
            listItem.next = null;

            return true;
        }

        public void RemoveAll()
        {
            ListItem en = firstChild;
            while (en != null)
            {
                en.parent = null;
                en = en.next;
            }

            firstChild = null;
            lastChild = null;
        }

        public ListItem FindWithIdentifierRecurse(int inIdentifier)
        {
            if (Identifier == inIdentifier)
                return this;

            ListItem listItem = firstChild;
            while (listItem != null)
            {
                ListItem found = listItem.FindWithIdentifierRecurse(inIdentifier);
                if (found != null)
                    return found;

                listItem = listItem.next;
            }
            return null;
        }

        void SetIntent(ListItem listItem, int indent)
        {
            listItem.indent = indent;

            // Update children
            ListItem en = listItem.FirstChild;
            while (en != null)
            {
                SetIntent(en, indent + 1);
                en = en.Next;
            }
        }
    }
}
