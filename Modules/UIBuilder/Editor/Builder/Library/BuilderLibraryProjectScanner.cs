// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using BuilderLibraryItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    class BuilderLibraryProjectScanner
    {
        #pragma warning disable CS0618 // Type or member is obsolete
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

        private static readonly HashSet<string> s_PermittedPackagesSet = new HashSet<string>()
        {
            "com.unity.dt.app-ui",
        };

        readonly SearchFilter m_SearchFilter;
        private static IEnumerable<HierarchyProperty> m_Assets;
        private static readonly Dictionary<string, string> m_AssetIDAndPathPair = new Dictionary<string, string>();

        public BuilderLibraryProjectScanner()
        {
            m_SearchFilter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { "VisualTreeAsset" }
            };
        }

        static bool AllowPackageType(Type type)
        {
            var packageInfo = PackageInfo.FindForAssembly(type.Assembly);
            return
                null != packageInfo &&
                s_PermittedPackagesSet.Contains(packageInfo.name);
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

        public void ImportUxmlSerializedDataFromSource(BuilderLibraryItem sourceCategory)
        {
            var categoryStack = new List<BuilderLibraryItem>();
            var emptyNamespaceControls = new List<TreeViewItem>();

            if (Unsupported.IsDeveloperMode())
            {
                var customControlsCategory = BuilderLibraryContent.CreateItem(BuilderConstants.LibraryCustomControlsSectionUxmlSerializedData, null, null, null);
                sourceCategory.AddChild(customControlsCategory);
                sourceCategory = customControlsCategory;
            }

            var shownTypes = new HashSet<Type>();
            UxmlSerializedDataRegistry.Register();

            var sortedEntries = UxmlSerializedDataRegistry.SerializedDataTypes.Values.OrderBy(o => o.FullName);
            foreach (var type in sortedEntries)
            {
                try
                {
                    var elementType = type.DeclaringType;
                    var hasNamespace = !string.IsNullOrEmpty(elementType.Namespace);

                    // Avoid adding our own internal factories (like Package Manager templates).
                    if (!Unsupported.IsDeveloperMode() && hasNamespace && s_NameSpacesToAvoid.Any(n => elementType.Namespace.StartsWith(n)))
                    {
                        if (!AllowPackageType(type))
                            continue;
                    }

                    // Avoid adding UI Builder's own types, even in internal mode.
                    if (hasNamespace && type.Namespace.StartsWith("Unity.UI.Builder"))
                        continue;

                    // Ignore UxmlObjects
                    if (!typeof(VisualElement).IsAssignableFrom(elementType))
                        continue;

                    // Ignore elements with HideInInspector
                    if (elementType.GetCustomAttribute<HideInInspector>() != null)
                        continue;

                    // Ignore elements with generic parameters and abstract elements
                    if (elementType.ContainsGenericParameters || elementType.IsAbstract)
                        continue;

                    // UxmlElements with a custom name appear in SerializedDataTypes twice, we only need 1 item with the custom name.
                    if (shownTypes.Contains(type))
                        continue;

                    var description = UxmlSerializedDataRegistry.GetDescription(elementType.FullName);
                    Debug.AssertFormat(description != null, "Expected to find a description for {0}", elementType.FullName);

                    shownTypes.Add(type);

                    string name;
                    var elementAttribute = elementType.GetCustomAttribute<UxmlElementAttribute>();
                    if (elementAttribute != null && !string.IsNullOrEmpty(elementAttribute.name))
                        name = elementAttribute.name;
                    else
                        name = elementType.Name;

                    // Generate a unique id.
                    // We prepend the name as its possible the same Type may be displayed for both UxmlSerializedData and UxmlTraits so we need to ensure the ids do not conflict.
                    var idCode = ("uxml-serialized-data" + elementType.FullName + elementType.FullName).GetHashCode();

                    var newItem = BuilderLibraryContent.CreateItem(name, "CustomCSharpElement", elementType, () =>
                    {
                        var data = description.CreateDefaultSerializedData();
                        var instance = data.CreateInstance();

                        // Special case for the TwoPaneSplitView as we need to add two children to not get an error log.
                        if (instance is TwoPaneSplitView splitView)
                        {
                            splitView.Add(new VisualElement());
                            splitView.Add(new VisualElement());
                        }

                        data.Deserialize(instance);
                        return instance as VisualElement;
                    }, id: idCode);
                    newItem.data.hasPreview = true;

                    CheckForUxmlElementInClassHierarchy(elementType);

                    if (!hasNamespace)
                    {
                        emptyNamespaceControls.Add(newItem);
                    }
                    else
                    {
                        AddCategoriesToStack(sourceCategory, categoryStack, elementType.Namespace.Split('.'), "csharp-uxml-serialized-data-");
                        if (categoryStack.Count == 0)
                            sourceCategory.AddChild(newItem);
                        else
                            categoryStack.Last().AddChild(newItem);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            sourceCategory.AddChildren(emptyNamespaceControls);
        }

        private void CheckForUxmlElementInClassHierarchy(Type elementType)
        {
            var uxmlElementAttribute = elementType.GetCustomAttribute<UxmlElementAttribute>();

            // Skip traits classes
            if (uxmlElementAttribute == null) return;

            var baseType = elementType.BaseType;

            while (baseType != null)
            {
                uxmlElementAttribute = baseType.GetCustomAttribute<UxmlElementAttribute>();

                if (uxmlElementAttribute == null)
                {
                    var memberFilter = new MemberFilter((x,_) => x.GetCustomAttribute<UxmlAttributeAttribute>() != null);
                    var members = baseType.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, memberFilter, null);

                    if (members.Length > 0)
                    {
                        Debug.LogWarning(string.Format(BuilderConstants.LibraryUxmlAttributeUsedInNonUxmlElementClassMessage, baseType.FullName));
                    }
                }

                baseType = baseType.BaseType;
            }
        }


        public void ImportFactoriesFromSource(BuilderLibraryItem sourceCategory)
        {
            var deferredFactories = new List<IUxmlFactory>();
            var processingData = new FactoryProcessingHelper();
            var emptyNamespaceControls = new List<TreeViewItem>();

            if (Unsupported.IsDeveloperMode())
            {
                var customControlsCategory = BuilderLibraryContent.CreateItem(BuilderConstants.LibraryCustomControlsSectionUxmTraits, null, null, null);
                sourceCategory.AddChild(customControlsCategory);
                sourceCategory = customControlsCategory;
            }

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
                {
                    if (!AllowPackageType(known.uxmlType))
                        continue;
                }

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

                // Ignore UxmlSerialized elements in non-developer mode
                if (!Unsupported.IsDeveloperMode() && UxmlSerializedDataRegistry.GetDescription(elementType?.FullName) != null)
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
                    AddCategoriesToStack(sourceCategory, categoryStack, split, "csharp-uxml-traits-");
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
            if (m_Assets == null)
                return 0;

            var sb = new StringBuilder();
            foreach (var asset in m_Assets)
            {
                m_AssetIDAndPathPair.TryGetValue(asset.guid, out var assetPath);
                if (!string.IsNullOrEmpty(assetPath))
                    sb.Append(assetPath);
            }

            var pathsStr = sb.ToString();
            return pathsStr.GetHashCode();
        }

        public void ImportUxmlFromProject(BuilderLibraryItem projectCategory, bool includePackages)
        {
            if (m_Assets == null)
                return;

            var categoryStack = new List<BuilderLibraryItem>();
            foreach (var asset in m_Assets)
            {
                m_AssetIDAndPathPair.TryGetValue(asset.guid, out var assetPath);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

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
        #pragma warning restore CS0618 // Type or member is obsolete

        internal void FindAssets()
        {
            m_Assets = AssetDatabase.FindAllAssets(m_SearchFilter);
            using var pooledHashSet = HashSetPool<string>.Get(out var assetGuids);
            foreach (var property in m_Assets)
            {
                m_AssetIDAndPathPair[property.guid] = AssetDatabase.GetAssetPath(property.instanceID);
                assetGuids.Add(property.guid);
            }

            // If an asset is deleted, we need to remove it from the cache.
            if (m_AssetIDAndPathPair.Count > m_Assets.Count())
            {
                var keyToRemove = "";
                var removeKey = false;
                foreach (var key in m_AssetIDAndPathPair.Keys)
                {
                   if (assetGuids.Contains(key))
                       continue;

                   keyToRemove = key;
                   removeKey = true;
                   break;
                }

                if (removeKey)
                    m_AssetIDAndPathPair.Remove(keyToRemove);
            }
        }
    }
}
