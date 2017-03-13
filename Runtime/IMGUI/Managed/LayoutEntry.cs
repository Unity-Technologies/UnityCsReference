// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Security;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    // Basic layout element
    internal class GUILayoutEntry
    {
        // The min and max sizes. Used during calculations...
        public float minWidth, maxWidth, minHeight, maxHeight;

        // The rectangle that this element ends up having
        public Rect rect = new Rect(0, 0, 0, 0);

        // Can this element stretch?
        public int stretchWidth, stretchHeight;

        // The style to use.
        GUIStyle m_Style = GUIStyle.none;

        public GUIStyle style
        {
            get { return m_Style; }
            set { m_Style = value; ApplyStyleSettings(value); }
        }

        internal static Rect kDummyRect = new Rect(0, 0, 1, 1);

        // The margins of this element.
        public virtual RectOffset margin  { get { return style.margin; } }

        public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style)
        {
            minWidth = _minWidth;
            maxWidth = _maxWidth;
            minHeight = _minHeight;
            maxHeight = _maxHeight;
            if (_style == null)
                _style = GUIStyle.none;
            style = _style;
        }

        public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style, GUILayoutOption[] options)
        {
            minWidth = _minWidth;
            maxWidth = _maxWidth;
            minHeight = _minHeight;
            maxHeight = _maxHeight;
            style = _style;
            ApplyOptions(options);
        }

        public virtual void CalcWidth() {}
        public virtual void CalcHeight() {}
        public virtual void SetHorizontal(float x, float width) { rect.x = x; rect.width = width; }
        public virtual void SetVertical(float y, float height) { rect.y = y; rect.height = height; }

        protected virtual void ApplyStyleSettings(GUIStyle style)
        {
            stretchWidth = (style.fixedWidth == 0 && style.stretchWidth) ? 1 : 0;
            stretchHeight = (style.fixedHeight == 0  && style.stretchHeight) ? 1 : 0;
            m_Style = style;
        }

        public virtual void ApplyOptions(GUILayoutOption[] options)
        {
            if (options == null)
                return;

            foreach (GUILayoutOption i in options)
            {
                switch (i.type)
                {
                    case GUILayoutOption.Type.fixedWidth:           minWidth = maxWidth = (float)i.value; stretchWidth = 0; break;
                    case GUILayoutOption.Type.fixedHeight:          minHeight = maxHeight = (float)i.value; stretchHeight = 0; break;
                    case GUILayoutOption.Type.minWidth:         minWidth = (float)i.value; if (maxWidth < minWidth) maxWidth = minWidth; break;
                    case GUILayoutOption.Type.maxWidth:         maxWidth = (float)i.value; if (minWidth > maxWidth) minWidth = maxWidth; stretchWidth = 0; break;
                    case GUILayoutOption.Type.minHeight:            minHeight = (float)i.value; if (maxHeight < minHeight) maxHeight = minHeight; break;
                    case GUILayoutOption.Type.maxHeight:            maxHeight = (float)i.value; if (minHeight > maxHeight) minHeight = maxHeight; stretchHeight = 0; break;
                    case GUILayoutOption.Type.stretchWidth:     stretchWidth = (int)i.value; break;
                    case GUILayoutOption.Type.stretchHeight:        stretchHeight = (int)i.value; break;
                }
            }

            if (maxWidth != 0 && maxWidth < minWidth)
                maxWidth = minWidth;
            if (maxHeight != 0 && maxHeight < minHeight)
                maxHeight = minHeight;
        }

        protected static int indent = 0;
        public override string ToString()
        {
            string space = "";
            for (int i = 0; i < indent; i++)
                space += " ";
            return space + UnityString.Format("{1}-{0} (x:{2}-{3}, y:{4}-{5})", style != null ? style.name : "NULL", GetType(), rect.x, rect.xMax, rect.y, rect.yMax) +
                "   -   W: " + minWidth + "-" + maxWidth + (stretchWidth != 0 ? "+" : "") + ", H: " + minHeight + "-" + maxHeight + (stretchHeight  != 0 ? "+" : "");
        }
    }

    // Layouter that makes elements which sizes will always conform to a specific aspect ratio.
    internal sealed class GUIAspectSizer : GUILayoutEntry
    {
        float aspect;

        public GUIAspectSizer(float aspect, GUILayoutOption[] options) : base(0, 0, 0, 0, GUIStyle.none)
        {
            this.aspect = aspect;
            ApplyOptions(options);
        }

        public override void CalcHeight()
        {
            minHeight = maxHeight = rect.width / aspect;
        }
    }

    // Will layout a button grid so it can fit within the given rect.
    // *undocumented*
    internal sealed class GUIGridSizer : GUILayoutEntry
    {
        // Helper: Create the layout group and scale it to fit
        public static Rect GetRect(GUIContent[] contents, int xCount, GUIStyle style, GUILayoutOption[] options)
        {
            Rect r = new Rect(0, 0, 0, 0);
            switch (Event.current.type)
            {
                case EventType.Layout:
                    GUILayoutUtility.current.topLevel.Add(new GUIGridSizer(contents, xCount, style, options));
                    break;
                case EventType.Used:
                    return kDummyRect;
                default:
                    r = GUILayoutUtility.current.topLevel.GetNext().rect;
                    break;
            }
            return r;
        }

        readonly int m_Count;
        readonly int m_XCount;
        readonly float m_MinButtonWidth = -1;
        readonly float m_MaxButtonWidth = -1;
        readonly float m_MinButtonHeight = -1;
        readonly float m_MaxButtonHeight = -1;

        private GUIGridSizer(GUIContent[] contents, int xCount, GUIStyle buttonStyle, GUILayoutOption[] options) : base(0, 0, 0, 0, GUIStyle.none)
        {
            m_Count = contents.Length;
            m_XCount = xCount;

            // Most settings comes from the button style (can we stretch, etc). Hence, I apply the style here
            ApplyStyleSettings(buttonStyle);

            // We can have custom options coming from userland. We apply this last so it overrides
            ApplyOptions(options);

            if (xCount == 0 || contents.Length == 0)
                return;

            // internal horizontal spacing
            float totalHorizSpacing = Mathf.Max(buttonStyle.margin.left, buttonStyle.margin.right) * (m_XCount - 1);
            //          Debug.Log (String.Format ("margins: {0}, {1}   totalHoriz: {2}", buttonStyle.margin.left, buttonStyle.margin.right, totalHorizSpacing));
            // internal horizontal margins
            float totalVerticalSpacing = Mathf.Max(buttonStyle.margin.top, buttonStyle.margin.bottom) * (rows - 1);


            // Handle fixedSize buttons
            if (buttonStyle.fixedWidth != 0)
                m_MinButtonWidth = m_MaxButtonWidth = buttonStyle.fixedWidth;
            //          Debug.Log ("buttonStyle.fixedHeight " + buttonStyle.fixedHeight);
            if (buttonStyle.fixedHeight != 0)
                m_MinButtonHeight = m_MaxButtonHeight = buttonStyle.fixedHeight;

            // Apply GUILayout.Width/Height/whatever properties.
            if (m_MinButtonWidth == -1)
            {
                if (minWidth != 0)
                    m_MinButtonWidth = (minWidth - totalHorizSpacing) / m_XCount;
                if (maxWidth != 0)
                    m_MaxButtonWidth = (maxWidth - totalHorizSpacing) / m_XCount;
            }

            if (m_MinButtonHeight == -1)
            {
                if (minHeight != 0)
                    m_MinButtonHeight = (minHeight - totalVerticalSpacing) / rows;
                if (maxHeight != 0)
                    m_MaxButtonHeight = (maxHeight - totalVerticalSpacing) / rows;
            }
            //          Debug.Log (String.Format ("minButtonWidth {0}, maxButtonWidth {1}, minButtonHeight {2}, maxButtonHeight{3}", minButtonWidth, maxButtonWidth, minButtonHeight, maxButtonHeight));

            // if anything is left unknown, we need to iterate over all elements and figure out the sizes.
            if (m_MinButtonHeight == -1 || m_MaxButtonHeight == -1 || m_MinButtonWidth == -1 || m_MaxButtonWidth == -1)
            {
                // figure out the max size. Since the buttons are in a grid, the max size determines stuff.
                float calcHeight = 0, calcWidth = 0;
                foreach (GUIContent i in contents)
                {
                    Vector2 size = buttonStyle.CalcSize(i);
                    calcWidth = Mathf.Max(calcWidth, size.x);
                    calcHeight = Mathf.Max(calcHeight, size.y);
                }

                // If the user didn't supply minWidth, we need to calculate that
                if (m_MinButtonWidth == -1)
                {
                    // if the user has supplied a maxButtonWidth, the buttons can never get larger.
                    if (m_MaxButtonWidth != -1)
                        m_MinButtonWidth = Mathf.Min(calcWidth, m_MaxButtonWidth);
                    else
                        m_MinButtonWidth = calcWidth;
                }

                // If the user didn't supply maxWidth, we need to calculate that
                if (m_MaxButtonWidth == -1)
                {
                    // if the user has supplied a minButtonWidth, the buttons can never get smaller.
                    if (m_MinButtonWidth != -1)
                        m_MaxButtonWidth = Mathf.Max(calcWidth, m_MinButtonWidth);
                    else
                        m_MaxButtonWidth = calcWidth;
                }

                // If the user didn't supply minWidth, we need to calculate that
                if (m_MinButtonHeight == -1)
                {
                    // if the user has supplied a maxButtonWidth, the buttons can never get larger.
                    if (m_MaxButtonHeight != -1)
                        m_MinButtonHeight = Mathf.Min(calcHeight, m_MaxButtonHeight);
                    else
                        m_MinButtonHeight = calcHeight;
                }

                // If the user didn't supply maxWidth, we need to calculate that
                if (m_MaxButtonHeight == -1)
                {
                    // if the user has supplied a minButtonWidth, the buttons can never get smaller.
                    if (m_MinButtonHeight != -1)
                        maxHeight = Mathf.Max(maxHeight, m_MinButtonHeight);
                    m_MaxButtonHeight = maxHeight;
                }
            }
            // We now know the button sizes. Calculate min & max values from that
            minWidth = m_MinButtonWidth * m_XCount + totalHorizSpacing;
            maxWidth = m_MaxButtonWidth * m_XCount + totalHorizSpacing;
            minHeight = m_MinButtonHeight * rows + totalVerticalSpacing;
            maxHeight = m_MaxButtonHeight * rows + totalVerticalSpacing;
            //          Debug.Log (String.Format ("minWidth {0}, maxWidth {1}, minHeight {2}, maxHeight{3}", minWidth, maxWidth, minHeight, maxHeight));
        }

        int rows
        {
            get
            {
                int rows = m_Count / m_XCount;
                if (m_Count % m_XCount != 0)
                    rows++;
                return rows;
            }
        }
    }

    // Class that can handle word-wrap sizing. this is specialcased as setting width can make the text wordwrap, which would then increase height...
    internal sealed class GUIWordWrapSizer : GUILayoutEntry
    {
        readonly GUIContent m_Content;
        // We need to differentiate between min & maxHeight we calculate for ourselves and one that is forced by the user
        // (When inside a scrollview, we can be told to layout twice, so we need to know the difference)
        readonly float m_ForcedMinHeight;
        readonly float m_ForcedMaxHeight;

        public GUIWordWrapSizer(GUIStyle style, GUIContent content, GUILayoutOption[] options) : base(0, 0, 0, 0, style)
        {
            m_Content = new GUIContent(content);
            ApplyOptions(options);
            m_ForcedMinHeight = minHeight;
            m_ForcedMaxHeight = maxHeight;
        }

        public override void CalcWidth()
        {
            if (minWidth == 0 || maxWidth == 0)
            {
                float _minWidth, _maxWidth;
                style.CalcMinMaxWidth(m_Content, out _minWidth, out _maxWidth);
                if (minWidth == 0)
                    minWidth = _minWidth;
                if (maxWidth == 0)
                    maxWidth = _maxWidth;
            }
        }

        public override void CalcHeight()
        {
            // When inside a scrollview, this can get called twice (as vertical scrollbar reduces width, which causes a reflow).
            // Hence, we need to use the separately cached values for min & maxHeight coming from the user...
            if (m_ForcedMinHeight == 0 || m_ForcedMaxHeight == 0)
            {
                float height = style.CalcHeight(m_Content, rect.width);
                if (m_ForcedMinHeight == 0)
                    minHeight = height;
                else
                    minHeight = m_ForcedMinHeight;

                if (m_ForcedMaxHeight == 0)
                    maxHeight = height;
                else
                    maxHeight = m_ForcedMaxHeight;
            }
        }
    }
}
