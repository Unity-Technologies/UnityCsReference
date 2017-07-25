// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEditorInternal;


namespace UnityEditor
{
    class FlowLayout : GUILayoutGroup
    {
        struct LineInfo
        {
            public float minSize, maxSize;
            public float start, size;
            public int topBorder, bottomBorder;
        }

        int        m_Lines;
        LineInfo[] m_LineInfo;

        public override void CalcWidth()
        {
            bool hasMinWidth = minWidth != 0;

            base.CalcWidth();

            if (isVertical)
            {
                // DONT
            }
            else
            {
                // Margin handling is somewhat different from other controls.
                // Since we don't know what will wrap where we'll just take the Min margin of all child elements
                if (!hasMinWidth)
                {
                    minWidth = 0;
                    foreach (GUILayoutEntry i in entries)
                    {
                        // Here we should probably include margins of the element, but that does seem kind of annoying
                        minWidth = Mathf.Max(m_ChildMinWidth, i.minWidth);
                    }
                }
            }
        }

        public override void SetHorizontal(float x, float width)
        {
            // Apply the base. Now everything is in one line (or column), and all we need is to insert the linebreaks
            base.SetHorizontal(x, width);

            if (resetCoords)
                x = 0;

            if (isVertical)
            {
                Debug.LogError("Wordwrapped vertical group. Don't. Just Don't");
            }
            else
            {  // we're horizontally laid out:
               // apply margins/padding here
               // If we have a style, adjust the sizing to take care of padding (if we don't the horizontal margins have been propagated fully up the hierarchy)...

                // Set the positions
                m_Lines = 0;
                float pulledOffset = 0;  // How far we need to pull each item back.
                foreach (GUILayoutEntry i in entries)
                {
                    if (i.rect.xMax - pulledOffset > x + width)
                    {
                        // TODO: When we move a line back, we should re-expand
                        pulledOffset = i.rect.x - i.margin.left;
                        m_Lines++;
                    }
                    i.SetHorizontal(i.rect.x - pulledOffset, i.rect.width);
                    i.rect.y = m_Lines;
                }
                m_Lines++;
            }
        }

        public override void CalcHeight()
        {
            if (entries.Count == 0)
            {
                maxHeight = minHeight = 0;
                return;
            }
            m_ChildMinHeight = m_ChildMaxHeight = 0;
            int _topMarginMin = 0,  _bottomMarginMin = 0;
            m_StretchableCountY = 0;
            if (isVertical)
            {
            }
            else
            {
                m_LineInfo = new LineInfo[m_Lines];
                for (int i = 0; i < m_Lines; i++)
                {
                    m_LineInfo[i].topBorder = 10000;
                    m_LineInfo[i].bottomBorder = 10000;
                }

                // Figure out border values for each line
                foreach (GUILayoutEntry i in entries)
                {
                    i.CalcHeight();
                    int j = (int)i.rect.y;
                    m_LineInfo[j].minSize = Mathf.Max(i.minHeight, m_LineInfo[j].minSize);
                    m_LineInfo[j].maxSize = Mathf.Max(i.maxHeight, m_LineInfo[j].maxSize);
                    m_LineInfo[j].topBorder = Mathf.Min(i.margin.top, m_LineInfo[j].topBorder);
                    m_LineInfo[j].bottomBorder = Mathf.Min(i.margin.bottom, m_LineInfo[j].bottomBorder);
                }

                for (int i = 0; i < m_Lines; i++)
                {
                    m_ChildMinHeight += m_LineInfo[i].minSize;
                    m_ChildMaxHeight += m_LineInfo[i].maxSize;
                }

                // Add in the the extra lines
                for (int i = 1; i < m_Lines; i++)
                {
                    float space = Mathf.Max(m_LineInfo[i - 1].bottomBorder, m_LineInfo[i].topBorder);
                    m_ChildMinHeight += space;
                    m_ChildMaxHeight += space;
                }
                _topMarginMin = m_LineInfo[0].topBorder;
                _bottomMarginMin = m_LineInfo[m_LineInfo.Length - 1].bottomBorder;
            }

            // Do the dance between children & parent for haggling over how many empty pixels to have
            float firstPadding, lastPadding;

            margin.top = _topMarginMin;
            margin.bottom = _bottomMarginMin;
            firstPadding = lastPadding = 0;

            minHeight = Mathf.Max(minHeight, m_ChildMinHeight + firstPadding + lastPadding);
            if (maxHeight == 0)
            {
                stretchHeight += m_StretchableCountY + (style.stretchHeight ? 1 : 0);
                maxHeight = m_ChildMaxHeight + firstPadding + lastPadding;
            }
            else
            {
                stretchHeight = 0;
            }
            maxHeight = Mathf.Max(maxHeight, minHeight);
        }

