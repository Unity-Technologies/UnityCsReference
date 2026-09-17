// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Turns a <see cref="RequestHighlightsCommand"/> — "show me where this is" — into the
/// <see cref="HighlightCommand"/> the windows actually draw from.
/// </summary>
/// <remarks>
/// <para>
/// One resolver for the whole editor, rather than one per thing that owns a panel. The same document can be on
/// screen several times at once — the UI Stage's authoring clone, the live scene panels rendering it, a UI
/// Viewport's preview of one of those — and a hover anywhere has to light it up everywhere. Every receiver
/// matches the elements it is handed by reference, so the answer is the union across every tracked panel.
/// </para>
/// <para>
/// Published once, which is the point: a receiver keeps only the most recent <see cref="HighlightCommand"/>, so
/// several owners each answering the same request would overwrite one another, and which one survived would
/// come down to registration order.
/// </para>
/// </remarks>
static partial class HighlightResolver
{
    [AutoStaticsCleanupOnCodeReload]
    static MatchedRulesExtractor s_RulesExtractor;

    [OnCodeInitializing]
    static void Bootstrap()
    {
        s_RulesExtractor = new MatchedRulesExtractor(AssetDatabase.GetAssetPath);
        UICommandQueue.RegisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        UICommandQueue.UnregisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
        s_RulesExtractor = null;
    }

    static void OnHighlightsRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        if (context.Command is not RequestHighlightsCommand command)
            return;

        var registry = VisualElementSelectionRegistry.Instance;
        if (registry == null)
            return;

        using var panelsHandle = ListPool<Panel>.Get(out var panels);
        registry.CollectTrackedPanels(panels);
        if (panels.Count == 0)
            return;

        using var elementSetHandle = HashSetPool<VisualElement>.Get(out var elementSet);
        using var ruleSetHandle = HashSetPool<StyleRule>.Get(out var ruleSet);

        // The elements the request itself names, as opposed to the ones a hovered rule happens to match. Only
        // these report their rules: a rule is answered with what it styles, not with every other rule that also
        // styles those same elements.
        using var namedHandle = HashSetPool<VisualElement>.Get(out var namedElements);

        // Added whatever panel it belongs to: the requester is showing it, so it is part of the answer even
        // when it lives somewhere nothing else tracks.
        if (command.Element != null)
            namedElements.Add(command.Element);

        foreach (var panel in panels)
        {
            var root = panel.visualTree;
            if (root == null)
                continue;

            // The same element, as this panel clones it. What lets a hover in the Hierarchy outline the box in
            // a viewport showing the same document, and the other way round.
            if (command.Element != null)
            {
                var counterpart = root.FindCorrespondingElement(command.Element);
                if (counterpart != null)
                    namedElements.Add(counterpart);
            }

            // Named by the UXML preview, which knows an id rather than an instance. Matched on the owning
            // document as well as the id, because ids are only unique within one — without it an unrelated UXML
            // that happened to number an element the same would be highlighted instead.
            if (command.ElementId.HasValue)
            {
                var byId = FindElement(root, command.ElementId.Value, command.ElementDocument);
                if (byId != null)
                    namedElements.Add(byId);
            }

            // Matching is purely structural, so a panel is only asked about a rule when its document is one the
            // rule's sheet actually styles — without that, a class name as ordinary as "header" would light up
            // every unrelated document in the scene that happens to use it.
            if (command.Rule != null && StylesPanel(panel, command.Rule))
            {
                foreach (var selector in command.Rule.complexSelectors)
                    HighlightUtility.GetMatchingElementsForSelector(root, selector, elementSet);
            }
        }

        elementSet.UnionWith(namedElements);

        // Reported even when it matches no element: the USS preview highlights the text of a hovered rule that
        // happens to style nothing.
        if (command.Rule != null)
            ruleSet.Add(command.Rule);

        AddMatchingRules(namedElements, ruleSet);

        if (elementSet.Count == 0 && ruleSet.Count == 0)
            return;

        HighlightCommand.Execute(command.Source, elementSet, ruleSet);
    }

    // Whether any document this panel renders references the sheet the rule lives in.
    static bool StylesPanel(Panel panel, StyleRule rule)
    {
        var sheet = rule.styleSheet;
        if (sheet == null)
            return false;

        using var _ = ListPool<VisualTreeAsset>.Get(out var documents);
        CollectPanelDocuments(panel, documents);

        using var __ = ListPool<StyleSheet>.Get(out var sheets);
        foreach (var document in documents)
        {
            sheets.Clear();
            UIAssetRegistry.CollectDocumentStyleSheets(document, sheets);
            foreach (var candidate in sheets)
            {
                if (ReferenceEquals(candidate, sheet))
                    return true;
            }
        }

        return false;
    }

    static void CollectPanelDocuments(Panel panel, List<VisualTreeAsset> results)
    {
        panel.visualTree.Query<VisualElement>().ForEach(element =>
        {
            var document = element.visualTreeAssetSource;
            if (document != null && !results.Contains(document))
                results.Add(document);
        });
    }

    static VisualElement FindElement(VisualElement root, int veaId, VisualTreeAsset document)
    {
        return root.Query().Where(e =>
        {
            var asset = e.visualElementAsset;
            return asset != null
                   && asset.id == veaId
                   && (document == null || asset.visualTreeAsset == document);
        }).First();
    }

    static void AddMatchingRules(HashSet<VisualElement> elementSet, HashSet<StyleRule> ruleSet)
    {
        foreach (var element in elementSet)
        {
            s_RulesExtractor.FindMatchingRules(element);
            foreach (var matchRecord in s_RulesExtractor.matchRecords)
            {
                var rule = matchRecord.complexSelector.rule;
                if (rule != null)
                    ruleSet.Add(rule);
            }

            s_RulesExtractor.Clear();
        }
    }
}
