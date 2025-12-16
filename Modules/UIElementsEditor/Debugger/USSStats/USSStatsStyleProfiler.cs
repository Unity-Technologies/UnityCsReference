// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Experimental.USSStats;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements.StyleSheets;

class StyleSheetStats
{
    public StyleSheet styleSheet;
    public double selfTimeMs;
    public int elementCount;
    public int totalQueryCount;

    public StyleSheetStats(StyleSheet sheet)
    {
        styleSheet = sheet;
    }
}

class ComplexSelectorStats
{
    public StyleComplexSelector complexSelector;
    public int totalFastRejections;
    public int totalRejections;
    public int totalMatches;
    public double totalTime;
    public double totalPercentage;
    public string ussText { get; }

    public ComplexSelectorStats(StyleComplexSelector selector, string ussText)
    {
        this.complexSelector = selector;
        this.ussText = ussText;
    }

    public bool wasTested => totalRejections + totalMatches > 0;


    public bool TryGetFastRejectionRate(out double rate)
    {
        if (complexSelector.isSimple || totalRejections == 0)
        {
            rate = 0;
            return false;
        }
        rate = (totalFastRejections * 100.0) / totalRejections;
        return true;
    }
}

struct USSStatsStyleProfiler : IStyleProfiler
{
    Dictionary<StyleSheet, StyleSheetStats> m_StyleSheetStatsCache;
    Dictionary<StyleComplexSelector, ComplexSelectorStats> m_SelectorStatsCache;

    bool m_ElementHasName;
    int m_ElementClassCount;
    Stopwatch m_StyleSheetStopwatch;
    Stopwatch m_SelectorStopwatch;

    StyleSheetStats m_StyleSheetStats;
    ComplexSelectorStats m_CurrentSelectorStats;

    public void Initialize(VisualElement root)
    {
        var styleSheets = new HashSet<StyleSheet>();
        GatherStyleSheets(root, styleSheets);

        m_StyleSheetStatsCache = PrewarmStyleSheetCache(styleSheets);
        m_SelectorStatsCache = PrewarmSelectorCache(styleSheets);

        m_SelectorStopwatch = new();
        m_StyleSheetStopwatch = new();
        m_ElementHasName = false;
        m_ElementClassCount = 0;
    }

    public void Clear()
    {
        m_StyleSheetStatsCache.Clear();
        m_SelectorStatsCache.Clear();
    }

    public List<StyleSheetProfilingResult> GetResults()
    {
        var list = new List<StyleSheetProfilingResult>();

        foreach (var styleSheet in m_StyleSheetStatsCache.Keys)
        {
            var selectors = new List<ComplexSelectorStats>();

            foreach (var rule in styleSheet.rules)
            {
                foreach (var selector in rule.complexSelectors)
                {
                    selectors.Add(m_SelectorStatsCache[selector]);
                }
            }
            selectors.Sort((a,b) => b.totalTime.CompareTo(a.totalTime));
            var result = new StyleSheetProfilingResult(m_StyleSheetStatsCache[styleSheet], selectors);
            list.Add(result);
        }

        list.Sort((a, b) => b.totalTimeMs.CompareTo(a.totalTimeMs));

        return list;
    }

    static Dictionary<StyleSheet, StyleSheetStats> PrewarmStyleSheetCache(HashSet<StyleSheet> styleSheets)
    {
        var result = new Dictionary<StyleSheet, StyleSheetStats>();
        foreach (var styleSheet in styleSheets)
        {
            result.Add(styleSheet, new StyleSheetStats(styleSheet));
        }
        return result;
    }

    static Dictionary<StyleComplexSelector, ComplexSelectorStats> PrewarmSelectorCache(HashSet<StyleSheet> styleSheets)
    {
        var exporter = new StyleSheetExporter();
        var result = new Dictionary<StyleComplexSelector, ComplexSelectorStats>();
        foreach (var styleSheet in styleSheets)
        {
            foreach (var rule in styleSheet.rules)
            {
                foreach (var selector in rule.complexSelectors)
                {
                    // Pre-create those profiling objects to avoid excessive cost during recording
                    string ussText = exporter.ToUssString(styleSheet, selector);
                    result.Add(selector, new ComplexSelectorStats(selector, ussText));
                }
            }
        }
        return result;
    }

    public static void GatherStyleSheets(VisualElement cursor, HashSet<StyleSheet> styleSheets)
    {
        if (cursor.styleSheetList != null)
        {
            foreach (StyleSheet sheet in cursor.styleSheetList)
            {
                // Skip deleted style sheets
                if (sheet == null)
                    continue;

                styleSheets.Add(sheet);

                if (sheet.flattenedRecursiveImports != null)
                {
                    styleSheets.UnionWith(sheet.flattenedRecursiveImports);
                }
            }
        }

        var count = cursor.hierarchy.childCount;
        for (int i = 0; i < count; ++i)
        {
            GatherStyleSheets(cursor.hierarchy[i], styleSheets);
        }
    }


    public void BeginMatchingElement(VisualElement element)
    {
        m_ElementHasName = !string.IsNullOrEmpty(element.name);
        m_ElementClassCount = element.classList.Count;
    }

    public void BeginMatchingStyleSheet(StyleSheet styleSheet)
    {
        m_StyleSheetStats = m_StyleSheetStatsCache[styleSheet];

        m_StyleSheetStats.elementCount++;

        if (m_ElementHasName && (styleSheet.nonEmptyTablesMask & (1 << (int)StyleSheet.OrderedSelectorType.Name)) != 0)
            m_StyleSheetStats.totalQueryCount++;

        if ((styleSheet.nonEmptyTablesMask & (1 << (int)StyleSheet.OrderedSelectorType.Class)) != 0)
            m_StyleSheetStats.totalQueryCount += m_ElementClassCount;

        if ((styleSheet.nonEmptyTablesMask & (1 << (int)StyleSheet.OrderedSelectorType.Type)) != 0)
            m_StyleSheetStats.totalQueryCount++;

        m_StyleSheetStopwatch.Restart();
    }

    public void BeginMatchingSelector(StyleComplexSelector complexSelector)
    {
        m_StyleSheetStopwatch.Stop();
        m_CurrentSelectorStats = m_SelectorStatsCache[complexSelector];
        m_SelectorStopwatch.Restart();
    }

    public void EndMatchingSelector(StyleComplexSelector complexSelector, bool match, bool passedAncestorFilter)
    {
        m_SelectorStopwatch.Stop();
        m_CurrentSelectorStats.totalTime += m_SelectorStopwatch.Elapsed.TotalMilliseconds;

        if (match)
            m_CurrentSelectorStats.totalMatches++;
        else
        {
            if (!passedAncestorFilter)
            {
                m_CurrentSelectorStats.totalFastRejections++;
            }
            m_CurrentSelectorStats.totalRejections++;
        }

        m_CurrentSelectorStats = null;

        m_StyleSheetStopwatch.Start();
    }

    public void EndMatchingStyleSheet(StyleSheet styleSheet)
    {
        m_StyleSheetStopwatch.Stop();
        m_StyleSheetStats.selfTimeMs += m_StyleSheetStopwatch.Elapsed.TotalMilliseconds;
        m_StyleSheetStats = null;
    }
}
