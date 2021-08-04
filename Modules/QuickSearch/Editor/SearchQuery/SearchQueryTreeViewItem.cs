// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    abstract class SearchQueryTreeViewItem : TreeViewItem
    {
        public SearchQueryTreeView treeView { get; private set; }

        protected SearchQueryTreeViewItem(SearchQueryTreeView treeView, string displayName, Texture2D icon)
        {
            this.icon = icon;
            this.displayName = displayName;
            children = new List<TreeViewItem>();
            this.treeView = treeView;
        }

        public abstract ISearchQuery query { get; }

        public virtual void Select()
        {
        }

        public virtual void Open()
        {
        }

        public virtual bool CanRename()
        {
            return false;
        }

        public virtual bool IsValid()
        {
            return true;
        }

        public virtual bool AcceptRename(string oldName, string newName)
        {
            return false;
        }

        public virtual bool CanStartDrag()
        {
            return false;
        }

        public virtual void OpenContextualMenu()
        {
        }

        public bool DrawRow(Rect rowRect)
        {
            return true;
        }
    }

    class SearchQueryCategoryTreeViewItem : SearchQueryTreeViewItem
    {
        public Action addQueryHandler;

        public SearchQueryCategoryTreeViewItem(SearchQueryTreeView treeView, Action addQueryHandler, GUIContent content)
            : base(treeView, content.text, content.image as Texture2D)
        {
            this.addQueryHandler = addQueryHandler;
            this.content = content;
            addBtnContent = new GUIContent("", EditorGUIUtility.FindTexture("SaveAs"), content.tooltip);
            id = $"SearchQueryCategoryTreeViewItem_{content.text}".GetHashCode();
        }

        public GUIContent content { get; protected set; }
        public GUIContent addBtnContent { get; protected set; }
        public override ISearchQuery query => null;
    }

    class SearchQueryUserTreeViewItem : SearchQueryTreeViewItem
    {
        SearchQuery m_Query;
        public SearchQueryUserTreeViewItem(SearchQueryTreeView treeView, SearchQuery query)
            : base(treeView, query.name, query.thumbnail)
        {
            m_Query = query;
            id = m_Query.guid.GetHashCode();
        }

        public override ISearchQuery query => m_Query;

        public override bool CanRename()
        {
            return true;
        }

        public override void OpenContextualMenu()
        {
            var menu = new GenericMenu();

            if (treeView.GetCurrentQuery() == m_Query)
            {
                menu.AddItem(new GUIContent("Save"), false, () => treeView.searchView.SaveActiveSearchQuery());
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Open in new window"), false, () =>
            {
                SearchQuery.Open(m_Query, SearchFlags.None);
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Rename"), false, () => treeView.BeginRename(this, 0f));
            menu.AddItem(new GUIContent("Set Icon..."), false, () => SearchQuery.ShowQueryIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                m_Query.thumbnail = newIcon;
                SearchQuery.SaveSearchQuery(m_Query);
            }));
            menu.AddItem(new GUIContent(Utils.GetRevealInFinderLabel()), false, () => EditorUtility.RevealInFinder(m_Query.filePath));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                SearchQuery.RemoveSearchQuery(m_Query);
                treeView.RemoveItem(this);
            });
            menu.ShowAsContext();
        }

        public override bool AcceptRename(string oldName, string newName)
        {
            m_Query.name = newName;
            SearchQuery.SaveSearchQuery(m_Query);
            return true;
        }

        public override void Open()
        {
            treeView.searchView.ExecuteSearchQuery(m_Query);
        }
    }

    class SearchQueryAssetTreeViewItem : SearchQueryTreeViewItem
    {
        SearchQueryAsset m_Query;
        public SearchQueryAssetTreeViewItem(SearchQueryTreeView treeView, SearchQueryAsset query)
            : base(treeView, query.name, query.thumbnail)
        {
            m_Query = query;
            id = m_Query.guid.GetHashCode();
        }

        public override ISearchQuery query => m_Query;

        public override bool CanRename()
        {
            return true;
        }

        public override bool IsValid()
        {
            return m_Query;
        }

        public override void OpenContextualMenu()
        {
            var menu = new GenericMenu();
            if (treeView.GetCurrentQuery() == (ISearchQuery)m_Query)
            {
                menu.AddItem(new GUIContent("Save"), false, () => treeView.searchView.SaveActiveSearchQuery());
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Open in new window"), false, () =>
            {
                SearchQuery.Open(m_Query, SearchFlags.None);
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Rename"), false, () => treeView.BeginRename(this, 0f));
            menu.AddItem(new GUIContent("Set Icon..."), false, () => SearchQuery.ShowQueryIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                m_Query.icon = newIcon;
                EditorUtility.SetDirty(m_Query);
            }));
            menu.AddItem(new GUIContent("Edit in Inspector"), false, () => Selection.activeObject = m_Query);
            menu.AddItem(new GUIContent(Utils.GetRevealInFinderLabel()), false, () => EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(m_Query)));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (!EditorUtility.DisplayDialog($"Deleting search query {m_Query.name}?",
                    $"You are about to delete the search query {m_Query.name}, are you sure?", "Yes", "No"))
                    return;
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Query));
                treeView.RemoveItem(this);
            });
            menu.ShowAsContext();
        }

        public override bool AcceptRename(string oldName, string newName)
        {
            m_Query.name = newName;
            return true;
        }

        public override void Open()
        {
            treeView.searchView.ExecuteSearchQuery(m_Query);
        }
    }
}
