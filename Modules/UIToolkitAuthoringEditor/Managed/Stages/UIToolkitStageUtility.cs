// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class UIToolkitStageUtility
{
    /// <summary>
    /// Asks for the elements of <paramref name="assets"/> to be selected in <paramref name="context"/> once they
    /// exist.
    /// </summary>
    /// <remarks>
    /// An asset does not name an element on its own: a document instantiated twice clones each of its assets
    /// twice. Any caller that knows which of those instances it just edited has to follow this with
    /// <see cref="ScopePendingSelectionRequestsTo(AuthoringContext, VisualElement)"/>, or the selection lands on
    /// whichever clone comes first.
    /// </remarks>
    public static void RequestSelectionOnNextUpdate(AuthoringContext context, IList<VisualElementAsset> assets)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.RequestSelectionOnNextUpdate(new List<VisualElementAsset>(assets));
    }

    /// <inheritdoc cref="VisualElementNodeHandler.RequestRenameOfPendingSelection"/>
    public static void RequestRenameOfPendingSelection(AuthoringContext context)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.RequestRenameOfPendingSelection();
    }

    /// <inheritdoc cref="VisualElementNodeHandler.RequestFocusOfPendingSelection"/>
    public static void RequestFocusOfPendingSelection(AuthoringContext context, BindingId attributePath, IPanel inspectorPanel)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.RequestFocusOfPendingSelection(attributePath, inspectorPanel);
    }

    /// <inheritdoc cref="VisualElementNodeHandler.ScopePendingSelectionRequestsTo(VisualElement)"/>
    public static void ScopePendingSelectionRequestsTo(AuthoringContext context, VisualElement parent)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.ScopePendingSelectionRequestsTo(parent);
    }

    /// <inheritdoc cref="VisualElementNodeHandler.ScopePendingSelectionRequestsTo(IReadOnlyList{VisualElement})"/>
    public static void ScopePendingSelectionRequestsTo(AuthoringContext context, IReadOnlyList<VisualElement> parents)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.ScopePendingSelectionRequestsTo(parents);
    }

    /// <summary>
    /// The handler holding the visual element rows of <paramref name="context"/>. For
    /// <see cref="AuthoringContext.Global"/>, the one of the first hierarchy window that has one.
    /// </summary>
    /// <remarks>
    /// Only the first window is used: the handler changes the editor selection, which every other hierarchy
    /// mirrors. Windows are skipped rather than stopped at, because <see cref="Resources.FindObjectsOfTypeAll"/>
    /// also returns windows that were never shown in this session — those have no hierarchy of their own yet, let
    /// alone handlers on it, and stopping at one would drop the request.
    /// </remarks>
    static bool TryGetElementHandler(AuthoringContext context, out VisualElementNodeHandler handler)
    {
        handler = null;

        if (!context.IsGlobal)
        {
            handler = context.ElementHandler;
            return handler != null;
        }

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
    public static void RequestSelectionOnNextUpdate(AuthoringContext context, VisualElement originElement,
        VisualTreeAsset stageDocument)
    {
        if (TryGetElementHandler(context, out var handler))
            handler.RequestSelectionOnNextUpdate(originElement, stageDocument);
    }

    /// <summary>
    /// Whether the Main Stage is the stage being displayed. Any other stage — a prefab stage, the UI Stage —
    /// shows its own content instead.
    /// </summary>
    public static bool IsAuthoringActiveInMainStage => StageUtility.GetCurrentStage() is MainStage;

    public static VisualElementEditFlags GetMainStageEditFlags(VisualElement element) =>
        element?.visualElementAsset != null
            ? VisualElementEditFlags.FullyEditable
            : VisualElementEditFlags.None;
}
