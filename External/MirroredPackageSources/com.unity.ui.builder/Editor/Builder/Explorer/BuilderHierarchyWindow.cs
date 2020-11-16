using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchyWindow : BuilderPaneWindow
    {
        BuilderHierarchy m_HierarchyPane;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " Hierarchy")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderHierarchyWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder Hierarchy");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;
            var viewport = viewportWindow.viewport;

            var classDragger = new BuilderClassDragger(this, root, selection, viewport, viewport.parentTracker);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, viewport, viewport.parentTracker);
            var contextMenuManipulator = new BuilderElementContextMenu(this, selection);

            m_HierarchyPane = new BuilderHierarchy(this, viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator, null);

            selection.AddNotifier(m_HierarchyPane);

            root.Add(m_HierarchyPane);

            // Command Handler
            commandHandler.RegisterPane(m_HierarchyPane);
        }

        public override void ClearUI()
        {
            if (m_HierarchyPane == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection != null)
                selection.RemoveNotifier(m_HierarchyPane);

            m_HierarchyPane.RemoveFromHierarchy();
            m_HierarchyPane = null;
        }
    }
}
