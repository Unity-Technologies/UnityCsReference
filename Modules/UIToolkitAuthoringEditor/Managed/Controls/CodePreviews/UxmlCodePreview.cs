// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
sealed partial class UxmlCodePreview : CodePreview<VisualTreeAsset>
{
    internal class VisualTreeAssetHighlightExporter : VisualTreeAssetExporter
    {
        readonly HashSet<UxmlAsset> m_HighlightedItems = new ();

        public HashSet<UxmlAsset> HighlightedItems => m_HighlightedItems;

        protected override void BeforeWriteUxmlAssetTag(ref ExportContext ctx, UxmlAsset asset)
        {
            var isMarked = HighlightedItems?.Contains(asset) ?? false;
            if (isMarked)
            {
                ctx.Append("<mark=#");
                ctx.Append("FFFFFF30");
                ctx.Append(">");
            }
            ctx.Append($"<link={asset.id}>");
        }

        protected override void AfterWriteUxmlAssetTag(ref ExportContext ctx, UxmlAsset asset)
        {
            ctx.Append("</link>");
            var isMarked = HighlightedItems?.Contains(asset) ?? false;
            if (isMarked)
                ctx.Append("</mark>");
        }
    }

    class Tracker(UxmlCodePreview owner)
        : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
    {
        internal override void OnVisualTreeAssetChanged() => owner.Refresh();
    }

    internal readonly VisualTreeAssetHighlightExporter Exporter = new();

    VisualTreeAssetExporter.ExportOptions Options
    {
        get
        {
            var options = VisualTreeAssetExporter.ExportOptions.Default;
            options.useColorHighlighting = true;
            options.styleExporterOptions = StyleExporterOptions;
            return options;
        }
    }

    StyleSheetExporter.UssExportOptions StyleExporterOptions
    {
        get
        {
            var options = StyleSheetExporter.UssExportOptions.Default;
            options.useColorHighlighting = false;
            return options;
        }
    }

    protected override string GetTitle() => "UXML Preview";
    protected override string GetExtension() => ".uxml";
    protected override string PrefColorsCategory { get; } = "UXML Syntax Highlighting";

    protected override IAuthoringLiveReloadAssetTracker<VisualTreeAsset> CreateTracker() => new Tracker(this);
    protected override void RegisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        => liveReloadSystem.RegisterAuthoringTrackerForAsset(tracker, asset);

    protected override void UnregisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        => liveReloadSystem.UnregisterAuthoringTrackerForAsset(tracker, asset);

    protected override string GenerateCodePreview()
        => Asset ? Exporter.ToUxmlString(Asset, Options) : string.Empty;

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
                var id = int.Parse(overTagEvent.linkID);
                HighlightUtility.RequestHighlights(id, CommandSources.UxmlPreview);
                break;
            }
            case PointerMoveLinkTagEvent overTagEvent:
            {
                var id = int.Parse(overTagEvent.linkID);
                HighlightUtility.RequestHighlights(id, CommandSources.UxmlPreview);
                break;
            }
            case PointerOutLinkTagEvent:
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
                if (highlightEvent.Elements != null)
                {
                    foreach (var element in highlightEvent.Elements)
                    {
                        Exporter.HighlightedItems.Add(element.visualElementAsset);
                    }
                }
                Refresh();
                break;
            default:
                break;
        }
    }
}
