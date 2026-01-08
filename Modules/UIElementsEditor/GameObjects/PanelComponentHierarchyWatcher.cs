// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIElements.Editor
{
    [InitializeOnLoad]
    internal static class PanelComponentHierarchyWatcher
    {
        private static int previousUIDocumentCount = 0;
        private static int previousPanelRendererCount = 0;

        static PanelComponentHierarchyWatcher()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        static void UpdateUIDocument(UIDocument doc) => doc.ReactToHierarchyChanged();
        static void UpdateUIDocument(PanelRenderer pr) => pr.RefreshAssets();

        static void OnHierarchyChanged()
        {
            if (EditorApplication.isPlaying)
            {
                // We only keep tabs in edit mode, in play mode we let the panel component instances
                // handle themselves as this will be the final result once they build.
                return;
            }

            bool updatedUIDocuments = UpdatePanelComponents<UIDocument>(ref previousUIDocumentCount, UpdateUIDocument);
            bool updatedRenderers = UpdatePanelComponents<PanelRenderer>(ref previousPanelRendererCount, UpdateUIDocument);

            if (updatedUIDocuments || updatedRenderers)
                EditorApplication.QueuePlayerLoopUpdate();
        }

        static bool UpdatePanelComponents<T>(ref int previousUpdateCount, Action<T> updateAction) where T : Object, IPanelComponent
        {
            var panelComponents = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

            // Early exit: no UIDocument to keep track of.
            if (panelComponents == null || panelComponents.Length == 0)
            {
                previousUpdateCount = 0;
                return false;
            }

            if (previousUpdateCount != 0 && previousUpdateCount != panelComponents.Length)
            {
                // No previous component set, so nothing to fix (because new stuff gets itself set up on creation);
                // OR there's a new/enabled item OR a deleted/disabled item so they all handled themselves already.
                // Just store the info for the next changed event and be done.
                previousUpdateCount = panelComponents.Length;
                return false;
            }

            foreach (var c in panelComponents)
            {
                updateAction(c);
            }

            return true;
        }
    }
}
