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
        var windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
        // Nothing to update if no hierarchy windows are opened.
        if (windows == null || windows.Length == 0 )
            return;

        foreach (var window in windows)
        {
            if (!window)
                continue;
            foreach (var h in window.Hierarchy.EnumerateNodeTypeHandlersBase())
            {
                if (h is not VisualElementNodeTypeHandler)
                    continue;
                var handler =
                    (VisualElementNodeTypeHandler)window.Hierarchy.GetNodeTypeHandlerBase(VisualElementNodeTypeHandler
                        .NodeTypeName);
                if (handler == null)
                    return;
                handler.RequestSelectionOnNextUpdate(new List<VisualElementAsset>(assets));
            }

            // Since the handler will change selection, all Hierarchy will eventually react to it, so only process the first window encountered.
            return;
        }
    }
}
