// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class UIToolkitStageUtility
{
    /// <summary>
    /// Asks for the elements of <paramref name="assets"/> to be selected once they exist.
    /// </summary>
    /// <remarks>
    /// An asset does not name an element on its own: a document instantiated twice clones each of its assets
    /// twice. Any caller that knows which of those instances it just edited has to follow this with
    /// <see cref="ScopePendingSelectionRequestsTo"/>, or the selection lands on whichever clone comes first.
    /// </remarks>
    public static void RequestSelectionOnNextUpdate(IList<VisualElementAsset> assets)
    {
        if (TryGetElementHandler(out var handler))
            handler.RequestSelectionOnNextUpdate(new List<VisualElementAsset>(assets));
    }

    /// <inheritdoc cref="VisualElementNodeHandler.ScopePendingSelectionRequestsTo"/>
    public static void ScopePendingSelectionRequestsTo(VisualElement parent)
    {
        if (TryGetElementHandler(out var handler))
            handler.ScopePendingSelectionRequestsTo(parent);
    }

    /// <inheritdoc cref="VisualElementNodeHandler.ScopePendingSelectionRequestsTo(IReadOnlyList{VisualElement})"/>
    public static void ScopePendingSelectionRequestsTo(IReadOnlyList<VisualElement> parents)
    {
        if (TryGetElementHandler(out var handler))
            handler.ScopePendingSelectionRequestsTo(parents);
    }

    /// <summary>
    /// The handler holding the visual element rows, taken from the first hierarchy window that has one.
    /// </summary>
    /// <remarks>
    /// Only the first is used: the handler changes the editor selection, which every other hierarchy mirrors.
    /// Windows are skipped rather than stopped at, because <see cref="Resources.FindObjectsOfTypeAll"/> also
    /// returns windows that were never shown in this session — those have no hierarchy of their own yet, let
    /// alone handlers on it, and stopping at one would drop the request.
    /// </remarks>
    static bool TryGetElementHandler(out VisualElementNodeHandler handler)
    {
        handler = null;

        var windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
        if (windows == null || windows.Length == 0)
            return false;

        foreach (var window in windows)
        {
            if (!window)
                continue;

            if (window.Hierarchy is not { IsCreated: true } hierarchy)
                continue;

            if (hierarchy.GetNodeTypeHandlerBase<VisualElementNodeHandler>() is not { } found)
                continue;

            handler = found;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Asks for the clone of <paramref name="originElement"/> to be selected in a stage rooted at
    /// <paramref name="stageDocument"/>. Call right after entering the stage to carry a Main Stage
    /// selection into it; repeated instances resolve to the carried one.
    /// </summary>
    public static void RequestSelectionOnNextUpdate(VisualElement originElement, VisualTreeAsset stageDocument)
    {
        if (TryGetElementHandler(out var handler))
            handler.RequestSelectionOnNextUpdate(originElement, stageDocument);
    }

    /// <summary>
    /// Whether the settings allow authoring the UI of the loaded scenes. This says nothing about which stage is
    /// currently displayed — see <see cref="IsAuthoringActiveInMainStage"/> for that.
    /// </summary>
    public static bool IsAuthoringEnabledInMainStage =>
        UIToolkitAuthoringSettings.EnableInSceneUIAuthoring && UIToolkitAuthoringSettings.EnableMainStageAuthoring;

    /// <summary>
    /// Whether Main Stage authoring is enabled <em>and</em> in effect right now, i.e. the Main Stage is the
    /// stage being displayed. Any other stage — a prefab stage, the UI Stage — shows its own content, which
    /// these settings have no say over.
    /// </summary>
    public static bool IsAuthoringActiveInMainStage =>
        IsAuthoringEnabledInMainStage && StageUtility.GetCurrentStage() is MainStage;

    public static VisualElementEditFlags GetMainStageEditFlags(VisualElement element) =>
        IsAuthoringEnabledInMainStage && element?.visualElementAsset != null
            ? VisualElementEditFlags.FullyEditable
            : VisualElementEditFlags.None;

    public static VisualElementEditFlags GetEditFlags(VisualElement element)
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
            return stage.Context.GetElementEditFlags(element);
        return GetMainStageEditFlags(element);
    }
}
