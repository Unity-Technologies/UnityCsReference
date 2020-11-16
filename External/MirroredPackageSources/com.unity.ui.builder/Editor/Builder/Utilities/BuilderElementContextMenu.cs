using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderElementContextMenu
    {
        readonly BuilderPaneWindow m_PaneWindow;
        readonly BuilderSelection m_Selection;

        bool m_WeStartedTheDrag;

        List<ManipulatorActivationFilter> activators { get; }
        ManipulatorActivationFilter m_CurrentActivator;

        protected BuilderDocument document => m_PaneWindow.document;
        protected BuilderPaneWindow paneWindow => m_PaneWindow;
        protected BuilderSelection selection => m_Selection;

        public BuilderElementContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;

            m_WeStartedTheDrag = false;

            activators = new List<ManipulatorActivationFilter>();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        public void RegisterCallbacksOnTarget(VisualElement target)
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.target as VisualElement;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            var target = evt.currentTarget as VisualElement;
            target.CaptureMouse();
            m_WeStartedTheDrag = true;
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            var target = evt.currentTarget as VisualElement;

            if (!target.HasMouseCapture() || !m_WeStartedTheDrag)
                return;

            if (!CanStopManipulation(evt))
                return;
            
            DisplayContextMenu(evt, target);

            target.ReleaseMouse();
            m_WeStartedTheDrag = false;
            evt.StopPropagation();
        }

        public void DisplayContextMenu(EventBase triggerEvent, VisualElement target)
        {
            if (target.elementPanel?.contextualMenuManager != null)
            {
                target.elementPanel.contextualMenuManager.DisplayMenu(triggerEvent, target);
                triggerEvent.PreventDefault();
            }
        }

        bool CanStartManipulation(IMouseEvent evt)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(evt))
                {
                    m_CurrentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        bool CanStopManipulation(IMouseEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            return ((MouseButton)evt.button == m_CurrentActivator.button);
        }

        void ReselectIfNecessary(VisualElement documentElement)
        {
            if (!m_Selection.selection.Contains(documentElement))
                m_Selection.Select(null, documentElement);
        }

        public virtual void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            var documentElement = target.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;

            var linkedOpenVTA = documentElement?.GetProperty(BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName) as VisualTreeAsset;

            var isValidTarget = documentElement != null && !linkedOpenVTA &&
                                (documentElement.IsPartOfActiveVisualTreeAsset(paneWindow.document) ||
                                 documentElement.GetStyleComplexSelector() != null);
            var isValidCopyTarget = documentElement != null && !linkedOpenVTA &&
                                    (documentElement.IsPartOfCurrentDocument() ||
                                     documentElement.GetStyleComplexSelector() != null);
            evt.StopImmediatePropagation();

            evt.menu.AppendAction(
                "Copy",
                a =>
                {
                    ReselectIfNecessary(documentElement);
                    if (isValidCopyTarget)
                        m_PaneWindow.commandHandler.CopySelection();
                },
                isValidCopyTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Paste",
                a =>
                {
                    m_PaneWindow.commandHandler.Paste();
                },
                string.IsNullOrEmpty(BuilderEditorUtility.systemCopyBuffer)
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Rename",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    var explorerItemElement = documentElement?.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                    if (explorerItemElement == null)
                        return;

                    explorerItemElement.ActivateRenameElementMode();

                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Duplicate",
                a =>
                {
                    ReselectIfNecessary(documentElement);
                    m_PaneWindow.commandHandler.DuplicateSelection();
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Delete",
                a =>
                {
                    ReselectIfNecessary(documentElement);
                    m_PaneWindow.commandHandler.DeleteSelection();
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            var linkedInstancedVTA = documentElement?.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;
            var linkedVEA = documentElement?.GetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName) as TemplateAsset;
            var activeOpenUXML = document.activeOpenUXMLFile;

            var isLinkedOpenVTAActiveVTA = linkedOpenVTA == activeOpenUXML.visualTreeAsset;
            var isLinkedInstancedVTAActiveVTA = linkedInstancedVTA == activeOpenUXML.visualTreeAsset;
            var isLinkedVEADirectChild = activeOpenUXML.visualTreeAsset.templateAssets.Contains(linkedVEA);

            var showOpenInBuilder = linkedInstancedVTA != null;
            var showReturnToParentAction = isLinkedOpenVTAActiveVTA && activeOpenUXML.isChildSubDocument;
            var showOpenInIsolationAction = isLinkedVEADirectChild;
            var showOpenInPlaceAction = showOpenInIsolationAction;
            var showSiblingOpenActions = !isLinkedOpenVTAActiveVTA && isLinkedInstancedVTAActiveVTA;

            if (showOpenInBuilder || showReturnToParentAction || showOpenInIsolationAction || showOpenInPlaceAction || showSiblingOpenActions)
                evt.menu.AppendSeparator();

            if (showOpenInBuilder)
            {
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyOpenInBuilder,
                    action => { paneWindow.LoadDocument(linkedInstancedVTA); });
            }

            if (showReturnToParentAction)
            {
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyReturnToParentDocument +
                    BuilderConstants.SingleSpace + "(" + activeOpenUXML.openSubDocumentParent.visualTreeAsset.name + ")",
                    action => document.GoToSubdocument(documentElement, paneWindow, activeOpenUXML.openSubDocumentParent));
            }

            if (showOpenInIsolationAction)
            {
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyPaneOpenSubDocument,
                    action => BuilderHierarchyUtilities.OpenAsSubDocument(paneWindow, linkedInstancedVTA));
            }

            if (showOpenInPlaceAction)
            {
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyPaneOpenSubDocumentInPlace,
                    action => BuilderHierarchyUtilities.OpenAsSubDocument(paneWindow, linkedInstancedVTA, linkedVEA));
            }

            if (showSiblingOpenActions)
            {
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyPaneOpenSubDocument,
                    action =>
                    {
                        document.GoToSubdocument(documentElement, paneWindow, activeOpenUXML.openSubDocumentParent);
                        BuilderHierarchyUtilities.OpenAsSubDocument(paneWindow, linkedInstancedVTA);
                    });

                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyPaneOpenSubDocumentInPlace,
                    action =>
                    {
                        document.GoToSubdocument(documentElement, paneWindow, activeOpenUXML.openSubDocumentParent);
                        BuilderHierarchyUtilities.OpenAsSubDocument(paneWindow, linkedInstancedVTA, linkedVEA);
                    });
            }

        }
    }
}
