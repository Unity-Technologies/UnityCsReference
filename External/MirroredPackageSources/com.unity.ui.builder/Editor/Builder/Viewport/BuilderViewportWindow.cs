using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderViewportWindow : BuilderPaneWindow, IBuilderViewportWindow
    {
        BuilderSelection m_Selection;

        BuilderToolbar m_Toolbar;
        BuilderViewport m_Viewport;

        public BuilderSelection selection => m_Selection;

        public BuilderViewport viewport => m_Viewport;
        public VisualElement documentRootElement => m_Viewport.documentRootElement;
        public BuilderCanvas canvas => m_Viewport.canvas;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " Viewport")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderViewportWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder Viewport");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, selection, null);
            selection.documentRootElement = m_Viewport.documentRootElement;

            // Create the rest of the panes.
            m_Toolbar = new BuilderToolbar(this, selection, m_Viewport, null, null, null, null);
            root.Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer,
                m_Viewport.selectionIndicator
            });

            // Command Handler
            commandHandler.RegisterPane(m_Viewport);
            commandHandler.RegisterToolbar(m_Toolbar);

            OnEnableAfterAllSerialization();
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            document.OnAfterBuilderDeserialize(m_Viewport.documentRootElement);
            m_Toolbar.OnAfterBuilderDeserialize();

            // Restore selection.
            selection.RestoreSelectionFromDocument(m_Viewport.sharedStylesAndDocumentElement);

            // We claim the change is coming from the Document because we don't
            // want the document hasUnsavedChanges flag to be set at this time.
            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);
        }

        public override void LoadDocument(VisualTreeAsset asset, bool unloadAllSubDocuments = true)
        {
            m_Toolbar.LoadDocument(asset, unloadAllSubDocuments);
        }
    }
}
