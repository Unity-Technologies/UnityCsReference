using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderHierarchyUtilities
    {
        public static bool OpenAsSubDocument(BuilderPaneWindow paneWindow, VisualTreeAsset vta, TemplateAsset vea = null)
        {
            bool didSaveChanges = paneWindow.document.CheckForUnsavedChanges();
            if (!didSaveChanges)
                return false;

            // This is important because if the user chose to not save changes to the
            // parent document, we restore the VTA from backup. The problem with that
            // is that the backup VTA was made before we fixed any USS assignments on
            // root elements. This is fine when simply restoring the backup before a
            // File > New or switching documents (just prior to closing the current document),
            // but this is not ok here because we need the parent document to continue
            // staying open and usable in the UI Builder.
            paneWindow.document.activeOpenUXMLFile.PostLoadDocumentStyleSheetCleanup();

            paneWindow.document.AddSubDocument(vea);
            paneWindow.LoadDocument(vta, false);

            return true;
        }
    }
}
