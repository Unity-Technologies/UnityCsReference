// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Search;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    class ListSelectionWindow : AdvancedDropdown
    {
        Action<int> m_ElementSelectedHandler;

        List<SearchProposition> m_Models;

        ListSelectionWindow(Rect rect, List<SearchProposition> models, Action<int> elementSelectedHandler)
            : base(new AdvancedDropdownState())
        {
            m_Models = models;
            m_ElementSelectedHandler = elementSelectedHandler;

            minimumSize = new Vector2(Mathf.Max(rect.width, 300f), 350f);
            maximumSize = new Vector2(Mathf.Max(rect.width, 400f), 450f);
        }

        public static void Open(Rect rect, List<SearchProposition> models, Action<int> elementSelectedHandler)
        {
            var win = new ListSelectionWindow(rect, models, elementSelectedHandler);
            win.Show(rect);
            win.Bind();
        }

        void Bind()
        {
            m_WindowInstance.windowClosed += OnClose;
            m_WindowInstance.selectionCanceled += OnSelectionCanceled;
        }

        void OnSelectionCanceled()
        {
            m_ElementSelectedHandler?.Invoke(-1);
        }

        void OnClose(AdvancedDropdownWindow win)
        {
            if (!win)
            {
                m_ElementSelectedHandler?.Invoke(-1);
                return;
            }

            var selectedItem = win.GetSelectedItem();
            m_ElementSelectedHandler?.Invoke(selectedItem.elementIndex);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var rootItem = new AdvancedDropdownItem("Browse types...");
            for (var i = 0; i < m_Models.Count; ++i)
            {
                var p = m_Models[i];
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
                    displayName = name,
                    icon = p.icon,
                    tooltip = p.help,
                    userData = p,
                    elementIndex = i
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

        AdvancedDropdownItem FindItem(string path, AdvancedDropdownItem root)
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

        AdvancedDropdownItem MakeParents(string prefix, in SearchProposition proposition, AdvancedDropdownItem parent)
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
    }
}
