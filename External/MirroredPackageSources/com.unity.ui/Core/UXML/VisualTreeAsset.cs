using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// An instance of this class holds a tree of `VisualElementAsset`s, created from a UXML file. Each node in the file corresponds to a `VisualElementAsset`. You can clone a `VisualTreeAsset` to yield a tree of `VisualElement`s.
    /// </summary>
    [Serializable]
    public class VisualTreeAsset : ScriptableObject
    {
        internal int GetNextChildSerialNumber()
        {
            int n = m_VisualElementAssets?.Count ?? 0;
            n += m_TemplateAssets?.Count ?? 0;
            return n;
        }

        private static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();

        [Serializable]
        internal struct UsingEntry
        {
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

#pragma warning disable 0649
        [SerializeField] private List<UsingEntry> m_Usings;
#pragma warning restore 0649

        /// <summary>
        /// The UXML templates used by this VisualTreeAsset.
        /// </summary>
        public IEnumerable<VisualTreeAsset> templateDependencies
        {
            get
            {
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
                        var vta = Panel.LoadResource(entry.path, typeof(VisualTreeAsset), GUIUtility.pixelsPerPoint) as
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

        [SerializeField] internal StyleSheet inlineSheet;

        [SerializeField] private List<VisualElementAsset> m_VisualElementAssets;

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
                                Panel.LoadResource(stylesheetPath, typeof(StyleSheet),
                                    GUIUtility.pixelsPerPoint) as StyleSheet;
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

        internal List<VisualElementAsset> visualElementAssets
        {
            get { return m_VisualElementAssets; }
            set { m_VisualElementAssets = value; }
        }

        [SerializeField] private List<TemplateAsset> m_TemplateAssets;

        internal List<TemplateAsset> templateAssets
        {
            get { return m_TemplateAssets; }
            set { m_TemplateAssets = value; }
        }

        [SerializeField] private List<SlotDefinition> m_Slots;

        internal List<SlotDefinition> slots
        {
            get { return m_Slots; }
            set { m_Slots = value; }
        }

        [SerializeField] private int m_ContentContainerId;
        [SerializeField] private int m_ContentHash;

        internal int contentContainerId
        {
            get { return m_ContentContainerId; }
            set { m_ContentContainerId = value; }
        }

        /// <summary>
        /// Build a tree of VisualElements from the asset.
        /// </summary>
        /// <returns>The root of the tree of VisualElements that was just cloned.</returns>
        public TemplateContainer Instantiate()
        {
            TemplateContainer target = new TemplateContainer(name);
            try
            {
                CloneTree(target, s_TemporarySlotInsertionPoints, null);
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
        /// This function will be deprecated. Use <see cref="VisualElement.Instantiate"/> instead.
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
        /// This function will be deprecated. Use <see cref="VisualElement.Instantiate"/> instead.
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
            int firstElementIndex;
            int elementAddedCount;

            CloneTree(target, out firstElementIndex, out elementAddedCount);
        }

        public void CloneTree(VisualElement target, out int firstElementIndex, out int elementAddedCount)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            firstElementIndex = target.childCount;
            try
            {
                CloneTree(target, s_TemporarySlotInsertionPoints, null);
            }
            finally
            {
                elementAddedCount = target.childCount - firstElementIndex;
                s_TemporarySlotInsertionPoints.Clear();
            }
        }

        internal void CloneTree(VisualElement target, Dictionary<string, VisualElement> slotInsertionPoints,
            List<TemplateAsset.AttributeOverride> attributeOverrides)
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
                    idToChildren.Add(asset.parentId, children);
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
                VisualElement rootVe = CloneSetupRecursively(rootElement, idToChildren,
                    new CreationContext(slotInsertionPoints, attributeOverrides, this, target));

                if (rootVe != null)
                {
                    // if contentContainer == this, the shadow and the logical hierarchy are identical
                    // otherwise, if there is a CC, we want to insert in the shadow
                    target.hierarchy.Add(rootVe);
                }
                else
                {
                    Debug.LogWarning("VisualTreeAsset instantiated an empty UI. Check the syntax of your UXML document.");
                }
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            VisualElement ve = Create(root, context);
            if (ve == null)
            {
                return null;
            }

            // context.target is the created templateContainer
            if (root.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer)
                    ((TemplateContainer)context.target).SetContentContainer(ve);
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
                    VisualElement childVe = CloneSetupRecursively(childVea, idToChildren, context);
                    if (childVe == null)
                        continue;

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

        static int CompareForOrder(VisualElementAsset a, VisualElementAsset b) => a.orderInDocument.CompareTo(b.orderInDocument);

        internal bool SlotDefinitionExists(string slotName)
        {
            if (m_Slots == null)
                return false;

            return m_Slots.Exists(s => s.name == slotName);
        }

        internal bool AddSlotDefinition(string slotName, int resId)
        {
            if (SlotDefinitionExists(slotName))
                return false;

            if (m_Slots == null)
                m_Slots = new List<SlotDefinition>(1);

            m_Slots.Add(new SlotDefinition {insertionPointId = resId, name = slotName});
            return true;
        }


        internal bool TryGetSlotInsertionPoint(int insertionPointId, out string slotName)
        {
            if (m_Slots == null)
            {
                slotName = null;
                return false;
            }

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

        internal VisualTreeAsset ResolveTemplate(string templateName)
        {
            if (m_Usings == null || m_Usings.Count == 0)
                return null;
            int index = m_Usings.BinarySearch(new UsingEntry(templateName, string.Empty), UsingEntry.comparer);
            if (index < 0)
                return null;

            if (m_Usings[index].asset)
                return m_Usings[index].asset;

            string path = m_Usings[index].path;
            return Panel.LoadResource(path, typeof(VisualTreeAsset), GUIUtility.pixelsPerPoint) as VisualTreeAsset;
        }

        internal bool TemplateExists(string templateName)
        {
            if (m_Usings == null || m_Usings.Count == 0)
                return false;
            var index = m_Usings.BinarySearch(new UsingEntry(templateName, string.Empty), UsingEntry.comparer);
            return index >= 0;
        }

        internal void RegisterTemplate(string templateName, string path)
        {
            InsertUsingEntry(new UsingEntry(templateName, path));
        }

        internal void RegisterTemplate(string templateName, VisualTreeAsset asset)
        {
            InsertUsingEntry(new UsingEntry(templateName, asset));
        }

        private void InsertUsingEntry(UsingEntry entry)
        {
            if (m_Usings == null)
                m_Usings = new List<UsingEntry>();

            // find insertion index so usings are sorted by alias
            int i = 0;
            while (i < m_Usings.Count && String.CompareOrdinal(entry.alias, m_Usings[i].alias) > 0)
                i++;

            m_Usings.Insert(i, entry);
        }


        internal static VisualElement Create(VisualElementAsset asset, CreationContext ctx)
        {
            List<IUxmlFactory> factoryList;
            if (!VisualElementFactoryRegistry.TryGetValue(asset.fullTypeName, out factoryList))
            {
                if (asset.fullTypeName.StartsWith("UnityEngine.Experimental.UIElements.") || asset.fullTypeName.StartsWith("UnityEditor.Experimental.UIElements."))
                {
                    string experimentalTypeName = asset.fullTypeName.Replace(".Experimental.UIElements", ".UIElements");
                    if (!VisualElementFactoryRegistry.TryGetValue(experimentalTypeName, out factoryList))
                    {
                        Debug.LogErrorFormat("Element '{0}' has no registered factory method.", asset.fullTypeName);
                        return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
                    }
                }
                else if (asset.fullTypeName == UxmlRootElementFactory.k_ElementName)
                {
                    // Support UXML without namespace for backward compatibility.
                    VisualElementFactoryRegistry.TryGetValue(typeof(UxmlRootElementFactory).Namespace + "." + asset.fullTypeName, out factoryList);
                }
                else
                {
                    Debug.LogErrorFormat("Element '{0}' has no registered factory method.", asset.fullTypeName);
                    return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
                }
            }

            IUxmlFactory factory = null;
            foreach (IUxmlFactory f in factoryList)
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

            VisualElement res = factory.Create(asset, ctx);
            if (res != null)
            {
                AssignClassListFromAssetToElement(asset, res);
                AssignStyleSheetFromAssetToElement(asset, res);
            }

            return res;
        }

        static void AssignClassListFromAssetToElement(VisualElementAsset asset, VisualElement element)
        {
            if (asset.classes != null)
            {
                for (int i = 0; i < asset.classes.Length; i++)
                    element.AddToClassList(asset.classes[i]);
            }
        }

        static void AssignStyleSheetFromAssetToElement(VisualElementAsset asset, VisualElement element)
        {
            if (asset.hasStylesheetPaths)
            {
                for (int i = 0; i < asset.stylesheetPaths.Count; i++)
                    element.AddStyleSheetPath(asset.stylesheetPaths[i]);
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
    }

    /// <summary>
    /// This class is used during UXML template instantiation.
    /// </summary>
    public struct CreationContext : IEquatable<CreationContext>
    {
        public static readonly CreationContext Default = new CreationContext();
        public VisualElement target { get; private set; }
        public VisualTreeAsset visualTreeAsset { get; private set; }
        public Dictionary<string, VisualElement> slotInsertionPoints { get; private set; }

        internal List<TemplateAsset.AttributeOverride> attributeOverrides { get; private set; }

        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            VisualTreeAsset vta, VisualElement target)
            : this(slotInsertionPoints, null, vta, target)
        {}

        internal CreationContext(
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<TemplateAsset.AttributeOverride> attributeOverrides,
            VisualTreeAsset vta, VisualElement target)
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            this.attributeOverrides = attributeOverrides;
            visualTreeAsset = vta;
        }

        public override bool Equals(object obj)
        {
            return obj is CreationContext && Equals((CreationContext)obj);
        }

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

        public static bool operator==(CreationContext context1, CreationContext context2)
        {
            return context1.Equals(context2);
        }

        public static bool operator!=(CreationContext context1, CreationContext context2)
        {
            return !(context1 == context2);
        }
    }
}
