// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements.Debugger;

namespace UnityEditor.UIElements.Experimental.UILayoutDebugger
{
    internal class UILayoutDebugger : VisualElement
    {
        private List<LayoutDebuggerItem> recordLayout = null;
        private int m_FrameIndex = 0;
        private int m_PassIndex = 0;
        private int m_LayoutLoop = 0;
        private int m_MaxItem = 1;
        private bool m_EnableLayoutComparison = false;
        private bool m_ShowYogaNodeDirty = false;
        private VisualElement m_LockSelectedVisualElement = null;

        const int kNumberOfLinesOfInfo = 4;
        const float kLineHeight = 16.0f;
        const float kVisualContentOffset = kNumberOfLinesOfInfo * kLineHeight;

        private LayoutDebuggerVisualElement m_LastDrawElement = null;
        private Vector2 m_LastDrawElementOffset = new Vector2(0.0f, 0.0f);

        internal LayoutPanelDebuggerImpl m_ParentWindow = null;

        public void SetShowYogaNodeDirty(bool show)
        {
            m_ShowYogaNodeDirty = show;
            MarkDirtyRepaint();
        }

        public void LockSelectedElement(bool lockSelectedElement)
        {
            if (lockSelectedElement)
            {
                m_LockSelectedVisualElement = m_LastDrawElement.m_OriginalVisualElement;
            }
            else
            {
                m_LockSelectedVisualElement = null;
            }
        }


