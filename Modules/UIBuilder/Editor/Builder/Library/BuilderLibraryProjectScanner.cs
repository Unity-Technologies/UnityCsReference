// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        static readonly List<string> s_NameSpacesToAvoid = new List<string> { "Unity", "UnityEngine", "UnityEditor" };

        private static readonly HashSet<string> s_PermittedPackagesSet = new HashSet<string>()
        {
            "com.unity.dt.app-ui",
        };

        readonly SearchFilter m_SearchFilter;
        private static IEnumerable<HierarchyIterator> m_Assets;
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

        public void ImportUxmlSerializedDataFromSource(BuilderLibraryItem sourceCategory)
        {
            var categoryStack = new List<BuilderLibraryItem>();
            var emptyNamespaceControls = new List<TreeViewItem>();

            var shownTypes = new HashSet<Type>();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var sortedEntries = UxmlSerializedDataRegistry.SerializedDataTypes.Values.OrderBy(o => o.FullName);
#pragma warning restore UA2001
            foreach (var type in sortedEntries)
            {
                try
                {
                    var elementType = type.DeclaringType;
                    var elementAttribute = elementType.GetCustomAttribute<UxmlElementAttribute>();
                    var hasNamespace = !string.IsNullOrEmpty(elementType.Namespace);

                    if (elementAttribute == null || elementAttribute.visibility == LibraryVisibility.Default)
                    {
                        // Avoid adding our own internal factories (like Package Manager templates).
                        if (!Unsupported.IsDeveloperMode() && hasNamespace && s_NameSpacesToAvoid.Exists(elementType.Namespace.StartsWith))
                        {
                            if (!AllowPackageType(type))
                                continue;
                        }

                        // Avoid adding UI Builder's own types, even in internal mode.
                        if (hasNamespace && type.Namespace.StartsWith("Unity.UI.Builder"))
                            continue;

                        // Ignore elements with HideInInspector
                        if (elementType.GetCustomAttribute<HideInInspector>() != null)
                            continue;
                    }
                    else
                    {
                        if (elementAttribute.visibility == LibraryVisibility.Hidden && !Unsupported.IsDeveloperMode())
                            continue;
                    }

                    // Ignore UxmlObjects
                    if (!typeof(VisualElement).IsAssignableFrom(elementType))
                        continue;

                    // Ignore elements with generic parameters and abstract elements
                    if (elementType.ContainsGenericParameters || elementType.IsAbstract)
                        continue;

                    // UxmlElements with a custom name appear in SerializedDataTypes twice, we only need 1 item with the custom name.
                    if (shownTypes.Contains(type))
                        continue;

                    shownTypes.Add(type);

                    string name;
                    if (elementAttribute != null && !string.IsNullOrEmpty(elementAttribute.name))
                        name = elementAttribute.name;
                    else
                        name = elementType.Name;

                    // Generate a unique id.
                    var idCode = elementType.FullName.GetHashCode();

                    var newItem = BuilderLibraryContent.CreateItem(name, "CustomCSharpElement", elementType, () =>
                    {
                        var description = UxmlSerializedDataRegistry.GetDescription(elementType.FullName);
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

                    bool hasCustomPath = elementAttribute != null && !string.IsNullOrEmpty(elementAttribute.libraryPath);
                    if (!hasNamespace && !hasCustomPath)
                    {
                        emptyNamespaceControls.Add(newItem);
                    }
                    else
                    {
                        var category = hasCustomPath ? elementAttribute.libraryPath.Split("/", StringSplitOptions.RemoveEmptyEntries) : elementType.Namespace.Split('.');
                        AddCategoriesToStack(sourceCategory, categoryStack, category, "csharp-uxml-serialized-data-");
                        if (categoryStack.Count == 0)
                            sourceCategory.AddChild(newItem);
                        else
                            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            categoryStack.Last().AddChild(newItem);
#pragma warning restore UA2001
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
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    categoryStack.Last().AddChild(newItem);
#pragma warning restore UA2001
            }
        }

        internal void FindAssets()
        {
            m_Assets = AssetDatabase.FindAllAssets(m_SearchFilter);
            using var pooledHashSet = HashSetPool<string>.Get(out var assetGuids);

            int assetCount = 0;
            foreach (var property in m_Assets)
            {
                m_AssetIDAndPathPair[property.guid] = AssetDatabase.GetAssetPath(property.entityId);
                assetGuids.Add(property.guid);
                assetCount++;
            }

            // If an asset is deleted, we need to remove it from the cache.
            if (m_AssetIDAndPathPair.Count > assetCount)
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
