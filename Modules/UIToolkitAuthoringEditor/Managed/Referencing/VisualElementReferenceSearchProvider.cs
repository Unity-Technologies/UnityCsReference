// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class VisualElementReferenceSearchProvider : SearchProvider
{
    public struct Data
    {
        public PanelRenderer panelRenderer;
        public VisualElementAsset[] path;
        public VisualElementAsset visualElementAsset;

        public AuthoringIdPath GeneratePath()
        {
            var generatedPath = new int[path.Length + 1];
            for (int i = 0; i < path.Length; i++)
            {
                generatedPath[i] = path[i].id;
            }
            generatedPath[^1] = visualElementAsset.id;
            return new AuthoringIdPath(generatedPath);
        }
    }

    readonly Scene m_Scene;
    readonly Type m_BaseType;
    readonly QueryEngine<Data> m_QueryEngine = new();

    public VisualElementReferenceSearchProvider(Type baseType, Scene scene)
    : base("ve", "VisualElement")
    {
        m_BaseType = baseType;
        m_Scene = scene;

        // The actual items we search against.
        fetchItems = FetchItems;

        fetchThumbnail = (item, _) => item.thumbnail;

        // The searchable data is what we search against when just typing in the search field.
        m_QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
    }

    IEnumerator FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
    {
        yield return SearchItem.clear;

        var query = m_QueryEngine.ParseQuery(context.searchQuery);
        if (!query.valid)
            yield break;
        var filteredObjects = query.Apply(GetSearchData());

        foreach (var data in filteredObjects)
        {
            if (data.panelRenderer == null)
                continue;

            var label = VisualElementReferenceTools.GenerateVisualElementAssetLabel(data.visualElementAsset);

            var serializedData = data.visualElementAsset.serializedData;
            var type = serializedData != null ? serializedData.GetType().DeclaringType : typeof(VisualElement);
            var icon = UIResources.GetIconForType(type, UIResources.RequestSize.Px64).texture;
            var description = GenerateDescription(data);
            yield return provider.CreateItem(context, description, label, description, icon, data);
        }
    }

    string GenerateDescription(Data searchData)
    {
        using var listPool = ListPool<string>.Get(out var pathParts);

        // Walk up the GameObject path
        var currentTransform = searchData.panelRenderer.transform;
        while (currentTransform != null)
        {
            pathParts.Add(currentTransform.name);
            currentTransform = currentTransform.parent;
        }

        // Reverse the list to go from root to leaf
        pathParts.Reverse();

        // Walk downt the visual element path
        foreach (var vea in searchData.path)
        {
            var name = VisualElementReferenceTools.GenerateVisualElementAssetLabel(vea);
            pathParts.Add(name);
        }

        // Add the target element
        pathParts.Add(VisualElementReferenceTools.GenerateVisualElementAssetLabel(searchData.visualElementAsset));

        using var sbPool = StringBuilderPool.Get(out var sb);
        sb.AppendJoin("/", pathParts);
        return sb.ToString();
    }

    IEnumerable<Data> GetSearchData()
    {
        using var _ = ListPool<VisualElementAsset>.Get(out var pathAssets);
        foreach(var go in SearchUtils.FetchGameObjects(m_Scene))
        {
            var pr = go.GetComponent<PanelRenderer>();
            if (pr == null)
                continue;

            // Add the root
            if (m_BaseType == typeof(VisualElement))
                yield return new Data { panelRenderer = pr, path = Array.Empty<VisualElementAsset>(), visualElementAsset = pr.visualTreeAsset.visualTree };

            foreach (var v in AppendVisualTreeSearchData(pr, pr.visualTreeAsset, pathAssets))
            {
                yield return v;
            }
        }
    }

    IEnumerable<Data> AppendVisualTreeSearchData(PanelRenderer pr, VisualTreeAsset visualTreeAsset, List<VisualElementAsset> pathAssets)
    {
        // The path is used by all referenceable elements in this visual tree
        var currentPath = pathAssets.Count == 0 ? Array.Empty<VisualElementAsset>() : pathAssets.ToArray();

        foreach (var uxmlElement in visualTreeAsset.DepthFirstTraversal())
        {
            if (uxmlElement is TemplateAsset templateAsset)
            {
                if (m_BaseType.IsAssignableFrom(typeof(TemplateContainer)))
                    yield return new Data { panelRenderer = pr, path = currentPath, visualElementAsset = templateAsset };

                var childVta = templateAsset.ResolveTemplate();
                if (childVta != null)
                {
                    pathAssets.Add(templateAsset);
                    foreach (var v in AppendVisualTreeSearchData(pr, childVta, pathAssets))
                    {
                        yield return v;
                    }
                    pathAssets.RemoveAt(pathAssets.Count - 1);
                }
            }
            else if (uxmlElement is VisualElementAsset vea && vea.serializedData != null && m_BaseType.IsAssignableFrom(vea.serializedData.GetType().DeclaringType))
            {
                yield return new Data { panelRenderer = pr, path = currentPath, visualElementAsset = vea };
            }
        }
    }

    static IEnumerable<string> GetSearchableData(Data data)
    {
        if (data.panelRenderer != null)
        {
            yield return data.panelRenderer.gameObject.name;
            yield return data.panelRenderer.visualTreeAsset.name;
            yield return VisualElementReferenceTools.GenerateVisualElementAssetLabel(data.visualElementAsset);
        }
    }
}
