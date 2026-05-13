// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UssCodePreview : CodePreview<StyleSheet>
{
    internal class StyleSheetHighlightExporter : StyleSheetExporter
    {
        readonly HashSet<StyleRule> m_HighlightedItems = new ();

        public HashSet<StyleRule> HighlightedItems => m_HighlightedItems;

        protected override void BeforeWritingRule(ref ExportContext ctx, StyleRule rule, int ruleIndex)
        {
            var isMarked = HighlightedItems?.Contains(rule) ?? false;
            if (isMarked)
            {
                ctx.Append("<mark=#");
                ctx.Append("FFFFFF30");
                ctx.Append(">");
            }

            ctx.Append($"<link={ruleIndex}>");
        }

        protected override void AfterWritingRule(ref ExportContext ctx, StyleRule rule, int ruleIndex)
        {
            ctx.Append("</link>");
            var isMarked = HighlightedItems?.Contains(rule) ?? false;
            if (isMarked)
            {
                ctx.Append("</mark>");
            }
        }
    }

    class Tracker(UssCodePreview owner) : LiveReloadStyleSheetAssetTracker, IAuthoringLiveReloadAssetTracker<StyleSheet>
    {
        public override void OnTrackedAssetChanged() => owner.Refresh();
    }

    internal readonly StyleSheetHighlightExporter Exporter = new ();

    StyleSheetExporter.UssExportOptions Options
    {
        get
        {
            var options = StyleSheetExporter.UssExportOptions.Default;
            options.useColorHighlighting = true;
            return options;
        }
    }

    protected override string GetTitle() => "USS Preview";
    protected override string GetExtension() => ".uss";
    protected override string PrefColorsCategory { get; } = "USS Syntax Highlighting";

    protected override IAuthoringLiveReloadAssetTracker<StyleSheet> CreateTracker() => new Tracker(this);
    protected override void RegisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        => liveReloadSystem.RegisterAuthoringTrackerForAsset(tracker, asset);

    protected override void UnregisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        => liveReloadSystem.UnregisterAuthoringTrackerForAsset(tracker, asset);

    protected override string GenerateCodePreview()
    {
        return Asset ? Exporter.ToUssString(Asset, Options) : string.Empty;
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                UICommandQueue.RegisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;

            case DetachFromPanelEvent:
                UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;
            case PointerOverLinkTagEvent overTagEvent:
            {
                var index = int.Parse(overTagEvent.linkID);
                var rule = Asset.rules[index];
                HighlightUtility.RequestHighlights(rule, CommandSources.UssPreview);
                break;
            }
            case PointerMoveLinkTagEvent overTagEvent:
            {
                var index = int.Parse(overTagEvent.linkID);
                var rule = Asset.rules[index];
                HighlightUtility.RequestHighlights(rule, CommandSources.UssPreview);
                break;
            }
            case PointerOutLinkTagEvent outTagEvent:
                HighlightUtility.ClearHighlights();
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    void OnHighlight(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        switch (context.Command)
        {
            case HighlightCommand highlightEvent:
                Exporter.HighlightedItems.Clear();
                if (highlightEvent.Rules != null)
                {
                    foreach (var rule in highlightEvent.Rules)
                    {
                        Exporter.HighlightedItems.Add(rule);
                    }
                }
                Refresh();
                break;
            default:
                break;
        }
    }
}
