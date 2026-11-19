// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

    AuthoringContext m_Context = AuthoringContext.Global;

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

    // The link tag carries the id alone, which only means something inside the document this preview exported.
    void RequestHighlightsForLink(string linkId)
    {
        if (!int.TryParse(linkId, out var id))
            return;

        HighlightUtility.RequestHighlights(m_Context, id, Asset, CommandSources.UxmlPreview);
    }

    protected override string GenerateCodePreview()
        => Asset ? Exporter.ToUxmlString(Asset, Options) : string.Empty;

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                m_Context = AuthoringContext.For(this);
                m_Context.Messages.RegisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;

            case DetachFromPanelEvent:
                m_Context.Messages.UnregisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;
            case PointerOverLinkTagEvent overTagEvent:
                RequestHighlightsForLink(overTagEvent.linkID);
                break;
            case PointerMoveLinkTagEvent overTagEvent:
                RequestHighlightsForLink(overTagEvent.linkID);
                break;
            case PointerOutLinkTagEvent:
                HighlightUtility.ClearHighlights(m_Context);
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
