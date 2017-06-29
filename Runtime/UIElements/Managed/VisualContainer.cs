// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.CSSLayout;
using UnityEngine.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public class VisualContainer : VisualElement, IEnumerable<VisualElement>
    {
        public struct Enumerator : IEnumerator<VisualElement>
        {
            private List<VisualElement>.Enumerator m_Enumerator;

            public Enumerator(ref List<VisualElement> list)
            {
                m_Enumerator = list.GetEnumerator();
            }

            public void Dispose()
            {
                m_Enumerator.Dispose();
            }

            public void Reset()
            {
                ((IEnumerator<VisualElement>)m_Enumerator).Reset();
            }

            public bool MoveNext()
            {
                return m_Enumerator.MoveNext();
            }

            public VisualElement Current
            {
                get { return m_Enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        private List<VisualElement> m_Children = new List<VisualElement>();

        private List<StyleSheet> m_StyleSheets;

        internal IEnumerable<StyleSheet> styleSheets
        {
            get
            {
                if (m_StyleSheets == null && m_StyleSheetPaths != null)
                {
                    LoadStyleSheetsFromPaths();
                }
                return m_StyleSheets;
            }
        }

        private List<string> m_StyleSheetPaths;

        public bool clipChildren
        {
            get; set;
        }

        public VisualContainer()
        {
            clipChildren = true;
            cssNode.SetMeasureFunction(null);
        }

        public int childrenCount
        {
            get { return m_Children.Count; }
        }

        [Obsolete("VisualElement.flexDirection will be removed. Use VisualElement.styles.flexDirection instead", false)]
        public FlexDirection flexDirection
        {
            get
            {
                return style.flexDirection;
            }
            set
            {
                style.flexDirection = value;
            }
        }

        [Obsolete("VisualElement.alignItems will be removed. Use VisualElement.styles.alignItems instead", false)]
        public Align alignItems
        {
            get
            {
                return style.alignItems;
            }
            set
            {
                style.alignItems = value;
            }
        }

        [Obsolete("VisualElement.alignContent will be removed. Use VisualElement.styles.alignContent instead", false)]
        public Align alignContent
        {
            get
            {
                return style.alignContent.value;
            }
            set
            {
                style.alignContent = value;
            }
        }

        [Obsolete("VisualElement.justifyContent will be removed. Use VisualElement.styles.justifyContent instead", false)]
        public Justify justifyContent
        {
            get
            {
                return style.justifyContent.value;
            }
            set
            {
                style.justifyContent = value;
            }
        }

        [Obsolete("VisualElement.flexWrap will be removed. Use VisualElement.styles.flexWrap instead", false)]
        public Wrap flexWrap
        {
            get
            {
                return style.flexWrap.value;
            }
            set
            {
                style.flexWrap = value;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref m_Children);
        }

        IEnumerator<VisualElement> IEnumerable<VisualElement>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        internal override void ChangePanel(BaseVisualElementPanel p)
        {
            if (p == panel) return;
            base.ChangePanel(p);
            Dirty(ChangeType.Styles);
            for (int i = 0; i < m_Children.Count; i++)
            {
                // make sure the child enters and leaves panel too
                m_Children[i].ChangePanel(p);
            }
        }

        public void AddChild(VisualElement child)
        {
            if (child == null)
                throw new ArgumentException("Cannot add null child");

            if (child.parent != null)
            {
                child.parent.RemoveChild(child);
            }

            child.parent = this;
            m_Children.Add(child);
            cssNode.Insert(cssNode.Count, child.cssNode);

            // child styles are dependent on topology
            child.Dirty(ChangeType.Styles);
        }

        public void InsertChild(int index, VisualElement child)
        {
            if (child == null)
                throw new ArgumentException("Cannot insert null child");

            if (index > m_Children.Count)
                throw new IndexOutOfRangeException("Index out of range: " + index);

            if (child.parent != null)
            {
                child.parent.RemoveChild(child);
            }
            child.parent = this;
            m_Children.Insert(index, child);
            cssNode.Insert(index, child.cssNode);

            // child styles are dependent on topology
            child.Dirty(ChangeType.Styles);
        }

        public void RemoveChild(VisualElement child)
        {
            if (child == null)
                throw new ArgumentException("Cannot add null child");

            if (child.parent != this)
                throw new ArgumentException("This visualElement is not my child");

            child.parent = null;
            m_Children.Remove(child);
            cssNode.RemoveAt(cssNode.IndexOf(child.cssNode));
            Dirty(ChangeType.Layout);
        }

        public void RemoveChildAt(int index)
        {
            if (index < 0 || index >= m_Children.Count)
                throw new IndexOutOfRangeException("Index out of range: " + index);

            var child = m_Children[index];
            child.parent = null;
            m_Children.RemoveAt(index);
            cssNode.RemoveAt(index);
            Dirty(ChangeType.Layout);
        }

        public void ClearChildren()
        {
            for (int i = 0; i < m_Children.Count; i++)
            {
                m_Children[i].parent = null;
            }
            m_Children.Clear();
            cssNode.Clear();
            Dirty(ChangeType.Layout);
        }

        public VisualElement GetChildAt(int index)
        {
            return m_Children[index];
        }

        public bool ContainsChild(VisualElement elem)
        {
            for (int i = 0; i < m_Children.Count; i++)
            {
                if (m_Children[i] == elem)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddStyleSheetPath(string sheetPath)
        {
            if (m_StyleSheetPaths == null)
            {
                m_StyleSheetPaths = new List<string>();
            }
            m_StyleSheetPaths.Add(sheetPath);
            //will trigger a reload on next access
            m_StyleSheets = null;
            Dirty(ChangeType.Styles);
        }

        public void RemoveStyleSheetPath(string sheetPath)
        {
            if (m_StyleSheetPaths == null)
            {
                Debug.LogWarning("Attempting to remove from null style sheet path list");
                return;
            }
            m_StyleSheetPaths.Remove(sheetPath);
            //will trigger a reload on next access
            m_StyleSheets = null;
            Dirty(ChangeType.Styles);
        }

        public bool HasStyleSheetPath(string sheetPath)
        {
            return m_StyleSheetPaths != null && m_StyleSheetPaths.Contains(sheetPath);
        }

        internal void LoadStyleSheetsFromPaths()
        {
            if (m_StyleSheetPaths == null || elementPanel == null)
            {
                return;
            }

            m_StyleSheets = new List<StyleSheet>();
            foreach (var styleSheetPath in m_StyleSheetPaths)
            {
                StyleSheet sheetAsset = elementPanel.loadResourceFunc(styleSheetPath, typeof(StyleSheet)) as StyleSheet;

                if (sheetAsset != null)
                {
                    // Every time we load a new style sheet, we cache some data on them
                    for (int i = 0, count = sheetAsset.complexSelectors.Length; i < count; i++)
                    {
                        sheetAsset.complexSelectors[i].CachePseudoStateMasks();
                    }
                    m_StyleSheets.Add(sheetAsset);
                }
                else
                {
                    Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", styleSheetPath));
                }
            }
        }
    }
}
