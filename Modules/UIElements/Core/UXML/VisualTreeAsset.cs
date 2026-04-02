// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// An instance of this class holds a tree of `VisualElementAsset's`, created from a UXML file. Each node in the file corresponds to a `VisualElementAsset`. You can clone a `VisualTreeAsset` to create a tree of `VisualElement's`.
    ///
    /// **Note**: You can't generate a `VisualTreeAsset` from raw UXML at runtime.
    /// </summary>
    /// <example>
    /// The following example loads a VisualTreeAsset from a UXML file in a custom Editor script.
    /// <code source="../../Tests/UIElementsExamples/Assets/ui-toolkit-manual-code-examples/doc-examples/VisualTreeAssetExample.cs" />
    /// </example>

    [HelpURL("UIE-VisualTree-landing")]
    [Serializable]
    public class VisualTreeAsset : ScriptableObject
    {
        /// <undoc/>
        internal delegate void AuthoringIdConflictResolvedHandler(UxmlAsset asset, int oldId, int newId);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string NoRegisteredFactoryErrorMessage = "Element '{0}' is missing a UxmlElementAttribute and has no registered factory method. Please ensure that you have the correct namespace imported.";
        internal const string TemplateAliasExistsError = $"{nameof(VisualTreeAsset)}: could not register a template alias for asset `{{0}}`, alias is already defined for asset '{{1}}'";

        [SerializeField]
        bool m_ImportedWithErrors;

        /// <summary>
        /// Whether there were errors encountered while importing the UXML File
        /// </summary>
        public bool importedWithErrors
        {
            get { return m_ImportedWithErrors; }
            internal set { m_ImportedWithErrors = value; }
        }

        [SerializeField]
        bool m_HasEditorElements;
        internal bool hasEditorElements
        {
            get { return m_HasEditorElements; }
            set { m_HasEditorElements = value; }
        }

        [SerializeField]
        bool m_HasUpdatedUrls;

        /// <summary>
        /// Indicates that some asset Urls were updated and that if we saved the asset again they could be updated.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool importerWithUpdatedUrls
        {
            get { return m_HasUpdatedUrls; }
            set { m_HasUpdatedUrls = value; }
        }

        [SerializeField]
        bool m_ImportedWithWarnings;

        [SerializeField]
        bool m_ImportedWithObsoleteAttributeNames;

        /// <summary>
        /// Whether there were warnings encountered while importing the UXML File
        /// </summary>
        public bool importedWithWarnings
        {
            get { return m_ImportedWithWarnings; }
            internal set { m_ImportedWithWarnings = value; }
        }

        internal bool importedWithObsoleteAttributeNames
        {
            get => m_ImportedWithObsoleteAttributeNames;
            set => m_ImportedWithObsoleteAttributeNames = value;
        }

        private static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();
        private static readonly List<int> s_VeaIdsPath = new List<int>();

        [Serializable]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct UsingEntry
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal static readonly IComparer<UsingEntry> comparer = new UsingEntryComparer();

            [SerializeField] public string alias;

            [SerializeField] public string path;

            [SerializeField] public VisualTreeAsset asset;

            public UsingEntry(string alias, string path)
            {
                this.alias = alias;
                this.path = path;
                this.asset = null;
            }

            public UsingEntry(string alias, VisualTreeAsset asset)
            {
                this.alias = alias;
                this.path = null;
                this.asset = asset;
            }
        }

        private class UsingEntryComparer : IComparer<UsingEntry>
        {
            public int Compare(UsingEntry x, UsingEntry y)
            {
                return string.CompareOrdinal(x.alias, y.alias);
            }
        }

        [Serializable]
        internal struct SlotDefinition
        {
            [SerializeField] public string name;

            [SerializeField] public int insertionPointId;
        }

        [Serializable]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct SlotUsageEntry
        {
            [SerializeField] public string slotName;
            [SerializeField] public int assetId;

            public SlotUsageEntry(string slotName, int assetId)
            {
                this.slotName = slotName;
                this.assetId = assetId;
            }
        }

        [Serializable]
        struct AssetEntry
        {
            [SerializeField] string m_Path;
            [SerializeField] string m_TypeFullName;
            [SerializeField] LazyLoadReference<Object> m_AssetReference;
            [SerializeField] EntityId m_EntityId;

            Type m_CachedType;
            public Type type => m_CachedType ??= Type.GetType(m_TypeFullName);
            public string path => m_Path;

            public Object asset
            {
                get
                {
                    if (m_AssetReference.isSet)
                    {
                        return m_AssetReference.asset == null && m_EntityId != EntityId.None ? CreateMissingReferenceObject(m_EntityId) : m_AssetReference.asset;
                    }

                    return m_EntityId != EntityId.None ? CreateMissingReferenceObject(m_EntityId) : null;
                }
            }

            public AssetEntry(string path, Type type, Object asset)
            {
                m_Path = path;
                m_TypeFullName = type.AssemblyQualifiedName;
                m_CachedType = type;
                m_AssetReference = asset;
                m_EntityId = asset is Object ? asset.GetEntityId() : EntityId.None;
            }
        }

        [SerializeField] List<UsingEntry> m_Usings = new List<UsingEntry>();

        internal List<UsingEntry> usings
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_Usings;
        }

        /// <summary>
        /// The UXML templates used by this VisualTreeAsset.
        /// </summary>
        public IEnumerable<VisualTreeAsset> templateDependencies
        {
            get
            {
                if (m_Usings.Count == 0)
                    yield break;

                HashSet<VisualTreeAsset> sent = new HashSet<VisualTreeAsset>();

                foreach (var entry in m_Usings)
                {
                    if (entry.asset != null && !sent.Contains(entry.asset))
                    {
                        sent.Add(entry.asset);
                        yield return entry.asset;
                    }
                    else if (!string.IsNullOrEmpty(entry.path))
                    {
                        var vta = Panel.LoadResource(entry.path, typeof(VisualTreeAsset),1) as
                            VisualTreeAsset;
                        if (vta != null && !sent.Contains(entry.asset))
                        {
                            sent.Add(entry.asset);
                            yield return vta;
                        }
                    }
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        [SerializeField] internal StyleSheet inlineSheet;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal StyleSheet GetOrCreateInlineStyleSheet()
        {
            if (inlineSheet == null)
                inlineSheet = StyleSheetUtility.CreateInstanceWithHideFlags();
            return inlineSheet;
        }

        [SerializeReference] private VisualElementAsset m_VisualTree;

        [NonSerialized] Dictionary<int, UxmlAsset> m_UsedIds;
        Dictionary<int, UxmlAsset> UsedIds
        {
            get
            {
                if (m_UsedIds == null)
                {
                    m_UsedIds = new Dictionary<int, UxmlAsset>();
                    CacheExistingIds();
                }
                return m_UsedIds;
            }
        }

        internal event AuthoringIdConflictResolvedHandler onAuthoringIdConflictResolved;

        internal VisualElementAsset visualTreeNoAlloc
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get
            {
                return m_VisualTree;
            }
        }

        internal VisualElementAsset visualTree
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get
            {
                if (m_VisualTree != null)
                {
                    return m_VisualTree;
                }
                var root = new VisualElementAsset("UnityEngine.UIElements.UXML");
                SetRootAsset(root);

                return root;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetRootAsset(VisualElementAsset root)
        {
            if (null != m_VisualTree)
                throw new InvalidOperationException("Trying to set a root asset, but it already exists");
            m_VisualTree = root;
            root.SetVisualTreeAsset(this);
        }

        /// <summary>
        /// The stylesheets used by this VisualTreeAsset.
        /// </summary>
        public IEnumerable<StyleSheet> stylesheets
        {
            get
            {
                using var setHandle = HashSetPool<StyleSheet>.Get(out var sent);
                using var _ = ListPool<UxmlAsset>.Get(out var list);
                list.AddRange(DepthFirstTraversal());

                foreach (var asset in list)
                {
                    if (asset is not VisualElementAsset vea)
                        continue;

                    if (vea.hasStylesheets)
                    {
                        foreach (var stylesheet in vea.stylesheets)
                        {
                            if (!sent.Contains(stylesheet))
                            {
                                sent.Add(stylesheet);
                                yield return stylesheet;
                            }
                        }
                    }

                    if (vea.hasStylesheetPaths)
                    {
                        foreach (var stylesheetPath in vea.stylesheetPaths)
                        {
                            var stylesheet =
                                Panel.LoadResource(stylesheetPath, typeof(StyleSheet), 1) as StyleSheet;
                            if (stylesheet != null && !sent.Contains(stylesheet))
                            {
                                sent.Add(stylesheet);
                                yield return stylesheet;
                            }
                        }
                    }
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal UxmlObjectAsset AddUxmlObject(UxmlAsset parent, string fieldUxmlName, string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
        {
            if (string.IsNullOrEmpty(fieldUxmlName))
            {
                var newAsset = new UxmlObjectAsset(fullTypeName, false, xmlNamespace);
                parent.Add(newAsset);
                return newAsset;
            }

            var fieldAsset = parent.GetField(fieldUxmlName);
            if (fieldAsset == null)
            {
                fieldAsset = new UxmlObjectAsset(fieldUxmlName, true, xmlNamespace);
                parent.Add(fieldAsset);
            }

            return AddUxmlObject(fieldAsset, null, fullTypeName, xmlNamespace);
        }

        private void Awake__Internal()
        {
            SetupReferences();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetupReferences()
        {
            foreach (var asset in DepthFirstTraversal())
            {
                asset.SetVisualTreeAssetWithOutNotify(this);
            }
        }

        [SerializeField]
        List<AssetEntry> m_AssetEntries = new List<AssetEntry>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool AssetEntryExists(string path, Type type)
        {
            foreach (var entry in m_AssetEntries)
            {
                if (entry.path == path && entry.type == type)
                    return true;
            }

            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RegisterAssetEntry(string path, Type type, Object asset)
        {
            m_AssetEntries.Add(new AssetEntry(path, type, asset));
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void TransferAssetEntries(VisualTreeAsset otherVta)
        {
            m_AssetEntries.Clear();
            m_AssetEntries.AddRange(otherVta.m_AssetEntries);
        }

        internal T GetAsset<T>(string path) where T : Object => GetAsset(path, typeof(T)) as T;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Object GetAsset(string path, Type type)
        {
            foreach (var entry in m_AssetEntries)
            {
                if (entry.path == path && type.IsAssignableFrom(entry.type))
                    return entry.asset;
            }

            return null;
        }

        internal Type GetAssetType(string path)
        {
            foreach (var entry in m_AssetEntries)
            {
                if (entry.path == path)
                    return entry.type;
            }

            return null;
        }

        #pragma warning disable CS0618 // Type or member is obsolete
        internal IBaseUxmlObjectFactory GetUxmlObjectFactory(UxmlObjectAsset uxmlObjectAsset)
        {
            if (!UxmlObjectFactoryRegistry.factories.TryGetValue(uxmlObjectAsset.fullTypeName, out var factories))
            {
                Debug.LogErrorFormat("Element '{0}' has no registered factory method.", uxmlObjectAsset.fullTypeName);
                return null;
            }

            IBaseUxmlObjectFactory factory = null;
            var ctx = new CreationContext(this);
            foreach (var f in factories)
            {
                if (f.AcceptsAttributeBag(uxmlObjectAsset, ctx))
                {
                    factory = f;
                    break;
                }
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Element '{0}' has a no factory that accept the set of XML attributes specified.", uxmlObjectAsset.fullTypeName);
                return null;
            }

            return factory;
        }
        #pragma warning restore CS0618 // Type or member is obsolete

        [SerializeField] private List<SlotDefinition> m_Slots = new List<SlotDefinition>();

        internal List<SlotDefinition> slots => m_Slots;

        [SerializeField] private int m_ContentContainerId;
        [SerializeField] private int m_ContentHash;

        internal int contentContainerId
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get { return m_ContentContainerId; }
            set { m_ContentContainerId = value; }
        }

        /// <summary>
        /// Build a tree of VisualElements from the asset.
        /// </summary>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        public TemplateContainer Instantiate()
        {
            TemplateContainer target = new TemplateContainer(name, this);
            try
            {
                var cc = new CreationContext(s_TemporarySlotInsertionPoints, null, null, null, null, s_VeaIdsPath, null, null);
                CloneTree(target, cc, null);
            }
            finally
            {
                s_TemporarySlotInsertionPoints.Clear();
                s_VeaIdsPath.Clear();
            }

            return target;
        }

        /// <summary>
        /// Build a tree of VisualElements from the asset and fills a <see cref="VisualElementAssetReferenceTable"/>.
        /// </summary>
        /// <param name="referenceTable">A table that can be used to resolve references.</param>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        internal TemplateContainer Instantiate(out VisualElementAssetReferenceTable referenceTable)
        {
            TemplateContainer target = new TemplateContainer(name, this);
            referenceTable = VisualElementAssetReferenceTable.Create(target);
            try
            {
                var cc = new CreationContext(s_TemporarySlotInsertionPoints, null, null, null, null, s_VeaIdsPath, null, null);
                CloneTree(target, cc, referenceTable.root);
            }
            finally
            {
                s_TemporarySlotInsertionPoints.Clear();
                s_VeaIdsPath.Clear();
            }

            return target;
        }

        /// <summary>
        /// Build a tree of VisualElements from the asset.
        /// </summary>
        /// <param name="bindingPath">The path to the property that you want to bind to the root of the cloned tree.</param>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        public TemplateContainer Instantiate(string bindingPath)
        {
            var tc = Instantiate();
            tc.bindingPath = bindingPath;
            return tc;
        }

        /* Will be deprecated. Use Instantiate() instead. */
        /// <summary>
        /// Build a tree of VisualElements from the asset.
        /// </summary>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        public TemplateContainer CloneTree()
        {
            return Instantiate();
        }

        /* Will be deprecated. Use Instantiate(string bindingPath) instead. */
        /// <summary>
        /// Build a tree of VisualElements from the asset.
        /// </summary>
        /// <param name="bindingPath">The path to the property that you want to bind to the root of the cloned tree.</param>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        public TemplateContainer CloneTree(string bindingPath)
        {
            return Instantiate(bindingPath);
        }

        /// <summary>
        /// Builds a tree of VisualElements from the asset.
        /// </summary>
        /// <param name="target">A VisualElement that will act as the root of the cloned tree.</param>
        public void CloneTree(VisualElement target)
        {
            CloneTree(target, out _, out _);
        }

        /// <summary>
        /// Builds a tree of VisualElements from the asset and fills a <see cref="VisualElementAssetReferenceTable"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="referenceTable">A table to use to resolve references.</param>
        /// <example>
        /// This example shows how to use the `VisualElementAssetReferenceTable` to resolve references to VisualElements after calling CloneTree.
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementAssetReferenceTable_CloneTreeExample.cs"/>
        /// </example>
        public void CloneTree(VisualElement target, out VisualElementAssetReferenceTable referenceTable) => CloneTree(target, out _, out _, out referenceTable);

        public void CloneTree(VisualElement target, out int firstElementIndex, out int elementAddedCount)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            firstElementIndex = target.childCount;
            try
            {
                var cc = new CreationContext(s_TemporarySlotInsertionPoints, null, null, null, null, s_VeaIdsPath, null, null);
                CloneTree(target, cc, null);
            }
            finally
            {
                elementAddedCount = target.childCount - firstElementIndex;
                s_TemporarySlotInsertionPoints.Clear();
                s_VeaIdsPath.Clear();
            }
        }

        internal void CloneTree(VisualElement target, out int firstElementIndex, out int elementAddedCount, out VisualElementAssetReferenceTable referenceTable)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            referenceTable = VisualElementAssetReferenceTable.Create(target);

            firstElementIndex = target.childCount;
            try
            {
                var cc = new CreationContext(s_TemporarySlotInsertionPoints, null, null, null, null, s_VeaIdsPath, null, null);
                CloneTree(target, cc, referenceTable.root);
            }
            finally
            {
                elementAddedCount = target.childCount - firstElementIndex;
                s_TemporarySlotInsertionPoints.Clear();
                s_VeaIdsPath.Clear();
            }
        }

        internal void CloneTree(VisualElement target, CreationContext cc, VisualElementAssetReferenceTable.DocumentNode parentAuthoringNode)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            // The top most element should be a root element.
            if (null == m_VisualTree)
                return;

            var root = m_VisualTree;
            AssignClassListFromAssetToElement(root, target);
            AssignStyleSheetFromAssetToElement(root, target);

            for (var i = 0; i < root.childCount; ++i)
            {
                // Assumes m_VisualElementAssets only contain VisualElementAsset.
                var child = root[i] as VisualElementAsset;

                var isTemplate = false;
                if (child is TemplateAsset)
                {
                    cc.veaIdsPath.Add(child.id);
                    isTemplate = true;
                }

                var newCc = new CreationContext(cc.slotInsertionPoints, cc.attributeOverrides, cc.serializedDataOverrides,
                    this, target, cc.veaIdsPath, null, cc.templateAsset);

                var childElement = CloneSetupRecursively(child, newCc, parentAuthoringNode);

                if (isTemplate)
                {
                    cc.veaIdsPath.Remove(child.id);
                }

                if (null != childElement)
                {
                    // if contentContainer == this, the shadow and the logical hierarchy are identical
                    // otherwise, if there is a CC, we want to insert in the shadow
                    target.hierarchy.Add(childElement);
                }
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset asset, CreationContext context, VisualElementAssetReferenceTable.DocumentNode parentAuthoringNode)
        {
            if (asset.skipClone)
                return null;

            var ve = Create(asset, context, parentAuthoringNode);

            if (ve == null)
                return null;

            // Save reference to the visualElementAsset so elements can be reinitialized when
            // we set their attributes in the editor
            ve.visualElementAsset = asset;

            // Save reference to the VisualTreeAsset itself on the containing VisualElement so it can be
            // tracked for live reloading on changes, and also accessible for users that need to keep track
            // of their cloned VisualTreeAssets.
            ve.visualTreeAssetSource = this;

            // context.target is the created templateContainer
            if (asset.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer tc)
                    tc.SetContentContainer(ve);
                else
                    Debug.LogError(
                        "Trying to clone a VisualTreeAsset with a custom content container into a element which is not a template container");
            }

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            if (context.slotInsertionPoints != null && TryGetSlotInsertionPoint(asset.id, out var slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve);
            }

            if (asset.ruleIndex != -1)
            {
                if (inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    var rule = inlineSheet.rules[asset.ruleIndex];
                    ve.SetInlineRule(inlineSheet, rule);
                }
            }

            var templateAsset = asset as TemplateAsset;

            for (var i = 0; i < asset.childCount; ++i)
            {
                var childVea = asset[i] as VisualElementAsset;

                // It can be a UxmlObjectAsset. We only want to clone VisualElementAssets
                if (childVea == null)
                {
                    continue;
                }

                var isTemplate = false;
                if (childVea is TemplateAsset)
                {
                    context.veaIdsPath.Add(childVea.id);
                    isTemplate = true;
                }

                var childVe = CloneSetupRecursively(childVea, context, parentAuthoringNode);

                if (isTemplate)
                {
                    context.veaIdsPath.Remove(childVea.id);
                }

                if (childVe == null)
                    continue;

                childVe.visualTreeAssetSource = this;

                // Save reference to the visualElementAsset so elements can be reinitialized when
                // we set their attributes in the editor
                childVe.visualElementAsset = childVea;

                var index = templateAsset?.slotUsages?.FindIndex(u => u.assetId == childVea.id) ?? -1;
                if (index != -1)
                {
                    VisualElement parentSlot;
                    var key = templateAsset.slotUsages[index].slotName;
                    Assert.IsFalse(string.IsNullOrEmpty(key),
                        "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                    if (context.slotInsertionPoints == null ||
                        !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                    {
                        Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key,
                            context.slotInsertionPoints == null
                                ? String.Empty
                                : String.Join(", ", context.slotInsertionPoints.Keys));
                        ve.Add(childVe);
                    }
                    else
                        parentSlot.Add(childVe);
                }
                else
                    ve.Add(childVe);
            }

            if (templateAsset != null && context.slotInsertionPoints != null)
                context.slotInsertionPoints.Clear();

            return ve;
        }

        internal bool SlotDefinitionExists(string slotName)
        {
            if (m_Slots.Count == 0)
                return false;

            return m_Slots.Exists(s => s.name == slotName);
        }

        internal bool AddSlotDefinition(string slotName, int resId)
        {
            if (SlotDefinitionExists(slotName))
                return false;

            m_Slots.Add(new SlotDefinition {insertionPointId = resId, name = slotName});
            return true;
        }

        internal bool TemplateIsAssetReference(TemplateAsset templateAsset)
        {
            if (TryGetUsingEntry(templateAsset.templateAlias, out UsingEntry usingEntry))
            {
                return usingEntry.asset != null;
            }
            throw new ArgumentException($"Template {templateAsset.templateAlias} isn't registered in this VisualTreeAsset ");
        }

        internal void FindElementsByNameInTemplate(TemplateAsset templateAsset, string visualElementName, List<VisualElementAsset> results)
        {
            if (TryGetUsingEntry(templateAsset.templateAlias, out UsingEntry usingEntry))
            {
                if (usingEntry.asset)
                {
                    usingEntry.asset.FindElementsByName(visualElementName, results);
                }
            }
        }

        UxmlAsset FindElementById(long id)
        {
            foreach (var element in DepthFirstTraversal())
            {
                if (element.id == id)
                    return element;
            }
            return null;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElementAsset FindElementByPath(ReadOnlySpan<int> authoringPath, List<VisualElementAsset> pathToElement = null)
        {
            // Is this a root reference?
            if (authoringPath.Length == 1 && authoringPath[0] == 0)
                return visualTree;

            var currentTree = this;
            UxmlAsset uxmlAsset = null;
            for (int i = 0; i < authoringPath.Length; i++)
            {
                var id = authoringPath[i];
                var element = currentTree.FindElementById(id) as VisualElementAsset;
                if (element == null)
                    return null;

                if (pathToElement != null)
                    pathToElement.Add(element);

                if (element is TemplateAsset templateAsset)
                {
                    if (templateAsset.ResolveTemplate() is {} templateAssetResolved)
                    {
                        currentTree = templateAssetResolved;
                        uxmlAsset = element;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (i < authoringPath.Length - 1)
                {
                    // Intermediate elements must be TemplateAssets
                    return null;
                }
                else
                {
                    // Last element in the path can be any UxmlAsset
                    uxmlAsset = element;
                }
            }

            return uxmlAsset as VisualElementAsset;
        }

        void FindElementsByName(string visualElementName, List<VisualElementAsset> results)
        {
            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(DepthFirstTraversal());
            foreach (var asset in list)
            {
                if (asset is not VisualElementAsset visualElementAsset)
                    continue;

                if (asset is TemplateAsset templateAsset)
                {
                    FindElementsByNameInTemplate(templateAsset, visualElementName, results);
                }
                else if (visualElementAsset.TryGetAttributeValue("name", out string value))
                {
                    if (value == visualElementName)
                    {
                        results.Add(visualElementAsset);
                    }
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryGetSlotInsertionPoint(int insertionPointId, out string slotName)
        {
            for (var index = 0; index < m_Slots.Count; index++)
            {
                var slotDefinition = m_Slots[index];
                if (slotDefinition.insertionPointId == insertionPointId)
                {
                    slotName = slotDefinition.name;
                    return true;
                }
            }

            slotName = null;
            return false;
        }

        internal bool TryGetUsingEntry(string templateName, out UsingEntry entry)
        {
            entry = default;

            if (m_Usings.Count == 0)
                return false;
            int index = m_Usings.BinarySearch(new UsingEntry(templateName, string.Empty), UsingEntry.comparer);
            if (index < 0)
                return false;

            entry = m_Usings[index];
            return true;
        }

        private void RemoveUsingEntry(UsingEntry entry)
        {
            m_Usings.Remove(entry);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal VisualTreeAsset ResolveTemplate(string templateName)
        {
            if (!TryGetUsingEntry(templateName, out UsingEntry entry))
                return null;

            if (entry.asset)
                return entry.asset;

            string path = entry.path;
            return Panel.LoadResource(path, typeof(VisualTreeAsset),1) as VisualTreeAsset;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal bool TemplateExists(string templateName)
        {
            if (m_Usings.Count == 0)
                return false;
            var index = m_Usings.BinarySearch(new UsingEntry(templateName, string.Empty), UsingEntry.comparer);
            return index >= 0;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RegisterTemplate(string templateName, string path)
        {
            InsertUsingEntry(new UsingEntry(templateName, path));
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void RegisterTemplate(string templateName, VisualTreeAsset asset)
        {
            InsertUsingEntry(new UsingEntry(templateName, asset));
        }

        internal bool TryRegisterTemplate(string templateName, VisualTreeAsset asset)
        {
            if (!asset || asset == null)
                throw new ArgumentNullException(nameof(asset));

            if (TemplateExists(templateName))
            {
                if (TryGetUsingEntry(templateName, out var entry) && asset == entry.asset)
                    return false;

                Debug.LogWarningFormat(TemplateAliasExistsError, asset, entry.asset);
                return false;
            }

            RegisterTemplate(templateName, asset);
            return true;
        }

        internal bool TryUnregisterTemplate(string templateName)
        {
            using var _ = ListPool<TemplateAsset>.Get(out var otherTemplates);
            otherTemplates.AddRange(DepthFirstTraversalOfType<TemplateAsset>());

            // If the template alias in not in use.
            if (!TryGetUsingEntry(templateName, out var entry))
                return false;

            // If there are no template nodes, it is safe to remove.
            if (otherTemplates.Count == 0)
            {
                RemoveUsingEntry(entry);
                return true;
            }

            foreach (var otherTemplate in otherTemplates)
            {
                if (string.CompareOrdinal(templateName, otherTemplate.templateAlias) == 0)
                {
                    return false;
                }
            }

            // If no other template nodes are linking it, it is safe to remove.
            RemoveUsingEntry(entry);
            return true;
        }

        private void InsertUsingEntry(UsingEntry entry)
        {
            // find insertion index so usings are sorted by alias
            int i = 0;
            while (i < m_Usings.Count && String.CompareOrdinal(entry.alias, m_Usings[i].alias) > 0)
                i++;

            m_Usings.Insert(i, entry);
        }

        #pragma warning disable CS0618 // Type or member is obsolete
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static VisualElement Create(VisualElementAsset asset, CreationContext ctx, VisualElementAssetReferenceTable.DocumentNode parentAuthoringNode = null)
        {
            VisualElement CreateError()
            {
                Debug.LogErrorFormat(NoRegisteredFactoryErrorMessage, asset.fullTypeName);
                return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
            }

            // The type is known by UxmlSerializedData system use that instead to create the element.
            if (asset.serializedData != null)
                return asset.Instantiate(ctx, parentAuthoringNode);

            if (!VisualElementFactoryRegistry.TryGetValue(asset.fullTypeName, out var factoryList))
            {
                if (asset.fullTypeName.StartsWith("UnityEngine.Experimental.UIElements.") || asset.fullTypeName.StartsWith("UnityEditor.Experimental.UIElements."))
                {
                    string experimentalTypeName = asset.fullTypeName.Replace(".Experimental.UIElements", ".UIElements");
                    if (!VisualElementFactoryRegistry.TryGetValue(experimentalTypeName, out factoryList))
                    {
                        return CreateError();
                    }
                }
                else if (asset.fullTypeName == UxmlRootElementFactory.k_ElementName)
                {
                    // Support UXML without namespace for backward compatibility.
                    VisualElementFactoryRegistry.TryGetValue(typeof(UxmlRootElementFactory).Namespace + "." + asset.fullTypeName, out factoryList);
                }
                else
                {
                    return CreateError();
                }
            }

            IUxmlFactory factory = null;
            foreach (var f in factoryList)
            {
                if (f.AcceptsAttributeBag(asset, ctx))
                {
                    factory = f;
                    break;
                }
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Element '{0}' has a no factory that accept the set of XML attributes specified.", asset.fullTypeName);
                return new Label(string.Format("Type with no factory: '{0}'", asset.fullTypeName));
            }

            var ve = factory.Create(asset, ctx);
            if (ve != null)
            {
                AssignClassListFromAssetToElement(asset, ve);
                AssignStyleSheetFromAssetToElement(asset, ve);
            }

            return ve;
        }
        #pragma warning restore CS0618 // Type or member is obsolete

        static void AssignClassListFromAssetToElement(VisualElementAsset asset, VisualElement element)
        {
            if (asset.classes != null)
            {
                element.AddToClassList(asset.classes);
            }
        }

        static void AssignStyleSheetFromAssetToElement(VisualElementAsset asset, VisualElement element)
        {
            if (asset.hasStylesheetPaths)
            {
                for (int i = 0; i < asset.stylesheetPaths.Count; i++)
                {
                    element.AddStyleSheetPath(asset.stylesheetPaths[i]);
                }
            }

            if (asset.hasStylesheets)
            {
                for (int i = 0; i < asset.stylesheets.Count; ++i)
                {
                    if (asset.stylesheets[i] != null)
                    {
                        element.styleSheets.Add(asset.stylesheets[i]);
                    }
                }
            }
        }

        /// <summary>
        /// A hash value computed from the template content.
        /// </summary>
        public int contentHash
        {
            get { return m_ContentHash; }
            set { m_ContentHash = value; }
        }

        internal void ExtractUsedUxmlQualifiedNames(HashSet<string> names)
        {
            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(DepthFirstTraversal());

            foreach (var asset in list)
            {
                if (asset is not VisualElementAsset)
                    continue;
                names.Add(asset.fullTypeName);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal IEnumerable<UxmlAsset> DepthFirstTraversal()
        {
            if (null == m_VisualTree)
                return Array.Empty<UxmlAsset>();
            return DepthFirstTraversal(m_VisualTree);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal IEnumerable<T> DepthFirstTraversalOfType<T>()
        {
            var elements = DepthFirstTraversal();

            foreach (var element in elements)
            {
                if (element is T tElement)
                {
                    yield return tElement;
                }
            }
        }

        internal IEnumerable<UxmlAsset> DepthFirstTraversal(UxmlAsset asset)
        {
            yield return asset;

            for (var i = 0; i < asset.childCount; ++i)
            {
                foreach (var child in DepthFirstTraversal(asset[i]))
                {
                    yield return child;
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int DepthFirstTraversalIndexOf(UxmlAsset uxmlAsset)
        {
            var index = 0;
            var assets = DepthFirstTraversal();

            foreach (var asset in assets)
            {
                if (asset == uxmlAsset)
                    return index;
                index++;
            }

            return -1;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int GenerateNewId(UxmlAsset vea)
        {
            return GenerateNewId(vea, vea.SiblingIndex(), UsedIds);
        }

        internal static int GenerateNewId(UxmlAsset uxmlAsset, Dictionary<int, UxmlAsset> excludeIds)
        {
            return GenerateNewId(uxmlAsset, uxmlAsset.SiblingIndex(), excludeIds);
        }

        internal static int GenerateNewId(UxmlAsset uxmlAsset, int siblingIndex, Dictionary<int, UxmlAsset> excludeIds)
        {
            if (uxmlAsset == null)
                throw new NullReferenceException();

            if (uxmlAsset.visualTreeAsset == null)
                throw new InvalidOperationException("Trying to generate an id for an asset that is not added to the hierarchy");

            // Root
            if (uxmlAsset == uxmlAsset.visualTreeAsset.visualTree)
            {
                return uxmlAsset.visualTreeAsset.contentHash == 0 ? Guid.NewGuid().GetHashCode() : uxmlAsset.visualTreeAsset.contentHash;
            }

            var typeNameHash = GetStableHashCode(uxmlAsset.fullTypeName);

            var conflictIndex = 0;

            unchecked
            {
                int newId;
                do
                {
                    newId = 17;
                    newId = (newId * 31) + uxmlAsset.parentAsset.id;
                    newId = (newId * 31) + typeNameHash;
                    newId = (newId * 31) + siblingIndex;
                    newId = (newId * 31) + conflictIndex++;
                } while (excludeIds.ContainsKey(newId) || newId == 0);
                return newId;
            }
        }

        private static int GetStableHashCode(string str)
        {
            unchecked
            {
                var hash = 0;
                foreach (var c in str)
                {
                    hash ^= (hash * 31) + c;
                }
                return hash;
            }
        }

        private bool IdExists(int id)
        {
            return UsedIds.ContainsKey(id);
        }

        internal void UnregisterId(UxmlAsset uxmlAsset)
        {
            if (m_UsedIds == null)
                return;
            if (m_UsedIds.TryGetValue(uxmlAsset.id, out var asset) && asset == uxmlAsset)
            {
                m_UsedIds.Remove(uxmlAsset.id);
            }
        }

        internal void RegisterId(UxmlAsset uxmlAsset, int siblingIndex = -1)
        {
            // Invalid id, regenerate one
            if (uxmlAsset.id == 0)
            {
                uxmlAsset.id = GenerateNewId(uxmlAsset, siblingIndex < 0 ? uxmlAsset.SiblingIndex() : siblingIndex, UsedIds);
                UsedIds[uxmlAsset.id] = uxmlAsset;
                if (uxmlAsset.hasAuthoringId)
                {
                    // Log/Report authoring conflict.
                    onAuthoringIdConflictResolved?.Invoke(uxmlAsset, 0, uxmlAsset.id);
                    uxmlAsset.hasAuthoringId = false;
                }
                return;
            }

            if(IdExists(uxmlAsset.id))
            {
                // Try to preserve the non-generated id.
                if (uxmlAsset.hasAuthoringId)
                {
                    var previousEntry = m_UsedIds[uxmlAsset.id];
                    // If the id is already used by a non-generated id, report an error
                    if (previousEntry.hasAuthoringId)
                    {
                        var previousId = uxmlAsset.id;
                        uxmlAsset.id = GenerateNewId(uxmlAsset, siblingIndex < 0 ? uxmlAsset.SiblingIndex() : siblingIndex, UsedIds);
                        UsedIds[uxmlAsset.id] = uxmlAsset;
                        // Log/Report authoring conflict.
                        onAuthoringIdConflictResolved?.Invoke(uxmlAsset, previousId, uxmlAsset.id);
                        uxmlAsset.hasAuthoringId = false;
                    }
                    // Assign a new id to the generated one
                    else
                    {
                        previousEntry.id = GenerateNewId(previousEntry, previousEntry.SiblingIndex(), UsedIds);
                        UsedIds[previousEntry.id] = previousEntry;
                        UsedIds[uxmlAsset.id] = uxmlAsset;
                    }
                }
                else
                {
                    uxmlAsset.id = GenerateNewId(uxmlAsset, siblingIndex < 0 ? uxmlAsset.SiblingIndex() : siblingIndex, UsedIds);
                    UsedIds[uxmlAsset.id] = uxmlAsset;
                }
            }
            else
            {
                UsedIds.Add(uxmlAsset.id, uxmlAsset);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElementAsset ReparentElementInDocument(VisualElementAsset vea, VisualElementAsset newParent, int index = -1)
        {
            var actualParent = newParent ?? visualTree;
            var actualIndex = index == -1 ? actualParent.childCount : index;

            actualParent.Insert(actualIndex, vea);

            // HACK: We clear ALL stylesheets here if element is no longer at root.
            // this is fine as long as we only support one uss but when we support more
            // we need to make sure we only remove the stylesheets that make sense to remove.
            // See: https://unity3d.atlassian.net/browse/UIT-469
            if (vea.isRoot)
            {
                vea.stylesheetPaths.Clear();
                vea.stylesheets.Clear();
            }

            return vea;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void Swallow(VisualElementAsset parent, VisualTreeAsset other)
        {
            using var _ = ListPool<UxmlAsset>.Get(out var list);
            var actualParent = parent ?? visualTree;

            for (var i = 0; i < other.visualTree.childCount; ++i)
            {
                list.Add(other.visualTree[i]);
            }

            for (var i = 0; i < list.Count; ++i)
            {
                var child = list[i];
                actualParent.Add(child);
            }
        }

        internal static void SwallowStyleRule(VisualTreeAsset previous, VisualTreeAsset next, VisualElementAsset vea)
        {
            if (vea.ruleIndex < 0)
                return;

            var toStyleSheet = next.GetOrCreateInlineStyleSheet();
            var fromStyleSheet = previous.inlineSheet;

            var fromRule = fromStyleSheet.rules[vea.ruleIndex];

            // Add rule to StyleSheet.
            var index = toStyleSheet.rules.Length;
            var toRule = toStyleSheet.AddRule();
            toRule.customPropertiesCount = fromRule.customPropertiesCount;

            // Add property values to sheet.
            for (var i = 0; i < fromRule.properties.Length; ++i)
            {
                var fromProperty = fromRule.properties[i];
                var toProperty = toRule.AddProperty(fromProperty.name);
                toProperty.requireVariableResolve = fromProperty.requireVariableResolve;
                StyleSheetUtility.TransferStylePropertyHandles(fromStyleSheet, fromProperty, toStyleSheet, toProperty);
            }
            vea.ruleIndex = index;
            toStyleSheet.RequestRebuild();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElementAsset AddElementOfType(VisualElementAsset parent, string fullTypeName)
        {
            var xmlns = VisualTreeAssetUtilities.FindUxmlNamespaceDefinitionForTypeName(this, parent, fullTypeName);
            var vea = new VisualElementAsset(fullTypeName, xmlns);
            parent ??= visualTree;
            parent.Add(vea);
            return vea;
        }

        private void CacheExistingIds()
        {
            m_UsedIds.Clear();
            foreach (var uxmlAsset in DepthFirstTraversal())
            {
                if (!m_UsedIds.TryAdd(uxmlAsset.id, uxmlAsset))
                {
                    // Deal with conflict, technically we shouldn't have any.
                }
            }
        }

        /// <summary>
        /// Regenerates the id of the uxml asset nodes inside the provided asset so that they are deterministic
        /// through an export => import operation.
        /// </summary>
        /// <param name="vta">The <see cref="VisualTreeAsset"/>.</param>
        /// <remarks>
        /// This will add an authoring id to the root node if it is not already present.
        /// </remarks>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void HarmonizeIds(VisualTreeAsset vta)
        {
            // Clear previous cache.
            if (vta.m_UsedIds == null)
                vta.m_UsedIds = new Dictionary<int, UxmlAsset>();
            else
                vta.m_UsedIds.Clear();

            var root = vta.visualTree;
            root.hasAuthoringId = true;
            root.SetAttribute(UxmlAsset.AuthoringIdAttribute, root.id.ToString());
            vta.RegisterId(root);

            for (var i = 0; i < root.childCount; ++i)
            {
                HarmonizeIds(root[i], i);
            }
        }

        private static void HarmonizeIds(UxmlAsset uxmlAsset, int siblingIndex)
        {
            if (uxmlAsset.hasAuthoringId)
                uxmlAsset.visualTreeAsset.RegisterId(uxmlAsset, siblingIndex);
            else
                uxmlAsset.id = 0; // This will regerenate the id.
            for(var i = 0; i < uxmlAsset.childCount; ++i)
                HarmonizeIds(uxmlAsset[i], i);
        }
    }

    /// <summary>
    /// This structure holds information used during UXML template instantiation.
    /// </summary>
    public struct CreationContext : IEquatable<CreationContext>
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct AttributeOverrideRange
        {
            internal readonly VisualTreeAsset sourceAsset;
            internal readonly List<TemplateAsset.AttributeOverride> attributeOverrides;

            public AttributeOverrideRange(VisualTreeAsset sourceAsset, List<TemplateAsset.AttributeOverride> attributeOverrides)
            {
                this.sourceAsset = sourceAsset;
                this.attributeOverrides = attributeOverrides;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct SerializedDataOverrideRange
        {
            internal readonly VisualTreeAsset sourceAsset;
            internal readonly int templateId;
            internal readonly List<TemplateAsset.UxmlSerializedDataOverride> attributeOverrides;

            public SerializedDataOverrideRange(VisualTreeAsset sourceAsset, List<TemplateAsset.UxmlSerializedDataOverride> attributeOverrides, int templateId)
            {
                this.sourceAsset = sourceAsset;
                this.attributeOverrides = attributeOverrides;
                this.templateId = templateId;
            }
        }

        /// <undoc/>
        // TODO why is this public? It's not used internally and could be obtained by default(CreationContext)
        public static readonly CreationContext Default = new CreationContext();

        /// <summary>
        /// The element into which the <see cref="visualTreeAsset"/> is being cloned or instantiated.
        /// <see cref="VisualTreeAsset.CloneTree()"/>
        /// <see cref="VisualTreeAsset.Instantiate()"/>
        /// </summary>
        public VisualElement target { get; private set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal List<int> veaIdsPath
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get;
            private set;
        }

        internal TemplateAsset templateAsset { get; private set; }

        /// <summary>
        /// The target UXML file to clone or instantiate.
        /// </summary>
        public VisualTreeAsset visualTreeAsset { get; private set; }

        /// <undoc/>
        // TODO This feature leaks in the API but isn't usable
        public Dictionary<string, VisualElement> slotInsertionPoints { get; private set; }

        internal List<AttributeOverrideRange> attributeOverrides { get; private set; }

        internal List<SerializedDataOverrideRange> serializedDataOverrides { get; private set; }

        internal List<string> namesPath { get; private set; }

        internal bool hasOverrides => attributeOverrides?.Count > 0 || serializedDataOverrides?.Count > 0;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal CreationContext(VisualTreeAsset vta)
            : this((Dictionary<string, VisualElement>) null, vta, null)
        { }

        internal CreationContext(Dictionary<string, VisualElement> slotInsertionPoints)
            : this(slotInsertionPoints, null, null, null)
        { }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<AttributeOverrideRange> attributeOverrides)
            : this(slotInsertionPoints, attributeOverrides, null, null)
        { }

        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            VisualTreeAsset vta, VisualElement target)
            : this(slotInsertionPoints, null, vta, target)
        { }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<AttributeOverrideRange> attributeOverrides,
            VisualTreeAsset vta, VisualElement target)
            : this(slotInsertionPoints, attributeOverrides, null, vta, target, null, null, null)
        { }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<AttributeOverrideRange> attributeOverrides,
            List<SerializedDataOverrideRange> serializedDataOverrides,
            VisualTreeAsset vta, VisualElement target, List<int> veaIdsPath, List<string> namesPath,
            TemplateAsset ta)
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            this.attributeOverrides = attributeOverrides;
            this.serializedDataOverrides = serializedDataOverrides;
            visualTreeAsset = vta;
            this.namesPath = namesPath;
            this.veaIdsPath = veaIdsPath;
            this.templateAsset = ta;
        }

        public override bool Equals(object obj)
        {
            return obj is CreationContext && Equals((CreationContext)obj);
        }

        /// <undoc/>
        public bool Equals(CreationContext other)
        {
            return EqualityComparer<VisualElement>.Default.Equals(target, other.target) &&
                EqualityComparer<VisualTreeAsset>.Default.Equals(visualTreeAsset, other.visualTreeAsset) &&
                EqualityComparer<Dictionary<string, VisualElement>>.Default.Equals(slotInsertionPoints, other.slotInsertionPoints);
        }

        public override int GetHashCode()
        {
            var hashCode = -2123482148;
            hashCode = hashCode * -1521134295 + EqualityComparer<VisualElement>.Default.GetHashCode(target);
            hashCode = hashCode * -1521134295 + EqualityComparer<VisualTreeAsset>.Default.GetHashCode(visualTreeAsset);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, VisualElement>>.Default.GetHashCode(slotInsertionPoints);
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(CreationContext context1, CreationContext context2)
        {
            return context1.Equals(context2);
        }

        /// <undoc/>
        public static bool operator!=(CreationContext context1, CreationContext context2)
        {
            return !(context1 == context2);
        }
    }
}
