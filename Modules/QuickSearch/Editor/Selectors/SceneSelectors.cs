// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class SceneSelectors
    {
        const string providerId = "scene";

        [SearchSelector("type", provider: providerId, priority: 99)]
        static object GetSceneObjectType(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            var components = go.GetComponents<Component>();
            if (components.Length == 0)
                return go.GetType().Name;
            else if (components.Length == 1)
                return components[0]?.GetType().Name;
            return components[1]?.GetType().Name;
        }

        [SearchSelector("tag", provider: providerId, priority: 99)]
        static object GetSceneObjectTag(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return go.tag;
        }

        [SearchSelector("path", provider: providerId, priority: 99)]
        static object GetSceneObjectPath(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return SearchUtils.GetHierarchyPath(go, false);
        }

        [SearchSelector("name", provider: providerId, priority: 99)]
        static object GetObjectName(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (!go)
                return null;
            return go.name;
        }

        [SearchSelector("enabled", provider: providerId, priority: 99)]
        static object GetEnabled(SearchSelectorArgs args)
        {
            return GetEnabled(args.current);
        }

        private static object GetEnabled(SearchItem item)
        {
            var go = item.ToObject<GameObject>();
            if (!go)
                return null;
            return go.activeSelf;
        }

        private static object DrawEnabled(SearchColumnEventArgs args)
        {
            if (args.value == null)
                return null;
            var w = 14f + EditorStyles.toggle.margin.horizontal;
            if (args.column.options.HasAny(SearchColumnFlags.TextAlignmentRight))
                args.rect.xMin = args.rect.xMax - w;
            else if (args.column.options.HasAny(SearchColumnFlags.TextAlignmentCenter))
                args.rect.xMin += (args.rect.width - w) / 2f;
            else
                args.rect.x += EditorStyles.toggle.margin.left;
            args.value = EditorGUI.Toggle(args.rect, (bool)args.value);
            return args.value;
        }

        static object SetEnabled(SearchColumnEventArgs args)
        {
            var go = args.item.ToObject<GameObject>();
            if (!go)
                return null;
            go.SetActive((bool)args.value);
            return go.activeSelf;
        }

        [SearchColumnProvider("GameObject/Enabled")]
        public static void InitializeObjectReferenceColumn(SearchColumn column)
        {
            column.getter = args => GetEnabled(args.item);
            column.drawer = args => DrawEnabled(args);
            column.setter = args => SetEnabled(args);
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items)
        {
            return PropertySelectors.Enumerate(items).Concat(new[]
            {
                new SearchColumn("GameObject/Enabled", "enabled", "GameObject/Enabled", options: SearchColumnFlags.TextAlignmentCenter)
            });
        }
    }
}
