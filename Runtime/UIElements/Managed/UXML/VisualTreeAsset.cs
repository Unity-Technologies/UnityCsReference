// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    [Serializable]
    public class VisualTreeAsset : ScriptableObject
    {
        [Serializable]
        internal struct UsingEntry
        {
            internal static readonly IComparer<UsingEntry> comparer = new UsingEntryComparer();

            [SerializeField]
            public string alias;

            [SerializeField]
            public string path;

            public UsingEntry(string alias, string path)
            {
                this.alias = alias;
                this.path = path;
            }
        }

        private class UsingEntryComparer : IComparer<UsingEntry>
        {
            public int Compare(UsingEntry x, UsingEntry y)
            {
                return Comparer<string>.Default.Compare(x.alias, y.alias);
            }
        }

        [Serializable]
        internal struct SlotDefinition
        {
            [SerializeField]
            public string name;

            [SerializeField]
            public int insertionPointId;
        }

        [Serializable]
        internal struct SlotUsageEntry
        {
            [SerializeField]
            public string slotName;
            [SerializeField]
            public int assetId;

            public SlotUsageEntry(string slotName, int assetId)
            {
                this.slotName = slotName;
                this.assetId = assetId;
            }
        }

        [SerializeField]
        private List<UsingEntry> m_Usings;

        [SerializeField]
        internal StyleSheet inlineSheet;

        [SerializeField]
        private List<VisualElementAsset> m_VisualElementAssets;

        internal List<VisualElementAsset> visualElementAssets
        {
            get { return m_VisualElementAssets; }
            set { m_VisualElementAssets = value; }
        }

        [SerializeField]
        private List<TemplateAsset> m_TemplateAssets;

        internal List<TemplateAsset> templateAssets
        {
            get { return m_TemplateAssets; }
            set { m_TemplateAssets = value; }
        }

        [SerializeField]
        private List<SlotDefinition> m_Slots;

        internal List<SlotDefinition> slots
        {
            get { return m_Slots; }
            set { m_Slots = value; }
        }

        [SerializeField]
        private int m_ContentContainerId;

        internal int contentContainerId
        {
            get { return m_ContentContainerId; }
            set { m_ContentContainerId = value; }
        }

        public VisualElement CloneTree(Dictionary<string, VisualElement> slotInsertionPoints)
        {
            var tc = new TemplateContainer(name);
            CloneTree(tc, slotInsertionPoints ?? new Dictionary<string, VisualElement>());
            return tc;
        }

        public void CloneTree(VisualElement target, Dictionary<string, VisualElement> slotInsertionPoints)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Cannot clone a Visual Tree in a null target");

            if ((m_VisualElementAssets == null || m_VisualElementAssets.Count <= 0) && (m_TemplateAssets == null || m_TemplateAssets.Count <= 0))
                return;

            Dictionary<int, List<VisualElementAsset>> idToChildren = new Dictionary<int, List<VisualElementAsset>>();
            int eltcount = m_VisualElementAssets == null ? 0 : m_VisualElementAssets.Count;
            int tplcount = m_TemplateAssets == null ? 0 : m_TemplateAssets.Count;
            for (int i = 0; i < eltcount + tplcount; i++)
            {
                VisualElementAsset asset = i < eltcount ? m_VisualElementAssets[i] : m_TemplateAssets[i - eltcount];
                List<VisualElementAsset> children;
                if (!idToChildren.TryGetValue(asset.parentId, out children))
                {
                    children = new List<VisualElementAsset>();
                    idToChildren.Add(asset.parentId, children);
                }

                children.Add(asset);
            }

            // all nodes under the tree root have a parentId == 0
            List<VisualElementAsset> rootAssets;
            if (idToChildren.TryGetValue(0, out rootAssets) && rootAssets != null)
            {
                foreach (VisualElementAsset rootElement in rootAssets)
                {
                    Assert.IsNotNull(rootElement);
                    VisualElement rootVe = CloneSetupRecursively(rootElement, idToChildren, new CreationContext(slotInsertionPoints, this, target));

                    // if contentContainer == this, the shadow and the logical hierarchy are identical
                    // otherwise, if there is a CC, we want to insert in the shadow
                    target.shadow.Add(rootVe);
                }
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root, Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            VisualElement ve = root.Create(context);

            // context.target is the created templateContainer
            if (root.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer)
                    ((TemplateContainer)context.target).SetContentContainer(ve);
                else
                    Debug.LogError("Trying to clone a VisualTreeAsset with a custom content container into a element which is not a template container");
            }

            ve.name = root.name;

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            string slotName;
            if (context.slotInsertionPoints != null && TryGetSlotInsertionPoint(root.id, out slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve);
            }

            if (root.classes != null)
            {
                for (int i = 0; i < root.classes.Length; i++)
                {
                    ve.AddToClassList(root.classes[i]);
                }
            }

            if (root.ruleIndex != -1)
            {
                if (inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    StyleRule r = inlineSheet.rules[root.ruleIndex];
                    var stylesData = new VisualElementStylesData(false);
                    ve.SetInlineStyles(stylesData);
                    stylesData.ApplyRule(inlineSheet, Int32.MaxValue, r,
                        StyleSheetCache.GetPropertyIDs(inlineSheet, root.ruleIndex));
                }
            }

            var templateAsset = root as TemplateAsset;
            List<VisualElementAsset> children;
            if (!idToChildren.TryGetValue(root.id, out children))
                return ve;

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

                int index = templateAsset.slotUsages == null ? -1 : templateAsset.slotUsages.FindIndex(u => u.assetId == childVea.id);
                if (index != -1)
                {
                    VisualElement parentSlot;
                    string key = templateAsset.slotUsages[index].slotName;
                    Assert.IsFalse(String.IsNullOrEmpty(key), "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                    if (context.slotInsertionPoints == null || !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                    {
                        Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key, context.slotInsertionPoints == null
                            ? String.Empty
                            : String.Join(", ", System.Linq.Enumerable.ToArray(context.slotInsertionPoints.Keys)));
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

            m_Slots.Add(new SlotDefinition { insertionPointId = resId, name = slotName });
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

        internal VisualTreeAsset ResolveUsing(string templateAlias)
        {
            if (m_Usings == null || m_Usings.Count == 0)
                return null;
            int index = m_Usings.BinarySearch(new UsingEntry(templateAlias, null), UsingEntry.comparer);
            if (index < 0)
                return null;

            string path = m_Usings[index].path;
            return Panel.loadResourceFunc == null ? null : Panel.loadResourceFunc(path, typeof(VisualTreeAsset)) as VisualTreeAsset;
        }

        internal bool AliasExists(string templateAlias)
        {
            if (m_Usings == null || m_Usings.Count == 0)
                return false;
            var index = m_Usings.BinarySearch(new UsingEntry(templateAlias, null), UsingEntry.comparer);
            return index >= 0;
        }

        internal void RegisterUsing(string alias, string path)
        {
            if (m_Usings == null)
                m_Usings = new List<UsingEntry>();

            // find insertion index so usings are sorted by alias
            int i = 0;
            while (i < m_Usings.Count && alias.CompareTo(m_Usings[i].alias) != -1)
                i++;

            m_Usings.Insert(i, new UsingEntry(alias, path));
        }

    }

    public struct CreationContext
    {
        public static readonly CreationContext Default = new CreationContext();
        public VisualElement target { get; }
        public VisualTreeAsset visualTreeAsset { get; }
        public Dictionary<string, VisualElement> slotInsertionPoints { get; }

        internal CreationContext(Dictionary<string, VisualElement> slotInsertionPoints, VisualTreeAsset vta, VisualElement target)
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            visualTreeAsset = vta;
        }
    }
}
