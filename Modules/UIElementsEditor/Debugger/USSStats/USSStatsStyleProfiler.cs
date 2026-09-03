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
    public int importedStyleSheetsCount;

    public double avoidedQueryPercentage
    {
        get
        {
            if (importedStyleSheetsCount == 0 || totalQueryCount == 0)
                return 0;

            int avoidedQueryCount = totalQueryCount * importedStyleSheetsCount;

            return avoidedQueryCount * 100d / (avoidedQueryCount + totalQueryCount);
        }
    }

    public StyleSheetStats(StyleSheet sheet)
    {
        styleSheet = sheet;
        importedStyleSheetsCount = sheet.flattenedRecursiveImports?.Count ?? 0;
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

    // Per-cache-entry buffers the native matcher fills with per-descriptor statistics during
    // the profiled pass; folded into m_SelectorStatsCache by GetResults. Keyed by owner sheet
    // (one acceleration cache entry per owner).
    Dictionary<StyleSheet, (SelectorAccelerationCacheEntry entry, SelectorMatchStatsInfo[] stats)> m_MatchStatsBuffers;

    bool m_ElementHasName;
    int m_ElementClassCount;
    Stopwatch m_StyleSheetStopwatch;

    StyleSheetStats m_StyleSheetStats;

    public void Initialize(VisualElement root)
    {
        var styleSheets = new HashSet<StyleSheet>();
        GatherStyleSheets(root, styleSheets);

        m_StyleSheetStatsCache = PrewarmStyleSheetCache(styleSheets);
        m_SelectorStatsCache = PrewarmSelectorCache(styleSheets);
        m_MatchStatsBuffers = new();

        m_StyleSheetStopwatch = new();
        m_ElementHasName = false;
        m_ElementClassCount = 0;
    }

    public void Clear()
    {
        m_StyleSheetStatsCache.Clear();
        m_SelectorStatsCache.Clear();
        m_MatchStatsBuffers.Clear();
    }

    public SelectorMatchStatsInfo[] BeginSelectorMatching(in SelectorAccelerationCacheEntry accelerationCacheEntry)
    {
        // Pause the sheet stopwatch for the duration of the matching call: that time is
        // attributed per selector through the stats buffer, not to sheet self time.
        m_StyleSheetStopwatch.Stop();

        var owner = accelerationCacheEntry.ownerStyleSheet;
        if (!m_MatchStatsBuffers.TryGetValue(owner, out var buffer))
        {
            buffer = (accelerationCacheEntry, new SelectorMatchStatsInfo[accelerationCacheEntry.m_AllDescriptorsCount]);
            m_MatchStatsBuffers[owner] = buffer;
        }
        return buffer.stats;
    }

    public void EndSelectorMatching()
    {
        m_StyleSheetStopwatch.Start();
    }

    public List<StyleSheetProfilingResult> GetResults()
    {
        // Fold the native per-descriptor statistics into the per-selector caches. Sheet self
        // time needs no adjustment: the sheet stopwatch is paused for the duration of every
        // matching call (Begin/EndSelectorMatching).
        foreach (var kvp in m_MatchStatsBuffers)
        {
            var (entry, stats) = kvp.Value;
            var descriptors = entry.allDescriptors;
            for (int i = 0; i < stats.Length; i++)
            {
                var stat = stats[i];
                if (stat.testedCount == 0)
                    continue;

                var selectorStats = m_SelectorStatsCache[entry.GetComplexSelector(in descriptors[i])];
                selectorStats.totalTime += stat.timeNs / 1_000_000.0;
                selectorStats.totalMatches += (int)stat.matchedCount;
                selectorStats.totalRejections += (int)(stat.testedCount - stat.matchedCount);
                selectorStats.totalFastRejections += (int)stat.fastRejectedCount;
            }
        }

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
            result.Add(styleSheet, new StyleSheetStats(styleSheet) );
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
        m_ElementClassCount = element.classListCount;
    }

    public void BeginMatchingStyleSheet(StyleSheet styleSheet, SelectorAccelerationCacheEntry accelerationCacheEntry)
    {
        m_StyleSheetStats = m_StyleSheetStatsCache[styleSheet];

        m_StyleSheetStats.elementCount++;

        if (m_ElementHasName && (accelerationCacheEntry.nonEmptyTablesMask & (1 << (int)SelectorAccelerationTableType.Name)) != 0)
            m_StyleSheetStats.totalQueryCount++;

        if ((accelerationCacheEntry.nonEmptyTablesMask & (1 << (int)SelectorAccelerationTableType.Class)) != 0)
            m_StyleSheetStats.totalQueryCount += m_ElementClassCount;

        if ((accelerationCacheEntry.nonEmptyTablesMask & (1 << (int)SelectorAccelerationTableType.Type)) != 0)
            m_StyleSheetStats.totalQueryCount++;

        m_StyleSheetStopwatch.Restart();
    }

    public void EndMatchingStyleSheet(StyleSheet styleSheet)
    {
        m_StyleSheetStopwatch.Stop();
        m_StyleSheetStats.selfTimeMs += m_StyleSheetStopwatch.Elapsed.TotalMilliseconds;
        m_StyleSheetStats = null;
    }
}
