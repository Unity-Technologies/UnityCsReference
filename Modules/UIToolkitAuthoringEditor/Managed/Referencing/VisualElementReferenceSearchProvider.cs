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
        public VisualElementAsset[] hierarchyPath;
        public VisualElementAsset[] authoringPath;
        public VisualElementAsset visualElementAsset;
        public string name;
        public string label;
        public string pathLabel;

        // Unique item ID: pathLabel + '#' + assetId, preventing deduplication
        // of siblings with identical type and name.
        public string itemId;

        // The parent's itemId. For VisualElement parents includes '#assetId',
        // for virtual GameObject ancestor nodes it is just the plain path label.
        // Null if this item has no parent.
        public string parentItemId;

        public Data(PanelRenderer pr, VisualElementAsset[] hierarchyPath, VisualElementAsset[] authoringPath, VisualElementAsset vea)
        {
            panelRenderer = pr;
            this.hierarchyPath = hierarchyPath;
            this.authoringPath = authoringPath;
            visualElementAsset = vea;
            name = VisualElementReferenceTools.GenerateVisualElementAssetLabel(visualElementAsset, false);
            label = VisualElementReferenceTools.GenerateVisualElementAssetLabel(visualElementAsset);
            pathLabel = GeneratePathLabel();
            itemId = $"{pathLabel}#{vea.id}";
            parentItemId = GenerateParentItemId();
        }

        public AuthoringIdPath GenerateHierarchyPath()
        {
            var generatedPath = new int[hierarchyPath.Length + 1];
            for (int i = 0; i < hierarchyPath.Length; i++)
            {
                generatedPath[i] = hierarchyPath[i].id;
            }
            generatedPath[^1] = visualElementAsset.id;
            return new AuthoringIdPath(generatedPath);
        }

        public AuthoringIdPath GenerateAuthoringPath()
        {
            var generatedPath = new int[authoringPath.Length];
            for (int i = 0; i < authoringPath.Length; i++)
            {
                generatedPath[i] = authoringPath[i].id;
            }
            return new AuthoringIdPath(generatedPath);
        }

        string GenerateParentItemId()
        {
            var lastSlash = pathLabel.LastIndexOf('/');
            if (lastSlash <= 0)
                return null;

            var parentPathLabel = pathLabel.Substring(0, lastSlash);

            // If the immediate parent is a VisualElement, append its asset id
            // to match the itemId format. Otherwise it is a virtual GameObject node.
            return hierarchyPath.Length > 0
                ? $"{parentPathLabel}#{hierarchyPath[^1].id}"
                : parentPathLabel;
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

            // Walk down the visual element path
            foreach (var vea in hierarchyPath)
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
    static readonly char[] k_Separator = new[] { '/' };

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

        fetchParentDescriptor = FetchParentDescriptor;
        fetchParentsTokenSeparatedIds = FetchParentsTokenSeparatedIds;

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

        foreach (var data in query.Apply(GetSearchData()))
        {
            if (data.panelRenderer == null)
                continue;

            var serializedData = data.visualElementAsset.serializedData;
            var type = serializedData != null ? serializedData.GetType().DeclaringType : typeof(VisualElement);
            var icon = UIResources.GetIconForType(type, UIResources.RequestSize.Px64).texture;
            yield return provider.CreateItem(context, data.itemId, data.label, data.pathLabel, icon, data);
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
        using var hierarchyPoolHandle = ListPool<VisualElementAsset>.Get(out var hierarchyStack);
        using var authoringPoolHandle = ListPool<VisualElementAsset>.Get(out var authoringStack);

        foreach (var go in SearchUtils.FetchGameObjects(m_Scene))
        {
            var pr = go.GetComponent<PanelRenderer>();
            if (pr == null || pr.visualTreeAsset == null)
                continue;

            var root = pr.visualTreeAsset.visualTree;

            // Add the root - it's part of authoring path as the starting UXML file
            if (m_BaseType == typeof(VisualElement))
            {
                authoringStack.Add(root);
                yield return new Data(pr, Array.Empty<VisualElementAsset>(), ToArrayOrEmpty(authoringStack), root);
                authoringStack.Clear();
            }

            // Push root onto hierarchyStack so its children appear nested under it
            hierarchyStack.Add(root);
            for (var i = 0; i < root.childCount; i++)
            {
                if (root[i] is VisualElementAsset child)
                {
                    foreach (var v in TraverseElement(pr, child, hierarchyStack, authoringStack))
                        yield return v;
                }
            }
            hierarchyStack.RemoveAt(hierarchyStack.Count - 1);
        }
    }

    IEnumerable<Data> TraverseElement(
        PanelRenderer pr, 
        VisualElementAsset vea, 
        List<VisualElementAsset> hierarchyStack,
        List<VisualElementAsset> authoringStack)
    {
        if (vea is TemplateAsset templateAsset)
        {
            // TemplateAsset is part of the authoring path (UXML boundary)
            authoringStack.Add(templateAsset);

            if (m_BaseType.IsAssignableFrom(typeof(TemplateContainer)))
                yield return new Data(pr, ToArrayOrEmpty(hierarchyStack), ToArrayOrEmpty(authoringStack), templateAsset);

            var childVta = templateAsset.ResolveTemplate();
            if (childVta != null && childVta.visualTree != null)
            {
                hierarchyStack.Add(templateAsset);

                var innerRoot = childVta.visualTree;
                for (var i = 0; i < innerRoot.childCount; i++)
                {
                    if (innerRoot[i] is VisualElementAsset innerChild)
                    {
                        foreach (var v in TraverseElement(pr, innerChild, hierarchyStack, authoringStack))
                            yield return v;
                    }
                }

                hierarchyStack.RemoveAt(hierarchyStack.Count - 1);
            }

            authoringStack.RemoveAt(authoringStack.Count - 1);
        }
        else
        {
            if (vea.serializedData != null && m_BaseType.IsAssignableFrom(vea.serializedData.GetType().DeclaringType))
            {
                // Add the target element to authoring path
                authoringStack.Add(vea);
                yield return new Data(pr, ToArrayOrEmpty(hierarchyStack), ToArrayOrEmpty(authoringStack), vea);
                authoringStack.RemoveAt(authoringStack.Count - 1);
            }

            hierarchyStack.Add(vea);
            for (var i = 0; i < vea.childCount; i++)
            {
                if (vea[i] is VisualElementAsset child)
                {
                    foreach (var v in TraverseElement(pr, child, hierarchyStack, authoringStack))
                        yield return v;
                }
            }
            hierarchyStack.RemoveAt(hierarchyStack.Count - 1);
        }
    }

    static VisualElementAsset[] ToArrayOrEmpty(List<VisualElementAsset> list)
    {
        return list.Count == 0 ? Array.Empty<VisualElementAsset>() : list.ToArray();
    }

    static IEnumerable<string> GetSearchableData(Data data)
    {
        yield return data.pathLabel;
    }

    static SearchItemParentDescriptor FetchParentDescriptor(SearchItem item, SearchContext context)
    {
        if (item.data is not Data data || data.parentItemId == null)
            return default;

        return new SearchItemParentDescriptor(data.parentItemId, SearchItemParentType.TokenSeparatedId);
    }

    static void FetchParentsTokenSeparatedIds(SearchItem item, SearchContext context, List<StringView> idsSubstrings)
    {
        var descriptor = item.GetParentDescriptor(context);
        if (string.IsNullOrEmpty(descriptor.Id))
            return;

        descriptor.Id.GetStringView().Split(k_Separator, StringSplitOptions.RemoveEmptyEntries, idsSubstrings);
    }
}
