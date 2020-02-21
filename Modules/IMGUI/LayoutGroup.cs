// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // *undocumented*
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements")]
    internal class GUILayoutGroup : GUILayoutEntry
    {
        public List<GUILayoutEntry> entries = new List<GUILayoutEntry>();
        public bool isVertical = true;                  // Is this group vertical
        public bool resetCoords = false;                // Reset coordinate for GetRect. Used for groups that are part of a window
        public float spacing = 0;                       // Spacing between the elements contained within
        public bool sameSize = true;                    // Are all subelements the same size
        public bool isWindow = false;                   // Is this a window at all?
        public int windowID = -1;                           // Optional window ID for toplevel windows. Used by Layout to tell GUI.Window of size changes...
        int m_Cursor = 0;
        protected int m_StretchableCountX = 100;
        protected int m_StretchableCountY = 100;
        protected bool m_UserSpecifiedWidth = false;
        protected bool m_UserSpecifiedHeight = false;
        // Should all elements be the same size?
        // TODO: implement
        //  bool equalSize = false;

        // The summed sizes of the children. This is used to determine whether or not the children should be stretched
        protected float m_ChildMinWidth = 100;
        protected float m_ChildMaxWidth = 100;
        protected float m_ChildMinHeight = 100;
        protected float m_ChildMaxHeight = 100;

        // How are subelements justified along the minor direction?
        // TODO: implement
        //  enum Align { start, middle, end, justify }
        //  Align align;

        protected int m_MarginLeft;
        protected int m_MarginRight;
        protected int m_MarginTop;
        protected int m_MarginBottom;
        public override int marginLeft => m_MarginLeft;
        public override int marginRight => m_MarginRight;
        public override int marginTop => m_MarginTop;
        public override int marginBottom => m_MarginBottom;

        private static readonly GUILayoutEntry none = new GUILayoutEntry(0, 1, 0, 1, GUIStyle.none);

        public GUILayoutGroup() : base(0, 0, 0, 0, GUIStyle.none) {}

        public GUILayoutGroup(GUIStyle _style, GUILayoutOption[] options) : base(0, 0, 0, 0, _style)
        {
            if (options != null)
                ApplyOptions(options);
            m_MarginLeft = _style.margin.left;
            m_MarginRight = _style.margin.right;
            m_MarginTop = _style.margin.top;
            m_MarginBottom = _style.margin.bottom;
        }

        public override void ApplyOptions(GUILayoutOption[] options)
        {
            if (options == null)
                return;
            base.ApplyOptions(options);
            foreach (GUILayoutOption i in options)
            {
                switch (i.type)
                {
                    case GUILayoutOption.Type.fixedWidth:
                    case GUILayoutOption.Type.minWidth:
                    case GUILayoutOption.Type.maxWidth:
                        m_UserSpecifiedHeight = true;
                        break;
                    case GUILayoutOption.Type.fixedHeight:
                    case GUILayoutOption.Type.minHeight:
                    case GUILayoutOption.Type.maxHeight:
                        m_UserSpecifiedWidth = true;
                        break;
                    // TODO:
                    //              case GUILayoutOption.Type.alignStart:       align = Align.start; break;
                    //              case GUILayoutOption.Type.alignMiddle:      align = Align.middle; break;
                    //              case GUILayoutOption.Type.alignEnd:     align = Align.end; break;
                    //              case GUILayoutOption.Type.alignJustify:     align = Align.justify; break;
                    //              case GUILayoutOption.Type.equalSize:        equalSize = true; break;
                    case GUILayoutOption.Type.spacing:      spacing = (int)i.value; break;
                }
            }
        }

        protected override void ApplyStyleSettings(GUIStyle style)
        {
            base.ApplyStyleSettings(style);
            RectOffset mar = style.margin;
            m_MarginLeft = mar.left;
            m_MarginRight = mar.right;
            m_MarginTop = mar.top;
            m_MarginBottom = mar.bottom;
        }

        public void ResetCursor() { m_Cursor = 0; }

        public Rect PeekNext()
        {
            if (m_Cursor < entries.Count)
            {
                GUILayoutEntry e = (GUILayoutEntry)entries[m_Cursor];
                return e.rect;
            }

            if (Event.current.type == EventType.Repaint)
                throw new ArgumentException("Getting control " + m_Cursor + "'s position in a group with only " + entries.Count + " controls when doing " + Event.current.rawType + "\nAborting");
            return kDummyRect;
        }

        public GUILayoutEntry GetNext()
        {
            if (m_Cursor < entries.Count)
            {
                GUILayoutEntry e = (GUILayoutEntry)entries[m_Cursor];
                m_Cursor++;
                return e;
            }

            if (Event.current.type == EventType.Repaint)
                throw new ArgumentException("Getting control " + m_Cursor + "'s position in a group with only " + entries.Count + " controls when doing " + Event.current.rawType + "\nAborting");
            return none;
        }

        //* undocumented
        public Rect GetLast()
        {
            if (m_Cursor == 0)
            {
                if (Event.current.type == EventType.Repaint)
                    Debug.LogError("You cannot call GetLast immediately after beginning a group.");
                return kDummyRect;
            }

            if (m_Cursor <= entries.Count)
            {
                GUILayoutEntry e = (GUILayoutEntry)entries[m_Cursor - 1];
                return e.rect;
            }

            if (Event.current.type == EventType.Repaint)
                Debug.LogError("Getting control " + m_Cursor + "'s position in a group with only " + entries.Count + " controls when doing " + Event.current.rawType);
            return kDummyRect;
        }

        public void Add(GUILayoutEntry e)
        {
            entries.Add(e);
        }

        public override void CalcWidth()
        {
            if (entries.Count == 0)
            {
                maxWidth = minWidth = style.padding.horizontal;
                return;
            }

            int leftMarginMin  = 0;
            int rightMarginMin = 0;

            m_ChildMinWidth = 0;
            m_ChildMaxWidth = 0;
            m_StretchableCountX = 0;
            bool first = true;
            if (isVertical)
            {
                foreach (GUILayoutEntry i in entries)
                {
                    i.CalcWidth();
                    if (i.consideredForMargin)
                    {
                        if (!first)
                        {
                            leftMarginMin = Mathf.Min(i.marginLeft, leftMarginMin);
                            rightMarginMin = Mathf.Min(i.marginRight, rightMarginMin);
                        }
                        else
                        {
                            leftMarginMin = i.marginLeft;
                            rightMarginMin = i.marginRight;
                            first = false;
                        }
                        m_ChildMinWidth = Mathf.Max(i.minWidth + i.marginHorizontal, m_ChildMinWidth);
                        m_ChildMaxWidth = Mathf.Max(i.maxWidth + i.marginHorizontal, m_ChildMaxWidth);
                    }
                    m_StretchableCountX += i.stretchWidth;
                }
                // Before, we added the margins to the width, now we want to suptract them again.
                m_ChildMinWidth -= leftMarginMin + rightMarginMin;
                m_ChildMaxWidth -= leftMarginMin + rightMarginMin;
            }
            else
            {
                int lastMargin = 0;
                foreach (GUILayoutEntry i in entries)
                {
                    i.CalcWidth();
                    int margin;
                    if (i.consideredForMargin)
                    {
                        if (!first)
                            margin = lastMargin > i.marginLeft ? lastMargin : i.marginLeft;
                        else
                        {
                            // the first element's margins are handles _leftMarginMin and should not be added to the children's sizes
                            margin = 0;
                            first = false;
                        }
                        m_ChildMinWidth += i.minWidth + spacing + margin;
                        m_ChildMaxWidth += i.maxWidth + spacing + margin;
                        lastMargin = i.marginRight;
                        m_StretchableCountX += i.stretchWidth;
                    }
                    else
                    {
                        m_ChildMinWidth += i.minWidth;
                        m_ChildMaxWidth += i.maxWidth;
                        m_StretchableCountX += i.stretchWidth;
                    }
                }
                m_ChildMinWidth -= spacing;
                m_ChildMaxWidth -= spacing;
                if (entries.Count != 0)
                {
                    leftMarginMin = entries[0].marginLeft;
                    rightMarginMin = lastMargin;
                }
                else
                {
                    leftMarginMin = rightMarginMin = 0;
                }
            }
            // Catch the cases where we have ONLY space elements in a group

            // calculated padding values.
            float leftPadding = 0;
            float rightPadding = 0;

            // If we have a style, the margins are handled i.r.t. padding.
            if (style != GUIStyle.none || m_UserSpecifiedWidth)
            {
                // Add the padding of this group to the total min & max widths
                leftPadding = Mathf.Max(style.padding.left, leftMarginMin);
                rightPadding = Mathf.Max(style.padding.right, rightMarginMin);
            }
            else
            {
                // If we don't have a GUIStyle, we pop the min of margins outward from children on to us.
                m_MarginLeft = leftMarginMin;
                m_MarginRight = rightMarginMin;
                leftPadding = rightPadding = 0;
            }

            // If we have a specified minwidth, take that into account...
            minWidth = Mathf.Max(minWidth, m_ChildMinWidth + leftPadding + rightPadding);

            if (maxWidth == 0)          // if we don't have a max width, take the one that was calculated
            {
                stretchWidth += m_StretchableCountX + (style.stretchWidth ? 1 : 0);
                maxWidth = m_ChildMaxWidth + leftPadding + rightPadding;
            }
            else
            {
                // Since we have a maximum width, this element can't stretch width.
                stretchWidth = 0;
            }
            // Finally, if our minimum width is greater than our maximum width, minWidth wins
            maxWidth = Mathf.Max(maxWidth, minWidth);

            // If the style sets us to be a fixed width that wins completely
            if (style.fixedWidth != 0)
            {
                maxWidth = minWidth = style.fixedWidth;
                stretchWidth = 0;
            }
        }

        public override void SetHorizontal(float x, float width)
        {
            base.SetHorizontal(x, width);

            if (resetCoords)
                x = 0;

            RectOffset padding = style.padding;

            if (isVertical)
            {
                // If we have a GUIStyle here, spacing from our edges to children are max (our padding, their margins)
                if (style != GUIStyle.none)
                {
                    foreach (GUILayoutEntry i in entries)
                    {
                        // NOTE: we can't use .horizontal here (As that could make things like right button margin getting eaten by large left padding - so we need to split up in left and right
                        float leftMar = Mathf.Max(i.marginLeft, padding.left);
                        float thisX = x + leftMar;
                        float thisWidth = width - Mathf.Max(i.marginRight, padding.right) - leftMar;
                        if (i.stretchWidth != 0)
                            i.SetHorizontal(thisX, thisWidth);
                        else
                            i.SetHorizontal(thisX, Mathf.Clamp(thisWidth, i.minWidth, i.maxWidth));
                    }
                }
                else
                {
                    // If not, PART of the subelements' margins have already been propagated upwards to this group, so we need to subtract that  from what we apply
                    float thisX = x - marginLeft;
                    float thisWidth = width + marginHorizontal;
                    foreach (GUILayoutEntry i in entries)
                    {
                        if (i.stretchWidth != 0)
                        {
                            i.SetHorizontal(thisX + i.marginLeft, thisWidth - i.marginHorizontal);
                        }
                        else
                            i.SetHorizontal(thisX + i.marginLeft, Mathf.Clamp(thisWidth - i.marginHorizontal, i.minWidth, i.maxWidth));
                    }
                }
            }
            else
            {  // we're horizontally laid out:
               // apply margins/padding here
               // If we have a style, adjust the sizing to take care of padding (if we don't the horizontal margins have been propagated fully up the hierarchy)...
                if (style != GUIStyle.none)
                {
                    float leftMar = padding.left, rightMar = padding.right;
                    if (entries.Count != 0)
                    {
                        leftMar = Mathf.Max(leftMar, entries[0].marginLeft);
                        rightMar = Mathf.Max(rightMar, entries[entries.Count - 1].marginRight);
                    }
                    x += leftMar;
                    width -= rightMar + leftMar;
                }

                // Find out how much leftover width we should distribute.
                float widthToDistribute = width - spacing * (entries.Count - 1);
                // Where to place us in height between min and max
                float minMaxScale = 0;
                // How much height to add to stretchable elements
                if (m_ChildMinWidth != m_ChildMaxWidth)
                    minMaxScale = Mathf.Clamp((widthToDistribute - m_ChildMinWidth) / (m_ChildMaxWidth - m_ChildMinWidth), 0, 1);

                // Handle stretching
                float perItemStretch = 0;
                if (widthToDistribute > m_ChildMaxWidth) // If we have too much space, we need to distribute it.
                {
                    if (m_StretchableCountX > 0)
                    {
                        perItemStretch = (widthToDistribute - m_ChildMaxWidth) / (float)m_StretchableCountX;
                    }
                }

                // Set the positions
                int lastMargin = 0;
                bool firstMargin = true;
                //          Debug.Log ("" + x + ", " + width + "   perItemStretch:" + perItemStretch);
                //          Debug.Log ("MinMaxScale"+ minMaxScale);
                foreach (GUILayoutEntry i in entries)
                {
                    float thisWidth = Mathf.Lerp(i.minWidth, i.maxWidth, minMaxScale);
                    //              Debug.Log (i.minWidth);
                    thisWidth += perItemStretch * i.stretchWidth;

                    if (i.consideredForMargin) // Skip margins on spaces.
                    {
                        int leftMargin = i.marginLeft;
                        if (firstMargin)
                        {
                            leftMargin = 0;
                            firstMargin = false;
                        }
                        int margin = lastMargin > leftMargin ? lastMargin : leftMargin;
                        x += margin;
                        lastMargin = i.marginRight;
                    }

                    i.SetHorizontal(Mathf.Round(x), Mathf.Round(thisWidth));
                    x += thisWidth + spacing;
                }
            }
        }

        public override void CalcHeight()
        {
            if (entries.Count == 0)
            {
                maxHeight = minHeight = style.padding.vertical;
                return;
            }

            int topMarginMin = 0;
            int bottomMarginMin = 0;

            m_ChildMinHeight = 0;
            m_ChildMaxHeight = 0;
            m_StretchableCountY = 0;

            if (isVertical)
            {
                int lastMargin = 0;
                bool first = true;
                foreach (GUILayoutEntry i in entries)
                {
                    i.CalcHeight();
                    int margin;

                    if (i.consideredForMargin)
                    {
                        if (!first)
                            margin = Mathf.Max(lastMargin, i.marginTop);
                        else
                        {
                            margin = 0;
                            first = false;
                        }

                        m_ChildMinHeight += i.minHeight + spacing + margin;
                        m_ChildMaxHeight += i.maxHeight + spacing + margin;
                        lastMargin = i.marginBottom;
                        m_StretchableCountY += i.stretchHeight;
                    }
                    else
                    {
                        m_ChildMinHeight += i.minHeight;
                        m_ChildMaxHeight += i.maxHeight;
                        m_StretchableCountY += i.stretchHeight;
                    }
                }

                m_ChildMinHeight -= spacing;
                m_ChildMaxHeight -= spacing;
                if (entries.Count != 0)
                {
                    topMarginMin = entries[0].marginTop;
                    bottomMarginMin = lastMargin;
                }
                else
                {
                    bottomMarginMin = topMarginMin = 0;
                }
            }
            else
            {
                bool first = true;
                foreach (GUILayoutEntry i in entries)
                {
                    i.CalcHeight();
                    if (i.consideredForMargin)
                    {
                        if (!first)
                        {
                            topMarginMin = Mathf.Min(i.marginTop, topMarginMin);
                            bottomMarginMin = Mathf.Min(i.marginBottom, bottomMarginMin);
                        }
                        else
                        {
                            topMarginMin = i.marginTop;
                            bottomMarginMin = i.marginBottom;
                            first = false;
                        }
                        m_ChildMinHeight = Mathf.Max(i.minHeight, m_ChildMinHeight);
                        m_ChildMaxHeight = Mathf.Max(i.maxHeight, m_ChildMaxHeight);
                    }
                    m_StretchableCountY += i.stretchHeight;
                }
            }
            float firstPadding = 0;
            float lastPadding = 0;

            // If we have a style, the margins are handled i.r.t. padding.
            if (style != GUIStyle.none || m_UserSpecifiedHeight)
            {
                // Add the padding of this group to the total min & max widths
                firstPadding = Mathf.Max(style.padding.top, topMarginMin);
                lastPadding = Mathf.Max(style.padding.bottom, bottomMarginMin);
            }
            else
            {
                // If we don't have a GUIStyle, we bubble the margins outward from children on to us.
                m_MarginTop = topMarginMin;
                m_MarginBottom = bottomMarginMin;
                firstPadding = lastPadding = 0;
            }
            //Debug.Log ("Margins: " + _topMarginMin + ", " + _bottomMarginMin + "          childHeights:" + childMinHeight + ", " + childMaxHeight);
            // If we have a specified minheight, take that into account...
            minHeight = Mathf.Max(minHeight, m_ChildMinHeight + firstPadding + lastPadding);

            if (maxHeight == 0)         // if we don't have a max height, take the one that was calculated
            {
                stretchHeight += m_StretchableCountY + (style.stretchHeight ? 1 : 0);
                maxHeight = m_ChildMaxHeight + firstPadding + lastPadding;
            }
            else
            {
                // Since we have a maximum height, this element can't stretch height.
                stretchHeight = 0;
            }
            // Finally, if out minimum height is greater than our maximum height, minHeight wins
            maxHeight = Mathf.Max(maxHeight, minHeight);

            // If the style sets us to be a fixed height
            if (style.fixedHeight != 0)
            {
                maxHeight = minHeight = style.fixedHeight;
                stretchHeight = 0;
            }
        }

        public override void SetVertical(float y, float height)
        {
            base.SetVertical(y, height);

            if (entries.Count == 0)
                return;

            RectOffset padding = style.padding;

            if (resetCoords)
                y = 0;

            if (isVertical)
            {
                // If we have a skin, adjust the sizing to take care of padding (if we don't have a skin the vertical margins have been propagated fully up the hierarchy)...
                if (style != GUIStyle.none)
                {
                    float topMar = padding.top, bottomMar = padding.bottom;
                    if (entries.Count != 0)
                    {
                        topMar = Mathf.Max(topMar, entries[0].marginTop);
                        bottomMar = Mathf.Max(bottomMar, entries[entries.Count - 1].marginBottom);
                    }
                    y += topMar;
                    height -= bottomMar + topMar;
                }

                // Find out how much leftover height we should distribute.
                float heightToDistribute = height - spacing * (entries.Count - 1);
                // Where to place us in height between min and max
                float minMaxScale = 0;
                // How much height to add to stretchable elements
                if (m_ChildMinHeight != m_ChildMaxHeight)
                    minMaxScale = Mathf.Clamp((heightToDistribute - m_ChildMinHeight) / (m_ChildMaxHeight - m_ChildMinHeight), 0, 1);

                // Handle stretching
                float perItemStretch = 0;
                if (heightToDistribute > m_ChildMaxHeight)          // If we have too much space - stretch any stretchable children
                {
                    if (m_StretchableCountY > 0)
                        perItemStretch = (heightToDistribute - m_ChildMaxHeight) / (float)m_StretchableCountY;
                }

                // Set the positions
                int lastMargin = 0;
                bool firstMargin = true;
                foreach (GUILayoutEntry i in entries)
                {
                    float thisHeight = Mathf.Lerp(i.minHeight, i.maxHeight, minMaxScale);
                    thisHeight += perItemStretch * i.stretchHeight;

                    if (i.consideredForMargin)
                    {   // Skip margins on spaces.
                        int topMargin = i.marginTop;
                        if (firstMargin)
                        {
                            topMargin = 0;
                            firstMargin = false;
                        }
                        int margin = lastMargin > topMargin ? lastMargin : topMargin;
                        y += margin;
                        lastMargin = i.marginBottom;
                    }
                    i.SetVertical(Mathf.Round(y), Mathf.Round(thisHeight));
                    y += thisHeight + spacing;
                }
            }
            else
            {
                // If we have a GUIStyle here, we need to respect the subelements' margins
                if (style != GUIStyle.none)
                {
                    foreach (GUILayoutEntry i in entries)
                    {
                        float topMar = Mathf.Max(i.marginTop, padding.top);
                        float thisY = y + topMar;
                        float thisHeight = height - Mathf.Max(i.marginBottom, padding.bottom) - topMar;

                        if (i.stretchHeight != 0)
                            i.SetVertical(thisY, thisHeight);
                        else
                            i.SetVertical(thisY, Mathf.Clamp(thisHeight, i.minHeight, i.maxHeight));
                    }
                }
                else
                {
                    // If not, the subelements' margins have already been propagated upwards to this group, so we can safely ignore them
                    float thisY = y - marginTop;
                    float thisHeight = height + marginVertical;
                    foreach (GUILayoutEntry i in entries)
                    {
                        if (i.stretchHeight != 0)
                            i.SetVertical(thisY + i.marginTop, thisHeight - i.marginVertical);
                        else
                            i.SetVertical(thisY + i.marginTop, Mathf.Clamp(thisHeight - i.marginVertical, i.minHeight, i.maxHeight));
                    }
                }
            }
        }

        public override string ToString()
        {
            string str = "", space = "";
            for (int i = 0; i < indent; i++)
                space += " ";
            str += /* space + */ base.ToString() + " Margins: " + m_ChildMinHeight + " {\n";
            indent += 4;
            foreach (GUILayoutEntry i in entries)
            {
                str += i + "\n";
            }
            str += space + "}";
            indent -= 4;
            return str;
        }
    }

    // Layout controller for content inside scroll views
    internal sealed class GUIScrollGroup : GUILayoutGroup
    {
        public float calcMinWidth, calcMaxWidth, calcMinHeight, calcMaxHeight;
        public float clientWidth, clientHeight;
        public bool allowHorizontalScroll = true;
        public bool allowVerticalScroll = true;
        public bool needsHorizontalScrollbar, needsVerticalScrollbar;
        public GUIStyle horizontalScrollbar, verticalScrollbar;

        [RequiredByNativeCode] // Created by reflection from GUILayout.BeginScrollView
        public GUIScrollGroup() {}

        public override void CalcWidth()
        {
            // Save the size values & reset so we calc the sizes of children without any contraints
            float _minWidth = minWidth;
            float _maxWidth = maxWidth;
            if (allowHorizontalScroll)
            {
                minWidth = 0;
                maxWidth = 0;
            }

            base.CalcWidth();
            calcMinWidth = minWidth;
            calcMaxWidth = maxWidth;

            // restore the stored constraints for our parent's sizing
            if (allowHorizontalScroll)
            {
                // Set an explicit small minWidth so it will correctly scroll when place inside horizontal groups
                if (minWidth > 32)
                    minWidth = 32;

                if (_minWidth != 0)
                    minWidth = _minWidth;
                if (_maxWidth != 0)
                {
                    maxWidth = _maxWidth;
                    stretchWidth = 0;
                }
            }
        }

        public override void SetHorizontal(float x, float width)
        {
            float _cWidth = needsVerticalScrollbar ? width - verticalScrollbar.fixedWidth - verticalScrollbar.margin.left : width;
            //if (allowVerticalScroll == false)
            //  Debug.Log ("width " + width);
            // If we get a vertical scrollbar, the width changes, so we need to do a recalculation with the new width.
            if (allowHorizontalScroll && _cWidth < calcMinWidth)
            {
                // We're too small horizontally, so we need a horizontal scrollbar.
                needsHorizontalScrollbar = true;

                // set the min and max width we calculated for the children so SetHorizontal works correctly
                minWidth = calcMinWidth;
                maxWidth = calcMaxWidth;
                base.SetHorizontal(x, calcMinWidth);

                // SetHorizontal also sets our width, but we know better
                rect.width = width;

                clientWidth = calcMinWidth;
            }
            else
            {
                // Got enough space.
                needsHorizontalScrollbar = false;

                // set the min and max width we calculated for the children so SetHorizontal works correctly
                if (allowHorizontalScroll)
                {
                    minWidth = calcMinWidth;
                    maxWidth = calcMaxWidth;
                }
                base.SetHorizontal(x, _cWidth);
                rect.width = width;

                // Store the client width
                clientWidth = _cWidth;
            }
        }

        public override void CalcHeight()
        {
            // Save the values & reset so we calc the sizes of children without any contraints
            float _minHeight = minHeight;
            float _maxHeight = maxHeight;
            if (allowVerticalScroll)
            {
                minHeight = 0;
                maxHeight = 0;
            }

            base.CalcHeight();

            calcMinHeight = minHeight;
            calcMaxHeight = maxHeight;

            // if we KNOW we need a horizontal scrollbar, claim space for it now
            // otherwise we get a vertical scrollbar and leftover space beneath the scrollview.
            if (needsHorizontalScrollbar)
            {
                float scrollerSize = horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                minHeight += scrollerSize;
                maxHeight += scrollerSize;
            }

            // restore the stored constraints from user SetHeight calls.
            if (allowVerticalScroll)
            {
                if (minHeight > 32)
                    minHeight = 32;

                if (_minHeight != 0)
                    minHeight = _minHeight;
                if (_maxHeight != 0)
                {
                    maxHeight = _maxHeight;
                    stretchHeight = 0;
                }
            }
        }

        public override void SetVertical(float y, float height)
        {
            // if we have a horizontal scrollbar, we have less space than we thought
            float availableHeight = height;
            if (needsHorizontalScrollbar)
                availableHeight -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;

            // Now we know how much height we have, and hence how much vertical space to distribute.
            // If we get a vertical scrollbar, the width changes, so we need to do a recalculation with the new width.
            if (allowVerticalScroll && availableHeight < calcMinHeight)
            {
                // We're too small vertically, so we need a vertical scrollbar.
                // This means that we have less horizontal space, which can change the vertical size.
                if (!needsHorizontalScrollbar && !needsVerticalScrollbar)
                {
                    // Subtract scrollbar width from the size...
                    clientWidth = rect.width - verticalScrollbar.fixedWidth - verticalScrollbar.margin.left;

                    // ...But make sure we never get too small.
                    if (clientWidth < calcMinWidth)
                        clientWidth = calcMinWidth;

                    // Set the new (smaller) size.
                    float outsideWidth = rect.width;        // store a backup of our own width
                    SetHorizontal(rect.x, clientWidth);

                    // This can have caused a reflow, so we need to recalclate from here on down
                    // (we already know we need a vertical scrollbar, so this size change cannot bubble upwards.
                    CalcHeight();

                    rect.width = outsideWidth;
                }


                // set the min and max height we calculated for the children so SetVertical works correctly
                float origMinHeight = minHeight, origMaxHeight = maxHeight;
                minHeight = calcMinHeight;
                maxHeight = calcMaxHeight;
                base.SetVertical(y, calcMinHeight);
                minHeight = origMinHeight;
                maxHeight = origMaxHeight;

                rect.height = height;
                clientHeight = calcMinHeight;
            }
            else
            {
                // set the min and max height we calculated for the children so SetVertical works correctly
                if (allowVerticalScroll)
                {
                    minHeight = calcMinHeight;
                    maxHeight = calcMaxHeight;
                }
                base.SetVertical(y, availableHeight);
                rect.height = height;
                clientHeight = availableHeight;
            }
        }
    }
}
