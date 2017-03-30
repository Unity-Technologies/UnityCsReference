// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMButtonGrid : IMElement
    {
        GUIContent[] s_EmptyContents = new GUIContent[0];
        GUIContent[] m_Contents;
        public GUIContent[] contents { get { return m_Contents; } set { m_Contents = value ?? s_EmptyContents; } }

        public int xCount { get; set; }
        public int selected { get; set; }

        public GUIStyle firstStyle { get { return m_FirstStyle; } set { m_FirstStyle = value ?? GUIStyle.none; } }
        GUIStyle m_FirstStyle = GUIStyle.none;

        public GUIStyle midStyle { get { return m_MidStyle; } set { m_MidStyle = value ?? GUIStyle.none; } }
        GUIStyle m_MidStyle = GUIStyle.none;

        public GUIStyle lastStyle { get { return m_LastStyle; } set { m_LastStyle = value ?? GUIStyle.none; } }
        GUIStyle m_LastStyle = GUIStyle.none;

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMButtonGrid".GetHashCode(), focusType, position);
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
            {
                int count;
                float elemWidth;
                float elemHeight;
                if (ComputeElemDimensions(out count, out elemWidth, out elemHeight))
                {
                    //Check if the mouse is over a button (nobody says the grid is filled out)
                    Rect[] buttonRects = CalcMouseRects(position, count, xCount, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, false);
                    if (GetButtonGridMouseSelection(buttonRects, args.mousePosition, true) != -1)
                    {
                        GUIUtility.hotControl = id;
                        return true;
                    }
                }
            }
            return false;
        }

        protected override bool DoMouseDrag(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
                return true;
            return false;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                int count;
                float elemWidth;
                float elemHeight;
                if (ComputeElemDimensions(out count, out elemWidth, out elemHeight))
                {
                    GUIUtility.hotControl = 0;

                    Rect[] buttonRects = CalcMouseRects(position, count, xCount, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, false);
                    int mouseSel = GetButtonGridMouseSelection(buttonRects, args.mousePosition, true);

                    GUI.changed = true;
                    selected = mouseSel;

                    return true;
                }
            }
            return false;
        }

        public override void DoRepaint(IStylePainter args)
        {
            int count;
            float elemWidth;
            float elemHeight;
            if (ComputeElemDimensions(out count, out elemWidth, out elemHeight))
            {
                GUIStyle selStyle = GUIStyle.none;

                GUIClip.Internal_Push(position, Vector2.zero, Vector2.zero, false);
                var localPosition = new Rect(0, 0, position.width, position.height);

                Rect[] buttonRects = CalcMouseRects(localPosition, count, xCount, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, false);
                var mousePosition = args.mousePosition - position.position;
                int mouseOverSel = GetButtonGridMouseSelection(buttonRects, mousePosition, id == GUIUtility.hotControl);

                bool mouseInside = localPosition.Contains(args.mousePosition);
                GUIUtility.mouseUsed |= mouseInside;

                for (int i = 0; i < count; i++)
                {
                    // Figure out the style
                    GUIStyle s;
                    if (i != 0)
                        s = midStyle;
                    else
                        s = firstStyle;

                    if (i == count - 1)
                        s = lastStyle;

                    if (count == 1)
                        s = style;

                    if (i != selected) // We draw the selected one last, so it overflows nicer
                    {
                        s.Draw(buttonRects[i], contents[i],
                            i == mouseOverSel && (enabled || id == GUIUtility.hotControl) &&
                            (id == GUIUtility.hotControl || GUIUtility.hotControl == 0), id == GUIUtility.hotControl && enabled, false,
                            false);
                    }
                    else
                    {
                        selStyle = s;
                    }
                }

                // Draw it at the end
                if (selected < count && selected > -1)
                {
                    selStyle.Draw(buttonRects[selected], contents[selected],
                        selected == mouseOverSel && (enabled || id == GUIUtility.hotControl) &&
                        (id == GUIUtility.hotControl || GUIUtility.hotControl == 0), id == GUIUtility.hotControl, true, false);
                }

                if (mouseOverSel >= 0)
                {
                    GUI.tooltip = contents[mouseOverSel].tooltip;
                }

                GUIClip.Internal_Pop();
            }
        }

        static Rect[] CalcMouseRects(Rect position, int count, int xCount, float elemWidth, float elemHeight,
            GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, bool addBorders)
        {
            int x = 0;
            float xPos = position.xMin, yPos = position.yMin;
            GUIStyle currentButtonStyle = style;
            Rect[] retval = new Rect[count];
            if (count > 1)
                currentButtonStyle = firstStyle;
            for (int i = 0; i < count; i++)
            {
                if (!addBorders)
                    retval[i] = new Rect(xPos, yPos, elemWidth, elemHeight);
                else
                    retval[i] = currentButtonStyle.margin.Add(new Rect(xPos, yPos, elemWidth, elemHeight));

                // Correct way to get the rounded width:
                retval[i].width = Mathf.Round(retval[i].xMax) - Mathf.Round(retval[i].x);
                // Round the position *after* the position has been rounded:
                retval[i].x = Mathf.Round(retval[i].x);

                // Don't round xPos here. If rounded, the right edge of this rect may
                // not line up correctly with the left edge of the next,
                // plus it can cause cumulative rounding errors.
                // (See case 366967)

                GUIStyle nextStyle = midStyle;
                if (i == count - 2)
                    nextStyle = lastStyle;
                xPos += elemWidth + Mathf.Max(currentButtonStyle.margin.right, nextStyle.margin.left);

                x++;
                if (x >= xCount)
                {
                    x = 0;
                    yPos += elemHeight + Mathf.Max(style.margin.top, style.margin.bottom);
                    xPos = position.xMin;
                }
            }
            return retval;
        }

        // Helper function: Get the index of the element under the mouse position
        int GetButtonGridMouseSelection(Rect[] buttonRects, Vector2 mousePos, bool findNearest)
        {
            // This could be implemented faster, but for now this is not supposed to be used for a gazillion elements :)

            for (int i = 0; i < buttonRects.Length; i++)
            {
                if (buttonRects[i].Contains(mousePos))
                    return i;
            }
            if (!findNearest)
                return -1;
            // We haven't found any we're over, so we need to find the closest button.
            float minDist = float.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < buttonRects.Length; i++)
            {
                Rect r = buttonRects[i];
                Vector2 v = new Vector2(Mathf.Clamp(mousePos.x, r.xMin, r.xMax), Mathf.Clamp(mousePos.y, r.yMin, r.yMax));
                float dSqr = (mousePos - v).sqrMagnitude;
                if (dSqr < minDist)
                {
                    minIndex = i;
                    minDist = dSqr;
                }
            }

            return minIndex;
        }

        private int CalcTotalHorizSpacing()
        {
            if (xCount < 2)
                return 0;
            if (xCount == 2)
                return Mathf.Max(firstStyle.margin.right, lastStyle.margin.left);

            int internalSpace = Mathf.Max(midStyle.margin.left, midStyle.margin.right);
            return Mathf.Max(firstStyle.margin.right, midStyle.margin.left) +
                Mathf.Max(midStyle.margin.right, lastStyle.margin.left) + internalSpace * (xCount - 3);
        }

        private bool ComputeElemDimensions(out int count, out float elemWidth, out float elemHeight)
        {
            count = contents.Length;
            elemWidth = 0.0f;
            elemHeight = 0.0f;

            if (count == 0)
                return false;

            if (xCount <= 0)
            {
                Debug.LogWarning(
                    "You are trying to create a SelectionGrid with zero or less elements to be displayed in the horizontal direction. Set xCount to a positive value.");
                return false;
            }

            // Figure out how large each element should be
            int rows = count / xCount;
            if (count % xCount != 0)
                rows++;

            float totalHorizSpacing = CalcTotalHorizSpacing();
            float totalVerticalSpacing = Mathf.Max(style.margin.top, style.margin.bottom) * (rows - 1);
            elemWidth = (position.width - totalHorizSpacing) / xCount;
            elemHeight = (position.height - totalVerticalSpacing) / rows;

            if (style.fixedWidth != 0)
                elemWidth = style.fixedWidth;

            if (style.fixedHeight != 0)
                elemHeight = style.fixedHeight;

            return true;
        }
    }
}
