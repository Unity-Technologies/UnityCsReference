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
    [HelpURL("UIE-VisualTree-landing")]
    [Serializable]
    public class VisualTreeAsset : ScriptableObject
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string LinkedVEAInTemplatePropertyName = "--unity-linked-vea-in-template";
        internal static string NoRegisteredFactoryErrorMessage = "Element '{0}' is missing a UxmlElementAttribute and has no registered factory method. Please ensure that you have the correct namespace imported.";

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
        bool m_ImportedWithWarnings;

        /// <summary>
        /// Whether there were warnings encountered while importing the UXML File
        /// </summary>
        public bool importedWithWarnings
        {
            get { return m_ImportedWithWarnings; }
            internal set { m_ImportedWithWarnings = value; }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int GetNextChildSerialNumber()
        {
            int n = m_VisualElementAssets?.Count ?? 0;
            n += m_TemplateAssets?.Count ?? 0;

            if (m_UxmlObjectEntries != null)
            {
                n += m_UxmlObjectEntries.Count;
                foreach (var entry in m_UxmlObjectEntries)
                {
                    if (entry.uxmlObjectAssets != null)
                        n += entry.uxmlObjectAssets.Count;
                }
            }
            return n;
        }

        private static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();

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
                return String.CompareOrdinal(x.alias, y.alias);
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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct UxmlObjectEntry
        {
            [SerializeField] public int parentId;
            [SerializeField] public List<UxmlObjectAsset> uxmlObjectAssets;

            public UxmlObjectEntry(int parentId, List<UxmlObjectAsset> uxmlObjectAssets)
            {
                this.parentId = parentId;
                this.uxmlObjectAssets = uxmlObjectAssets;
            }

            public UxmlObjectAsset GetField(string fieldName)
            {
                foreach (var asset in uxmlObjectAssets)
                {
                    if (asset.isField && asset.fullTypeName == fieldName)
                        return asset;
                }

                return null;
            }

            public override string ToString() => $"UxmlObjectEntry parent:{parentId} ({uxmlObjectAssets?.Count})";
        }

        [Serializable]
        struct AssetEntry
        {
            [SerializeField] string m_Path;
            [SerializeField] string m_TypeFullName;
            [SerializeField] LazyLoadReference<Object> m_AssetReference;
            [SerializeField] int m_InstanceID;

            Type m_CachedType;
            public Type type => m_CachedType ??= Type.GetType(m_TypeFullName);
            public string path => m_Path;

            public Object asset
            {
                get
                {
                    if (m_AssetReference.isSet)
                    {
                        return m_AssetReference.asset == null && m_InstanceID != 0 ? CreateMissingReferenceObject(m_InstanceID) : m_AssetReference.asset;
                    }

                    return m_InstanceID != 0 ? CreateMissingReferenceObject(m_InstanceID) : null;
                }
            }

            public AssetEntry(string path, Type type, Object asset)
            {
                m_Path = path;
                m_TypeFullName = type.AssemblyQualifiedName;
                m_CachedType = type;
                m_AssetReference = asset;
                m_InstanceID = asset is Object ? asset.GetInstanceID() : 0;
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField] internal StyleSheet inlineSheet;

        [SerializeField] internal List<VisualElementAsset> m_VisualElementAssets = new List<VisualElementAsset>();

        /// <summary>
        /// The stylesheets used by this VisualTreeAsset.
        /// </summary>
        public IEnumerable<StyleSheet> stylesheets
        {
            get
            {
                HashSet<StyleSheet> sent = new HashSet<StyleSheet>();

                foreach (var vea in m_VisualElementAssets)
                {
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

        [SerializeField] internal List<TemplateAsset> m_TemplateAssets = new List<TemplateAsset>();
        [SerializeField] private List<UxmlObjectEntry> m_UxmlObjectEntries = new List<UxmlObjectEntry>();
        [SerializeField] private List<int> m_UxmlObjectIds = new List<int>();

        internal List<VisualElementAsset> visualElementAssets
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_VisualElementAssets;
        }

        internal List<TemplateAsset> templateAssets
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_TemplateAssets;
        }

        internal List<UxmlObjectEntry> uxmlObjectEntries
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_UxmlObjectEntries;
        }

        internal List<int> uxmlObjectIds
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_UxmlObjectIds;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RemoveElementAndDependencies(VisualElementAsset asset)
        {
            m_VisualElementAssets.Remove(asset);
            RemoveUxmlObjectEntryDependencies(asset.id);
        }

        // Called when parsing Uxml
        internal void RegisterUxmlObject(UxmlObjectAsset uxmlObjectAsset)
        {
            var entry = GetUxmlObjectEntry(uxmlObjectAsset.parentId);

            if (entry.uxmlObjectAssets != null)
            {
                entry.uxmlObjectAssets.Add(uxmlObjectAsset);
            }
            else
            {
                m_UxmlObjectEntries.Add(new UxmlObjectEntry(uxmlObjectAsset.parentId, new List<UxmlObjectAsset> { uxmlObjectAsset }));
                m_UxmlObjectIds.Add(uxmlObjectAsset.id);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal UxmlObjectAsset AddUxmlObject(UxmlAsset parent, string fieldUxmlName, string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
        {
            var entry = GetUxmlObjectEntry(parent.id);
            if (entry.uxmlObjectAssets == null)
            {
                entry = new UxmlObjectEntry(parent.id, new List<UxmlObjectAsset>());
                m_UxmlObjectEntries.Add(entry);
                m_UxmlObjectIds.Add(GetNextUxmlObjectId(parent.id));
            }

            if (string.IsNullOrEmpty(fieldUxmlName))
            {
                var newAsset = new UxmlObjectAsset(fullTypeName, false, xmlNamespace);
                newAsset.parentId = parent.id;
                newAsset.id = GetNextUxmlObjectId(parent.id);
                entry.uxmlObjectAssets.Add(newAsset);
                return newAsset;
            }

            var fieldAsset = entry.GetField(fieldUxmlName);
            if (fieldAsset == null)
            {
                fieldAsset = new UxmlObjectAsset(fieldUxmlName, true, xmlNamespace);
                entry.uxmlObjectAssets.Add(fieldAsset);
                fieldAsset.parentId = parent.id;
                fieldAsset.id = GetNextUxmlObjectId(parent.id);
            }

            return AddUxmlObject(fieldAsset, null, fullTypeName, xmlNamespace);
        }

        int GetNextUxmlObjectId(int parentId)
        {
            return (GetNextChildSerialNumber() + 585386304) * -1521134295 + parentId;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RemoveUxmlObject(int id, bool onlyIfIsField = false)
        {
            for (var i = 0; i < m_UxmlObjectEntries.Count; ++i)
            {
                var entry = m_UxmlObjectEntries[i];
                for (var j = 0; j < entry.uxmlObjectAssets.Count; ++j)
                {
                    var asset = entry.uxmlObjectAssets[j];
                    if (asset.id == id)
                    {
                        if (onlyIfIsField && !asset.isField)
                            return;

                        entry.uxmlObjectAssets.RemoveAt(j);

                        RemoveUxmlObjectEntryDependencies(asset.id);

                        // Remove parent field if empty
                        if (entry.uxmlObjectAssets.Count == 0)
                        {
                            var index = m_UxmlObjectEntries.IndexOf(entry);
                            m_UxmlObjectEntries.RemoveAt(index);
                            m_UxmlObjectIds.RemoveAt(index);

                            RemoveUxmlObject(entry.parentId, true);
                        }
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes UxmlObjectEntry instances with the same parent indicated by the specified parentId.
        /// </summary>
        /// <param name="parentId">The ID of the parent asset.</param>
        void RemoveUxmlObjectEntryDependencies(int parentId)
        {
            if (m_UxmlObjectEntries.Count == 0)
                return;

            // Find direct children
            var uxmlObjectRoots = ListPool<UxmlObjectEntry>.Get();
            foreach (var child in m_UxmlObjectEntries)
            {
                if (parentId == child.parentId)
                {
                    uxmlObjectRoots.Add(child);
                }
            }

            foreach (var entry in uxmlObjectRoots)
            {
                var index = m_UxmlObjectEntries.IndexOf(entry);
                m_UxmlObjectEntries.RemoveAt(index);
                m_UxmlObjectIds.RemoveAt(index);

                foreach (var asset in entry.uxmlObjectAssets)
                {
                    RemoveUxmlObjectEntryDependencies(asset.id);
                }
            }

            ListPool<UxmlObjectEntry>.Release(uxmlObjectRoots);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void CollectUxmlObjectAssets(UxmlAsset parent, string fieldName, List<UxmlObjectAsset> foundEntries)
        {
            foreach (var e in m_UxmlObjectEntries)
            {
                if (e.parentId == parent.id)
                {
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        var fieldAsset = e.GetField(fieldName);
                        if (fieldAsset != null)
                        {
                            CollectUxmlObjectAssets(fieldAsset, null, foundEntries);
                        }
                    }
                    else
                    {
                        foreach (var asset in e.uxmlObjectAssets)
                        {
                            if (!asset.isField)
                                foundEntries.Add(asset);
                        }
                    }
                    return;
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetUxmlObjectAssets(UxmlAsset parent, string fieldName, List<UxmlObjectAsset> entries)
        {
            foreach (var e in m_UxmlObjectEntries)
            {
                if (e.parentId == parent.id)
                {
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        var fieldAsset = e.GetField(fieldName);
                        if (fieldAsset != null)
                        {
                            SetUxmlObjectAssets(fieldAsset, null, entries);
                        }
                    }
                    else
                    {
                        // Remove all non-field assets
                        for (int i = e.uxmlObjectAssets.Count - 1; i >= 0; --i)
                        {
                            if (!e.uxmlObjectAssets[i].isField)
                            {
                                e.uxmlObjectAssets.RemoveAt(i);
                            }
                        }

                        e.uxmlObjectAssets.AddRange(entries);

                        // Remove parent field if empty
                        if (e.uxmlObjectAssets.Count == 0)
                        {
                            var index = m_UxmlObjectEntries.IndexOf(e);
                            m_UxmlObjectEntries.RemoveAt(index);
                            m_UxmlObjectIds.RemoveAt(index);

                            RemoveUxmlObject(e.parentId, true);
                        }
                    }
                    return;
                }
            }
        }

        // Obsolete - Used by UxmlTraits system
        #pragma warning disable CS0618 // Type or member is obsolete
        internal List<T> GetUxmlObjects<T>(IUxmlAttributes asset, CreationContext cc) where T : new()
        {
            if (asset is UxmlAsset ua)
            {
                var entry = GetUxmlObjectEntry(ua.id);

                if (entry.uxmlObjectAssets != null)
                {
                    List<T> uxmlObjects = null;

                    foreach (var uxmlObjectAsset in entry.uxmlObjectAssets)
                    {
                        var factory = GetUxmlObjectFactory(uxmlObjectAsset);

                        if (!(factory is IUxmlObjectFactory<T> typedFactory))
                            continue;

                        var obj = typedFactory.CreateObject(uxmlObjectAsset, cc);

                        if (uxmlObjects == null)
                            uxmlObjects = new List<T>() { obj };
                        else
                            uxmlObjects.Add(obj);
                    }

                    return uxmlObjects;
                }
            }

            return null;
        }
        #pragma warning restore CS0618 // Type or member is obsolete

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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal UxmlObjectEntry GetUxmlObjectEntry(int id)
        {
            if (m_UxmlObjectEntries != null)
            {
                foreach (var e in m_UxmlObjectEntries)
                {
                    if (e.parentId == id)
                    {
                        return e;
                    }
                }
            }

            return default;
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
                CloneTree(target, new CreationContext(s_TemporarySlotInsertionPoints));
            }
            finally
            {
                s_TemporarySlotInsertionPoints.Clear();
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
        /// <remarks>
        /// This function will be deprecated. Use <see cref="VisualTreeAsset.Instantiate"/> instead.
        /// </remarks>
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
        /// <remarks>
        /// This function will be deprecated. Use <see cref="VisualTreeAsset.Instantiate"/> instead.
        /// </remarks>
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

        public void CloneTree(VisualElement target, out int firstElementIndex, out int elementAddedCount)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            firstElementIndex = target.childCount;
            try
            {
                CloneTree(target, new CreationContext(s_TemporarySlotInsertionPoints));
            }
            finally
            {
                elementAddedCount = target.childCount - firstElementIndex;
                s_TemporarySlotInsertionPoints.Clear();
            }
        }

        internal void CloneTree(VisualElement target, CreationContext cc)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if ((visualElementAssets == null || visualElementAssets.Count <= 0) &&
                (templateAssets == null || templateAssets.Count <= 0))
                return;

            Dictionary<int, List<VisualElementAsset>> idToChildren = new Dictionary<int, List<VisualElementAsset>>();
            int eltcount = visualElementAssets == null ? 0 : visualElementAssets.Count;
            int tplcount = templateAssets == null ? 0 : templateAssets.Count;
            for (int i = 0; i < eltcount + tplcount; i++)
            {
                VisualElementAsset asset = i < eltcount ? visualElementAssets[i] : templateAssets[i - eltcount];
                List<VisualElementAsset> children;
                if (!idToChildren.TryGetValue(asset.parentId, out children))
                {
                    children = new List<VisualElementAsset>();
                    idToChildren[asset.parentId] = children;
                }

                children.Add(asset);
            }

            List<VisualElementAsset> rootAssets;

            // Tree root have a parentId == 0
            idToChildren.TryGetValue(0, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
            {
                return;
            }

            Debug.Assert(rootAssets.Count == 1);

            var root = rootAssets[0];
            AssignClassListFromAssetToElement(root, target);
            AssignStyleSheetFromAssetToElement(root, target);

            // Get the first-level elements. These will be instantiated and added to target.
            rootAssets.Clear();
            idToChildren.TryGetValue(root.id, out rootAssets);

            if (rootAssets == null || rootAssets.Count == 0)
            {
                return;
            }

            rootAssets.Sort(CompareForOrder);
            foreach (var rootElement in rootAssets)
            {
                Assert.IsNotNull(rootElement);

                var rootVe = CloneSetupRecursively(rootElement, idToChildren,
                    new CreationContext(cc.slotInsertionPoints, cc.attributeOverrides, cc.serializedDataOverrides, this, target));

                if (rootVe == null)
                    continue;
                // Save reference to the visualElementAsset so elements can be reinitialized when
                // we set their attributes in the editor
                rootVe.SetProperty(LinkedVEAInTemplatePropertyName, rootElement);

                // Save reference to the VisualTreeAsset itself on the containing VisualElement so it can be
                // tracked for live reloading on changes, and also accessible for users that need to keep track
                // of their cloned VisualTreeAssets.
                rootVe.visualTreeAssetSource = this;

                // if contentContainer == this, the shadow and the logical hierarchy are identical
                // otherwise, if there is a CC, we want to insert in the shadow
                target.hierarchy.Add(rootVe);
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            if (root.skipClone)
                return null;

            var ve = Create(root, context);

            if (ve == null)
            {
                return null;
            }

            // context.target is the created templateContainer
            if (root.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer tc)
                    tc.SetContentContainer(ve);
                else
                    Debug.LogError(
                        "Trying to clone a VisualTreeAsset with a custom content container into a element which is not a template container");
            }

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            string slotName;
            if (context.slotInsertionPoints != null && TryGetSlotInsertionPoint(root.id, out slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve);
            }

            if (root.ruleIndex != -1)
            {
                if (inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    StyleRule r = inlineSheet.rules[root.ruleIndex];
                    ve.SetInlineRule(inlineSheet, r);
                }
            }

            var templateAsset = root as TemplateAsset;
            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(root.id, out children))
            {
                children.Sort(CompareForOrder);

                foreach (VisualElementAsset childVea in children)
                {
                    // this will fill the slotInsertionPoints mapping
                    var childVe = CloneSetupRecursively(childVea, idToChildren, context);

                    if (childVe == null)
                        continue;

                    // Save reference to the visualElementAsset so elements can be reinitialized when
                    // we set their attributes in the editor
                    childVe.SetProperty(LinkedVEAInTemplatePropertyName, childVea);

                    // if the parent is not a template asset, just add the child to whatever hierarchy we currently have
                    // if ve is a scrollView (with contentViewport as contentContainer), this will go to the right place
                    if (templateAsset == null)
                    {
                        ve.Add(childVe);
                        continue;
                    }

                    int index = templateAsset.slotUsages == null
                        ? -1
                        : templateAsset.slotUsages.FindIndex(u => u.assetId == childVea.id);
                    if (index != -1)
                    {
                        VisualElement parentSlot;
                        string key = templateAsset.slotUsages[index].slotName;
                        Assert.IsFalse(String.IsNullOrEmpty(key),
                            "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                        if (context.slotInsertionPoints == null ||
                            !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                        {
                            Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key,
                                context.slotInsertionPoints == null
                                ? String.Empty
                                : String.Join(", ",
                                    System.Linq.Enumerable.ToArray(context.slotInsertionPoints.Keys)));
                            ve.Add(childVe);
                        }
                        else
                            parentSlot.Add(childVe);
                    }
                    else
                        ve.Add(childVe);
                }
            }

            if (templateAsset != null && context.slotInsertionPoints != null)
                context.slotInsertionPoints.Clear();

            return ve;
        }

        internal static int CompareForOrder(VisualElementAsset a, VisualElementAsset b) => a.orderInDocument.CompareTo(b.orderInDocument);

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

        void FindElementsByName(string visualElementName, List<VisualElementAsset> results)
        {
            foreach (var visualElementAsset in visualElementAssets)
            {
                if (visualElementAsset.TryGetAttributeValue("name", out string value))
                {
                    if (value == visualElementName)
                    {
                        results.Add(visualElementAsset);
                    }
                }
            }

            foreach (var templateAsset in templateAssets)
            {
                FindElementsByNameInTemplate(templateAsset, visualElementName, results);
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RegisterTemplate(string templateName, VisualTreeAsset asset)
        {
            InsertUsingEntry(new UsingEntry(templateName, asset));
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
        internal static VisualElement Create(VisualElementAsset asset, CreationContext ctx)
        {
            VisualElement CreateError()
            {
                Debug.LogErrorFormat(NoRegisteredFactoryErrorMessage, asset.fullTypeName);
                return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
            }

            // The type is known by UxmlSerializedData system use that instead to create the element.
            if (asset.serializedData != null)
                return asset.Instantiate(ctx);

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
                for (int i = 0; i < asset.classes.Length; i++)
                {
                    element.AddToClassList(asset.classes[i]);
                }
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

        // Used for Live Reload.
        internal int GetAttributePropertiesDirtyCount()
        {
            var dirtyCount = 0;
            foreach (var vea in visualElementAssets)
            {
                dirtyCount += vea.GetPropertiesDirtyCount();
            }

            return dirtyCount;
        }

        internal void ExtractUsedUxmlQualifiedNames(HashSet<string> names)
        {
            foreach (var asset in m_VisualElementAssets)
            {
                names.Add(asset.fullTypeName);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal VisualElementAsset GetRootUxmlElement()
        {
            return visualElementAssets?.Count > 0 ? visualElementAssets[0] : null;
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

        internal struct SerializedDataOverrideRange
        {
            internal readonly VisualTreeAsset sourceAsset;
            internal readonly List<TemplateAsset.UxmlSerializedDataOverride> attributeOverrides;

            public SerializedDataOverrideRange(VisualTreeAsset sourceAsset, List<TemplateAsset.UxmlSerializedDataOverride> attributeOverrides)
            {
                this.sourceAsset = sourceAsset;
                this.attributeOverrides = attributeOverrides;
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

        /// <summary>
        /// The target UXML file to clone or instantiate.
        /// </summary>
        public VisualTreeAsset visualTreeAsset { get; private set; }

        /// <undoc/>
        // TODO This feature leaks in the API but isn't usable
        public Dictionary<string, VisualElement> slotInsertionPoints { get; private set; }

        internal List<AttributeOverrideRange> attributeOverrides { get; private set; }

        internal List<SerializedDataOverrideRange> serializedDataOverrides { get; private set; }

        internal bool hasOverrides => attributeOverrides?.Count > 0 || serializedDataOverrides?.Count > 0;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal CreationContext(VisualTreeAsset vta)
            : this((Dictionary<string, VisualElement>) null, vta, null)
        { }

        internal CreationContext(Dictionary<string, VisualElement> slotInsertionPoints)
            : this(slotInsertionPoints, null, null, null)
        { }

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
            : this(slotInsertionPoints, attributeOverrides, null, vta, target)
        { }

        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<AttributeOverrideRange> attributeOverrides,
            List<SerializedDataOverrideRange> serializedDataOverrides,
            VisualTreeAsset vta, VisualElement target)
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            this.attributeOverrides = attributeOverrides;
            this.serializedDataOverrides = serializedDataOverrides;
            visualTreeAsset = vta;
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
