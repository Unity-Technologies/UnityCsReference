// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Debug = UnityEngine.Debug;

namespace UnityEditor.UIElements.Experimental.USSStats;

class USSStatsImpl : PanelDebugger
{
    static FontDefinition robotoMonoFont => FontDefinition.FromFont(EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font);
    const string InfoMessage = """
                               This tool allows you to profile the style matching performance of UI Toolkit UI.
                               Select a panel to generate USS selector performance statistics.
                               Copy or save the report to Markdown for further analysis.
                               """;

    HelpBox m_InfoBox;
    PopupField<string> m_StyleSheetPopup;
    VisualElement m_StatsContainer;
    MultiColumnListView m_ResultsListView;
    VisualElement m_ResultsContainer;
    List<StyleSheetProfilingResult> m_LastProfilingResults;

    DateTime m_LastCaptureTime;
    string m_LastCapturePanelName;
    int m_CurrentStyleSheetIndex;

    List<ComplexSelectorStats> currentSelectors => m_ResultsListView.itemsSource as List<ComplexSelectorStats>;

    enum ColumnIndex
    {
        Name,
        TimeMs,
        TimePct,
        Matches,
        Rejections,
        FastRejectionRate
    }

    protected override void OnSelectPanelDebug(IPanelDebug pdbg)
    {
        if (pdbg == null || pdbg.panel == null)
        {
            ClearResults();
        }
        else
        {
            ObtainResults();
        }
    }


    public void Initialize(EditorWindow debuggerWindow, VisualElement root)
    {
        base.Initialize(debuggerWindow);

        m_InfoBox = new HelpBox(InfoMessage, HelpBoxMessageType.Info);

        m_Toolbar.Add(new ToolbarSpacer() { flex = true });
        m_Toolbar.Add(new ToolbarButton(ClearResults) { text = "Clear results" });
        m_Toolbar.Add(new ToolbarButton(CopyResults) { text = "Copy" });
        m_Toolbar.Add(new ToolbarButton(SaveResults) { text = "Save to file" });

        // Create stylesheet selector popup
        m_StyleSheetPopup = new PopupField<string>("StyleSheet", new List<string>(), 0);
        m_StyleSheetPopup.style.display = DisplayStyle.None;
        m_StyleSheetPopup.RegisterValueChangedCallback(evt => OnStyleSheetChanged());

        // Create stats container with labels
        m_StatsContainer = new VisualElement();
        m_StatsContainer.style.paddingBottom = 5;
        m_StatsContainer.style.paddingTop = 5;
        m_StatsContainer.style.paddingLeft = 5;
        m_StatsContainer.style.paddingRight = 5;
        m_StatsContainer.style.display = DisplayStyle.None;

        // Create results container
        m_ResultsContainer = new VisualElement();
        m_ResultsContainer.style.flexGrow = 1;
        m_ResultsContainer.style.display = DisplayStyle.None;

        // Create MultiColumnListView
        m_ResultsListView = new MultiColumnListView();
        m_ResultsListView.style.flexGrow = 1;
        m_ResultsListView.sortingMode = ColumnSortingMode.Default;

        m_ResultsListView.columns.Add(new Column
        {
            title = "Selector",
            stretchable = true,
            width = 250,
            makeCell = () => new Label { style = { unityFontDefinition = robotoMonoFont } },
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var item = currentSelectors[index];
                if (item != null)
                    label.text = item.ussText;
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.Name)
        });