        void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (evt.button == 2)
            {
                Vector2 pos = new Vector2(evt.localPosition.x, evt.localPosition.y - kVisualContentOffset);
                Debug.Log(pos);
                SelectVisualElement(pos);
                evt.StopImmediatePropagation();
            }
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.LeftArrow)
            {
                GotoPreviousItem();
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.RightArrow)
            {
                GotoNextItem();
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                m_MaxItem = GotoParent();
                m_MaxItem = m_ParentWindow.UpdateSlider(m_MaxItem);
                evt.StopImmediatePropagation();
            }
        }

        void OnWheelEvent(WheelEvent evt)
        {
            if (evt.delta.y < 0)
            {
                GotoPreviousItem();
            }
            else
            {
                GotoNextItem();
            }
            evt.StopImmediatePropagation();
        }

        private void GotoPreviousItem()
        {
            m_MaxItem--;
            m_MaxItem = m_ParentWindow.UpdateSlider(m_MaxItem);
        }

        private void GotoNextItem()
        {
            m_MaxItem++;
            m_MaxItem = m_ParentWindow.UpdateSlider(m_MaxItem);
        }

        private void GotoElement(LayoutDebuggerVisualElement currentElement, LayoutDebuggerVisualElement elementToFind, ref int count, ref int nextMaxItem)
        {
            if (!currentElement.IsVisualElementVisible())
            {
                return;
            }

            if (currentElement == elementToFind)
            {
                nextMaxItem = count;
                return;
            }

            count++;

            foreach (var c in currentElement.m_Children)
            {
                GotoElement(c, elementToFind, ref count, ref nextMaxItem);
            }
        }

        private int GotoParent()
        {
            for (int i = 0; i < recordLayout.Count; ++i)
            {
                var record = recordLayout[i];
                if (record.m_FrameIndex == m_FrameIndex)
                {
                    if (record.m_LayoutLoop == m_LayoutLoop)
                    {
                        int nextMaxItem = -1;
                        int count = 0;

                        GotoElement(record.m_VE, m_LastDrawElement.parent, ref count, ref nextMaxItem);

                        if (nextMaxItem != -1)
                        {
                            MarkDirtyRepaint();
                            return nextMaxItem;
                        }

                        return m_MaxItem;
                    }
                }
            }

            return m_MaxItem;
        }

        private void SelectVisualElement(Vector2 pos)
        {
            foreach (var record in recordLayout)
            {
                if (record.m_FrameIndex == m_FrameIndex)
                {
                    if (record.m_PassIndex == m_PassIndex)
                    {
                        if (record.m_LayoutLoop == m_LayoutLoop)
                        {
                            int count = 0;
                            int nextMaxItem = 0;
                            SelectVisualElement(record.m_VE, pos, ref count, ref nextMaxItem);
                            m_MaxItem = nextMaxItem;
                            MarkDirtyRepaint();

                            if (m_ParentWindow != null)
                            {
                                m_ParentWindow.UpdateSlider(m_MaxItem);
                            }

                            return;
                        }
                    }
                }
            }
        }

        private void SelectVisualElement(LayoutDebuggerVisualElement ve, Vector2 pos, ref int count, ref int nextMaxItem, float offset_x = 0.0f, float offset_y = 0.0f)
        {
            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            if (count > m_MaxItem)
            {
                return;
            }

            Rect rect = new Rect();
            rect.x = ve.layout.x + offset_x;
            rect.y = ve.layout.y + offset_y;
            rect.width = ve.layout.width;
            rect.height = ve.layout.height;

            if (rect.Contains(pos))
            {
                nextMaxItem = count;
            }

            count++;

            for (int i = 0; i < ve.m_Children.Count; ++i)
            {
                var child = ve.m_Children[i];
                SelectVisualElement(child, pos, ref count, ref nextMaxItem, rect.x, rect.y);
            }
        }

        public UILayoutDebugger() : base()
        {
            focusable = true;
            generateVisualContent += OnGenerateVisualContent;

            style.marginRight = 2;
            style.marginLeft = 2;
            style.marginBottom = 2;
            style.marginTop = 2;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            RegisterCallback<WheelEvent>(OnWheelEvent);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
            UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
            UnregisterCallback<WheelEvent>(OnWheelEvent);
        }

        public void EnableLayoutComparison(bool enable)
        {
            m_EnableLayoutComparison = enable;
            MarkDirtyRepaint();
        }

        internal void SetMaxItem(int maxItem)
        {
            m_MaxItem = maxItem;

            if (m_LockSelectedVisualElement != null)
            {
                if (recordLayout != null)
                {
                    for (int i = 0; i < recordLayout.Count; i++)
                    {
                        var record = recordLayout[i];
                        if (record.m_FrameIndex == m_FrameIndex)
                        {
                            if (record.m_PassIndex == m_PassIndex)
                            {
                                if (record.m_LayoutLoop == m_LayoutLoop)
                                {
                                    // Update MaxItem
                                    int findLockedCurrentIndex = 0;
                                    FindLockedItem(record.m_VE, ref findLockedCurrentIndex);
                                }
                            }
                        }
                    }
                }
                m_ParentWindow.UpdateSlider(m_MaxItem, false);
            }
            MarkDirtyRepaint();
        }

        public void SetItemIndex(int index)
        {
            if (recordLayout != null)
            {
                if (index < recordLayout.Count)
                {
                    SetIndices(
                        recordLayout[index].m_FrameIndex,
                        recordLayout[index].m_PassIndex,
                        recordLayout[index].m_LayoutLoop);
                }
            }
        }

        internal void SetIndices(int frameIndex, int passIndex, int layoutLoop)
        {
            m_FrameIndex = frameIndex;
            m_PassIndex = passIndex;
            m_LayoutLoop = layoutLoop;
            MarkDirtyRepaint();
        }

        internal void SetRecord(List<LayoutDebuggerItem> _recordLayout)
        {
            recordLayout = _recordLayout;

            if (recordLayout != null)
            {
                // adjust size with base element
                if (recordLayout.Count > 0)
                {
                    style.width = recordLayout[0].m_VE.layout.width;
                    style.height = recordLayout[0].m_VE.layout.height + kVisualContentOffset;
                }
            }
        }

        internal static void CountLayoutItem(Rect rectRootVE, LayoutDebuggerVisualElement ve, ref int itemCount, ref int zeroSizeCount, ref int outOfRootVE, float offset_x = 0.0f, float offset_y = 0.0f)
        {
            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            Rect rect = new Rect();

            rect.x = ve.layout.x + offset_x;
            rect.y = ve.layout.y + offset_y;
            rect.width = ve.layout.width;
            rect.height = ve.layout.height;

            itemCount++;

            if (rect.width == 0 && rect.height == 0)
            {
                zeroSizeCount++;
            }

            if (!rectRootVE.Overlaps(rect))
            {
                outOfRootVE++;
            }

            for (int i = 0; i < ve.m_Children.Count; ++i)
            {
                var child = ve.m_Children[i];
                CountLayoutItem(rectRootVE, child, ref itemCount, ref zeroSizeCount, ref outOfRootVE, rect.x, rect.y);
            }
        }

        private void CountDepth(LayoutDebuggerVisualElement ve, ref int depth)
        {
            if (ve.parent != null)
            {
                depth++;
                CountDepth(ve.parent, ref depth);
            }
        }

        private void FindLockedItem(LayoutDebuggerVisualElement ve, ref int currentIndex)
        {
            if (ve.m_OriginalVisualElement == m_LockSelectedVisualElement)
            {
                m_MaxItem = currentIndex;
                return;
            }

            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            currentIndex++;

            if (ve.m_Children != null)
            {
                var childCount = ve.m_Children.Count;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = ve.m_Children[i];
                    FindLockedItem(child, ref currentIndex);
                }
            }
        }

        // Note: Can draw pass the bound of the VisualElement
        public void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (recordLayout != null)
            {
                var paint2D = mgc.painter2D;
                for (int i = 0; i < recordLayout.Count; i++)
                {
                    var record = recordLayout[i];

                    if (record.m_FrameIndex == m_FrameIndex)
                    {
                        if (record.m_PassIndex == m_PassIndex)
                        {
                            if (record.m_LayoutLoop == m_LayoutLoop)
                            {
                                int count = 0;
                                int countOfZeroSizeElement = 0;
                                int outOfRootVE = 0;

                                Rect rectRootVE = new Rect();
                                rectRootVE.x = record.m_VE.layout.x;
                                rectRootVE.y = record.m_VE.layout.y;
                                rectRootVE.width = record.m_VE.layout.width;
                                rectRootVE.height = record.m_VE.layout.height;

                                if (m_LockSelectedVisualElement != null)
                                {
                                    // Update MaxItem
                                    int findLockedCurrentIndex = 0;
                                    FindLockedItem(record.m_VE, ref findLockedCurrentIndex);
                                }

                                CountLayoutItem(rectRootVE, record.m_VE, ref count, ref countOfZeroSizeElement, ref outOfRootVE);

                                int currentIndex = 0;
                                if (m_EnableLayoutComparison)
                                {
                                    DisplayLayoutComparison(paint2D, recordLayout[i].m_VE, recordLayout[i + 1].m_VE, 0.5f, kVisualContentOffset + 0.5f);
                                }
                                else
                                {
                                    DisplayLayoutRecursive(paint2D, ref currentIndex, record.m_VE, 0.5f, kVisualContentOffset + 0.5f);
                                }

                                int depth = 0;
                                CountDepth(m_LastDrawElement, ref depth);

                                string className = m_LastDrawElement.m_OriginalVisualElement.GetType().ToString();

                                Color textColor = Color.white;

                                if (m_LockSelectedVisualElement != null)
                                {
                                    if (m_LockSelectedVisualElement != m_LastDrawElement.m_OriginalVisualElement)
                                    {
                                        textColor = Color.red;
                                        className += string.Format(" - Locked item not found (Name: {0} Class: {1})",
                                            m_LockSelectedVisualElement.name,
                                            m_LockSelectedVisualElement.GetType().ToString());
                                    }
                                }

                                string infoString = string.Format("CurrentElement: Name: {0} Class: {1}",
                                    m_LastDrawElement.name,
                                    className
                                    );

                                int numberOfLinesOfInfo = 0;

                                mgc.DrawText(infoString, new Vector2(0.0f, (numberOfLinesOfInfo++) * kLineHeight), 12.0f, textColor);


                                infoString = string.Format("CurrentElement: Local ({0} {1}, {2}, {3}) Global ({4} {5}, {6}, {7}) Depth:{8}",
                                    m_LastDrawElement.layout.x,
                                    m_LastDrawElement.layout.y,
                                    m_LastDrawElement.layout.width,
                                    m_LastDrawElement.layout.height,
                                    m_LastDrawElement.layout.x + m_LastDrawElementOffset.x - 0.5f,
                                    m_LastDrawElement.layout.y + m_LastDrawElementOffset.y - 0.5f,
                                    m_LastDrawElement.layout.width,
                                    m_LastDrawElement.layout.height,
                                    depth);

                                mgc.DrawText(infoString, new Vector2(0.0f, (numberOfLinesOfInfo++) * kLineHeight), 12.0f, Color.white);

                                infoString = string.Format("CurrentElement: Visible:{0} Enable:{1} EnableInHierarchy:{2} YogaNodeDirty:{3}",
                                    m_LastDrawElement.visible,
                                    m_LastDrawElement.enable,
                                    m_LastDrawElement.enabledInHierarchy,
                                    m_LastDrawElement.isYogaNodeDirty);

                                mgc.DrawText(infoString, new Vector2(0.0f, (numberOfLinesOfInfo++) * kLineHeight), 12.0f, Color.white);

                                infoString = string.Format("Count of ZeroSize Element:{0} {1}%",
                                    countOfZeroSizeElement,
                                    100.0f * countOfZeroSizeElement / count);

                                mgc.DrawText(infoString, new Vector2(0.0f, numberOfLinesOfInfo * kLineHeight), 12.0f, Color.white);

                                infoString = string.Format("Count of Out of Root Element:{0} {1}%",
                                    outOfRootVE,
                                    100.0f * outOfRootVE / count);

                                mgc.DrawText(infoString, new Vector2(350.0f, (numberOfLinesOfInfo++) * kLineHeight), 12.0f, Color.white);

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DisplayLayoutComparison(Painter2D paint2D, LayoutDebuggerVisualElement veA, LayoutDebuggerVisualElement veB, float offset_x, float offset_y)
        {
            // Display Item B
            float x = veB.layout.x + offset_x;
            float y = veB.layout.y + offset_y;
            float w = veB.layout.width;
            float h = veB.layout.height;

            if (veA == null || (veA.layout.x != veB.layout.x) ||
                 (veA.layout.y != veB.layout.y) ||
                 (veA.layout.width != veB.layout.width) ||
                 (veA.layout.height != veB.layout.height) ||
                 (veA.name != veB.name))
            {

                if (w > 0 && h > 0)
                {
                    paint2D.BeginPath();
                    paint2D.MoveTo(new Vector2(x, y));
                    paint2D.LineTo(new Vector2(x + w, y));
                    paint2D.LineTo(new Vector2(x + w, y + h));
                    paint2D.LineTo(new Vector2(x, y + h));
                    paint2D.ClosePath();
                    paint2D.Stroke();
                }
            }

            if (veB.m_Children == null)
            {
                return;
            }

            for (int i = 0; i < veB.m_Children.Count; i++)
            {
                if (veA != null && veA.m_Children != null)
                {
                    if (i < veA.m_Children.Count)
                    {
                        DisplayLayoutComparison(paint2D, veA.m_Children[i], veB.m_Children[i], x, y);
                    }
                    else
                    {
                        DisplayLayoutComparison(paint2D, null, veB.m_Children[i], x, y);
                    }
                }
                else
                {
                    DisplayLayoutComparison(paint2D, null, veB.m_Children[i], x, y);
                }
            };
        }

        private void DisplayLayoutRecursive(Painter2D paint2D, ref int currentIndex, LayoutDebuggerVisualElement ve, float offset_x, float offset_y)
        {
            if (currentIndex == m_MaxItem)
            {
                m_LastDrawElement = ve;
                m_LastDrawElementOffset = new Vector2(offset_x, offset_y);
            }

            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            float x = ve.layout.x + offset_x;
            float y = ve.layout.y + offset_y;
            float w = ve.layout.width;
            float h = ve.layout.height;

            bool selected = false;

            if (currentIndex < (m_MaxItem))
            {
                float alpha = (float)currentIndex / (float)m_MaxItem;
                paint2D.strokeColor = new Color(alpha, 1.0f - alpha, 0.0f);
            }
            else
            {
                paint2D.strokeColor = Color.cyan;
                selected = true;
            }

            if (currentIndex <= m_MaxItem)
            {
                if (w > 0 && h > 0)
                {
                    bool show = true;

                    if (m_ShowYogaNodeDirty && ve.isYogaNodeDirty == false)
                    {
                        show = false;
                    }

                    paint2D.BeginPath();
                    paint2D.MoveTo(new Vector2(x, y));
                    paint2D.LineTo(new Vector2(x + w, y));
                    paint2D.LineTo(new Vector2(x + w, y + h));
                    paint2D.LineTo(new Vector2(x, y + h));
                    paint2D.ClosePath();

                    if (show)
                    {
                        paint2D.Stroke();
                    }

                    if (currentIndex == 0)
                    {
                        paint2D.fillColor = Color.black;
                        paint2D.Fill();
                    }
                }
                else if (selected)
                {
                    if (w == 0) w = 1;
                    if (h == 0) h = 1;

                    paint2D.BeginPath();
                    paint2D.MoveTo(new Vector2(x, y));
                    paint2D.LineTo(new Vector2(x + w, y));
                    paint2D.LineTo(new Vector2(x + w, y + h));
                    paint2D.LineTo(new Vector2(x, y + h));
                    paint2D.ClosePath();
                    paint2D.Stroke();
                }
            }

            currentIndex++;

            if (ve.m_Children != null)
            {
                var childCount = ve.m_Children.Count;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = ve.m_Children[i];
                    DisplayLayoutRecursive(paint2D, ref currentIndex, child, x, y);
                }
            }
        }
    }
}
