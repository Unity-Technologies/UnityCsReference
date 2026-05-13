// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Utility class to request highlights across different windows.
/// </summary>
static class HighlightUtility
{
    public static void RequestHighlights(StyleRule rule, CommandSources.CommandSource source)
    {
        using var command = RequestHighlightsCommand.GetPooled(rule);
        command.Source = source;
        UICommandQueue.EnqueueCommand(command);
    }

    public static void RequestHighlights(VisualElement element, CommandSources.CommandSource source)
    {
        using var command = RequestHighlightsCommand.GetPooled(element);
        command.Source = source;
        UICommandQueue.EnqueueCommand(command);
    }

    public static void RequestHighlights(int elementId, CommandSources.CommandSource source)
    {
        using var command = RequestHighlightsCommand.GetPooled(elementId);
        command.Source = source;
        UICommandQueue.EnqueueCommand(command);
    }

    public static void ClearHighlights()
    {
        using var command = HighlightCommand.GetPooled(null, null);
        UICommandQueue.EnqueueCommand(command);
    }

    public static void GetMatchingElementsForSelector(VisualElement root, StyleComplexSelector complexSelector, HashSet<VisualElement> matched)
    {
        var rightmost = complexSelector.selectors[^1];

        // Build a UQuery pre-filtered by the rightmost selector's parts
        var query = root.Query();
        foreach (var part in rightmost.parts)
        {
            switch (part.type)
            {
                case StyleSelectorType.Class: query = query.Class(part.value); break;
                case StyleSelectorType.ID:    query = query.Name(part.value);  break;
                case StyleSelectorType.Type:  query = query.Where(e => e.typeName == part.value); break;
                case StyleSelectorType.PseudoClass:
                {
                    if (part.value == "root")
                        query = query.Where(e => e.isRootVisualContainer); break;
                }
            }
        }

        query.ForEach(candidate =>
        {
            var prev = candidate.pseudoStates;
            candidate.pseudoStates = PseudoStates.Active | PseudoStates.Disabled |
                                     PseudoStates.Focus | PseudoStates.Hover | PseudoStates.Checked;
            if (candidate.isRootVisualContainer && candidate.styleSheets.Contains(complexSelector.rule.styleSheet))
                candidate.pseudoStates |= PseudoStates.Root;
            if (LegacySelectorHelper.MatchRightToLeft(candidate, complexSelector))
                matched.Add(candidate);
            candidate.pseudoStates = prev;
        });
    }
}