        m_ResultsListView.columns.Add(new Column
        {
            title = "Time (ms)",
            width = 100,
            makeCell = () => new Label(),
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var item = currentSelectors[index];
                label.text = $"{item.totalTime:F4}";
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.TimeMs)
        });

        m_ResultsListView.columns.Add(new Column
        {
            title = "Time (%)",
            width = 100,
            makeCell = () => new Label(),
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var item = currentSelectors[index];
                label.text = $"{item.totalPercentage:F2}";
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.TimePct)
        });

        m_ResultsListView.columns.Add(new Column
        {
            title = "Matches",
            width = 100,
            makeCell = () => new Label(),
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var item = currentSelectors[index];
                if (item != null)
                    label.text = item.totalMatches.ToString();
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.Matches)
        });

        m_ResultsListView.columns.Add(new Column
        {
            title = "Rejections",
            width = 100,
            makeCell = () => new Label(),
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var item = currentSelectors[index];
                if (item != null)
                    label.text = item.totalRejections.ToString();
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.Rejections)
        });

        m_ResultsListView.columns.Add(new Column
        {
            title = "Fast Rejection Rate (%)",
            width = 150,
            makeCell = () => new Label(),
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var selector = currentSelectors[index];
                if (selector != null)
                {
                    string fastRejectionRate = "";
                    if (selector.TryGetFastRejectionRate(out double rate))
                    {
                        fastRejectionRate = $"{rate:F2}";
                    }
                    label.text = fastRejectionRate;
                    label.tooltip = string.IsNullOrEmpty(fastRejectionRate) ? Tooltips.k_FastRejectionRateNA : Tooltips.k_FastRejectionRate;
                }
            },
            comparison = (i, j) => CompareStats(currentSelectors[i], currentSelectors[j], ColumnIndex.FastRejectionRate)
        });

        m_ResultsContainer.Add(m_ResultsListView);

        root.Add(m_Toolbar);
        root.Add(m_InfoBox);
        root.Add(m_StyleSheetPopup);
        root.Add(m_StatsContainer);
        root.Add(m_ResultsContainer);
    }

    void OnStyleSheetChanged()
    {
        if (m_StyleSheetPopup.index >= 0 && m_StyleSheetPopup.index < m_LastProfilingResults.Count)
        {
            m_CurrentStyleSheetIndex = m_StyleSheetPopup.index;
            UpdateStatsDisplay();
            UpdateListView();
        }
    }

    void UpdateStatsDisplay()
    {
        if (m_LastProfilingResults == null || m_CurrentStyleSheetIndex >= m_LastProfilingResults.Count)
            return;

        var sheetResult = m_LastProfilingResults[m_CurrentStyleSheetIndex];

        m_StatsContainer.Clear();

        var properties = Formatting.GetStyleSheetProperties(sheetResult);
        foreach (var property in properties)
        {
            var label = new TextField();
            label.readOnly = true;
            label.label = property.Key;
            label.value = property.Value.value;
            label.tooltip = property.Value.tooltip;
            m_StatsContainer.Add(label);
        }
    }

    void UpdateListView()
    {
        if (m_LastProfilingResults == null || m_CurrentStyleSheetIndex >= m_LastProfilingResults.Count)
        {
            m_ResultsListView.itemsSource = null;
            m_ResultsListView.RefreshItems();
            return;
        }

        var currentSheet = m_LastProfilingResults[m_CurrentStyleSheetIndex];

        var displayedSelectors = new List<ComplexSelectorStats>();
        foreach (var selector in currentSheet.selectors)
        {
            if (selector.wasTested)
            {
                displayedSelectors.Add(selector);
            }
        }

        m_ResultsListView.itemsSource = displayedSelectors;
        m_ResultsListView.RefreshItems();
    }

    void SaveResults()
    {
        if (m_LastProfilingResults == null)
            return;

        var tempResults = GetSortedResultsByCurrentMode(m_LastProfilingResults);
        var markdown = Formatting.ToMarkDown(tempResults, m_LastCaptureTime, m_LastCapturePanelName);
        var path = EditorUtility.SaveFilePanel("Save USS Profiling Results", "", m_LastCapturePanelName ?? "Results", "md");

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.WriteAllText(path, markdown);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not write results file to {path}");
            Debug.LogException(e);
        }
    }

    void ObtainResults()
    {
        if (selectedPanel == null)
            return;

        m_LastProfilingResults = USSStatsStyleUpdater.ProfileStyles(selectedPanel.panel);

        m_LastCaptureTime = DateTime.Now;
        m_LastCapturePanelName = selectedPanel.panel.name;

        if (!Stopwatch.IsHighResolution)
        {
            m_InfoBox.text = "Warning: Stopwatch is not high resolution on this system. Profiling results may be inaccurate.";
            m_InfoBox.messageType = HelpBoxMessageType.Warning;
            m_InfoBox.style.display = DisplayStyle.Flex;
        }
        else
        {
            m_InfoBox.style.display = DisplayStyle.None;
        }

        // Update popup with stylesheet names
        var styleSheetNames = new List<string>();
        foreach(var r in m_LastProfilingResults)
            styleSheetNames.Add(r.sheet.name);
        m_StyleSheetPopup.choices = styleSheetNames;
        m_StyleSheetPopup.index = 0;
        m_StyleSheetPopup.style.display = DisplayStyle.Flex;

        m_CurrentStyleSheetIndex = 0;
        m_StatsContainer.style.display = DisplayStyle.Flex;
        m_ResultsContainer.style.display = DisplayStyle.Flex;

        UpdateStatsDisplay();
        UpdateListView();
    }

    void CopyResults()
    {
        if (m_LastProfilingResults != null)
        {
            var tempResults = GetSortedResultsByCurrentMode(m_LastProfilingResults);
            var markdown = Formatting.ToMarkDown(tempResults, m_LastCaptureTime, m_LastCapturePanelName);
            EditorGUIUtility.systemCopyBuffer = markdown;
        }
    }

    void ClearResults()
    {
        m_LastProfilingResults = null;
        m_CurrentStyleSheetIndex = 0;
        m_StyleSheetPopup.style.display = DisplayStyle.None;
        m_StatsContainer.style.display = DisplayStyle.None;
        m_ResultsContainer.style.display = DisplayStyle.None;
        m_InfoBox.style.display = DisplayStyle.Flex;
        m_InfoBox.text = InfoMessage;
        m_InfoBox.messageType = HelpBoxMessageType.Info;
        m_ResultsListView.itemsSource = null;
        m_ResultsListView.RefreshItems();
    }

    List<StyleSheetProfilingResult> GetSortedResultsByCurrentMode(List<StyleSheetProfilingResult> results)
    {
        var tempResults = new List<StyleSheetProfilingResult>();
        foreach (var result in results)
        {
            tempResults.Add(result.DuplicateWithSorting(ModeAwareCompare));
        }
        return tempResults;
    }

    static int CompareStats(ComplexSelectorStats a, ComplexSelectorStats b, ColumnIndex index)
    {
        if (a == null || b == null)
            return 0;

        int CompareRejectionRate(ComplexSelectorStats x, ComplexSelectorStats y)
        {
            bool xHasRate = x.TryGetFastRejectionRate(out double xr);
            bool yHasRate = y.TryGetFastRejectionRate(out double yr);
            if (!xHasRate && !yHasRate)
                return 0;
            if (!xHasRate)
                return -1;
            if (!yHasRate)
                return 1;
            return xr.CompareTo(yr);
        }

        return index switch
        {
            ColumnIndex.Name => string.Compare(a.ussText, b.ussText, StringComparison.Ordinal),
            ColumnIndex.TimeMs => a.totalTime.CompareTo(b.totalTime),
            ColumnIndex.TimePct => a.totalPercentage.CompareTo(b.totalPercentage),
            ColumnIndex.Matches => a.totalMatches.CompareTo(b.totalMatches),
            ColumnIndex.Rejections => a.totalRejections.CompareTo(b.totalRejections),
            ColumnIndex.FastRejectionRate => CompareRejectionRate(a, b),
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    int ModeAwareCompare(ComplexSelectorStats a, ComplexSelectorStats b)
    {
        foreach (var sortColumnDescription in m_ResultsListView.sortedColumns)
        {
            int result = CompareStats(a, b, (ColumnIndex)sortColumnDescription.columnIndex);

            if (result != 0)
            {
                if (sortColumnDescription.direction == SortDirection.Ascending)
                    return result;
                return -result;
            }
        }
        return 0;
    }

    static class Tooltips
    {
        public const string k_SheetTotalTime = "Total time spent processing this stylesheet, including selector matching time.";
        public const string k_SheetSelfTime = "Time spent in this stylesheet excluding selector matching time. This overhead grows with the number of elements and size of the style sheet.";
        public const string k_SheetSelectorTime = "Total time spent matching selectors in this stylesheet. This time grows with the number of elements and complexity of the selectors.";
        public const string k_SheetApplicableElems = "Number of elements the stylesheet was applied to. This counter is not incremented when this stylesheet is imported in another stylesheet.";
        public const string k_SheetQueries = "Total number of style queries performed for this stylesheet. An element leads to one query for its type, one for its name (if it is defined) and one for each class list. This counter is not incremented when this stylesheet is imported in another stylesheet.";
        public const string k_SheetImportCount = "Number of stylesheets imported in this stylesheet.";
        public const string k_SheetAvoidQueryPct = "Percentage by which the potential number of style queries was reduced by importing other style sheets into this one";
        public const string k_SheetTests = "Total number of selector tests performed for this stylesheet.";
        public const string k_SheetMatches = "Total number of selector matches for this stylesheet.";
        public const string k_SheetRejRate = "Overall rejection rate for selectors in this stylesheet.";
        public const string k_FastRejectionRateNA = "Fast rejection rate is not applicable for simple selectors or selectors with no rejections.";
        public const string k_FastRejectionRate = """
                                                  Percentage of rejections that were fast rejections for this selector.
                                                  Fast rejections apply to complex selectors only and indicate how often the selector was able to be rejected without traversing the full hierarchy.
                                                  The main reason this number may be lower than 100% is the presence of pseudo state selectors.
                                                  Less frequently, a very large hierarchy will exceed the buffer allocated to this purpose and fall back to a full traversal.
                                                  """;
    }

    static class Formatting
    {
        public static Dictionary<string, (string value, string tooltip)> GetStyleSheetProperties(StyleSheetProfilingResult sheetResult)
        {
            var properties = new Dictionary<string, (string value, string tooltip)>();

            properties["Total time"] = ($"{FormatTime(sheetResult.totalTimeMs)}", Tooltips.k_SheetTotalTime);
            properties["Self time"] = ($"{FormatTime(sheetResult.styleSheetStats.selfTimeMs)}", Tooltips.k_SheetSelfTime);
            properties["Selector time"] = ($"{FormatTime(sheetResult.totalSelectorTimeMs)}", Tooltips.k_SheetSelectorTime);
            properties["Applicable elements"] = ($"{sheetResult.styleSheetStats.elementCount}", Tooltips.k_SheetApplicableElems);
            properties["Total query count"] = ($"{sheetResult.styleSheetStats.totalQueryCount}", Tooltips.k_SheetQueries);

            if (sheetResult.styleSheetStats.importedStyleSheetsCount > 0)
            {
                properties["Imported stylesheets counted"] = ($"{sheetResult.styleSheetStats.importedStyleSheetsCount}", Tooltips.k_SheetImportCount);
                properties["Avoided query percentage"] = ($"{FormatPct(sheetResult.styleSheetStats.avoidedQueryPercentage)}", Tooltips.k_SheetAvoidQueryPct);
            }

            properties["Total selector tests"] = ($"{sheetResult.totalSelectorsTested}", Tooltips.k_SheetTests);
            properties["Total matches"] = ($"{sheetResult.totalSelectorsMatched}", Tooltips.k_SheetMatches);

            if (sheetResult.totalSelectorsTested > 0)
            {
                double overallRejectionRate = (sheetResult.totalSelectorsRejected * 100.0) / sheetResult.totalSelectorsTested;
                properties["Overall rejection rate"] = ($"{FormatPct(overallRejectionRate)}", Tooltips.k_SheetRejRate);
            }

            return properties;
        }

        public static string ToMarkDown(List<StyleSheetProfilingResult> results, DateTime captureTime, string panelName)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"# Profiling results for panel : {panelName}");
            builder.AppendLine();
            builder.AppendLine($"Version: {Application.unityVersion}");
            builder.AppendLine($"Date: {captureTime:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("## Selector matching performance report");
            builder.AppendLine();

            var columns = new string[]
            {
                "Selectors",
                "Time (ms)",
                "Time (%)",
                "Matches",
                "Rejections",
                "Fast Rejection Rate (%)"
            };


            foreach (var sheetProfilingResult in results)
            {
                builder.AppendLine($"### Results for stylesheet: {sheetProfilingResult.sheet.name}");
                builder.AppendLine();

                var properties = GetStyleSheetProperties(sheetProfilingResult);
                foreach (var property in properties)
                {
                    builder.AppendLine($"{ property.Key}: {property.Value.value}");
                }
                builder.AppendLine();

                int maxSelectorLength = columns[0].Length;


                List<string> selectorValues = new List<string>(sheetProfilingResult.selectors.Count);
                foreach (var selector in sheetProfilingResult.selectors)
                {
                    if (selector.wasTested == false)
                        continue;
                    string value = $"`{selector.ussText}`";
                    selectorValues.Add(value);
                    maxSelectorLength = Math.Max(maxSelectorLength, value.Length);
                }

                var columnSizes = new int[columns.Length];
                for (int i = 1; i < columns.Length; i++)
                    columnSizes[i] = columns[i].Length;
                columnSizes[0] = maxSelectorLength;

                for (int i = 0; i < columns.Length; i++)
                {
                    builder.Append($"|{columns[i].PadRight(columnSizes[i])}");
                }
                builder.Append("|");
                builder.AppendLine("");

                for (int i = 0; i < columns.Length; i++)
                {
                    builder.Append($"|{"".PadRight(columnSizes[i], '-')}");
                }
                builder.Append("|");
                builder.AppendLine("");

                int index = -1;

                foreach (var selector in sheetProfilingResult.selectors)
                {
                    if (selector.wasTested == false)
                        continue;


                    string fastRejectionRate = "";
                    if (selector.TryGetFastRejectionRate(out double rate))
                    {
                        fastRejectionRate = $"{rate:F2}";
                    }

                    var columnValues = new string[]
                    {
                        selectorValues[++index],
                        $"{selector.totalTime:F4}",
                        $"{selector.totalPercentage:F2}",
                        selector.totalMatches.ToString(),
                        selector.totalRejections.ToString(),
                        fastRejectionRate
                    };

                    for (int i = 0; i < columns.Length; i++)
                    {
                        builder.Append($"|{columnValues[i].PadRight(columnSizes[i])}");
                    }
                    builder.Append("|");
                    builder.AppendLine();
                }
            }

            builder.AppendLine();

            return builder.ToString();
        }

        public static string FormatPct(double pct) => $"{pct:F2}%";

        public static string FormatTime(double milliseconds) => $"{milliseconds:F4} ms";
    }
}
