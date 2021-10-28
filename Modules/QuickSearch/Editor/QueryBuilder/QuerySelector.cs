// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class QuerySelector : AdvancedDropdown, IBlockEditor
    {
        private readonly string m_Title;
        private readonly IBlockSource m_BlockSource;
        private readonly IEnumerable<SearchProposition> m_Propositions;

        public SearchContext context => m_BlockSource.context;
        public EditorWindow window => m_WindowInstance;

        public QuerySelector(Rect rect, IBlockSource dataSource)
            : base(new AdvancedDropdownState())
        {
            m_BlockSource = dataSource;
            m_Title = m_BlockSource.name ?? string.Empty;
            m_Propositions = m_BlockSource.FetchPropositions().Where(p => p.valid);

            minimumSize = new Vector2(Mathf.Max(rect.width, 300f), 350f);
            maximumSize = new Vector2(Mathf.Max(rect.width, 400f), 450f);
        }

        public static QuerySelector Open(Rect r, IBlockSource source)
        {
            var w = new QuerySelector(r, source);
            w.Show(r);
            w.Bind();
            return w;
        }

        readonly struct ItemPropositionComparer : IComparer<AdvancedDropdownItem>
        {
            public int Compare(AdvancedDropdownItem x, AdvancedDropdownItem y)
            {
                if (x.userData is SearchProposition px && y.userData is SearchProposition py)
                    return px.priority.CompareTo(py.priority);
                return x.CompareTo(y);
            }
        }

        private void Bind()
        {
            m_WindowInstance.windowClosed += OnClose;
            m_WindowInstance.selectionCanceled += OnSelectionCanceled;
            m_DataSource.searchMatchItem = OnSearchItemMatch;
            m_DataSource.searchMatchItemComparer = new ItemPropositionComparer();
        }

        private bool OnSearchItemMatch(in AdvancedDropdownItem item, in string[] words, out bool didMatchStart)
        {
            didMatchStart = false;
            var label = item.displayName ?? item.name;
            var pp = label.LastIndexOf('(');
            pp = pp == -1 ? label.Length : pp;
            foreach (var w in words)
            {
                var fp = label.IndexOf(w, 0, pp, StringComparison.OrdinalIgnoreCase);
                if (fp == -1)
                    return false;
                didMatchStart |= (fp == 0 || label[fp-1] == ' ');
            }
            return true;
        }

        private void OnClose(AdvancedDropdownWindow w = null)
        {
            m_BlockSource.CloseEditor();
        }

        private void OnSelectionCanceled()
        {
            OnClose();
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var rootItem = new AdvancedDropdownItem(m_Title);
            var formatNames = m_BlockSource.formatNames;
            foreach (var p in m_Propositions)
            {
                var path = p.path;
                var name = p.label;
                var prefix = p.category;

                if (name.LastIndexOf('/') != -1)
                {
                    var ls = path.LastIndexOf('/');
                    name = path.Substring(ls+1);
                    prefix = path.Substring(0, ls);
                }

                var newItem = new AdvancedDropdownItem(path)
                {
                    displayName = formatNames ? ObjectNames.NicifyVariableName(name) : name,
                    icon = p.icon,
                    tooltip = p.help,
                    userData = p
                };

                var parent = rootItem;
                if (prefix != null)
                    parent = MakeParents(prefix, p, rootItem);

                var fit = FindItem(name, parent);
                if (fit == null)
                    parent.AddChild(newItem);
                else if (p.icon)
                    fit.icon = p.icon;
            }

            return rootItem;
        }

        private AdvancedDropdownItem FindItem(string path, AdvancedDropdownItem root)
        {
            var pos = path.IndexOf('/');
            var name = pos == -1 ? path : path.Substring(0, pos);
            var suffix = pos == -1 ? null : path.Substring(pos + 1);

            foreach (var c in root.children)
            {
                if (suffix == null && string.Equals(c.name, name, StringComparison.Ordinal))
                    return c;

                if (suffix == null)
                    continue;

                var f = FindItem(suffix, c);
                if (f != null)
                    return f;
            }

            return null;
        }

        private AdvancedDropdownItem MakeParents(string prefix, in SearchProposition proposition, AdvancedDropdownItem parent)
        {
            var parts = prefix.Split('/');

            foreach (var p in parts)
            {
                var f = FindItem(p, parent);
                if (f != null)
                {
                    if (f.icon == null)
                        f.icon = proposition.icon;
                    parent = f;
                }
                else
                {
                    var newItem = new AdvancedDropdownItem(p) { icon = proposition.icon };
                    parent.AddChild(newItem);
                    parent = newItem;
                }
            }

            return parent;
        }

        protected override void ItemSelected(AdvancedDropdownItem i)
        {
            if (i.userData is SearchProposition sp)
                m_BlockSource.Apply(sp);
        }
    }
}
