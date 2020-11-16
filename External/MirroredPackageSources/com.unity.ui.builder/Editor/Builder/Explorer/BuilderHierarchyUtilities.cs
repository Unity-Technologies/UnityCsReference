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
            paneWindow.document.AddSubDocument(vea);
            paneWindow.LoadDocument(vta, false);

            return true;
        }
    }
}
