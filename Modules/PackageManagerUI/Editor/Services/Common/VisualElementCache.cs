// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// We had a demonstrated speed improvement with our UI when caching queried objects.
    /// So until VisualElement provides the same functionality, this allows objects to easily cache queried elements
    /// </summary>
    class VisualElementCache
    {
        private Dictionary<string, VisualElement> Cache = new Dictionary<string, VisualElement>();
        private VisualElement Root;

        public VisualElementCache(VisualElement root)
        {
            Root = root;
        }

        public T Create<T>(string query) where T : VisualElement
        {
            return Root.Q<T>(query);
        }

        public T Get<T>(string query) where T : VisualElement
        {
            if (!Cache.ContainsKey(query))
                Cache[query] = Create<T>(query);

            return Cache[query] as T;
        }
    }
}
