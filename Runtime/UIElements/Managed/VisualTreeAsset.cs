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
        [SerializeField]
        internal StyleSheet inlineSheet;

        [SerializeField]
        private List<VisualElementAsset> m_VisualElementAssets;

        internal List<VisualElementAsset> visualElementAssets { get { return m_VisualElementAssets; } set { m_VisualElementAssets = value; } }

        public VisualContainer CloneTree()
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
                rootVe = CloneSetupRecursively(root, null, dict);
            }

            if (rootVe == null)
                return new VisualContainer();

            VisualContainer container = rootVe as VisualContainer;
            if (container == null)
            {
                container = new VisualContainer();
                container.AddChild(rootVe);
            }

            // todo: use referenced stylesheets
            return container;
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root, VisualContainer parent, Dictionary<int, List<VisualElementAsset>> dict)
        {
            VisualElement ve = root.Create();
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
                    var style = new VisualElementStyles(false);
                    ve.SetInlineStyles(style);
                    style.ApplyRule(inlineSheet, Int32.MaxValue, r,
                        StyleSheetCache.GetPropertyIDs(inlineSheet, root.ruleIndex), Resources.Load);
                }
            }

            if (parent != null)
                parent.AddChild(ve);

            var container = ve as VisualContainer;
            if (container != null)
            {
                foreach (var childVea in dict[root.id])
                {
                    VisualElement childVe = CloneSetupRecursively(childVea, container, dict);
                    if (childVe != null)
                        container.AddChild(childVe);
                }
            }

            return ve;
        }
    }

    [Serializable]
    internal abstract class VisualElementAsset : ScriptableObject
    {
        [SerializeField]
        private int m_Id;
        public int id { get { return m_Id; } set { m_Id = value; } }

        [SerializeField]
        private int m_ParentId;
        public int parentId { get { return m_ParentId; } set { m_ParentId = value; } }

        [SerializeField]
        private int m_RuleIndex;
        public int ruleIndex { get { return m_RuleIndex; } set { m_RuleIndex = value; } }

        [SerializeField]
        private string m_Text;
        public string text { get { return m_Text; } set { m_Text = value; } }

        [SerializeField]
        private string[] m_Classes;
        public string[] classes { get { return m_Classes; } set { m_Classes = value; } }

        public abstract VisualElement Create();
    }

    internal abstract class VisualElementAsset<T> : VisualElementAsset where T : VisualElement
    {
        protected abstract T CreateElementInstance();

        public override VisualElement Create()
        {
            var res = CreateElementInstance();
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
    internal class ButtonAsset : VisualElementAsset<Button>
    {
        protected override Button CreateElementInstance()
        {
            return new Button(null);
        }
    }

    [Serializable]
    internal class ImageAsset : VisualElementAsset<Image>
    {
        protected override Image CreateElementInstance()
        {
            return new Image();
        }
    }

    [Serializable]
    internal class LabelAsset : VisualElementAsset<Label>
    {
        protected override Label CreateElementInstance()
        {
            return new Label();
        }
    }

    [Serializable]
    internal class RepeatButtonAsset : VisualElementAsset<RepeatButton>
    {
        [SerializeField]
        private long m_Delay;
        [SerializeField]
        private long m_Interval;
        protected override RepeatButton CreateElementInstance()
        {
            return new RepeatButton(null, m_Delay, m_Interval);
        }
    }

    [Serializable]
    internal class ScrollerAsset : VisualElementAsset<Scroller>
    {
        [SerializeField]
        private Slider.Direction m_Direction;
        [SerializeField]
        private float m_LowValue;
        [SerializeField]
        private float m_HighValue;

        protected override Scroller CreateElementInstance()
        {
            return new Scroller(m_LowValue, m_HighValue, null, m_Direction);
        }
    }

    [Serializable]
    internal class ScrollerButtonAsset : VisualElementAsset<ScrollerButton>
    {
        [SerializeField]
        private long m_Delay;
        [SerializeField]
        private long m_Interval;

        protected override ScrollerButton CreateElementInstance()
        {
            return new ScrollerButton(null, m_Delay, m_Interval);
        }
    }

    [Serializable]
    internal class ScrollViewAsset : VisualElementAsset<ScrollView>
    {
        protected override ScrollView CreateElementInstance()
        {
            return new ScrollView();
        }
    }

    [Serializable]
    internal class SliderAsset : VisualElementAsset<Slider>
    {
        [SerializeField]
        private float m_Start;
        [SerializeField]
        private float m_End;
        [SerializeField]
        private Slider.Direction m_Direction;

        protected override Slider CreateElementInstance()
        {
            return new Slider(m_Start, m_End, null, m_Direction);
        }
    }

    [Serializable]
    internal class TextFieldAsset : VisualElementAsset<TextField>
    {
        protected override TextField CreateElementInstance()
        {
            return new TextField();
        }
    }

    [Serializable]
    internal class ToggleAsset : VisualElementAsset<Toggle>
    {
        protected override Toggle CreateElementInstance()
        {
            return new Toggle(null);
        }
    }

    [Serializable]
    internal class VisualContainerAsset : VisualElementAsset<VisualContainer>
    {
        protected override VisualContainer CreateElementInstance()
        {
            return new VisualContainer();
        }
    }

    [Serializable]
    internal class IMGUIContainerAsset : VisualElementAsset<IMGUIContainer>
    {
        protected override IMGUIContainer CreateElementInstance()
        {
            return new IMGUIContainer(null);
        }
    }
}
