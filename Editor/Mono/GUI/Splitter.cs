// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    /// *undocumented*
    [System.Serializable]
    internal class SplitterState
    {
        const int defaultSplitSize = 6;

        public int ID;
        public int splitterInitialOffset;
        public int currentActiveSplitter = -1;

        public int[] realSizes;
        public float[] relativeSizes; // these should always add up to 1

        public int[] minSizes;
        public int[] maxSizes;

        public int lastTotalSize = 0;

        public int splitSize;

        public float xOffset;

        public SplitterState(params float[] relativeSizes)
        {
            Init(relativeSizes, null, null, 0);
        }

        public SplitterState(int[] realSizes, int[] minSizes, int[] maxSizes)
        {
            this.realSizes = realSizes;
            this.minSizes = minSizes == null ? new int[realSizes.Length] : minSizes;
            this.maxSizes = maxSizes == null ? new int[realSizes.Length] : maxSizes;
            relativeSizes = new float[realSizes.Length];

            this.splitSize = splitSize == 0 ? defaultSplitSize : splitSize;

            RealToRelativeSizes();
        }

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            Init(relativeSizes, minSizes, maxSizes, 0);
        }

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
        {
            Init(relativeSizes, minSizes, maxSizes, splitSize);
        }

        private void Init(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
        {
            this.relativeSizes = relativeSizes;
            this.minSizes = minSizes == null ? new int[relativeSizes.Length] : minSizes;
            this.maxSizes = maxSizes == null ? new int[relativeSizes.Length] : maxSizes;
            realSizes = new int[relativeSizes.Length];

            this.splitSize = splitSize == 0 ? defaultSplitSize : splitSize;

            NormalizeRelativeSizes();
        }

        public void NormalizeRelativeSizes()
        {
            float check = 1.0f; // try to avoid rounding issues
            float total = 0;
            int k;

            // distribute space relatively
            for (k = 0; k < relativeSizes.Length; k++)
                total += relativeSizes[k];

            for (k = 0; k < relativeSizes.Length; k++)
            {
                relativeSizes[k] = relativeSizes[k] / total;
                check -= relativeSizes[k];
            }

            relativeSizes[relativeSizes.Length - 1] += check;
        }

        public void RealToRelativeSizes()
        {
            float check = 1.0f; // try to avoid rounding issues
            float total = 0;
            int k;

            // distribute space relatively
            for (k = 0; k < realSizes.Length; k++)
                total += realSizes[k];

            for (k = 0; k < realSizes.Length; k++)
            {
                relativeSizes[k] = realSizes[k] / total;
                check -= relativeSizes[k];
            }
            if (relativeSizes.Length > 0)
                relativeSizes[relativeSizes.Length - 1] += check;
        }

        public void RelativeToRealSizes(int totalSpace)
        {
            int spaceToShare = totalSpace, k;

            for (k = 0; k < relativeSizes.Length; k++)
            {
                realSizes[k] = (int)Mathf.Round(relativeSizes[k] * totalSpace);

                if (realSizes[k] < minSizes[k])
                    realSizes[k] = minSizes[k];

                spaceToShare -= realSizes[k];
            }

            if (spaceToShare < 0)
            {
                for (k = 0; k < relativeSizes.Length; k++)
                {
                    if (realSizes[k] > minSizes[k])
                    {
                        int spaceInThisOne = realSizes[k] - minSizes[k];
                        int spaceToTake = -spaceToShare < spaceInThisOne ? -spaceToShare : spaceInThisOne;

                        spaceToShare += spaceToTake;
                        realSizes[k] -= spaceToTake;

                        if (spaceToShare >= 0)
                            break;
                    }
                }
            }

            int last = realSizes.Length - 1;
            if (last >= 0)
            {
                realSizes[last] += spaceToShare; // try to avoid rounding issues

                if (realSizes[last] < minSizes[last]) // but never ignore min size!
                    realSizes[last] = minSizes[last];
            }
        }

        public void DoSplitter(int i1, int i2, int diff)
        {
            // TODO: This does not handle all cases properly. Theres a hope we will not encounter those cases in the editor.
            // Needs to be fixed once its passed to users.
            int h1 = realSizes[i1];
            int h2 = realSizes[i2];
            int m1 = minSizes[i1];
            int m2 = minSizes[i2];
            int x1 = maxSizes[i1];
            int x2 = maxSizes[i2];

            bool diffed = false;

            if (m1 == 0) m1 = 16;
            if (m2 == 0) m2 = 16;

            // min constraint
            if (h1 + diff < m1)
            {
                diff -= m1 - h1;
                realSizes[i2] += realSizes[i1] - m1;
                realSizes[i1] = m1;

                if (i1 != 0)
                    DoSplitter(i1 - 1, i2, diff);
                else
                    // can't resize more...
                    splitterInitialOffset -= diff;

                diffed = true;
            }
            else if (h2 - diff < m2)
            {
                diff -= h2 - m2;
                realSizes[i1] += realSizes[i2] - m2;
                realSizes[i2] = m2;

                if (i2 != realSizes.Length - 1)
                    DoSplitter(i1, i2 + 1, diff);
                else
                    // can't resize more...
                    splitterInitialOffset -= diff;

                diffed = true;
            }

            // max constraint
            if (!diffed)
            {
                if ((x1 != 0) && (h1 + diff > x1))
                {
                    diff -= realSizes[i1] - x1;
                    realSizes[i2] += realSizes[i1] - x1;
                    realSizes[i1] = x1;

                    if (i1 != 0)
                        DoSplitter(i1 - 1, i2, diff);
                    else
                        // can't resize more...
                        splitterInitialOffset -= diff;

                    diffed = true;
                }
                else if ((x2 != 0) && (h2 - diff > x2))
                {
                    diff -= h2 - x2;
                    realSizes[i1] += realSizes[i2] - x2;
                    realSizes[i2] = x2;

                    if (i2 != realSizes.Length - 1)
                        DoSplitter(i1, i2 + 1, diff);
                    else
                        // can't resize more...
                        splitterInitialOffset -= diff;

                    diffed = true;
                }
            }

            // normal case - we have space for resizing
            if (!diffed)
            {
                realSizes[i1] += diff;
                realSizes[i2] -= diff;
            }
        }
    }

    class SplitterGUILayout
    {
        static int splitterHash = "Splitter".GetHashCode();

        /// *undocumented*
        internal class GUISplitterGroup : GUILayoutGroup
        {
            public SplitterState state;

            public override void SetHorizontal(float x, float width)
            {
                if (!isVertical)
                {
                    int k;

                    state.xOffset = x;

                    if (width != state.lastTotalSize)
                    {
                        state.RelativeToRealSizes((int)width);
                        state.lastTotalSize = (int)width;

                        // maintain constraints while resizing
                        for (k = 0; k < state.realSizes.Length - 1; k++)
                            state.DoSplitter(k, k + 1, 0);
                    }

                    k = 0;

                    foreach (GUILayoutEntry i in entries)
                    {
                        float thisSize = state.realSizes[k];

                        i.SetHorizontal(Mathf.Round(x), Mathf.Round(thisSize));
                        x += thisSize + spacing;
                        k++;
                    }
                }
                else
                {
                    base.SetHorizontal(x, width);
                }
            }

            public override void SetVertical(float y, float height)
            {
                rect.y = y; rect.height = height;

                RectOffset padding = style.padding;

                if (isVertical)
                {
                    // If we have a skin, adjust the sizing to take care of padding (if we don't have a skin the vertical margins have been propagated fully up the hierarchy)...
                    if (style != GUIStyle.none)
                    {
                        float topMar = padding.top, bottomMar = padding.bottom;
                        if (entries.Count != 0)
                        {
                            topMar = Mathf.Max(topMar, ((GUILayoutEntry)entries[0]).margin.top);
                            bottomMar = Mathf.Max(bottomMar, ((GUILayoutEntry)entries[entries.Count - 1]).margin.bottom);
                        }
                        y += topMar;
                        height -= bottomMar + topMar;
                    }

                    // Set the positions
                    int k;

                    if (height != state.lastTotalSize)
                    {
                        state.RelativeToRealSizes((int)height);
                        state.lastTotalSize = (int)height;

                        // maintain constraints while resizing
                        for (k = 0; k < state.realSizes.Length - 1; k++)
                            state.DoSplitter(k, k + 1, 0);
                    }

                    k = 0;

                    foreach (GUILayoutEntry i in entries)
                    {
                        float thisSize = state.realSizes[k];

                        i.SetVertical(Mathf.Round(y), Mathf.Round(thisSize));
                        y += thisSize + spacing;
                        k++;
                    }
                }
                else
                {
                    // If we have a GUIStyle here, we need to respect the subelements' margins
                    if (style != GUIStyle.none)
                    {
                        foreach (GUILayoutEntry i in entries)
                        {
                            float topMar = Mathf.Max(i.margin.top, padding.top);
                            float thisY = y + topMar;
                            float thisHeight = height - Mathf.Max(i.margin.bottom, padding.bottom) - topMar;

                            if (i.stretchHeight != 0)
                                i.SetVertical(thisY, thisHeight);
                            else
                                i.SetVertical(thisY, Mathf.Clamp(thisHeight, i.minHeight, i.maxHeight));
                        }
                    }
                    else
                    {
                        // If not, the subelements' margins have already been propagated upwards to this group, so we can safely ignore them
                        float thisY = y - margin.top;
                        float thisHeight = height + margin.vertical;
                        foreach (GUILayoutEntry i in entries)
                        {
                            if (i.stretchHeight != 0)
                                i.SetVertical(thisY + i.margin.top, thisHeight - i.margin.vertical);
                            else
                                i.SetVertical(thisY + i.margin.top, Mathf.Clamp(thisHeight - i.margin.vertical, i.minHeight, i.maxHeight));
                        }
                    }
                }
            }
        }

        public static void BeginSplit(SplitterState state, GUIStyle style, bool vertical, params GUILayoutOption[] options)
        {
            int pos;
            var g = (GUISplitterGroup)GUILayoutUtility.BeginLayoutGroup(style, null, typeof(GUISplitterGroup));
            state.ID = GUIUtility.GetControlID(splitterHash, FocusType.Passive);

            switch (Event.current.GetTypeForControl(state.ID))
            {
                case EventType.Layout:
                {
                    g.state = state;
                    g.resetCoords = false;
                    g.isVertical = vertical;
                    g.ApplyOptions(options);
                    break;
                }
                case EventType.MouseDown:
                {
                    if ((Event.current.button == 0) && (Event.current.clickCount == 1))
                    {
                        int cursor = g.isVertical ? (int)g.rect.y : (int)g.rect.x;
                        pos = g.isVertical ? (int)Event.current.mousePosition.y : (int)Event.current.mousePosition.x;

                        for (int i = 0; i < state.relativeSizes.Length - 1; i++)
                        {
                            Rect splitterRect = g.isVertical ?
                                new Rect(state.xOffset + g.rect.x, cursor + state.realSizes[i] - state.splitSize / 2, g.rect.width, state.splitSize) :
                                new Rect(state.xOffset + cursor + state.realSizes[i] - state.splitSize / 2, g.rect.y, state.splitSize, g.rect.height);

                            if (splitterRect.Contains(Event.current.mousePosition))
                            {
                                state.splitterInitialOffset = pos;
                                state.currentActiveSplitter = i;
                                GUIUtility.hotControl = state.ID;
                                Event.current.Use();
                                break;
                            }

                            cursor += (int)state.realSizes[i];
                        }
                    }
                    break;
                }
                case EventType.MouseDrag:
                {
                    if ((GUIUtility.hotControl == state.ID) && (state.currentActiveSplitter >= 0))
                    {
                        pos = g.isVertical ? (int)Event.current.mousePosition.y : (int)Event.current.mousePosition.x;
                        int diff = pos - state.splitterInitialOffset;

                        if (diff != 0)
                        {
                            state.splitterInitialOffset = pos;
                            state.DoSplitter(state.currentActiveSplitter, state.currentActiveSplitter + 1, diff);
                        }

                        Event.current.Use();
                    }
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == state.ID)
                    {
                        GUIUtility.hotControl = 0;
                        state.currentActiveSplitter = -1;
                        state.RealToRelativeSizes();
                        Event.current.Use();
                    }
                    break;
                }
                case EventType.Repaint:
                {
                    int cursor = g.isVertical ? (int)g.rect.y : (int)g.rect.x;

                    for (var i = 0; i < state.relativeSizes.Length - 1; i++)
                    {
                        var splitterRect = g.isVertical ?
                            new Rect(state.xOffset + g.rect.x, cursor + state.realSizes[i] - state.splitSize / 2, g.rect.width, state.splitSize) :
                            new Rect(state.xOffset + cursor + state.realSizes[i] - state.splitSize / 2, g.rect.y, state.splitSize, g.rect.height);

                        EditorGUIUtility.AddCursorRect(splitterRect, g.isVertical ? MouseCursor.ResizeVertical : MouseCursor.SplitResizeLeftRight, state.ID);

                        cursor += state.realSizes[i];
                    }
                }

                break;
            }
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            BeginSplit(state, GUIStyle.none, false, options);
        }

        public static void BeginVerticalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            BeginSplit(state, GUIStyle.none, true, options);
        }

        public static void BeginHorizontalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
        {
            BeginSplit(state, style, false, options);
        }

        public static void BeginVerticalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
        {
            BeginSplit(state, style, true, options);
        }

        public static void EndVerticalSplit()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        public static void EndHorizontalSplit()
        {
            GUILayoutUtility.EndLayoutGroup();
        }
    }
}
