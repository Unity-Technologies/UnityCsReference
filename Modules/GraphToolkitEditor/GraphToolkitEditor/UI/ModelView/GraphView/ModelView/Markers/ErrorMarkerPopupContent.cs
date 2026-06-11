// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

class ErrorMarkerPopupContent : VisualElement
{
    internal const string k_EntriesContainer = "entries-container";
    internal const string k_CurrentGraphErrorsName = "current-graph-errors";
    internal const string k_SubgraphSummaryContainerName = "subgraph-summary-container";
    internal const string k_SubgraphSummaryLabelNamePrefix = "subgraph-summary-";

    const string k_BottomContainer = "bottom-container";

    const string k_USSClassName = "ge-error-marker-popup-window";
    static readonly string k_EntriesContainerUssClassName = k_USSClassName.WithUssElement(k_EntriesContainer);
    static readonly string k_ErrorListUssClassName = k_USSClassName.WithUssElement("error-list");
    static readonly string k_SubgraphSummaryContainerUssClassName = k_USSClassName.WithUssElement("subgraph-summary-container");
    static readonly string k_SubgraphSummaryEntryUssClassName = k_USSClassName.WithUssElement("subgraph-summary-entry");
    static readonly string k_SubgraphSummaryIconUssClassName = k_SubgraphSummaryEntryUssClassName.WithUssElement("icon");
    static readonly string k_SubgraphSummaryLabelUssClassName = k_SubgraphSummaryEntryUssClassName.WithUssElement("label");

    const string k_StylesheetName = "Markers/ErrorMarkerPopupWindow.uss";
    const string k_UnityThemeEnvVariableStyleClassName = "unity-theme-env-variables";
    const string k_ErrorMarkerPopupThemeClassName = "ge-error-marker-popup-theme";

    GraphView m_GraphView;

    public event Action CloseRequest;

    public ErrorMarkerPopupContent(ErrorMarkerPopupModel model, GraphView graphView)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        AddToClassList(k_USSClassName);
        AddToClassList(k_UnityThemeEnvVariableStyleClassName);
        AddToClassList(k_ErrorMarkerPopupThemeClassName);
        this.AddStylesheetWithSkinVariants(k_StylesheetName);

        m_GraphView = graphView;

        var scrollView = new ScrollView { name = k_EntriesContainer };
        scrollView.AddToClassList(k_EntriesContainerUssClassName);
        scrollView.style.flexGrow = 1;
        Add(scrollView);

        // Add current graph errors
        if (model.CurrentGraphErrors.Count > 0)
        {
            var currentGraphList = CreateErrorList(model.CurrentGraphErrors, k_CurrentGraphErrorsName);
            scrollView.Add(currentGraphList);
        }

        // Add subgraph error summaries
        if (model.SubgraphErrors.Count > 0)
        {
            var summaryContainer = new VisualElement { name = k_SubgraphSummaryContainerName };
            summaryContainer.AddToClassList(k_SubgraphSummaryContainerUssClassName);
            scrollView.Add(summaryContainer);

            foreach (var subgraphGroup in model.SubgraphErrors)
            {
                if (subgraphGroup.Count > 0)
                {
                    var summaries = CreateSubgraphSummaryLabels(subgraphGroup);
                    foreach (var summary in summaries)
                    {
                        summaryContainer.Add(summary);
                    }
                }
            }
        }

        var bottomContainer = new VisualElement
        {
            name = k_BottomContainer,
            style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd }
        };
        Add(bottomContainer);
    }

    VisualElement CreateErrorList(List<ErrorMarkerModel> errors, string listName)
    {
        var list = new VisualElement { name = listName };
        list.AddToClassList(k_ErrorListUssClassName);
        foreach (var errorModel in errors)
        {
            list.Add(new MarkerEntry(errorModel, OnActionClicked));
        }

        return list;
    }

    List<VisualElement> CreateSubgraphSummaryLabels(ErrorMarkerPopupModel.SubgraphErrorGroup subgraphGroup)
    {
        var entries = new List<VisualElement>();
        var navigationTarget = subgraphGroup.NavigationTarget;
        EventCallback<ClickEvent> clickCallback =
            navigationTarget == null ? null : _ => OnNavigateClicked(navigationTarget);

        var errorEntry = TryCreateSubgraphSummaryEntry(subgraphGroup.GraphPath, subgraphGroup.Errors.Count,
            ErrorMarkerUtilities.GetLogString(LogType.Error, false), clickCallback);
        if (errorEntry != null)
            entries.Add(errorEntry);

        var warningEntry = TryCreateSubgraphSummaryEntry(subgraphGroup.GraphPath, subgraphGroup.Warnings.Count,
            ErrorMarkerUtilities.GetLogString(LogType.Warning, false), clickCallback);
        if (warningEntry != null)
            entries.Add(warningEntry);

        var infoEntry = TryCreateSubgraphSummaryEntry(subgraphGroup.GraphPath, subgraphGroup.Infos.Count,
            ErrorMarkerUtilities.GetLogString(LogType.Log, false), clickCallback);
        if (infoEntry != null)
            entries.Add(infoEntry);

        return entries;
    }

    static VisualElement TryCreateSubgraphSummaryEntry(string graphPath, int count, string logString,
        EventCallback<ClickEvent> clickCallback)
    {
        if (count == 0)
            return null;

        var severityKey = logString.ToLower();

        var entry = new VisualElement { name = $"{k_SubgraphSummaryLabelNamePrefix}{graphPath}-{severityKey}" };
        entry.AddToClassList(k_SubgraphSummaryEntryUssClassName);
        entry.AddToClassList(k_SubgraphSummaryEntryUssClassName.WithUssModifier(severityKey));

        var icon = new VisualElement();
        icon.AddToClassList(k_SubgraphSummaryIconUssClassName);
        entry.Add(icon);

        var label = new Label(ErrorMarkerUtilities.GetSubgraphMessageString(graphPath, count, logString));
        label.AddToClassList(k_SubgraphSummaryLabelUssClassName);
        entry.Add(label);

        if (clickCallback != null)
            entry.RegisterCallback(clickCallback);

        return entry;
    }

    void OnNavigateClicked(ErrorMarkerModel model)
    {
        if (model is GraphProcessingErrorModel error)
            ErrorMarkerUtilities.LoadGraphAndFrameElement(error,  m_GraphView);
        CloseRequest?.Invoke();
    }

    void OnActionClicked(ErrorMarkerModel model, object target)
    {
        if (model?.Action != null && target != null)
        {
            model.Action.Action(target);
        }

        CloseRequest?.Invoke();
    }
}
