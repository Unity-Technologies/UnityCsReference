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
        private bool m_ShowDirty = false;
        private Label m_InfoLine1;
        private Label m_InfoLine2;
        private Label m_InfoLine3;
        private Label m_InfoLine4;
        private VisualElement m_LayoutDisplay;
        private VisualElement m_LockSelectedVisualElement = null;

        public LayoutDebuggerVisualElement lastDrawElement
        {
            get { return m_LastDrawElement; }
            set
            {
                m_LastDrawElement = value;
                foreach (var record in recordLayout)
                {
                    if (record.m_FrameIndex == m_FrameIndex)
                    {
                        if (record.m_PassIndex == m_PassIndex)
                        {
                            if (record.m_LayoutLoop == m_LayoutLoop)
                            {
                                int temp = 0;
                                FindLastDrawnItem(record.m_VE, ref temp);
                            }
                        }
                    }
                }

                m_MaxItem = m_ParentWindow.UpdateSlider(m_MaxItem);
            }
        }

        private LayoutDebuggerVisualElement m_LastDrawElement = null;
        private Vector2 m_LastDrawElementOffset = new Vector2(0.0f, 0.0f);

        internal LayoutPanelDebuggerImpl m_ParentWindow = null;

        public UILayoutDebugger() : base()
        {
            focusable = true;
            style.marginRight = 2;
            style.marginLeft = 2;
            style.marginBottom = 2;
            style.marginTop = 2;

            style.overflow = Overflow.Hidden;

            style.minWidth = 100.0f;
            style.minHeight = 100.0f;

            style.flexDirection = FlexDirection.Column;

            m_InfoLine1 = new Label();
            m_InfoLine1.enableRichText = true;
            m_InfoLine2 = new Label();
            m_InfoLine2.enableRichText = true;
            m_InfoLine3 = new Label();
            m_InfoLine3.enableRichText = true;
            m_InfoLine4 = new Label();
            m_InfoLine4.enableRichText = true;

            VisualElement ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Column;
            ve.style.flexShrink = 0.0f;

            ve.Add(m_InfoLine1);
            ve.Add(m_InfoLine2);
            ve.Add(m_InfoLine3);
            ve.Add(m_InfoLine4);

            m_LayoutDisplay = new VisualElement();
            m_LayoutDisplay.style.flexShrink = 0.0f;

            Add(ve);
            Add(m_LayoutDisplay);

            m_LayoutDisplay.generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void UpdateDisplay()
        {
            m_LayoutDisplay.MarkDirtyRepaint();
            UpdateInfo();
        }

        void UpdateInfo()
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
                                int count = 0;
                                int countOfZeroSizeElement = 0;
                                int outOfRootVE = 0;

                                Rect rectRootVE = new Rect();
                                rectRootVE.x = record.m_VE.layout.x;
                                rectRootVE.y = record.m_VE.layout.y;
                                rectRootVE.width = record.m_VE.layout.width;
                                rectRootVE.height = record.m_VE.layout.height;

                                m_LayoutDisplay.style.width = rectRootVE.width;
                                m_LayoutDisplay.style.height = rectRootVE.height;

                                int currentIndex = 0;
                                DisplayLayoutRecursive(null, ref currentIndex, record.m_VE, 0.5f, 0.5f);

                                CountLayoutItem(rectRootVE, record.m_VE, ref count, ref countOfZeroSizeElement, ref outOfRootVE);

                                int depth = 0;
                                CountDepth(m_LastDrawElement, ref depth);

                                string className = m_LastDrawElement.m_OriginalVisualElement.GetType().ToString();

                                if (m_LockSelectedVisualElement != null)
                                {
                                    if (m_LockSelectedVisualElement != m_LastDrawElement.m_OriginalVisualElement)
                                    {
                                        className += string.Format("<color=\"red\"> - Locked item not found (Name: {0} Class: {1})",
                                            m_LockSelectedVisualElement.name,
                                            m_LockSelectedVisualElement.GetType().ToString());
                                    }
                                }

                                m_InfoLine1.text = string.Format("<color=\"white\">CurrentElement: Name: {0} Class: {1}",
                                    m_LastDrawElement.name,
                                    className
                                    );

                                m_InfoLine2.text = string.Format("<color=\"white\">CurrentElement: Local ({0} {1}, {2}, {3}) Global ({4} {5}, {6}, {7}) Depth:{8}",
                                    m_LastDrawElement.layout.x,
                                    m_LastDrawElement.layout.y,
                                    m_LastDrawElement.layout.width,
                                    m_LastDrawElement.layout.height,
                                    m_LastDrawElement.layout.x + m_LastDrawElementOffset.x - 0.5f,
                                    m_LastDrawElement.layout.y + m_LastDrawElementOffset.y - 0.5f,
                                    m_LastDrawElement.layout.width,
                                    m_LastDrawElement.layout.height,
                                    depth);


                                m_InfoLine3.text = string.Format("<color=\"white\">CurrentElement: Visible:{0} Enable:{1} EnableInHierarchy:{2} YogaNodeDirty:{3}",
                                    m_LastDrawElement.visible,
                                    m_LastDrawElement.enable,
                                    m_LastDrawElement.enabledInHierarchy,
                                    m_LastDrawElement.isDirty);

                                m_InfoLine4.text = string.Format("<color=\"white\">Count of ZeroSize Element:{0} {1}%   Count of Out of Root Element:{0} {1}%",
                                    countOfZeroSizeElement,
                                    100.0f * countOfZeroSizeElement / count,
                                    outOfRootVE,
                                    100.0f * outOfRootVE / count);

                                break;
                            }
                        }
                    }
                }
            }
        }

        public void SetShowDirty(bool show)
        {
            m_ShowDirty = show;
            UpdateDisplay();
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
                Vector2 pos = new Vector2(evt.localPosition.x, evt.localPosition.y);
                SelectVisualElement(pos);
                evt.StopImmediatePropagation();
                m_ParentWindow.UpdateInfo();
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
            m_ParentWindow.UpdateInfo();
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
                            UpdateDisplay();
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
                            UpdateDisplay();

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
                if (m_ShowDirty)
                {
                    if (ve.isDirty)
                    {
                        nextMaxItem = count;
                    }
                }
                else
                {
                    nextMaxItem = count;
                }
            }

            count++;

            for (int i = 0; i < ve.m_Children.Count; ++i)
            {
                var child = ve.m_Children[i];
                SelectVisualElement(child, pos, ref count, ref nextMaxItem, rect.x, rect.y);
            }
        }

        private void GetAllElements(LayoutDebuggerVisualElement ve, ref List<LayoutDebuggerVisualElement> foundElements)
        {
            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            if (m_LastDrawElement != null)
            {
                if (ve.m_OriginalVisualElement == m_LastDrawElement.m_OriginalVisualElement)
                {
                    foundElements.Add(ve);
                }
            }

            for (int i = 0; i < ve.m_Children.Count; ++i)
            {
                var child = ve.m_Children[i];
                GetAllElements(child, ref foundElements);
            }
        }

        public class InfoData
        {
            public int m_FrameIndex;
            public int m_PassIndex;
            public int m_LayoutLoop;
            public int m_SortIndex;
            public string m_Info;
            public bool m_HighLight;
            public LayoutDebuggerVisualElement m_VE;
        };

        List<InfoData> data = new List<InfoData>();

        static private void CreateLastLayout(LayoutDebuggerVisualElement ve, Dictionary<VisualElement, Rect> lastLayout)
        {
            lastLayout[ve.m_OriginalVisualElement] = ve.layout;

            for (int i = 0; i < ve.m_Children.Count; i++)
            {
                CreateLastLayout(ve.m_Children[i], lastLayout);
            }
        }

        public void FillUpdateInfoOfSelectedElement(MultiColumnListView listView)
        {
            data.Clear();

            if (m_ShowDirty)
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
                                Queue<LayoutDebuggerVisualElement> foundElements = new Queue<LayoutDebuggerVisualElement>();
                                foundElements.Enqueue(record.m_VE);
                                int sortIndex = 0;

                                Dictionary<VisualElement, Rect> lastLayout = new Dictionary<VisualElement, Rect>();

                                if (i > 0)
                                {
                                    CreateLastLayout(recordLayout[i - 1].m_VE, lastLayout);
                                }

                                while (foundElements.Count > 0)
                                {
                                    var ve = foundElements.Dequeue();

                                    if ((ve.layout.width > 0 && ve.layout.height > 0))
                                    {
                                        if (ve.isDirty)
                                        {
                                            InfoData item = new InfoData();
                                            item.m_FrameIndex = record.m_FrameIndex;
                                            item.m_PassIndex = record.m_PassIndex;
                                            item.m_LayoutLoop = record.m_LayoutLoop;
                                            item.m_Info = ve.m_OriginalVisualElement.GetType().ToString() + " " + ve.m_OriginalVisualElement.name + " " + ve.layout.ToString();

                                            item.m_HighLight = false;

                                            if (lastLayout.ContainsKey(ve.m_OriginalVisualElement) && (ve.layout != lastLayout[ve.m_OriginalVisualElement]))
                                            {
                                                item.m_Info += " last:" + lastLayout[ve.m_OriginalVisualElement].ToString();
                                                item.m_HighLight = true;
                                            }
                                            else if (!lastLayout.ContainsKey(ve.m_OriginalVisualElement))
                                            {
                                                if (i > 0)
                                                {
                                                    item.m_Info += " last: N/A (New Visual Element)";
                                                }
                                                else
                                                {
                                                    item.m_Info += " last: N/A";
                                                }
                                                item.m_HighLight = true;
                                            }

                                            item.m_SortIndex = sortIndex;
                                            sortIndex++;

                                            item.m_VE = ve;
                                            data.Add(item);
                                        }
                                    }

                                    for (int j = 0; j < ve.m_Children.Count; ++j)
                                    {
                                        var child = ve.m_Children[j];
                                        foundElements.Enqueue(child);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                int sortIndex = 0;
                foreach (var record in recordLayout)
                {
                    List<LayoutDebuggerVisualElement> foundElements = new List<LayoutDebuggerVisualElement>();
                    GetAllElements(record.m_VE, ref foundElements);

                    for (int i = 0; i < foundElements.Count; i++)
                    {
                        InfoData item = new InfoData();
                        item.m_FrameIndex = record.m_FrameIndex;
                        item.m_PassIndex = record.m_PassIndex;
                        item.m_LayoutLoop = record.m_LayoutLoop;
                        item.m_Info = foundElements[i].m_OriginalVisualElement.GetType().ToString() + " " + foundElements[i].m_OriginalVisualElement.name + " " + foundElements[i].layout.ToString();
                        item.m_SortIndex = sortIndex;

                        bool addItem = true;

                        if (data.Count > 0)
                        {
                            int index = data.Count - 1;
                            if (data[index].m_Info == item.m_Info)
                            {
                                addItem = false;
                            }
                        }

                        if (addItem)
                        {
                            data.Add(item);
                            sortIndex++;
                        }
                    }
                }
            }

            data.Sort((a, b) => (a.m_SortIndex - b.m_SortIndex));

            listView.itemsSource = data;

            listView.columns["Pos"].bindCell = (VisualElement element, int index) => (element as Label).text = data[index].m_SortIndex.ToString();
            listView.columns["FrameIndex"].bindCell = (VisualElement element, int index) => (element as Label).text = data[index].m_FrameIndex.ToString();
            listView.columns["PassIndex"].bindCell = (VisualElement element, int index) => (element as Label).text = data[index].m_PassIndex.ToString();
            listView.columns["LayoutLoop"].bindCell = (VisualElement element, int index) => (element as Label).text = data[index].m_LayoutLoop.ToString();
            listView.columns["VisualElement"].bindCell = (VisualElement element, int index) =>
            {
                (element as Label).text = data[index].m_Info;
                if (data[index].m_HighLight)
                {
                    (element as Label).style.color = Color.cyan;
                }
            };
            listView.Rebuild();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);

            m_LayoutDisplay.RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            m_LayoutDisplay.RegisterCallback<WheelEvent>(OnWheelEvent);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);

            m_LayoutDisplay.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
            m_LayoutDisplay.UnregisterCallback<WheelEvent>(OnWheelEvent);
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
            UpdateDisplay();
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
            UpdateDisplay();
        }

        internal void SetRecord(List<LayoutDebuggerItem> _recordLayout)
        {
            recordLayout = _recordLayout;

            if (recordLayout != null)
            {
                // adjust size with base element
                if (recordLayout.Count > 0)
                {
                    m_LayoutDisplay.style.width = recordLayout[0].m_VE.layout.width;
                    m_LayoutDisplay.style.height = recordLayout[0].m_VE.layout.height;
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

        private void FindLastDrawnItem(LayoutDebuggerVisualElement ve, ref int currentIndex)
        {
            if (ve == m_LastDrawElement)
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
                    FindLastDrawnItem(child, ref currentIndex);
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

                                int currentIndex = 0;
                                DisplayLayoutRecursive(paint2D, ref currentIndex, record.m_VE, 0.5f, 0.5f);

                                break;
                            }
                        }
                    }
                }
            }
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

            // As we are using the Painter2D LineTo and MoveTo functions, we have to reduce the width and height by one pixel to make
            // sure we are drawing the exact number of pixel, a with of 2 would result in a 3 pixels width, which is wrong.
            float w = ve.layout.width - 1.0f;
            float h = ve.layout.height - 1.0f;

            bool selected = false;

            if (currentIndex < (m_MaxItem))
            {
                float alpha = (float)currentIndex / (float)m_MaxItem;

                if (paint2D != null)
                {
                    paint2D.strokeColor = new Color(alpha, 1.0f - alpha, 0.0f);
                }
            }
            else
            {
                if (paint2D != null)
                {
                    paint2D.strokeColor = Color.cyan;
                }
                selected = true;
            }

            if (paint2D != null)
            {
                if (currentIndex <= m_MaxItem)
                {
                    if (w > 0 && h > 0)
                    {
                        bool show = true;

                        if (m_ShowDirty && ve.isDirty == false)
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
