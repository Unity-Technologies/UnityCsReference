// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        public string name;
        public string label;
        public string pathLabel;

        public Data(PanelRenderer pr, VisualElementAsset[] path, VisualElementAsset vea)
        {
            panelRenderer = pr;
            this.path = path;
            visualElementAsset = vea;
            name = VisualElementReferenceTools.GenerateVisualElementAssetLabel(visualElementAsset, false);
            label = VisualElementReferenceTools.GenerateVisualElementAssetLabel(visualElementAsset);
            pathLabel = GeneratePathLabel();
        }

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

        string GeneratePathLabel()
        {
            using var listPool = ListPool<string>.Get(out var pathParts);

            // Walk up the GameObject path
            var currentTransform = panelRenderer.transform;
            while (currentTransform != null)
            {
                pathParts.Add(currentTransform.name);
                currentTransform = currentTransform.parent;
            }

            // Reverse the list to go from root to leaf
            pathParts.Reverse();

            // Walk downt the visual element path
            foreach (var vea in path)
            {
                var name = VisualElementReferenceTools.GenerateVisualElementAssetLabel(vea);
                pathParts.Add(name);
            }

            // Add the target element
            pathParts.Add(VisualElementReferenceTools.GenerateVisualElementAssetLabel(visualElementAsset));

            using var sbPool = StringBuilderPool.Get(out var sb);
            sb.AppendJoin("/", pathParts);
            return sb.ToString();
        }
    }

    const string k_NameToken = "name";
    const string k_TypeToken = "type";

    readonly Scene m_Scene;
    readonly Type m_BaseType;
    readonly QueryEngine<Data> m_QueryEngine = new();

    public VisualElementReferenceSearchProvider(Type baseType, Scene scene)
    : base("ve", "VisualElement")
    {
        m_BaseType = baseType;
        m_Scene = scene;

        // Propositions are used to provide the search filter options in the menu.
        fetchPropositions = FetchPropositions;

        // The actual items we search against.
        fetchItems = FetchItems;

        fetchThumbnail = (item, _) => item.thumbnail;

        // The searchable data is what we search against when just typing in the search field.
        m_QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
        m_QueryEngine.AddFilter(k_NameToken, o => o.name);
        m_QueryEngine.AddFilter(k_TypeToken, o => o.visualElementAsset.fullTypeName);
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

            var serializedData = data.visualElementAsset.serializedData;
            var type = serializedData != null ? serializedData.GetType().DeclaringType : typeof(VisualElement);
            var icon = UIResources.GetIconForType(type, UIResources.RequestSize.Px64).texture;
            yield return provider.CreateItem(context, data.pathLabel, data.label, data.pathLabel, icon, data);
        }
    }

    IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
    {
        yield return new SearchProposition(null, "Name", $"{k_NameToken}:", "Filter by element name.");

        

        foreach (var sdt in UxmlSerializedDataRegistry.SerializedDataTypes)
        {
            yield return new SearchProposition("Type", sdt.Value.DeclaringType.Name, $"{k_TypeToken}={sdt.Key}", "Filter by element type.");
        }
    }

    IEnumerable<Data> GetSearchData()
    {
        using var _ = ListPool<VisualElementAsset>.Get(out var pathAssets);
        foreach(var go in UnityEditor.Search.SearchUtils.FetchGameObjects(m_Scene))
        {
            var pr = go.GetComponent<PanelRenderer>();
            if (pr == null)
                continue;

            // Add the root
            if (m_BaseType == typeof(VisualElement))
                yield return new Data(pr, Array.Empty<VisualElementAsset>(), pr.visualTreeAsset.visualTree);

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
                    yield return new Data(pr, currentPath, templateAsset);

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
                yield return new Data(pr, currentPath, vea);
            }
        }
    }

    static IEnumerable<string> GetSearchableData(Data data)
    {
        yield return data.pathLabel;
    }
}
