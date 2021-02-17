using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIElements.Editor
{
    [InitializeOnLoad]
    internal static class UIDocumentHierarchyWatcher
    {
        private static int previousUIDocumentCount = 0;

        static UIDocumentHierarchyWatcher()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        static void OnHierarchyChanged()
        {
            if (EditorApplication.isPlaying)
            {
                // We only keep tabs in edit mode, in play mode we let the UIDocument instances
                // handle themselves as this will be the final result once they build.
                return;
            }

            var uiDocuments = Object.FindObjectsOfType<UIDocument>();

            // Early exit: no UIDocument to keep track of.
            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                previousUIDocumentCount = 0;
                return;
            }

            if (previousUIDocumentCount != 0 && previousUIDocumentCount != uiDocuments.Length)
            {
                // No previous uiDocument set, so nothing to fix (because new stuff gets itself set up on creation);
                // OR there's a new/enabled item OR a deleted/disabled item so they all handled themselves already.
                // Just store the info for the next changed event and be done.
                previousUIDocumentCount = uiDocuments.Length;
                return;
            }

            foreach (var uiDocument in uiDocuments)
            {
                uiDocument.ReactToHierarchyChanged();
            }
            previousUIDocumentCount = uiDocuments.Length;

            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
