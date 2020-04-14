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
        ListItem m_Parent;
        ListItem m_FirstChild;
        ListItem m_LastChild;
        ListItem m_Prev;
        ListItem m_Next;

        Texture m_Icon;
        string m_Name;
        int m_Indent;
        bool m_Expanded;
        bool m_Exclusive;
        bool m_Dummy;
        bool m_Hidden;
        bool m_Accept;
        object m_Item;
        string[] m_Actions;
        int m_Identifier;

        static Texture2D s_DefaultIcon = null;

        public ListItem()
        {
            Clear();
            m_Identifier = (int)(VCSProviderIdentifier.UnsetIdentifier);
        }

        ~ListItem()
        {
            Clear();
        }

        public Texture Icon
        {
            get
            {
                if (s_DefaultIcon == null)
                {
                    s_DefaultIcon = EditorGUIUtility.LoadIcon("VersionControl/File");
                    s_DefaultIcon.hideFlags = HideFlags.HideAndDontSave;
                }

                Asset asset = m_Item as Asset;
                if (m_Icon == null && asset != null)
                {
                    if (asset.isInCurrentProject)
                    {
                        m_Icon = AssetDatabase.GetCachedIcon(asset.path);
                    }
                    else
                    {
                        m_Icon = s_DefaultIcon;
                    }
                }

                return m_Icon;
            }
            set { m_Icon = value; }
        }

        public int Identifier
        {
            get
            {
                if (m_Identifier == (int)(VCSProviderIdentifier.UnsetIdentifier))
                {
                    m_Identifier = Provider.GenerateID();
                }
                return m_Identifier;
            }
        }

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public int Indent
        {
            get { return m_Indent; }
            set { SetIntent(this, value); }
        }

        public object Item
        {
            get { return m_Item; }
            set { m_Item = value; }
        }

        public Asset Asset
        {
            get { return m_Item as Asset; }
            set { m_Item = value; }
        }

        public bool HasPath()
        {
            Asset a = m_Item as Asset;
            return a != null && a.path != null;
        }

        public ChangeSet Change
        {
            get { return m_Item as ChangeSet; }
            set { m_Item = value; }
        }

        public bool Expanded
        {
            get { return m_Expanded; }
            set { m_Expanded = value; }
        }

        public bool Exclusive
        {
            get { return m_Exclusive; }
            set { m_Exclusive = value; }
        }

        public bool Dummy
        {
            get { return m_Dummy; }
            set { m_Dummy = value; }
        }

        public bool Hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public bool HasChildren
        {
            get { return FirstChild != null; }
        }

        public bool HasActions
        {
            get { return m_Actions != null && m_Actions.Length != 0; }
        }

        public string[] Actions
        {
            get { return m_Actions; }
            set { m_Actions = value; }
        }

        public bool CanExpand
        {
            get
            {
                // Asset asset = item as Asset;
                ChangeSet change = m_Item as ChangeSet;
                //              return ((asset != null && asset.isFolder) ||
                //                      change != null ||
                //                      HasChildren);
                return (change != null || HasChildren);
            }
        }

        public bool CanAccept
        {
            get { return m_Accept; }
            set { m_Accept = value; }
        }

        public int OpenCount
        {
            get
            {
                if (!Expanded)
                    return 0;

                int count = 0;
                ListItem listItem = m_FirstChild;

                while (listItem != null)
                {
                    if (!listItem.Hidden)
                    {
                        ++count;
                        count += listItem.OpenCount;
                    }

                    listItem = listItem.m_Next;
                }

                return count;
            }
        }

        public int ChildCount
        {
            get
            {
                int count = 0;
                ListItem listItem = m_FirstChild;

                while (listItem != null)
                {
                    ++count;
                    listItem = listItem.m_Next;
                }

                return count;
            }
        }

        internal int VisibleChildCount
        {
            get
            {
                if (!m_Expanded) return 0;

                int count = 0;
                ListItem listItem = m_FirstChild;

                while (listItem != null)
                {
                    if (!listItem.Hidden && !listItem.Dummy) ++count;
                    listItem = listItem.m_Next;
                }

                return count;
            }
        }

        internal int VisibleItemCount
        {
            get
            {
                if (!m_Expanded) return 0;

                int count = 0;
                ListItem listItem = m_FirstChild;

                while (listItem != null)
                {
                    if (!listItem.Hidden) ++count;
                    listItem = listItem.m_Next;
                }

                return count;
            }
        }

        public ListItem Parent
        {
            get { return m_Parent; }
        }

        public ListItem FirstChild
        {
            get { return m_FirstChild; }
        }

        public ListItem LastChild
        {
            get { return m_LastChild; }
        }

        public ListItem Prev
        {
            get { return m_Prev; }
        }

        public ListItem Next
        {
            get { return m_Next; }
        }

        public ListItem PrevOpen
        {
            get
            {
                // Previous sibbling or its last open child
                ListItem enumChild = m_Prev;

                while (enumChild != null)
                {
                    if (enumChild.m_LastChild == null || !enumChild.Expanded)
                        return enumChild;

                    enumChild = enumChild.m_LastChild;
                }

                // Move to parent as long as its not the root
                if (m_Parent != null && m_Parent.m_Parent != null)
                    return m_Parent;

                return null;
            }
        }

        public ListItem NextOpen
        {
            get
            {
                // Next child
                if (Expanded && m_FirstChild != null)
                    return m_FirstChild;

                // Next sibbling
                if (m_Next != null)
                    return m_Next;

                // Find a parent with a next
                ListItem enumParent = m_Parent;

                while (enumParent != null)
                {
                    if (enumParent.Next != null)
                        return enumParent.Next;

                    enumParent = enumParent.m_Parent;
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
            m_Parent = null;
            m_FirstChild = null;
            m_LastChild = null;
            m_Prev = null;
            m_Next = null;

            m_Icon = null;
            m_Name = string.Empty;
            m_Indent = 0;
            m_Expanded = false;
            m_Exclusive = false;
            m_Dummy = false;
            m_Accept = false;
            m_Item = null;
        }

        public void Add(ListItem listItem)
        {
            listItem.m_Parent = this;
            listItem.m_Next = null;
            listItem.m_Prev = m_LastChild;

            // recursively update the indent
            listItem.Indent = m_Indent + 1;

            if (m_FirstChild == null)
                m_FirstChild = listItem;

            if (m_LastChild != null)
                m_LastChild.m_Next = listItem;

            m_LastChild = listItem;
        }

        public bool Remove(ListItem listItem)
        {
            if (listItem == null)
                return false;

            // Can only remove children of this item
            if (listItem.m_Parent != this)
                return false;

            if (listItem == m_FirstChild)
                m_FirstChild = listItem.m_Next;

            if (listItem == m_LastChild)
                m_LastChild = listItem.m_Prev;

            if (listItem.m_Prev != null)
                listItem.m_Prev.m_Next = listItem.m_Next;

            if (listItem.m_Next != null)
                listItem.m_Next.m_Prev = listItem.m_Prev;

            listItem.m_Parent = null;
            listItem.m_Prev = null;
            listItem.m_Next = null;

            return true;
        }

        public void RemoveAll()
        {
            ListItem en = m_FirstChild;
            while (en != null)
            {
                en.m_Parent = null;
                en = en.m_Next;
            }

            m_FirstChild = null;
            m_LastChild = null;
        }

        public ListItem FindWithIdentifierRecurse(int inIdentifier)
        {
            if (Identifier == inIdentifier)
                return this;

            ListItem listItem = m_FirstChild;
            while (listItem != null)
            {
                ListItem found = listItem.FindWithIdentifierRecurse(inIdentifier);
                if (found != null)
                    return found;

                listItem = listItem.m_Next;
            }
            return null;
        }

        void SetIntent(ListItem listItem, int indent)
        {
            listItem.m_Indent = indent;

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
