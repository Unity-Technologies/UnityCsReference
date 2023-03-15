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
    class LayoutPanelDebuggerImpl : PanelDebugger
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/UILayoutDebugger/UILayoutDebugger.uss";

        private List<LayoutDebuggerItem> m_RecordLayout = null;

        int m_FrameIndex = 0;
        int m_PassIndex = 0;
        int m_LayoutLoop = 0;

        int m_MinFrameIndex = 0;
        int m_MaxFrameIndex = 0;

        Dictionary<int, int> m_MinMaxPassIndex = new Dictionary<int, int>();
        Dictionary<Tuple<int, int>, int> m_MinMaxLayoutLoop = new Dictionary<Tuple<int, int>, int>();

        int m_MinLayoutLoop = 0;
        int m_MaxLayoutLoop = 0;

        ToolbarToggle m_RecordLayoutToggle;
        Toggle m_FrameResetPassIndexLayoutLoop;
        Toggle m_PassResetLayoutLoop;
        Toggle m_LayoutLoopAllowFrameIndexPassIndexUpdate;
        VisualElement m_Interface;
        Label m_Label = null;
        UILayoutDebugger m_Display = null;
        Slider m_Slider = null;

        static UIRLayoutUpdater GetLayoutUpdater(IPanel panel)
        {
            return (panel as BaseVisualElementPanel)?.GetUpdater(VisualTreeUpdatePhase.Layout) as UIRLayoutUpdater;
        }

        internal void SetRecord(List<LayoutDebuggerItem> _recordLayout)
        {
            m_RecordLayout = _recordLayout;

            m_MinFrameIndex = Int32.MaxValue;
            m_MaxFrameIndex = -1;

            m_MinMaxLayoutLoop.Clear();
            m_MinMaxPassIndex.Clear();

            foreach (var record in m_RecordLayout)
            {
                m_MinFrameIndex = Math.Min(m_MinFrameIndex, record.m_FrameIndex);
                m_MaxFrameIndex = Math.Max(m_MaxFrameIndex, record.m_FrameIndex);

                if (m_MinMaxPassIndex.ContainsKey(record.m_FrameIndex))
                {
                    m_MinMaxPassIndex[record.m_FrameIndex] = Math.Max(m_MinMaxPassIndex[record.m_FrameIndex], record.m_PassIndex);
                }
                else
                {
                    m_MinMaxPassIndex.Add(record.m_FrameIndex, record.m_PassIndex);
                }

                Tuple<int, int> t = new Tuple<int, int>(record.m_FrameIndex, record.m_PassIndex);

                if (m_MinMaxLayoutLoop.ContainsKey(t))
                {
                    m_MinMaxLayoutLoop[t] = Math.Max(m_MinMaxLayoutLoop[t], record.m_LayoutLoop);
                }
                else
                {
                    m_MinMaxLayoutLoop.Add(t, 0);
                }
            }

            m_Display.SetRecord(_recordLayout);
        }
        public void UpdateSlider(int value)
        {
            m_Slider.value = value;
        }

        public void UpdateLabel()
        {
            if (m_RecordLayout == null)
            {
                return;
            }

            int maxItem = 0;

            if (m_RecordLayout.Count == 0)
            {
                m_FrameIndex = 0;
                m_PassIndex = 0;
                m_MaxLayoutLoop = 0;
                return;
            }
            else
            {
                m_FrameIndex = Math.Clamp(m_FrameIndex, m_MinFrameIndex, m_MaxFrameIndex);
                m_PassIndex = Math.Clamp(m_PassIndex, 0, m_MinMaxPassIndex[m_FrameIndex]);
                m_MaxLayoutLoop = Math.Clamp(m_LayoutLoop, 0, m_MinMaxLayoutLoop[new Tuple<int, int>(m_FrameIndex, m_PassIndex)]);

                int maxZero = 0;
                int outOfRoot = 0;

                m_LayoutLoop = Math.Clamp(m_LayoutLoop, m_MinLayoutLoop, m_MaxLayoutLoop);

                foreach (var record in m_RecordLayout)
                {
                    if (record.m_FrameIndex == m_FrameIndex)
                    {
                        if (record.m_PassIndex == m_PassIndex)
                        {
                            if (record.m_LayoutLoop == m_LayoutLoop)
                            {
                                Rect rect = new Rect();
                                rect.x = record.m_VE.layout.x;
                                rect.y = record.m_VE.layout.y;
                                rect.width = record.m_VE.layout.width;
                                rect.height = record.m_VE.layout.height;

                                UILayoutDebugger.CountLayoutItem(rect, record.m_VE, ref maxItem, ref maxZero, ref outOfRoot);
                                break;
                            }
                        }
                    }
                }
            }

            m_Label.text = "Informations:" +
                " FrameIndex(" + m_MinFrameIndex + ", " + m_MaxFrameIndex + "):" + m_FrameIndex +
                " PassIndex(" + 0 + ", " + m_MinMaxPassIndex[m_FrameIndex] + "):" + m_PassIndex +
                " LayoutLoop(" + 0 + ", " + m_MinMaxLayoutLoop[new Tuple<int, int>(m_FrameIndex, m_PassIndex)] + "):" + m_LayoutLoop;

            m_Slider.lowValue = 0;
            m_Slider.highValue = maxItem-1;
            m_Slider.value = maxItem-1;
            m_Slider.MarkDirtyRepaint();
        }

        private static bool IsEqual(LayoutDebuggerVisualElement a, LayoutDebuggerVisualElement b)
        {
            if (a.name != b.name)
            {
                return false;
            }

            if (a.layout != b.layout)
            {
                return false;
            }

            if (a.visible != b.visible)
            {
                return false;
            }

            if (a.enabledInHierarchy != b.enabledInHierarchy)
            {
                return false;
            }

            if ((a.m_Children == null) != (b.m_Children == null))
            {
                return false;
            }

            if ((a.m_Children != null) && (b.m_Children != null))
            {
                if (a.m_Children.Count != b.m_Children.Count)
                {
                    return false;
                }

                for (int i = 0; i < a.m_Children.Count; i++)
                {
                    if (!IsEqual(a.m_Children[i], b.m_Children[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private List<LayoutDebuggerItem> RemoveDuplicate()
        {
            if (m_RecordLayout.Count == 0)
            {
                return m_RecordLayout;
            }
            else
            {
                int currentFrame = 0;

                List<LayoutDebuggerItem> list = new List<LayoutDebuggerItem>();

                LayoutDebuggerItem a = m_RecordLayout[0];

                list.Add(new LayoutDebuggerItem(currentFrame, a.m_PassIndex, a.m_LayoutLoop, a.m_VE));

                for (int i = 1; i < m_RecordLayout.Count; i++)
                {
                    LayoutDebuggerItem b = m_RecordLayout[i];

                    if (!IsEqual(a.m_VE,b.m_VE))
                    {
                        if (a.m_FrameIndex != b.m_FrameIndex)
                        {
                            currentFrame++;
                        }

                        list.Add(new LayoutDebuggerItem(currentFrame, b.m_PassIndex, b.m_LayoutLoop, b.m_VE));
                        a = b;
                    }
                }

                return list;
            }
        }

        private void FindNextComplexUpdateLoop()
        {
            int nextFrameIndex = m_FrameIndex + 1;

            for (int i = 0; i < m_RecordLayout.Count; i++)
            {
                if (m_RecordLayout[i].m_FrameIndex == nextFrameIndex)
                {
                    for (; i < m_RecordLayout.Count; i++)
                    {
                        int j = i + 1;

                        int bestFrameIndex = -1;

                        for (; j < m_RecordLayout.Count; j++)
                        {
                            if (m_RecordLayout[i].m_FrameIndex == m_RecordLayout[j].m_FrameIndex)
                            {
                                if (m_RecordLayout[i].m_PassIndex == m_RecordLayout[j].m_PassIndex)
                                {
                                    if (m_RecordLayout[i].m_LayoutLoop == 0 && m_RecordLayout[j].m_LayoutLoop > 1)
                                    {
                                        bestFrameIndex = m_RecordLayout[i].m_FrameIndex;
                                    }
                                }
                            }
                        }

                        if (bestFrameIndex != -1)
                        {
                            m_FrameIndex = bestFrameIndex;
                            return;
                        }
                    }

                    break;
                }
            }
        }

        public void Initialize(EditorWindow debuggerWindow, VisualElement root)
        {
            base.Initialize(debuggerWindow);
            var sheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            root.styleSheets.Add(sheet);
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 0;
            root.style.flexShrink = 0;


            m_RecordLayoutToggle = new ToolbarToggle() { name = "recordLayoutToggle" };
            m_RecordLayoutToggle.text = "Record Layout Updates";
            m_RecordLayoutToggle.RegisterValueChangedCallback((e) =>
            {
                if (selectedPanel != null)
                {
                    var layoutUpdater = GetLayoutUpdater(selectedPanel.panel);
                    layoutUpdater.recordLayout = e.newValue;

                    if (e.newValue == false)
                    {
                        UnityEditor.EditorApplication.update -= UIRLayoutUpdater.IncrementMainLoopCount;

                        m_RecordLayoutToggle.text = "Record Layout Updates (" + layoutUpdater.recordLayoutCount + ")";
                        m_Interface.SetEnabled(true);
                        SetRecord(layoutUpdater.GetListOfRecord());
                        UpdateLabel();
                        m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                        m_Display.SetMaxItem((int)m_Slider.value);
                    }
                    else
                    {
                        UnityEditor.EditorApplication.update += UIRLayoutUpdater.IncrementMainLoopCount;
                    }
                }
            });
            m_Toolbar.Add(m_RecordLayoutToggle);

            root.Add(m_Toolbar);

            VisualElement row = createNewRow();

            m_Label = new UnityEngine.UIElements.Label();
            row.Add(m_Label);
            root.Add(row);

            m_Display = new UILayoutDebugger();
            m_Display.m_ParentWindow = this;
            m_Display.style.flexDirection = FlexDirection.Column;
            m_Display.style.flexGrow = 0;
            m_Display.style.flexShrink = 0;

            row = createNewRow();

            Button firstFrameIndex = new Button();
            firstFrameIndex.text = "Goto first Frame Index";
            firstFrameIndex.clicked += () =>
            {
                m_FrameIndex = 0;
                m_PassIndex = 0;
                m_LayoutLoop = 0;
                UpdateLabel();
                m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
            };
            row.Add(firstFrameIndex);

            Button gotoNextComplexUpdateLoop = new Button();
            gotoNextComplexUpdateLoop.text = "Goto next complex update";
            gotoNextComplexUpdateLoop.clicked += () =>
            {
                FindNextComplexUpdateLoop();
                UpdateLabel();
                m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
            };
            row.Add(gotoNextComplexUpdateLoop);
            root.Add(row);

            row = createNewRow();

            // Use minimum height to simulate grid layout.

            const float minHeight = 20;
            VisualElement column = createNewColumn();

            {
                Button frameIndexDown = new Button();
                frameIndexDown.style.minHeight = minHeight;
                frameIndexDown.text = "Frame Index--";
                frameIndexDown.clicked += () =>
                {
                    m_FrameIndex--;
                    UpdateLabel();
                    m_LayoutLoop = m_MaxLayoutLoop;
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(frameIndexDown);

                Button passIndexLoopDown = new Button();
                passIndexLoopDown.style.minHeight = minHeight;
                passIndexLoopDown.text = "Pass Index--";
                passIndexLoopDown.clicked += () =>
                {
                    m_PassIndex--;
                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(passIndexLoopDown);

                Button layoutLoopDown = new Button();
                layoutLoopDown.style.minHeight = minHeight;
                layoutLoopDown.text = "Layout Loop--";
                layoutLoopDown.clicked += () =>
                {
                    m_LayoutLoop--;

                    if (m_LayoutLoopAllowFrameIndexPassIndexUpdate.value)
                    {
                        if (m_LayoutLoop < 0)
                        {
                            if (m_PassIndex == 0)
                            {
                                if (m_FrameIndex > m_MinFrameIndex)
                                {
                                    m_FrameIndex--;
                                    m_FrameIndex = Math.Clamp(m_FrameIndex, m_MinFrameIndex, m_MaxFrameIndex);
                                    m_PassIndex = m_MinMaxPassIndex[m_FrameIndex];
                                    m_LayoutLoop = m_MinMaxLayoutLoop[new Tuple<int, int>(m_FrameIndex, m_PassIndex)];
                                }
                                else
                                {
                                    m_PassIndex = 0;
                                    m_LayoutLoop = 0;
                                }
                            }
                            else
                            {
                                m_PassIndex--;
                                m_LayoutLoop = m_MinMaxLayoutLoop[new Tuple<int, int>(m_FrameIndex, m_PassIndex)];
                            }
                        }
                    }

                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(layoutLoopDown);
            }
            row.Add(column);

            column = createNewColumn();
            {
                Button frameIndexUp = new Button();
                frameIndexUp.style.minHeight = minHeight;
                frameIndexUp.text = "Frame Index++";
                frameIndexUp.clicked += () =>
                {
                    m_FrameIndex++;
                    UpdateLabel();
                    m_LayoutLoop = m_MaxLayoutLoop;
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(frameIndexUp);

                Button passIndexUp = new Button();
                passIndexUp.style.minHeight = minHeight;
                passIndexUp.text = "Pass Index++";
                passIndexUp.clicked += () =>
                {
                    m_PassIndex++;
                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(passIndexUp);

                Button layoutLoopUp = new Button();
                layoutLoopUp.style.minHeight = minHeight;
                layoutLoopUp.text = "Layout Loop++";
                layoutLoopUp.clicked += () =>
                {
                    m_LayoutLoop++;
                    if (m_LayoutLoopAllowFrameIndexPassIndexUpdate.value)
                    {
                        if (m_LayoutLoop > m_MinMaxLayoutLoop[new Tuple<int, int>(m_FrameIndex, m_PassIndex)])
                        {
                            if (m_PassIndex < m_MinMaxPassIndex[m_FrameIndex])
                            {
                                m_PassIndex++;
                                m_LayoutLoop = 0;
                            }
                            else
                            {
                                if (m_FrameIndex < m_MaxFrameIndex)
                                {
                                    m_FrameIndex ++;
                                    m_PassIndex = 0;
                                    m_LayoutLoop = 0;
                                }
                            }
                        }
                    }

                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                };
                column.Add(layoutLoopUp);
            }
            row.Add(column);

            column = createNewColumn();
            {
                m_FrameResetPassIndexLayoutLoop = new Toggle();
                m_FrameResetPassIndexLayoutLoop.style.minHeight = minHeight;
                m_FrameResetPassIndexLayoutLoop.text = "Reset Pass and Layout on change";
                m_FrameResetPassIndexLayoutLoop.SetEnabled(false);
                column.Add(m_FrameResetPassIndexLayoutLoop);

                m_PassResetLayoutLoop = new Toggle();
                m_PassResetLayoutLoop.style.minHeight = minHeight;
                m_PassResetLayoutLoop.text = "Reset Layout on change";
                m_PassResetLayoutLoop.SetEnabled(false);
                column.Add(m_PassResetLayoutLoop);

                m_LayoutLoopAllowFrameIndexPassIndexUpdate = new Toggle();
                m_LayoutLoopAllowFrameIndexPassIndexUpdate.style.minHeight = minHeight;
                m_LayoutLoopAllowFrameIndexPassIndexUpdate.text = "Update FrameIndex and PassIndex on underflow/overflow";
                column.Add(m_LayoutLoopAllowFrameIndexPassIndexUpdate);

            }

            row.Add(column);
            root.Add(row);

            row = createNewRow();

            m_Slider = new Slider("Last VE");
            m_Slider.lowValue = 0;
            m_Slider.highValue = 0;
            m_Slider.showInputField = true;
            m_Slider.pageSize = 1.0f;
            m_Slider.style.flexGrow = 1;
            m_Slider.RegisterValueChangedCallback(v =>
            {
                m_Display.SetMaxItem((int)v.newValue);
            });

            m_Interface = createNewColumn();
            m_Interface.Add(row);

            row = createNewRow();

            ToolbarToggle removeDuplicates = new ToolbarToggle();
            removeDuplicates.text = "Remove Duplicates";
            removeDuplicates.RegisterValueChangedCallback((e) =>
            {
                if (e.newValue)
                {
                    SetRecord(RemoveDuplicate());
                    m_Display.SetRecord(m_RecordLayout);
                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                    m_Display.SetMaxItem((int)m_Slider.value);
                }
                else
                {
                    SetRecord(m_RecordLayout);
                    UpdateLabel();
                    m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
                    m_Display.SetMaxItem((int)m_Slider.value);
                }

            });
            row.Add(removeDuplicates);

            ToolbarToggle enableLayoutComparison = new ToolbarToggle();
            enableLayoutComparison.text = "Enable layout comparison";
            enableLayoutComparison.RegisterValueChangedCallback((e) =>
            {
                m_Display.EnableLayoutComparison(e.newValue);
            });
            row.Add(enableLayoutComparison);

            row.Add(m_Slider);

            UpdateLabel();
            m_Display.SetIndices(m_FrameIndex, m_PassIndex, m_LayoutLoop);
            m_Display.SetMaxItem((int)m_Slider.value);

            m_Interface.Add(row);

            row = createNewRow();

            Toggle toggle = new Toggle();
            toggle.text = "Only show YogaNodeDirty=true";
            toggle.value = false;
            toggle.RegisterValueChangedCallback((e) =>
            {
                m_Display.SetShowYogaNodeDirty(e.newValue);
            });

            row.Add(toggle);
            m_Interface.Add(row);

            m_Interface.SetEnabled(false);

            root.Add(m_Interface);
            root.Add(m_Display);
        }

        private VisualElement createNewRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 0;
            row.style.flexShrink = 0;
            return row;
        }

        private VisualElement createNewColumn()
        {
            VisualElement column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.flexGrow = 0;
            column.style.flexShrink = 0;
            return column;
        }

    }
}
