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
    internal class UILayoutDebuggerHistogram : VisualElement
    {
        public UILayoutDebuggerHistogramGraph m_Graph;

        public UILayoutDebuggerHistogram() : base()
        {
            style.borderLeftWidth = 1.0f;
            style.borderTopWidth = 1.0f;
            style.borderBottomWidth = 1.0f;
            style.borderRightWidth = 1.0f;

            style.borderLeftColor = Color.black;
            style.borderTopColor = Color.black;
            style.borderBottomColor = Color.black;
            style.borderRightColor = Color.black;

            style.flexGrow = 1;
            style.flexShrink = 0;

            style.minHeight = 40 * 3;

            style.marginRight = 2;
            style.marginLeft = 2;
            style.marginBottom = 2;
            style.marginTop = 2;

            style.overflow = Overflow.Hidden;

            VisualElement col = new VisualElement();
            col.style.flexDirection = FlexDirection.Column;
            col.style.flexShrink = 0;

            m_Graph = new UILayoutDebuggerHistogramGraph();

            m_Graph.style.flexGrow = 1;

            m_Graph.style.minHeight = 40 * 3;

            m_Graph.style.marginRight = 2;
            m_Graph.style.marginLeft = 2;
            m_Graph.style.marginBottom = 2;
            m_Graph.style.marginTop = 2;

            RadioButton radioButton = new RadioButton();
            radioButton.text = "MaxLayoutLoop";
            radioButton.value = true;
            radioButton.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                if (evt.newValue)
                {
                    m_Graph.mode = UILayoutDebuggerHistogramGraph.GraphMode.MaxLayoutLoop;
                    MarkDirtyRepaint();
                }
            });

            GroupBox groupBox = new GroupBox();
            groupBox.style.flexGrow = 1;
            groupBox.style.flexDirection = FlexDirection.Row;
            groupBox.Add(radioButton);

            radioButton = new RadioButton();
            radioButton.text = "MaxVisualElement";
            radioButton.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                if (evt.newValue)
                {
                    m_Graph.mode = UILayoutDebuggerHistogramGraph.GraphMode.MaxVisualElement;
                    MarkDirtyRepaint();
                }
            });
            groupBox.Add(radioButton);

            radioButton = new RadioButton();
            radioButton.text = "MaxPassIndex";
            radioButton.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                if (evt.newValue)
                {
                    m_Graph.mode = UILayoutDebuggerHistogramGraph.GraphMode.MaxPassIndex;
                    MarkDirtyRepaint();
                }
            });
            groupBox.Add(radioButton);


            col.Add(groupBox);

            col.Add(m_Graph);

            Add(col);
        }
    };

    internal class UILayoutDebuggerHistogramGraph : VisualElement
    {
        public enum GraphMode
        {
            MaxLayoutLoop,
            MaxVisualElement,
            MaxPassIndex,
        };

        private GraphMode m_Mode;

        public GraphMode mode
        {
            get { return m_Mode; }
            set
            {
                m_Mode = value;
                ProcessRecord();
            }
        }

        private class GraphItem
        {
            public GraphItem(int frameIndex = -1,
                             int passIndex = -1,
                             int layoutLoop = -1,
                             int maxItem = -1)
            {
                m_FrameIndex = frameIndex;
                m_PassIndex = passIndex;
                m_LayoutLoop = layoutLoop;
                m_MaxItem = maxItem;
            }

            public int m_FrameIndex;
            public int m_PassIndex;
            public int m_LayoutLoop;
            public int m_MaxItem;
        }


        private List<LayoutDebuggerItem> recordLayout = null;
        private List<GraphItem> maxItemList = null;
        private Vector2 m_HoverPosition = new Vector2(-1, 0);
        private GraphItem selectedItem = new GraphItem();

        public int selectedLayoutDebuggerItem = -1;

        internal void SetRecord(List<LayoutDebuggerItem> _recordLayout)
        {
            recordLayout = _recordLayout;
            ProcessRecord();
            MarkDirtyRepaint();
        }

        public UILayoutDebuggerHistogramGraph() : base()
        {
            focusable = true;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SelectItem()
        {
            selectedLayoutDebuggerItem = -1;
            if ((recordLayout == null) || (recordLayout.Count == 0))
            {
                return;
            }

            for (int i = 0; i < recordLayout.Count; i++)
            {
                if ( (recordLayout[i].m_FrameIndex == selectedItem.m_FrameIndex) &&
                     (recordLayout[i].m_PassIndex == selectedItem.m_PassIndex) &&
                     (recordLayout[i].m_LayoutLoop == selectedItem.m_LayoutLoop) )
                {
                    selectedLayoutDebuggerItem = i;
                    break;
                }
            }
            MarkDirtyRepaint();
        }

        public void DisableHover()
        {
            m_HoverPosition = new Vector2(-1, 0);
            MarkDirtyRepaint();
        }

        public void SelectItemFromIndices(int frameIndex, int passIndex, int layoutLoop)
        {
            selectedLayoutDebuggerItem = -1;
            if ((recordLayout == null) || (recordLayout.Count == 0))
            {
                return;
            }

            for (int i = 0; i < recordLayout.Count; i++)
            {
                if ( (recordLayout[i].m_FrameIndex == frameIndex) &&
                     (recordLayout[i].m_PassIndex == passIndex)  &&
                     (recordLayout[i].m_LayoutLoop == layoutLoop) )
                {
                    selectedLayoutDebuggerItem = i;
                    break;
                }
            }
            MarkDirtyRepaint();
        }


        public int GetSelectedLayoutDebuggerItem()
        {
            return selectedLayoutDebuggerItem;
        }

        private void ProcessRecord()
        {
            if ((recordLayout == null) || (recordLayout.Count == 0))
            {
                return;
            }

            maxItemList = new List<GraphItem>();

            if (m_Mode == GraphMode.MaxLayoutLoop)
            {
                int lastFrameIndex = -1;
                int passIndex = -1;
                int layoutLoop = -1;

                for (int i = 0; i < recordLayout.Count; i++)
                {
                    if (lastFrameIndex == recordLayout[i].m_FrameIndex)
                    {
                        if (recordLayout[i].m_LayoutLoop > layoutLoop)
                        {
                            layoutLoop = recordLayout[i].m_LayoutLoop;
                            passIndex = recordLayout[i].m_PassIndex;
                        }
                    }
                    else
                    {
                        if (lastFrameIndex != -1)
                        {
                            maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, layoutLoop, layoutLoop));
                        }

                        lastFrameIndex = recordLayout[i].m_FrameIndex;
                        layoutLoop = recordLayout[i].m_LayoutLoop;
                    }

                    if (i == recordLayout.Count - 1)
                    {
                        maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, layoutLoop, layoutLoop));
                    }
                }
            }
            else if (m_Mode == GraphMode.MaxVisualElement)
            {
                int lastFrameIndex = -1;
                int passIndex = -1;
                int layoutLoop = -1;
                int maxVE = -1;

                List<int> maxVEList = new List<int>();

                for (int i = 0; i < recordLayout.Count; i++)
                {
                    maxVEList.Add(recordLayout[i].m_VE.CountTotalElement());
                }

                for (int i = 0; i < recordLayout.Count; i++)
                {
                    if (lastFrameIndex == recordLayout[i].m_FrameIndex)
                    {
                        if (maxVEList[i] > maxVE)
                        {
                            maxVE = maxVEList[i];
                            layoutLoop = recordLayout[i].m_LayoutLoop;
                            passIndex = recordLayout[i].m_PassIndex;
                        }
                    }
                    else
                    {
                        if (lastFrameIndex != -1)
                        {
                            maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, layoutLoop, maxVE));
                        }
                        lastFrameIndex = recordLayout[i].m_FrameIndex;
                        passIndex = recordLayout[i].m_PassIndex;
                        layoutLoop = recordLayout[i].m_LayoutLoop;
                        maxVE = maxVEList[i];
                    }

                    if (i == recordLayout.Count - 1)
                    {
                        maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, layoutLoop, maxVE));
                    }
                }
            }
            else if (m_Mode == GraphMode.MaxPassIndex)
            {
                int lastFrameIndex = -1;
                int passIndex = -1;

                for (int i = 0; i < recordLayout.Count; i++)
                {
                    if (lastFrameIndex == recordLayout[i].m_FrameIndex)
                    {
                        if (recordLayout[i].m_PassIndex > passIndex)
                        {
                            passIndex = recordLayout[i].m_PassIndex;
                        }
                    }
                    else
                    {
                        if (lastFrameIndex != -1)
                        {
                            maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, 0, passIndex));
                        }

                        lastFrameIndex = recordLayout[i].m_FrameIndex;
                        passIndex = recordLayout[i].m_PassIndex;
                    }

                    if (i == recordLayout.Count - 1)
                    {
                        maxItemList.Add(new GraphItem(lastFrameIndex, passIndex, 0, passIndex));
                    }
                }
            }

            MarkDirtyRepaint();
        }

        public void OnMouseMove(MouseMoveEvent evt)
        {
            m_HoverPosition = evt.localMousePosition;
            MarkDirtyRepaint();
        }

        public void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mgc.painter2D.fillColor = Color.black;
            float w = resolvedStyle.width;
            float y = 14 * 2;
            float h = resolvedStyle.height - y;

            mgc.painter2D.BeginPath();
            mgc.painter2D.MoveTo(new Vector2(0, 0));
            mgc.painter2D.LineTo(new Vector2(w, 0));
            mgc.painter2D.LineTo(new Vector2(w, y + h));
            mgc.painter2D.LineTo(new Vector2(0, y + h));
            mgc.painter2D.ClosePath();
            mgc.painter2D.Fill();

            if ((maxItemList == null) || (maxItemList.Count == 0))
            {
                return;
            }

            float xStep;

            if (maxItemList.Count >= (int)w)
            {
                xStep = 1.0f;
            }
            else
            {
                xStep = w / (float)maxItemList.Count;
            }

            int totalMaxItem = 0;

            for (int i = 0; i < maxItemList.Count; i++)
            {
                totalMaxItem = Math.Max(totalMaxItem, maxItemList[i].m_MaxItem);
            }

            if (m_Mode == GraphMode.MaxLayoutLoop)
            {
                totalMaxItem = Math.Max(totalMaxItem, UIRLayoutUpdater.kMaxValidateLayoutCount + 1);
            }

            int colorIndex = 0;

            int hoverIndex = -1;
            int alreadySelectedIndex = -1;

            float x = 0;

            for (int i = 0; i < maxItemList.Count; i++)
            {
                bool maxSelected = false;
                if (selectedLayoutDebuggerItem != -1)
                {
                    if ((maxItemList[i].m_FrameIndex == recordLayout[selectedLayoutDebuggerItem].m_FrameIndex) &&
                        (maxItemList[i].m_PassIndex == recordLayout[selectedLayoutDebuggerItem].m_PassIndex) &&
                        (maxItemList[i].m_LayoutLoop == recordLayout[selectedLayoutDebuggerItem].m_LayoutLoop))
                    {
                        alreadySelectedIndex = i;
                        maxSelected = true;
                    }
                    else  if (maxItemList[i].m_FrameIndex == recordLayout[selectedLayoutDebuggerItem].m_FrameIndex)
                    {
                        if (alreadySelectedIndex == -1)
                        {
                            alreadySelectedIndex = i;
                        }
                    }
                }

                Color color = (colorIndex % 2) == 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.75f, 0.75f, 0.75f);

                colorIndex++;

                if (m_HoverPosition.x >= 0)
                {
                    if ((x < m_HoverPosition.x) && (m_HoverPosition.x < (x + xStep)))
                    {
                        color = Color.cyan;
                        hoverIndex = i;
                    }
                }

                if (alreadySelectedIndex == i)
                {
                    color = maxSelected ? Color.green : new Color(0.0f, 0.5f, 0.0f);
                }


                if (totalMaxItem == 0)
                {
                    totalMaxItem = 1;
                }

                mgc.painter2D.fillColor = color;
                mgc.painter2D.BeginPath();
                mgc.painter2D.MoveTo(new Vector2(x, y + h));
                mgc.painter2D.LineTo(new Vector2(x + xStep, y + h));

                float barHeight = h * maxItemList[i].m_MaxItem / totalMaxItem;

                mgc.painter2D.LineTo(new Vector2(x + xStep, y + h - barHeight));
                mgc.painter2D.LineTo(new Vector2(x, y + h - barHeight));
                mgc.painter2D.ClosePath();
                mgc.painter2D.Fill();

                x += xStep;
            }


            if (m_Mode == GraphMode.MaxLayoutLoop)
            {
                mgc.painter2D.strokeColor = Color.red;
                mgc.painter2D.BeginPath();
                mgc.painter2D.MoveTo(new Vector2(0, y + h - h * UIRLayoutUpdater.kMaxValidateLayoutCount / totalMaxItem));
                mgc.painter2D.LineTo(new Vector2(w, y + h - h * UIRLayoutUpdater.kMaxValidateLayoutCount / totalMaxItem));
                mgc.painter2D.Stroke();
            }

            if (hoverIndex >= 0)
            {
                int frameIndex = maxItemList[hoverIndex].m_FrameIndex;
                int passIndex = maxItemList[hoverIndex].m_PassIndex;
                int layoutLoop = maxItemList[hoverIndex].m_LayoutLoop;
                int maxItem = maxItemList[hoverIndex].m_MaxItem;

                selectedItem = new GraphItem(frameIndex, passIndex, layoutLoop);

                mgc.DrawText("FrameIndex: " + frameIndex +
                    " PassIndex: " + passIndex +
                    " LayoutLoop: " + layoutLoop +
                    " " + m_Mode.ToString() + " " + maxItem , new Vector2(0, 14), 12, Color.cyan);
            }
            else
            {
                selectedItem = new GraphItem();
            }

            if (alreadySelectedIndex >= 0)
            {
                mgc.DrawText("FrameIndex: " + maxItemList[alreadySelectedIndex].m_FrameIndex +
                    " PassIndex: " + maxItemList[alreadySelectedIndex].m_PassIndex +
                    " LayoutLoop: " + maxItemList[alreadySelectedIndex].m_LayoutLoop +
                    " " + m_Mode.ToString() + " " + maxItemList[alreadySelectedIndex].m_MaxItem,
                    new Vector2(0, 0), 12, Color.green);
            }
        }
    }
}
