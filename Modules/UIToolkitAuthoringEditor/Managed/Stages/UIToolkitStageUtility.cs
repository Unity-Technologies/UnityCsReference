// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class UIToolkitStageUtility
{
    public static void RequestSelectionOnNextUpdate(IList<VisualElementAsset> assets)
    {
        if (TryGetElementHandler(out var handler))
            handler.RequestSelectionOnNextUpdate(new List<VisualElementAsset>(assets));
    }

    /// <inheritdoc cref="VisualElementNodeTypeHandler.RequestRenameOfPendingSelection"/>
    public static void RequestRenameOfPendingSelection()
    {
        if (TryGetElementHandler(out var handler))
            handler.RequestRenameOfPendingSelection();
    }

    // Since the handler will change selection, all Hierarchy will eventually react to it, so one window is enough.
    // Each window owns its own Hierarchy and handler, and a rename the request carries only fires in the window the
    // user last interacted with, so that window's handler is preferred over an arbitrary one.
    static bool TryGetElementHandler(out VisualElementNodeTypeHandler handler)
    {
        if (TryGetElementHandler(HierarchyWindow.LastInteractedWindow, out handler))
            return true;

        var windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
        // Nothing to update if no hierarchy windows are opened.
        if (windows == null)
            return false;

        foreach (var window in windows)
        {
            if (TryGetElementHandler(window, out handler))
                return true;
        }

        return false;
    }

    static bool TryGetElementHandler(HierarchyWindow window, out VisualElementNodeTypeHandler handler)
    {
        handler = null;
        if (!window)
            return false;

        foreach (var h in window.Hierarchy.EnumerateNodeTypeHandlersBase())
        {
            if (h is not VisualElementNodeTypeHandler)
                continue;
            handler =
                (VisualElementNodeTypeHandler)window.Hierarchy.GetNodeTypeHandlerBase(VisualElementNodeTypeHandler
                    .NodeTypeName);
            return handler != null;
        }

        return false;
    }
}
