// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<SlotDefinition> m_Slots;

        internal List<SlotDefinition> slots
        {
            get { return m_Slots; }
            set { m_Slots = value; }
        }

        [SerializeField]
        private VisualElementAsset m_ContentContainer;

        internal  VisualElementAsset contentContainer
        {
            get { return m_ContentContainer; }
            set { m_ContentContainer = value; }
        }

        internal VisualElement CloneTree(Dictionary<string, VisualElement> slotInsertionPoints)
        {
            var tc = new TemplateContainer(name);
            CloneTree(tc, slotInsertionPoints ?? new Dictionary<string, VisualElement>());
            return tc;
        }

        internal void CloneTree(VisualElement target, Dictionary<string, VisualElement> slotInsertionPoints)
        {
            if (m_VisualElementAssets != null && m_VisualElementAssets.Count > 0)
            {
                Dictionary<int, List<VisualElementAsset>> dict = new Dictionary<int, List<VisualElementAsset>>();
                for (int i = 0; i < m_VisualElementAssets.Count; i++)
                {
                    VisualElementAsset asset = m_VisualElementAssets[i];
                    List<VisualElementAsset> children;
                    if (!dict.TryGetValue(asset.parentId, out children))
                    {
                        dict.Add(asset.parentId, children = new List<VisualElementAsset>());
                    }

                    children.Add(asset);
                }

                // all nodes under the tree root have a parentId == 0
                List<VisualElementAsset> rootAssets;
                if (dict.TryGetValue(0, out rootAssets) && rootAssets != null)
                {
                    foreach (VisualElementAsset rootElement in rootAssets)
                    {
                        VisualElement rootVe = CloneSetupRecursively(rootElement, dict, new CreationContext(slotInsertionPoints, this, target));
                        // if contentContainer == this, the shadow and the logical hierarchy are identical
                        // otherwise, if there is a CC, we want to insert in the shadow
                        target.shadow.Add(rootVe);
                    }
                }
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root, Dictionary<int, List<VisualElementAsset>> dict, CreationContext context)
        {
            VisualElement ve = root.Create(context);

            // context.target is the created templateContainer
            if (root == context.visualTreeAsset.contentContainer)
            {
                if (context.target is TemplateContainer)
                    ((TemplateContainer)context.target).SetContentContainer(ve);
                else
                    Debug.LogErrorFormat("Cannot clone a template in an existing element which is not a Templatecontainer if the template defines a custom contentcontainer");
            }

            ve.name = root.name;
            ve.pickingMode = root.pickingMode;

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            string slotName;
            if (context.slotInsertionPoints != null && TryGetSlotInsertionPoint(root.id, out slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve as VisualContainer);
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
                        StyleSheetCache.GetPropertyIDs(inlineSheet, root.ruleIndex), Resources.Load);
                }
            }

            if (root.stylesheets != null)
            {
                for (int i = 0; i < root.stylesheets.Count; i++)
                {
                    ve.AddStyleSheetPath(root.stylesheets[i]);
                }
            }

            var templateAsset = root as TemplateAsset;
            List<VisualElementAsset> children;
            if (!dict.TryGetValue(root.id, out children))
                return ve;

            foreach (var childVea in children)
            {
                // this will fill the slotInsertionPoints mapping
                VisualElement childVe = CloneSetupRecursively(childVea, dict, context);
                if (childVe == null)
                    continue;

                // if the parent is not a template asset, just add the child to whatever hierarchy we currently have
                // if ve is a scrollView (with contentViewport as contentContainer), this will go to the right place
                if (templateAsset == null)
                {
                    ve.Add(childVe);
                    continue;
                }

                int index = templateAsset.slotUsages.FindIndex(u => u.assetId == childVea.id);
                if (index != -1)
                {
                    VisualElement parentSlot;
                    var key = templateAsset.slotUsages[index].slotName;
                    if (context.slotInsertionPoints == null || !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                    {
                        Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key, context.slotInsertionPoints == null ? String.Empty : String.Join(", ", context.slotInsertionPoints.Keys.ToArray()));
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
            {
                return false;
            }
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

    internal struct CreationContext
    {
        public readonly VisualElement target;
        public static readonly CreationContext Default = new CreationContext();
        internal CreationContext(Dictionary<string, VisualElement> slotInsertionPoints, VisualTreeAsset vta, VisualElement target) : this()
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            visualTreeAsset = vta;
        }

        public VisualTreeAsset visualTreeAsset { get; internal set; }
        public Dictionary<string, VisualElement> slotInsertionPoints { get; private set; }
    }

    [Serializable]
    internal abstract class VisualElementAsset : ScriptableObject
    {
        [SerializeField]
        private int m_Id;

        public int id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        [SerializeField]
        private int m_ParentId;

        public int parentId
        {
            get { return m_ParentId; }
            set { m_ParentId = value; }
        }

        [SerializeField]
        private int m_RuleIndex;

        public int ruleIndex
        {
            get { return m_RuleIndex; }
            set { m_RuleIndex = value; }
        }

        [SerializeField]
        private string m_Text;

        public string text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        [SerializeField]
        private string[] m_Classes;

        public string[] classes
        {
            get { return m_Classes; }
            set { m_Classes = value; }
        }

        [SerializeField]
        private List<string> m_Stylesheets;

        public List<string> stylesheets
        {
            get { return m_Stylesheets == null ? (m_Stylesheets = new List<string>()) : m_Stylesheets; }
            set { m_Stylesheets = value; }
        }

        [SerializeField]
        PickingMode m_PickingMode;

        public PickingMode pickingMode
        {
            get { return m_PickingMode; }
            set { m_PickingMode = value; }
        }

        public abstract VisualElement Create(CreationContext creationContext);
    }

    [Serializable]
    internal abstract class VisualElementAsset<T> : VisualElementAsset where T : VisualElement
    {
        protected abstract T CreateElementInstance(CreationContext creationContext);

        public override VisualElement Create(CreationContext creationContext)
        {
            T res = CreateElementInstance(creationContext);
            res.name = name;
            for (int i = 0; classes != null && i < classes.Length; i++)
            {
                res.AddToClassList(classes[i]);
            }
            if (!string.IsNullOrEmpty(text))
            {
                res.text = text;
            }
            return res;
        }
    }

    [Serializable]
    internal class TemplateAsset : VisualElementAsset<TemplateContainer>
    {
        [SerializeField]
        private string m_TemplateAlias;

        public string templateAlias
        {
            get { return m_TemplateAlias; }
            set { m_TemplateAlias = value; }
        }

        [SerializeField]
        private List<VisualTreeAsset.SlotUsageEntry> m_SlotUsages;

        internal List<VisualTreeAsset.SlotUsageEntry> slotUsages
        {
            get { return m_SlotUsages; }
            set { m_SlotUsages = value; }
        }

        protected override TemplateContainer CreateElementInstance(CreationContext ctx)
        {
            VisualTreeAsset vta = ctx.visualTreeAsset.ResolveUsing(m_TemplateAlias);

            var tc = new TemplateContainer(m_TemplateAlias);
            if (vta == null)
            {
                Debug.LogErrorFormat("Could not resolve template with alias '{0}'", m_TemplateAlias);
                tc.Add(new Label(string.Format("Unknown Element: '{0}'", m_TemplateAlias)));
            }
            else
                vta.CloneTree(tc, ctx.slotInsertionPoints);

            return tc;
        }

        public void AddSlotUsage(string slotName, int resId)
        {
            if (m_SlotUsages == null)
                m_SlotUsages = new List<VisualTreeAsset.SlotUsageEntry>();
            m_SlotUsages.Add(new VisualTreeAsset.SlotUsageEntry(slotName, resId));
        }
    }

    [Serializable]
    internal class ButtonAsset : VisualElementAsset<Button>
    {
        protected override Button CreateElementInstance(CreationContext ctx)
        {
            return new Button(null);
        }
    }

    [Serializable]
    internal class ImageAsset : VisualElementAsset<Image>
    {
        protected override Image CreateElementInstance(CreationContext ctx)
        {
            return new Image();
        }
    }

    [Serializable]
    internal class LabelAsset : VisualElementAsset<Label>
    {
        protected override Label CreateElementInstance(CreationContext ctx)
        {
            return new Label();
        }
    }

    [Serializable]
    internal class RepeatButtonAsset : VisualElementAsset<RepeatButton>
    {
        [SerializeField]
        internal long m_Delay;
        [SerializeField]
        internal long m_Interval;

        protected override RepeatButton CreateElementInstance(CreationContext ctx)
        {
            return new RepeatButton(null, m_Delay, m_Interval);
        }
    }

    [Serializable]
    internal class ScrollerAsset : VisualElementAsset<Scroller>
    {
        [SerializeField]
        internal Slider.Direction m_Direction;
        [SerializeField]
        internal float m_LowValue;
        [SerializeField]
        internal float m_HighValue;

        protected override Scroller CreateElementInstance(CreationContext ctx)
        {
            return new Scroller(m_LowValue, m_HighValue, null, m_Direction);
        }
    }

    [Serializable]
    internal class ScrollerButtonAsset : VisualElementAsset<ScrollerButton>
    {
        [SerializeField]
        internal long m_Delay;
        [SerializeField]
        internal long m_Interval;

        protected override ScrollerButton CreateElementInstance(CreationContext ctx)
        {
            return new ScrollerButton(null, m_Delay, m_Interval);
        }
    }

    [Serializable]
    internal class ScrollViewAsset : VisualElementAsset<ScrollView>
    {
        protected override ScrollView CreateElementInstance(CreationContext ctx)
        {
            return new ScrollView();
        }
    }

    [Serializable]
    internal class SliderAsset : VisualElementAsset<Slider>
    {
        [SerializeField]
        internal float m_LowValue;
        [SerializeField]
        internal float m_HighValue;
        [SerializeField]
        internal Slider.Direction m_Direction;

        protected override Slider CreateElementInstance(CreationContext ctx)
        {
            return new Slider(m_LowValue, m_HighValue == m_LowValue ? m_LowValue + 1 : m_HighValue, null, m_Direction);
        }
    }

    [Serializable]
    internal class TextFieldAsset : VisualElementAsset<TextField>
    {
        protected override TextField CreateElementInstance(CreationContext ctx)
        {
            return new TextField();
        }
    }

    [Serializable]
    internal class ToggleAsset : VisualElementAsset<Toggle>
    {
        protected override Toggle CreateElementInstance(CreationContext ctx)
        {
            return new Toggle(null);
        }
    }

    [Serializable]
    internal class VisualContainerAsset : VisualElementAsset<VisualContainer>
    {
        protected override VisualContainer CreateElementInstance(CreationContext ctx)
        {
            return new VisualContainer();
        }
    }

    [Serializable]
    internal class IMGUIContainerAsset : VisualElementAsset<IMGUIContainer>
    {
        protected override IMGUIContainer CreateElementInstance(CreationContext ctx)
        {
            return new IMGUIContainer(null);
        }
    }
}
