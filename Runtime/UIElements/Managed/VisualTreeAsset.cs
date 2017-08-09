// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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

        public VisualContainer CloneTree(IPanel panel)
        {
            VisualElement rootVe = null;
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
                VisualElementAsset root = dict[0][0];
                rootVe = CloneSetupRecursively(root, null, dict, (BaseVisualElementPanel)panel);
            }

            if (rootVe == null)
                return new VisualContainer();

            VisualContainer container = rootVe as VisualContainer;
            if (container == null)
            {
                container = new VisualContainer();
                container.AddChild(rootVe);
            }

            return container;
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root, VisualContainer parent, Dictionary<int, List<VisualElementAsset>> dict, BaseVisualElementPanel panel)
        {
            VisualContainer actualParent = parent != null ? parent.GetChildContainer() : null;
            VisualElement ve = root.Create(panel, this);
            ve.name = root.name;
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

            if (parent != null)
            {
                if (actualParent != null)
                    actualParent.AddChild(ve);
                else
                    Debug.LogWarning("Cannot insert element in hierarchy, parent has no childContainer");
            }

            var container = ve as VisualContainer;
            if (container != null)
            {
                if (root.stylesheets != null)
                {
                    for (int i = 0; i < root.stylesheets.Count; i++)
                    {
                        container.AddStyleSheetPath(root.stylesheets[i]);
                    }
                }

                List<VisualElementAsset> children;
                if (dict.TryGetValue(root.id, out children))
                {
                    foreach (var childVea in children)
                    {
                        VisualElement childVe = CloneSetupRecursively(childVea, container, dict, panel);
                        if (childVe != null)
                            container.AddChild(childVe);
                    }
                }
            }

            return ve;
        }

        internal VisualTreeAsset ResolveUsing(BaseVisualElementPanel panel, string templateAlias)
        {
            if (m_Usings == null || m_Usings.Count == 0)
                return null;
            int index = m_Usings.BinarySearch(new UsingEntry(templateAlias, null), UsingEntry.comparer);
            if (index < 0)
                return null;

            string path = m_Usings[index].path;
            return panel.loadResourceFunc == null ? null : panel.loadResourceFunc(path, typeof(VisualTreeAsset)) as VisualTreeAsset;
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

        public abstract VisualElement Create(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset);
    }

    [Serializable]
    internal abstract class VisualElementAsset<T> : VisualElementAsset where T : VisualElement
    {
        protected abstract T CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset);

        public override VisualElement Create(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            var res = CreateElementInstance(panel, visualTreeAsset);
            res.name = name;
            for (int i = 0; classes != null && i < classes.Length; i++)
            {
                res.AddToClassList(classes[i]);
            }

            res.text = text;
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

        protected override TemplateContainer CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            VisualTreeAsset vea = visualTreeAsset.ResolveUsing(panel, m_TemplateAlias);
            var tc = new TemplateContainer(m_TemplateAlias);
            VisualElement content = vea == null
                ? (VisualElement) new Label(string.Format("Unknown Element: '{0}'", m_TemplateAlias))
                : vea.CloneTree(panel);

            if (vea == null)
            {
                Debug.LogErrorFormat("Could not resolve template with alias '{0}'", m_TemplateAlias);
            }

            tc.AddChild(content);
            return tc;
        }
    }

    [Serializable]
    internal class ButtonAsset : VisualElementAsset<Button>
    {
        protected override Button CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new Button(null);
        }
    }

    [Serializable]
    internal class ImageAsset : VisualElementAsset<Image>
    {
        protected override Image CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new Image();
        }
    }

    [Serializable]
    internal class LabelAsset : VisualElementAsset<Label>
    {
        protected override Label CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
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

        protected override RepeatButton CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
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

        protected override Scroller CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
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

        protected override ScrollerButton CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new ScrollerButton(null, m_Delay, m_Interval);
        }
    }

    [Serializable]
    internal class ScrollViewAsset : VisualElementAsset<ScrollView>
    {
        protected override ScrollView CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
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

        protected override Slider CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new Slider(m_LowValue, m_HighValue == m_LowValue ? m_LowValue + 1 : m_HighValue, null, m_Direction);
        }
    }

    [Serializable]
    internal class TextFieldAsset : VisualElementAsset<TextField>
    {
        protected override TextField CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new TextField();
        }
    }

    [Serializable]
    internal class ToggleAsset : VisualElementAsset<Toggle>
    {
        protected override Toggle CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new Toggle(null);
        }
    }

    [Serializable]
    internal class VisualContainerAsset : VisualElementAsset<VisualContainer>
    {
        protected override VisualContainer CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new VisualContainer();
        }
    }

    [Serializable]
    internal class IMGUIContainerAsset : VisualElementAsset<IMGUIContainer>
    {
        protected override IMGUIContainer CreateElementInstance(BaseVisualElementPanel panel, VisualTreeAsset visualTreeAsset)
        {
            return new IMGUIContainer(null);
        }
    }
}
