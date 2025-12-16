// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements.Experimental.USSStats;

internal class StyleSheetProfilingResult
{
    public StyleSheet sheet => styleSheetStats.styleSheet;

    public StyleSheetStats styleSheetStats { get; }

    public IReadOnlyList<ComplexSelectorStats> selectors { get; }

    public double totalSelectorTimeMs;

    public double totalTimeMs;

    public int totalSelectorsTested => totalSelectorsMatched + totalSelectorsRejected;
    public int totalSelectorsMatched;
    public int totalSelectorsRejected;

    public StyleSheetProfilingResult(StyleSheetStats styleSheetStats, List<ComplexSelectorStats> selectors)
    {
        this.styleSheetStats = styleSheetStats;
        this.selectors = selectors;
        ComputeSums();
    }

    public StyleSheetProfilingResult DuplicateWithSorting(Comparison<ComplexSelectorStats> comparison)
    {
        var sortedSelectors = new List<ComplexSelectorStats>(selectors);
        sortedSelectors.Sort(comparison);
        return new StyleSheetProfilingResult(styleSheetStats, sortedSelectors);
    }

    void ComputeSums()
    {
        double sumMs = 0;

        foreach (var selector in selectors)
        {
            if (!selector.wasTested)
            {
                Debug.Assert(selector.totalTime == 0);
                continue;
            }
            sumMs += selector.totalTime;
            totalSelectorsMatched += selector.totalMatches;
            totalSelectorsRejected += selector.totalRejections;
        }

        totalSelectorTimeMs = sumMs;
        totalTimeMs = totalSelectorTimeMs + styleSheetStats.selfTimeMs;

        if (totalSelectorTimeMs > 0)
        {
            foreach (var selector in selectors)
            {
                selector.totalPercentage = (selector.totalTime * 100) / totalSelectorTimeMs;
            }
        }
    }
}
