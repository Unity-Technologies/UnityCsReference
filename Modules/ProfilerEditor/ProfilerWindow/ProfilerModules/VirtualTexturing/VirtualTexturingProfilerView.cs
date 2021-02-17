// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Profiling;
using UnityEngine.UIElements;
using System.Text;

namespace UnityEditor
{
    [Serializable]
    internal class VirtualTexturingProfilerView
    {
        Vector2 m_ListScroll, m_SystemScroll, m_PlayerScroll;
        static readonly Guid m_VTProfilerGuid = new Guid("AC871613E8884327B4DD29D7469548DC");

        MultiColumnHeaderState m_HeaderState;
        VTMultiColumnHeader m_Header;

        struct FrameData
        {
            public GraphicsFormat format;
            public int demand;
            public float bias;
            public int size;
        }

        List<FrameData> m_FrameData = new List<FrameData>();
        bool m_SortingChanged = false;
        [SerializeField]
        bool m_SortAscending = false;
        [SerializeField]
        int m_SortedColumn = -1;

        void Init()
        {
            int baseWidth = 270;
            m_HeaderState = new MultiColumnHeaderState(new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TrTextContent("  Cache Format"), width = baseWidth, autoResize = false},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TrTextContent("Demand"), width = baseWidth, autoResize = false},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TrTextContent("Bias"), width = baseWidth, autoResize = false},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TrTextContent("Size"), width = baseWidth, autoResize = false}
            });

            foreach (var column in m_HeaderState.columns)
            {
                column.sortingArrowAlignment = TextAlignment.Center;
                column.headerTextAlignment = TextAlignment.Left;
                column.allowToggleVisibility = false;
                column.sortedAscending = true;
            }

            m_Header = new VTMultiColumnHeader(m_HeaderState);
            m_Header.height = 34;
            m_Header.ResizeToFit();

            m_Header.sortingChanged += header => { m_SortingChanged = true; m_SortedColumn = header.sortedColumnIndex; };

            //Used to preserve sorting after domain reloads
            if (m_SortedColumn != -1)
                m_Header.SetSorting(m_SortedColumn, m_SortAscending);
        }

        internal void DrawUIPane(IProfilerWindowController win)
        {
            if (Styles.backgroundStyle == null)
                Styles.CreateStyles();
            //make sure all background textures exist
            Styles.CheckBackgroundTextures();

            EditorGUILayout.BeginVertical();
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            const int padding = 1;

            Rect aggregateHeader = rect;
            aggregateHeader.width = Mathf.Min(434, rect.width);
            aggregateHeader.height = 34;

            Rect aggregateData = aggregateHeader;
            aggregateData.y += aggregateHeader.height + padding;
            float aggregateBoxHeight = rect.height / 2 - aggregateHeader.height - padding * 2;
            aggregateData.height = aggregateBoxHeight;

            Rect infoBox = new Rect();

            if (ProfilerDriver.GetStatisticsAvailabilityState(ProfilerArea.VirtualTexturing, win.GetActiveVisibleFrameIndex()) == 0)
            {
                GUI.Label(aggregateHeader, "No Virtual Texturing data was collected.");
                EditorGUILayout.EndVertical();
                return;
            }

            GUI.Box(rect, "", Styles.backgroundStyle);

            using (RawFrameDataView frameDataView = ProfilerDriver.GetRawFrameDataView(win.GetActiveVisibleFrameIndex(), 0))
            {
                if (frameDataView.valid)
                {
                    Assert.IsTrue(frameDataView.threadName == "Main Thread");

                    RawFrameDataView fRender = GetRenderThread(frameDataView);

                    //system statistics
                    var stringBuilder = new StringBuilder(1024);
                    int requiredTiles = (int)GetCounterValue(frameDataView, "Required Tiles");
                    stringBuilder.AppendLine($" Tiles required this frame: {requiredTiles}");
                    stringBuilder.AppendLine($" Max Cache Mip Bias: {GetCounterValueAsFloat(frameDataView, "Max Cache Mip Bias")}");
                    stringBuilder.AppendLine($" Max Cache Demand: {GetCounterValue(frameDataView, "Max Cache Demand")}%");
                    stringBuilder.AppendLine($" Total CPU Cache Size: {EditorUtility.FormatBytes(GetCounterValue(frameDataView, "Total CPU Cache Size"))}");
                    stringBuilder.AppendLine($" Total GPU Cache Size: {EditorUtility.FormatBytes(GetCounterValue(frameDataView, "Total GPU Cache Size"))}");
                    stringBuilder.AppendLine($" Atlases: {GetCounterValue(frameDataView, "Atlases")}");

                    string aggregatedText = stringBuilder.ToString();
                    float aggregateTextHeight = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(aggregatedText), rect.width);
                    float actualHeight = Mathf.Max(aggregateBoxHeight, aggregateTextHeight);

                    DrawScrollBackground(new Rect(aggregateData.width - 12, aggregateData.y, 14, aggregateData.height));
                    GUI.Box(aggregateHeader, " System Statistics", Styles.headerStyle);
                    m_SystemScroll = GUI.BeginScrollView(aggregateData, m_SystemScroll, new Rect(0, 0, aggregateData.width / 2, actualHeight));
                    GUI.Box(new Rect(0, 0, aggregateData.width, actualHeight), aggregatedText, Styles.statStyle);
                    GUI.EndScrollView();

                    //player build statistics
                    aggregateHeader.y += aggregateHeader.height + aggregateData.height + padding * 2;
                    aggregateData.y += aggregateHeader.height + aggregateData.height + padding * 2;
                    stringBuilder.Clear();
                    stringBuilder.AppendLine($" Missing Disk Data: {EditorUtility.FormatBytes(GetCounterValue(frameDataView, "Missing Disk Data"))}");
                    stringBuilder.AppendLine($" Missing Streaming Tiles: {GetCounterValue(frameDataView, "Missing Streaming Tiles")}");
                    stringBuilder.AppendLine($" Read From Disk: {EditorUtility.FormatBytes(GetCounterValue(frameDataView, "Read From Disk"))}");

                    aggregatedText = stringBuilder.ToString();
                    aggregateTextHeight = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(aggregatedText), rect.width);
                    actualHeight = Mathf.Max(aggregateBoxHeight, aggregateTextHeight);

                    DrawScrollBackground(new Rect(aggregateData.width - 12, aggregateData.y, 14, aggregateData.height));
                    GUI.Box(aggregateHeader, " Player Build Statistics", Styles.headerStyle);
                    m_PlayerScroll = GUI.BeginScrollView(aggregateData, m_PlayerScroll, new Rect(0, 0, aggregateData.width / 2, actualHeight));
                    GUI.Box(new Rect(0, 0, aggregateData.width, actualHeight), aggregatedText, Styles.statStyle);
                    GUI.EndScrollView();

                    //PER CACHE DATA
                    Rect perCacheHeader = rect;
                    perCacheHeader.width = rect.width - aggregateHeader.width - padding;
                    perCacheHeader.height = 34;
                    perCacheHeader.x += aggregateHeader.width + padding;

                    Rect formatBox = perCacheHeader;
                    formatBox.height = rect.height - perCacheHeader.height - padding;
                    formatBox.y += perCacheHeader.height + padding;

                    GUI.Box(perCacheHeader, "  Per Cache Statistics", Styles.headerStyle);

                    if (m_Header == null || m_HeaderState == null)
                        Init();

                    //Keep using the last VT tick untill a new one is available.
                    if (requiredTiles > 0)
                    {
                        ReadFrameData(fRender);
                    }

                    if (m_SortingChanged)
                        SortFrameData();
                    DrawFormatData(formatBox);

                    if (fRender != frameDataView)
                    {
                        fRender.Dispose();
                    }
                }
                else
                {
                    GUI.Label(aggregateData, "No frame data available");
                }
            }
            EditorGUILayout.EndVertical();

            infoBox.y = rect.y;
            infoBox.x = rect.width - 33;
            infoBox.width = 34;
            infoBox.height = 34;

            DrawDocLink(infoBox);
        }

        internal void DrawDocLink(Rect infoBox)
        {
            if (GUI.Button(infoBox, Styles.helpButtonContent, Styles.headerStyle))
            {
                Application.OpenURL(Styles.linkToDocs);
            }
        }

        internal void ReadFrameData(RawFrameDataView fRender)
        {
            var frameData = fRender.GetFrameMetaData<int>(m_VTProfilerGuid, 0);
            //This function only gets called when there are tiles on the screen so if we received no cache data this frame we use the old data
            if (frameData.Length == 0)
            {
                return;
            }

            m_FrameData.Clear();

            for (int i = 0; i < frameData.Length; i += 4)
            {
                FrameData data = new FrameData();
                data.format = (GraphicsFormat)frameData[i];
                data.demand = frameData[i + 1];
                data.bias = frameData[i + 2] / 100.0f;
                data.size = frameData[i + 3];

                m_FrameData.Add(data);
            }

            SortFrameData();
        }

        internal void DrawScrollBackground(Rect rect)
        {
            GUI.Box(rect, "", Styles.evenStyle);
        }

        internal RawFrameDataView GetRenderThread(RawFrameDataView frameDataView)
        {
            //Find render thread
            using (ProfilerFrameDataIterator frameDataIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameDataIterator.GetThreadCount(frameDataView.frameIndex);
                for (int i = 0; i < threadCount; ++i)
                {
                    RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameDataView.frameIndex, i);
                    if (frameData.threadName == "Render Thread")
                    {
                        return frameData;
                    }
                    else
                    {
                        frameData.Dispose();
                    }
                }
                // If there's no render thread, metadata was pushed on main thread
                return frameDataView;
            }
        }

        int SortFormats(FrameData a, FrameData b)
        {
            if (m_SortAscending)
                return b.format.CompareTo(a.format);
            else return a.format.CompareTo(b.format);
        }

        int SortDemand(FrameData a, FrameData b)
        {
            if (m_SortAscending)
                return b.demand.CompareTo(a.demand);
            else return a.demand.CompareTo(b.demand);
        }

        int SortBias(FrameData a, FrameData b)
        {
            if (m_SortAscending)
                return b.bias.CompareTo(a.bias);
            else return a.bias.CompareTo(b.bias);
        }

        int SortSize(FrameData a, FrameData b)
        {
            if (m_SortAscending)
                return b.size.CompareTo(a.size);
            else return a.size.CompareTo(b.size);
        }

        internal void SortFrameData()
        {
            int sortedColumn = m_Header.sortedColumnIndex;
            if (sortedColumn == -1)
                return;

            m_SortAscending = m_Header.IsSortedAscending(sortedColumn);

            switch (sortedColumn)
            {
                case 0:
                    m_FrameData.Sort(SortFormats);
                    break;
                case 1:
                    m_FrameData.Sort(SortDemand);
                    break;
                case 2:
                    m_FrameData.Sort(SortBias);
                    break;
                case 3:
                    m_FrameData.Sort(SortSize);
                    break;
            }
            m_SortingChanged = false;
        }

        internal void DrawFormatData(Rect formatRect)
        {
            const int itemHeight = 34;

            Rect headerBox = formatRect;
            headerBox.height = itemHeight;

            Rect listBox = formatRect;
            listBox.y += headerBox.height + 1;
            listBox.height -= headerBox.height;

            m_Header.OnGUI(headerBox, 0);

            DrawScrollBackground(new Rect(listBox.x + listBox.width - 12, listBox.y, 14, listBox.height));
            m_ListScroll = GUI.BeginScrollView(listBox, m_ListScroll, new Rect(0, 0, listBox.width / 2.0f, Mathf.Max(itemHeight * m_FrameData.Count, listBox.height)), false, false);
            Rect formatColumn = m_Header.GetColumnRect(0);
            Rect demandColumn = m_Header.GetColumnRect(1);
            Rect biasColumn = m_Header.GetColumnRect(2);
            Rect sizeColumn = m_Header.GetColumnRect(3);

            GUIStyle dataStyle;

            for (int i = 0; i < m_FrameData.Count; ++i)
            {
                if (i % 2 == 0)
                {
                    dataStyle = Styles.evenStyle;
                }
                else
                {
                    dataStyle = Styles.oddStyle;
                }

                //add one extra pixel to fix rounding issue with monitor scaling
                GUI.Box(new Rect(formatColumn.x, i * itemHeight, formatColumn.width + 1, itemHeight), "  " + m_FrameData[i].format.ToString(), dataStyle);
                GUI.Box(new Rect(demandColumn.x, i * itemHeight, demandColumn.width + 1, itemHeight), ' ' + m_FrameData[i].demand.ToString() + '%', dataStyle);
                GUI.Box(new Rect(biasColumn.x, i * itemHeight, biasColumn.width + 1, itemHeight), ' ' + m_FrameData[i].bias.ToString(), dataStyle);
                GUI.Box(new Rect(sizeColumn.x, i * itemHeight, sizeColumn.width + 1, itemHeight), ' ' + EditorUtility.FormatBytes(m_FrameData[i].size), dataStyle);
                GUI.Box(new Rect(sizeColumn.x + sizeColumn.width, i * itemHeight, formatRect.width - sizeColumn.x - sizeColumn.width, itemHeight), "", dataStyle);
            }

            //add empty boxes to fill screen if needed;
            int minElements = (int)listBox.height / itemHeight + 1;

            for (int i = m_FrameData.Count; i < minElements; ++i)
            {
                if (i % 2 == 0)
                {
                    dataStyle = Styles.evenStyle;
                }
                else
                {
                    dataStyle = Styles.oddStyle;
                }

                //add one extra pixel to fix rounding issue with monitor scaling
                GUI.Box(new Rect(formatColumn.x, i * itemHeight, formatColumn.width + 1, itemHeight), "", dataStyle);
                GUI.Box(new Rect(demandColumn.x, i * itemHeight, demandColumn.width + 1, itemHeight), "", dataStyle);
                GUI.Box(new Rect(biasColumn.x, i * itemHeight, biasColumn.width + 1, itemHeight), "", dataStyle);
                GUI.Box(new Rect(sizeColumn.x, i * itemHeight, sizeColumn.width + 1, itemHeight), "", dataStyle);
                GUI.Box(new Rect(sizeColumn.x + sizeColumn.width, i * itemHeight, formatRect.width - sizeColumn.x - sizeColumn.width, itemHeight), "", dataStyle);
            }

            GUI.EndScrollView();

            Rect emptyFiller = formatRect;
            emptyFiller.x = m_Header.GetColumnRect(3).x + m_Header.GetColumnRect(3).width + formatRect.x;
            emptyFiller.width = formatRect.width;
            emptyFiller.height = itemHeight;
            GUI.Box(emptyFiller, "", Styles.headerStyle);
        }

        static long GetCounterValue(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return -1;

            return frameData.GetCounterValueAsInt(id);
        }

        static float GetCounterValueAsFloat(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return -1;

            return frameData.GetCounterValueAsFloat(id);
        }

        internal static class Styles
        {
            enum backgroundColor
            {
                header, even, odd, background, COLORCOUNT
            }

            static Color[] colors = new Color[(int)backgroundColor.COLORCOUNT];
            static Texture2D[] backgrounds = new Texture2D[(int)backgroundColor.COLORCOUNT];
            static Color textColor;

            public static GUIStyle headerStyle;
            public static GUIStyle statStyle;
            public static GUIStyle backgroundStyle;
            public static GUIStyle evenStyle;
            public static GUIStyle oddStyle;
            public static readonly GUIStyle scrollStyle = "ProfilerScrollviewBackground";

            public const string linkToDocs = "https://docs.unity3d.com/2020.2/Documentation/Manual/profiler-virtual-texturing-module.html";
            public static readonly GUIContent helpButtonContent = EditorGUIUtility.TrIconContent("_Help@2x", "Open Manual (in a web browser)");

            static Styles()
            {
                //generate background colors
                if (EditorGUIUtility.isProSkin)
                {
                    colors[(int)backgroundColor.even] = new Color(0.176f, 0.176f, 0.176f);
                    colors[(int)backgroundColor.odd] = new Color(0.196f, 0.196f, 0.196f);
                    colors[(int)backgroundColor.header] = new Color(0.235f, 0.235f, 0.235f);
                    colors[(int)backgroundColor.background] = Color.black;
                }
                else
                {
                    colors[(int)backgroundColor.even] = new Color(0.792f, 0.792f, 0.792f);
                    colors[(int)backgroundColor.odd] = new Color(0.761f, 0.761f, 0.761f);
                    colors[(int)backgroundColor.header] = new Color(0.796f, 0.796f, 0.796f);
                    colors[(int)backgroundColor.background] = new Color(0.6f, 0.6f, 0.6f);
                }

                for (int i = 0; i < (int)backgroundColor.COLORCOUNT; ++i)
                {
                    backgrounds[i] = new Texture2D(1, 1);
                    backgrounds[i].SetPixel(0, 0, colors[i]);
                    backgrounds[i].Apply();
                }
            }

            public static void CreateStyles()
            {
                if (EditorGUIUtility.isProSkin)
                    textColor = new Color(0xBA, 0xBA, 0xBA);
                else textColor = new Color(0.055f, 0.055f, 0.055f);

                GUIStyleState state = new GUIStyleState();
                state.background = backgrounds[(int)backgroundColor.header];
                state.textColor = textColor;

                headerStyle = new GUIStyle();
                headerStyle.normal = state;
                headerStyle.active = state;
                headerStyle.focused = state;
                headerStyle.hover = state;
                headerStyle.alignment = TextAnchor.MiddleLeft;
                headerStyle.fontStyle = FontStyle.Normal;

                state.background = backgrounds[(int)backgroundColor.odd];

                statStyle = new GUIStyle();
                statStyle.normal = state;
                statStyle.active = state;
                statStyle.focused = state;
                statStyle.hover = state;
                statStyle.alignment = TextAnchor.UpperLeft;
                statStyle.padding.top = 5;
                statStyle.fontStyle = FontStyle.Normal;

                state.background = backgrounds[(int)backgroundColor.even];

                evenStyle = new GUIStyle();
                evenStyle.normal = state;
                evenStyle.active = state;
                evenStyle.focused = state;
                evenStyle.hover = state;
                evenStyle.fontStyle = FontStyle.Normal;
                evenStyle.alignment = TextAnchor.MiddleLeft;

                state.background = backgrounds[(int)backgroundColor.odd];

                oddStyle = new GUIStyle();
                oddStyle.normal = state;
                oddStyle.active = state;
                oddStyle.focused = state;
                oddStyle.hover = state;
                oddStyle.fontStyle = FontStyle.Normal;
                oddStyle.alignment = TextAnchor.MiddleLeft;

                state.background = backgrounds[(int)backgroundColor.background];

                backgroundStyle = new GUIStyle();
                backgroundStyle.normal = state;
                backgroundStyle.active = state;
                backgroundStyle.focused = state;
                backgroundStyle.hover = state;
                backgroundStyle.padding = new RectOffset(-2, -2, -2, -2);
            }

            public static Texture2D GetBackground()
            {
                return backgrounds[(int)backgroundColor.background];
            }

            //when exiting playmode the background textures get reset to null
            public static void CheckBackgroundTextures()
            {
                bool shouldRecreateStyles = false;

                for (int i = 0; i < (int)backgroundColor.COLORCOUNT; ++i)
                {
                    if (backgrounds[i] == null)
                    {
                        backgrounds[i] = new Texture2D(1, 1);
                        backgrounds[i].SetPixel(0, 0, colors[i]);
                        backgrounds[i].Apply();

                        shouldRecreateStyles = true;
                    }
                }

                if (shouldRecreateStyles)
                    CreateStyles();
            }
        }

        internal class VTMultiColumnHeader : MultiColumnHeader
        {
            public VTMultiColumnHeader(MultiColumnHeaderState state)
                : base(state)
            {
            }

            protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
            {
                //add one extra pixel to fix rounding issue with monitor scaling
                headerRect.width++;
                GUI.Box(headerRect, column.headerContent, Styles.headerStyle);

                if (canSort && column.canSort)
                {
                    SortingButton(column, headerRect, columnIndex);
                }
            }

            Rect GetArrowRect(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
            {
                int xOffset = 0;

                switch (columnIndex)
                {
                    case 0:
                        xOffset = 95;
                        break;
                    case 1:
                        xOffset = 57;
                        break;
                    default:
                        xOffset = 36;
                        break;
                }

                Rect arrowRect = new Rect(headerRect.x + xOffset - 8, headerRect.y + headerRect.height / 2.0f - 8, 16, 16);
                return arrowRect;
            }

            new void SortingButton(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
            {
                // Button logic
                if (EditorGUI.Button(headerRect, GUIContent.none, GUIStyle.none))
                {
                    ColumnHeaderClicked(column, columnIndex);
                }

                // Draw sorting arrow
                if (columnIndex == state.sortedColumnIndex && Event.current.type == EventType.Repaint)
                {
                    var arrowRect = GetArrowRect(column, headerRect, columnIndex);
                    string arrow;

                    if (column.sortedAscending)
                        arrow = "\u25B4";
                    else arrow = "\u25BE";


                    GUIStyle style = Styles.headerStyle;
                    style.fontSize *= 2;
                    GUI.Label(arrowRect, arrow, DefaultStyles.arrowStyle);
                }
            }
        }
    }
}
