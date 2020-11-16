using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibrary : BuilderPaneContent, IBuilderSelectionNotifier
    {
        public enum BuilderLibraryTab
        {
            Standard,
            Project
        }

        public enum LibraryViewMode
        {
            IconTile,
            TreeView
        }

        const string k_UssClassName = "unity-builder-library";
        const string k_ContentContainerName = "content";

        readonly BuilderPaneWindow m_PaneWindow;
        readonly VisualElement m_DocumentElement;
        readonly BuilderSelection m_Selection;
        readonly BuilderLibraryDragger m_Dragger;
        readonly BuilderTooltipPreview m_TooltipPreview;

        readonly ToggleButtonStrip m_HeaderButtonStrip;
        readonly VisualElement m_LibraryContentContainer;

        BuilderLibraryTreeView m_ProjectTreeView;
        BuilderLibraryPlainView m_ControlsPlainView;
        BuilderLibraryTreeView m_ControlsTreeView;

        bool m_EditorExtensionMode;

        [SerializeField] bool m_ShowPackageTemplates;
        [SerializeField] LibraryViewMode m_ViewMode = LibraryViewMode.IconTile;
        [SerializeField] BuilderLibraryTab m_ActiveTab = BuilderLibraryTab.Standard;

        public BuilderLibrary(
            BuilderPaneWindow paneWindow, BuilderViewport viewport,
            BuilderSelection selection, BuilderLibraryDragger dragger,
            BuilderTooltipPreview tooltipPreview)
        {
            m_PaneWindow = paneWindow;
            m_DocumentElement = viewport.documentRootElement;
            m_Selection = selection;
            m_Dragger = dragger;
            m_TooltipPreview = tooltipPreview;

            viewDataKey = "unity-ui-builder-library";

            // Load styles.
            AddToClassList(k_UssClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + ".uss"));
            styleSheets.Add(EditorGUIUtility.isProSkin
                ? BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + "Dark.uss")
                : BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + "Light.uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUssPathNoExt + ".uxml");
            template.CloneTree(this);

            m_EditorExtensionMode = paneWindow.document.fileSettings.editorExtensionMode;
            m_LibraryContentContainer = this.Q<VisualElement>(k_ContentContainerName);

            m_HeaderButtonStrip = this.Q<ToggleButtonStrip>();
            m_HeaderButtonStrip.choices = Enum.GetNames(typeof(BuilderLibraryTab));
            m_HeaderButtonStrip.labels = new List<string> { BuilderConstants.LibraryStandardControlsTabName, BuilderConstants.LibraryProjectTabName };
            m_HeaderButtonStrip.RegisterValueChangedCallback(e =>
            {
                SwitchLibraryTab((BuilderLibraryTab)Enum.Parse(typeof(BuilderLibraryTab), e.newValue));
            });

            AddFocusable(m_HeaderButtonStrip);
            BuilderLibraryContent.RegenerateLibraryContent();
            BuilderLibraryContent.OnLibraryContentUpdated += RebuildView;
        }

        void SwitchLibraryTab(BuilderLibraryTab newTab)
        {
            m_ActiveTab = newTab;
            SaveViewData();
            RefreshView();
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
                return;

            pane.AppendActionToEllipsisMenu(BuilderConstants.LibraryShowPackageFiles,
                a => TogglePackageFilesVisibility(),
                a => m_ShowPackageTemplates
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu(BuilderConstants.LibraryViewModeToggle,
                a => SwitchControlsViewMode(),
                a => m_ViewMode == LibraryViewMode.TreeView
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu(BuilderConstants.LibraryEditorExtensionsAuthoring,
                a => ToggleEditorExtensionsAuthoring(),
                a => m_PaneWindow.document.fileSettings.editorExtensionMode
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        void ToggleEditorExtensionsAuthoring()
        {
            var newValue = !m_PaneWindow.document.fileSettings.editorExtensionMode;
            m_PaneWindow.document.fileSettings.editorExtensionMode = newValue;
            m_Selection.NotifyOfStylingChangePostStylingUpdate();
            SwitchLibraryTab(BuilderLibraryTab.Standard);

            if (newValue)
                Builder.ShowWarning(BuilderConstants.InspectorEditorExtensionAuthoringActivated);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            RefreshView();
        }

        public void OnAfterBuilderDeserialize()
        {
            RebuildView();
        }

        void TogglePackageFilesVisibility()
        {
            m_ShowPackageTemplates = !m_ShowPackageTemplates;
            SaveViewData();
            RebuildView();
        }

        internal void SetViewMode(LibraryViewMode viewMode)
        {
            if (m_ViewMode == viewMode)
                return;

            m_ViewMode = viewMode;
            SaveViewData();
            RefreshView();
        }

        void SwitchControlsViewMode()
        {
            SetViewMode(m_ViewMode == LibraryViewMode.IconTile
                ? LibraryViewMode.TreeView
                : LibraryViewMode.IconTile);
        }

        BuilderLibraryTreeView controlsTreeView
        {
            get
            {
                if (m_ControlsTreeView != null)
                    return m_ControlsTreeView;

                var controlsTree = m_EditorExtensionMode
                    ? BuilderLibraryContent.standardControlsTree
                    : BuilderLibraryContent.standardControlsTreeNoEditor;

                m_ControlsTreeView = new BuilderLibraryTreeView(controlsTree);
                m_ControlsTreeView.viewDataKey = "unity-ui-builder-library-controls-tree";
                SetUpLibraryView(m_ControlsTreeView);

                return m_ControlsTreeView;
            }
        }

        BuilderLibraryTreeView projectTreeView
        {
            get
            {
                if (m_ProjectTreeView != null)
                    return m_ProjectTreeView;

                var projectContentTree = m_ShowPackageTemplates
                    ? BuilderLibraryContent.projectContentTree
                    : BuilderLibraryContent.projectContentTreeNoPackages;

                m_ProjectTreeView = new BuilderLibraryTreeView(projectContentTree);
                m_ProjectTreeView.viewDataKey = "unity-ui-builder-library-project-view";
                SetUpLibraryView(m_ProjectTreeView);

                return m_ProjectTreeView;
            }
        }

        BuilderLibraryPlainView controlsPlainView
        {
            get
            {
                if (m_ControlsPlainView != null)
                    return m_ControlsPlainView;

                var controlsTree = m_EditorExtensionMode
                    ? BuilderLibraryContent.standardControlsTree
                    : BuilderLibraryContent.standardControlsTreeNoEditor;

                m_ControlsPlainView = new BuilderLibraryPlainView(controlsTree);
                m_ControlsPlainView.viewDataKey = "unity-ui-builder-library-controls-plane";
                SetUpLibraryView(m_ControlsPlainView);

                return m_ControlsPlainView;
            }
        }

        void SetUpLibraryView(BuilderLibraryView builderLibraryView)
        {
            builderLibraryView.SetupView(m_Dragger, m_TooltipPreview,
                this, m_PaneWindow,
                m_DocumentElement, m_Selection);
        }

        void RebuildView()
        {
            m_LibraryContentContainer.Clear();
            m_ProjectTreeView = null;
            m_ControlsPlainView = null;
            m_ControlsTreeView = null;

            RefreshView();
        }

        void RefreshView()
        {
            m_LibraryContentContainer.Clear();
            m_HeaderButtonStrip.SetValueWithoutNotify(m_ActiveTab.ToString());
            switch (m_ActiveTab)
            {
                case BuilderLibraryTab.Standard:
                    if (m_ViewMode == LibraryViewMode.TreeView)
                        SetActiveView(controlsTreeView);
                    else
                        SetActiveView(controlsPlainView);
                    break;

                case BuilderLibraryTab.Project:
                    SetActiveView(projectTreeView);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetActiveView(BuilderLibraryView builderLibraryView)
        {
            m_LibraryContentContainer.Add(builderLibraryView);
            builderLibraryView.Refresh();
            primaryFocusable = builderLibraryView.primaryFocusable;
        }

        public void ResetCurrentlyLoadedUxmlStyles()
        {
            RefreshView();
        }

        public void SelectionChanged() { }
        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType) { }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            if (m_EditorExtensionMode != m_PaneWindow.document.fileSettings.editorExtensionMode)
            {
                m_EditorExtensionMode = m_PaneWindow.document.fileSettings.editorExtensionMode;
                RebuildView();
            }
        }
    }
}
