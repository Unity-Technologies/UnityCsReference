// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchyWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderHierarchyWindow() {}
        #pragma warning restore UAL0015

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
