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

        public void ImportFactoriesFromSource(BuilderLibraryTreeItem sourceCategory)
        {
            var deferredFactories = new List<IUxmlFactory>();
            var processingData = new FactoryProcessingHelper();
            var emptyNamespaceControls = new List<ITreeViewItem>();

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

            var categoryStack = new List<BuilderLibraryTreeItem>();
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
                var overrides = new List<TemplateAsset.AttributeOverride>();
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

                var newItem = new BuilderLibraryTreeItem(
                    known.uxmlName, "CustomCSharpElement", elementType, () => known.Create(asset, context));
                newItem.hasPreview = true;

                if (string.IsNullOrEmpty(split[0]))
                {
                    emptyNamespaceControls.Add(newItem);
                }
                else
                {
                    AddCategoriesToStack(sourceCategory, categoryStack, split);
                    if (categoryStack.Count == 0)
                        sourceCategory.AddChild(newItem);
                    else
                        categoryStack.Last().AddChild(newItem);
                }
            }

            sourceCategory.AddChildren(emptyNamespaceControls);
        }

        static void AddCategoriesToStack(BuilderLibraryTreeItem sourceCategory, List<BuilderLibraryTreeItem> categoryStack, string[] split)
        {
            if (categoryStack.Count > split.Length)
            {
                categoryStack.RemoveRange(split.Length, categoryStack.Count - split.Length);
            }

            string fullName = string.Empty;
            for (int i = 0; i < split.Length; ++i)
            {
                var part = split[i];
                fullName += part;
                if (categoryStack.Count > i)
                {
                    if (categoryStack[i].name == part)
                    {
                        continue;
                    }
                    else if (categoryStack[i].name != part)
                    {
                        categoryStack.RemoveRange(i, categoryStack.Count - i);
                    }
                }

                if (categoryStack.Count <= i)
                {
                    var newCategory = new BuilderLibraryTreeItem(part,
                        null, null, null,
                        null, new List<TreeViewItem<string>>(),
                        null, fullName.GetHashCode());

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

        public void ImportUxmlFromProject(BuilderLibraryTreeItem projectCategory, bool includePackages)
        {
            var assets = AssetDatabase.FindAllAssets(m_SearchFilter);
            var categoryStack = new List<BuilderLibraryTreeItem>();
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

                // Anoter way to check the above. Leaving it here for references in case the above stops working.
                //AssetDatabase.GetAssetFolderInfo(assetPath, out bool isRoot, out bool isImmutable);
                //if (isImmutable)
                    //continue;

                var split = prettyPath.Split('/');
                AddCategoriesToStack(projectCategory, categoryStack, split);

                var vta = asset.pptrValue as VisualTreeAsset;
                var newItem = new BuilderLibraryTreeItem(asset.name + ".uxml", null, typeof(TemplateContainer),
                    () =>
                    {
                        if (vta == null)
                            return null;

                        var tree = vta.CloneTree();
                        tree.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, assetPath);
                        tree.name = vta.name;
                        return tree;
                    },
                    (inVta, inParent, ve) =>
                    {
                        var vea = inVta.AddTemplateInstance(inParent, assetPath) as VisualElementAsset;
                        vea.AddProperty("name", vta.name);
                        ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, vta);
                        return vea;
                    },
                    null, vta);
                newItem.SetIcon((Texture2D) EditorGUIUtility.IconContent("UxmlScript Icon").image);
                newItem.hasPreview = true;

                if (categoryStack.Count == 0)
                    projectCategory.AddChild(newItem);
                else
                    categoryStack.Last().AddChild(newItem);
            }
        }
    }
}
