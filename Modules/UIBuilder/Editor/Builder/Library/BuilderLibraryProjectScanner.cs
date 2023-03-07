// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using BuilderLibraryItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    class BuilderLibraryProjectScanner
    {
        class FactoryProcessingHelper
        {
            public class AttributeRecord
            {
                public XmlQualifiedName name { get; set; }
                public UxmlAttributeDescription desc { get; set; }
            }

            public Dictionary<string, AttributeRecord> attributeTypeNames;

            public SortedDictionary<string, IUxmlFactory> knownTypes;

            public FactoryProcessingHelper()
            {
                attributeTypeNames = new Dictionary<string, AttributeRecord>();
                knownTypes = new SortedDictionary<string, IUxmlFactory>();
            }

            public void RegisterElementType(IUxmlFactory factory)
            {
                knownTypes.Add(XmlQualifiedName.ToString(factory.uxmlName, factory.uxmlNamespace), factory);
            }

            public bool IsKnownElementType(string elementName, string elementNameSpace)
            {
                return knownTypes.ContainsKey(XmlQualifiedName.ToString(elementName, elementNameSpace));
            }
        }
        static readonly List<string> s_NameSpacesToAvoid = new List<string> { "Unity", "UnityEngine", "UnityEditor" };
        readonly SearchFilter m_SearchFilter;

        public BuilderLibraryProjectScanner()
        {
            m_SearchFilter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { "VisualTreeAsset" }
            };
        }

        bool ProcessFactory(IUxmlFactory factory, FactoryProcessingHelper processingData)
        {
            if (!string.IsNullOrEmpty(factory.substituteForTypeName))
            {
                if (!processingData.IsKnownElementType(factory.substituteForTypeName, factory.substituteForTypeNamespace))
                {
                    // substituteForTypeName is not yet known. Defer processing to later.
                    return false;
                }
            }

            processingData.RegisterElementType(factory);

            return true;
        }

        public void ImportFactoriesFromSource(BuilderLibraryItem sourceCategory)
        {
            var deferredFactories = new List<IUxmlFactory>();
            var processingData = new FactoryProcessingHelper();
            var emptyNamespaceControls = new List<TreeViewItem>();

            foreach (var factories in VisualElementFactoryRegistry.factories)
            {
                if (factories.Value.Count == 0)
                    continue;

                var factory = factories.Value[0];
                if (!ProcessFactory(factory, processingData))
                {
                    // Could not process the factory now, because it depends on a yet unprocessed factory.
                    // Defer its processing.
                    deferredFactories.Add(factory);
                }
            }

            List<IUxmlFactory> deferredFactoriesCopy;
            do
            {
                deferredFactoriesCopy = new List<IUxmlFactory>(deferredFactories);
                foreach (var factory in deferredFactoriesCopy)
                {
                    deferredFactories.Remove(factory);
                    if (!ProcessFactory(factory, processingData))
                    {
                        // Could not process the factory now, because it depends on a yet unprocessed factory.
                        // Defer its processing again.
                        deferredFactories.Add(factory);
                    }
                }
            }
            while (deferredFactoriesCopy.Count > deferredFactories.Count);

            if (deferredFactories.Count > 0)
            {
                Debug.Log("Some factories could not be processed because their base type is missing.");
            }

            var categoryStack = new List<BuilderLibraryItem>();
            foreach (var known in processingData.knownTypes.Values)
            {
                var split = known.uxmlNamespace.Split('.');
                if (split.Length == 0)
                    continue;

                // Avoid adding our own internal factories (like Package Manager templates).
                if (!Unsupported.IsDeveloperMode() && split.Length > 0 && s_NameSpacesToAvoid.Contains(split[0]))
                    continue;

                // Avoid adding UI Builder's own types, even in internal mode.
                if (split.Length >= 3 && split[0] == "Unity" && split[1] == "UI" && split[2] == "Builder")
                    continue;

                var asset = new VisualElementAsset(known.uxmlQualifiedName);
                var slots = new Dictionary<string, VisualElement>();
                var overrides = new List<CreationContext.AttributeOverrideRange>();
                var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
                var context = new CreationContext(slots, overrides, vta, null);

                Type elementType = null;
                var factoryType = known.GetType();
                while (factoryType != null && elementType == null)
                {
                    if (factoryType.IsGenericType && factoryType.GetGenericTypeDefinition() == typeof(UxmlFactory<,>))
                        elementType = factoryType.GetGenericArguments()[0];
                    else
                        factoryType = factoryType.BaseType;
                }

                if (elementType == typeof(TemplateContainer))
                    continue;

                TreeViewItemData<BuilderLibraryTreeItem> newItem = default;

                // Special case for the TwoPaneSplitView as we need to add two children to not get an error log.
                if (elementType == typeof(TwoPaneSplitView))
                {
                    newItem = BuilderLibraryContent.CreateItem(known.uxmlName, "CustomCSharpElement", elementType, () =>
                    {
                        var splitView = known.Create(asset, context);
                        splitView.Add(new VisualElement());
                        splitView.Add(new VisualElement());
                        return splitView;
                    }, (treeAsset, veaParent, element) =>
                    {
                        var visualElementAsset = treeAsset.AddElement(veaParent, element);

                        for (var i = 0; i < element.childCount; ++i)
                            treeAsset.AddElement(visualElementAsset, element[i]);

                        return visualElementAsset;
                    });
                }
                else
                {
                    newItem = BuilderLibraryContent.CreateItem(
                        known.uxmlName, "CustomCSharpElement", elementType, () => known.Create(asset, context));
                }
                newItem.data.hasPreview = true;

                if (string.IsNullOrEmpty(split[0]))
                {
                    emptyNamespaceControls.Add(newItem);
                }
                else
                {
                    AddCategoriesToStack(sourceCategory, categoryStack, split, "csharp-");
                    if (categoryStack.Count == 0)
                        sourceCategory.AddChild(newItem);
                    else
                        categoryStack.Last().AddChild(newItem);
                }

                vta.Destroy();
            }

            sourceCategory.AddChildren(emptyNamespaceControls);
        }

        static void AddCategoriesToStack(BuilderLibraryItem sourceCategory, List<BuilderLibraryItem> categoryStack, string[] split, string idNamePrefix)
        {
            if (categoryStack.Count > split.Length)
            {
                categoryStack.RemoveRange(split.Length, categoryStack.Count - split.Length);
            }

            string fullName = string.Empty;
            for (int i = 0; i < split.Length; ++i)
            {
                var part = split[i];
                if (string.IsNullOrWhiteSpace(fullName))
                    fullName += part;
                else
                    fullName += "." + part;

                if (categoryStack.Count > i)
                {
                    var data = categoryStack[i].data;
                    if (data.name == part)
                    {
                        continue;
                    }
                    else if (data.name != part)
                    {
                        categoryStack.RemoveRange(i, categoryStack.Count - i);
                    }
                }

                if (categoryStack.Count <= i)
                {
                    var newCategory = BuilderLibraryContent.CreateItem(part,
                        null, null, null,
                        null, null,
                        null,  (idNamePrefix + fullName).GetHashCode());

                    if (categoryStack.Count == 0)
                        sourceCategory.AddChild(newCategory);
                    else
                        categoryStack[i - 1].AddChild(newCategory);

                    categoryStack.Add(newCategory);
                }
            }
        }

        public int GetAllProjectUxmlFilePathsHash()
        {
            var assets = AssetDatabase.FindAllAssets(m_SearchFilter);

            var sb = new StringBuilder();
            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.instanceID);
                sb.Append(assetPath);
            }

            var pathsStr = sb.ToString();
            return pathsStr.GetHashCode();
        }

        public void ImportUxmlFromProject(BuilderLibraryItem projectCategory, bool includePackages)
        {
            var categoryStack = new List<BuilderLibraryItem>();
            var assets = AssetDatabase.FindAllAssets(m_SearchFilter);
            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.instanceID);
                var prettyPath = assetPath;
                prettyPath = Path.GetDirectoryName(prettyPath);
                prettyPath = prettyPath.ConvertSeparatorsToUnity();
                if (prettyPath.StartsWith("Packages/") && !includePackages)
                    continue;

                if (prettyPath.StartsWith(BuilderConstants.UIBuilderPackageRootPath))
                    continue;

                // Check to make sure the asset is actually writable.
                var packageInfo = PackageInfo.FindForAssetPath(assetPath);
                if (packageInfo != null && packageInfo.source != PackageSource.Embedded && packageInfo.source != PackageSource.Local)
                    continue;

                // Another way to check the above. Leaving it here for references in case the above stops working.
                //AssetDatabase.GetAssetFolderInfo(assetPath, out bool isRoot, out bool isImmutable);
                //if (isImmutable)
                //continue;

                var split = prettyPath.Split('/');
                AddCategoriesToStack(projectCategory, categoryStack, split, "uxml-");

                var vta = asset.pptrValue as VisualTreeAsset;
                var newItem = BuilderLibraryContent.CreateItem(asset.name + ".uxml", nameof(TemplateContainer), typeof(TemplateContainer),
                    () =>
                    {
                        if (vta == null)
                            return null;

                        var tree = vta.CloneTree();
                        tree.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, assetPath);
                        return tree;
                    },
                    (inVta, inParent, ve) =>
                    {
                        var vea = inVta.AddTemplateInstance(inParent, assetPath) as VisualElementAsset;
                        ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, vta);
                        return vea;
                    },
                    null, vta);

                var data = newItem.data;
                if (data.icon == null)
                {
                    data.SetIcon((Texture2D) EditorGUIUtility.IconContent("VisualTreeAsset Icon").image);
                }
                data.hasPreview = true;

                if (categoryStack.Count == 0)
                    projectCategory.AddChild(newItem);
                else
                    categoryStack.Last().AddChild(newItem);
            }
        }
    }
}