        public override void SetVertical(float y, float height)
        {
            if (entries.Count == 0)
            {
                base.SetVertical(y, height);
                return;
            }

            if (isVertical)
            {
                base.SetVertical(y, height);
            }
            else
            {
                if (resetCoords)
                    y = 0;

                float clientY, clientHeight;
                clientY = y - margin.top;
                clientHeight = y + margin.vertical;

                // Figure out how to distribute the elements between the different lines
                float heightToDistribute = clientHeight - spacing * (m_Lines - 1);
                float minMaxScale = 0;
                if (m_ChildMinHeight != m_ChildMaxHeight)
                    minMaxScale = Mathf.Clamp((heightToDistribute - m_ChildMinHeight) / (m_ChildMaxHeight - m_ChildMinHeight), 0, 1);

                float lineY = clientY;
                for (int i = 0; i < m_Lines; i++)
                {
                    if (i > 0)
                        lineY += Mathf.Max(m_LineInfo[i].topBorder, m_LineInfo[i - 1].bottomBorder);
                    m_LineInfo[i].start = lineY;
                    m_LineInfo[i].size = Mathf.Lerp(m_LineInfo[i].minSize, m_LineInfo[i].maxSize, minMaxScale);
                    lineY += m_LineInfo[i].size + spacing;
                }


                foreach (GUILayoutEntry i in entries)
                {
                    LineInfo li = m_LineInfo[(int)i.rect.y];
                    if (i.stretchHeight != 0)
                        i.SetVertical(li.start + i.margin.top, li.size - i.margin.vertical);
                    else
                        i.SetVertical(li.start + i.margin.top, Mathf.Clamp(li.size - i.margin.vertical, i.minHeight, i.maxHeight));
                }
            }
        }
    }


    // @TODO Make this serialize
    // @TODO Handle animate-from nothing (with fade?)
    // @TODO Figure out how to implement fade-away of contents
    // @TODO Switch a bunch of others to use this
    internal class GUISlideGroup
    {
        internal static GUISlideGroup current = null;
        Dictionary<int, Rect> animIDs = new Dictionary<int, Rect>();
        const float kLerp = .1f;
        const float kSnap = .5f;

        public void Begin()
        {
            if (current != null)
            {
                Debug.LogError("You cannot nest animGroups");
                return;
            }

            current = this;
        }

        public void End()
        {
            current = null;
        }

        public void Reset()
        {
            current = null;
            animIDs.Clear();
        }

        public Rect BeginHorizontal(int id, params GUILayoutOption[] options)
        {
            SlideGroupInternal g = (SlideGroupInternal)GUILayoutUtility.BeginLayoutGroup(GUIStyle.none, options, typeof(SlideGroupInternal));
            g.SetID(this, id);
            g.isVertical = false;
            return g.m_FinalRect;
        }

        public void EndHorizontal()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        public Rect GetRect(int id, Rect r)
        {
            bool dummy;
            if (Event.current.type != EventType.Repaint)
                return r;
            return GetRect(id, r, out dummy);
        }

        Rect GetRect(int id, Rect r, out bool changed)
        {
            if (!animIDs.ContainsKey(id))
            {
                animIDs.Add(id, r);
                changed = false;
                return r;
            }

            Rect current = animIDs[id];
            if (current.y != r.y || current.height != r.height || current.x != r.x || current.width != r.width)
            {
                float lerp = kLerp;
                if (Mathf.Abs(current.y - r.y) > kSnap)
                    r.y = Mathf.Lerp(current.y, r.y, lerp);
                if (Mathf.Abs(current.height - r.height) > kSnap)
                    r.height = Mathf.Lerp(current.height, r.height, lerp);
                if (Mathf.Abs(current.x - r.x) > kSnap)
                    r.x = Mathf.Lerp(current.x, r.x, lerp);
                if (Mathf.Abs(current.width - r.width) > kSnap)
                    r.width = Mathf.Lerp(current.width, r.width, lerp);
                animIDs[id] = r;
                changed = true;
                HandleUtility.Repaint();
            }
            else
                changed = false;
            return r;
        }

        class SlideGroupInternal : GUILayoutGroup
        {
            int           m_ID;
            GUISlideGroup m_Owner;
#pragma warning disable 649
            internal Rect m_FinalRect;
            public void SetID(GUISlideGroup owner, int id)
            {
                m_ID = id;
                m_Owner = owner;
            }

            public override void SetHorizontal(float x, float width)
            {
                m_FinalRect.x = x;
                m_FinalRect.width = width;
                base.SetHorizontal(x, width);
            }

            public override void SetVertical(float y, float height)
            {
                m_FinalRect.y = y;
                m_FinalRect.height = height;

                Rect r = new Rect(rect.x, y, rect.width, height);
                bool changed;
                r = m_Owner.GetRect(m_ID, r, out changed);

                if (changed)
                    base.SetHorizontal(r.x, r.width);
                base.SetVertical(r.y, r.height);
            }
        }
    }
}  // namespace
