using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.StyleSheets;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderToolbar m_Toolbar;
        BuilderSelection m_Selection;

        List<VisualElement> m_CutElements = new List<VisualElement>();

        List<BuilderPaneContent> m_Panes = new List<BuilderPaneContent>();

        bool m_ControlWasPressed;
        IVisualElementScheduledItem m_ControlUnpressScheduleItem;

        public BuilderCommandHandler(
            BuilderPaneWindow paneWindow,
            BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Toolbar = null;
            m_Selection = selection;
        }

        public void OnEnable()
        {
            var root = m_PaneWindow.rootVisualElement;
            root.focusable = true; // We want commands to work anywhere in the builder.

            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.RegisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                // Make sure Delete key works on Mac keyboards.
                pane.primaryFocusable.RegisterCallback<KeyDownEvent>(OnDelete);
            }

            // Ctrl+S to save.
            m_PaneWindow.rootVisualElement.RegisterCallback<KeyUpEvent>(OnSaveDocument);
            m_ControlUnpressScheduleItem = m_PaneWindow.rootVisualElement.schedule.Execute(UnsetControlFlag);

            // Undo/Redo
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                pane.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);
            }

            m_PaneWindow.rootVisualElement.UnregisterCallback<KeyUpEvent>(OnSaveDocument);

            // Undo/Redo
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void RegisterPane(BuilderPaneContent paneContent)
        {
            m_Panes.Add(paneContent);
        }

        public void RegisterToolbar(BuilderToolbar toolbar)
        {
            m_Toolbar = toolbar;
        }

        public void OnCommandValidate(ValidateCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Cut: evt.StopPropagation(); return;
                case EventCommandNames.Copy: evt.StopPropagation(); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: evt.StopPropagation(); return;
                case EventCommandNames.Duplicate: evt.StopPropagation(); return;
                case EventCommandNames.Paste: evt.StopPropagation(); return;
                case EventCommandNames.Rename: evt.StopPropagation(); return;
            }
        }

        public void OnCommandExecute(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Cut: CutSelection(); return;
                case EventCommandNames.Copy: CopySelection(); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: DeleteSelection(); return;
                case EventCommandNames.Duplicate: DuplicateSelection(); return;
                case EventCommandNames.Paste: Paste(); return;
                case EventCommandNames.Rename: RenameSelection(); return;
            }
        }

        void OnUndoRedo()
        {
            m_PaneWindow.OnEnableAfterAllSerialization();
        }

        void UnsetControlFlag()
        {
            m_ControlWasPressed = false;
            m_ControlUnpressScheduleItem.Pause();
        }

        void OnSaveDocument(KeyUpEvent evt)
        {
            if (m_Toolbar == null)
                return;

            if (evt.keyCode == KeyCode.LeftCommand ||
                evt.keyCode == KeyCode.RightCommand ||
                evt.keyCode == KeyCode.LeftControl ||
                evt.keyCode == KeyCode.RightControl)
            {
                m_ControlUnpressScheduleItem.ExecuteLater(100);
                m_ControlWasPressed = true;
                return;
            }

            if (evt.keyCode != KeyCode.S)
                return;

            if (!evt.modifiers.HasFlag(EventModifiers.Control) &&
                !evt.modifiers.HasFlag(EventModifiers.Command) &&
                !m_ControlWasPressed)
                return;

            m_ControlWasPressed = false;

            m_Toolbar.SaveDocument(false);

            evt.StopPropagation();
        }

        void OnDelete(KeyDownEvent evt)
        {
            // HACK: This must be a bug. TextField leaks its key events to everyone!
            if (evt.leafTarget is ITextInputField)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteSelection();
                    evt.StopPropagation();
                    break;
                case KeyCode.Escape:
                {
                    if (m_CutElements.Count > 0)
                    {
                        m_CutElements.Clear();
                        BuilderEditorUtility.systemCopyBuffer = null;
                    }
                }
                break;
            }
        }

        public void DeleteSelection()
        {
            if (m_Selection.isEmpty)
                return;

            // Must save a copy of the selection here and then clear selection before
            // we delete the elements. Otherwise the selection clearing will fail
            // to remove the special selection objects because it won't be able
            // to query parent information of selected elements (they have already
            // been removed from the hierarchy).
            var selectionCopy = m_Selection.selection.ToList();
            m_Selection.ClearSelection(null, true);

            bool somethingWasDeleted = false;
            foreach (var element in selectionCopy)
                somethingWasDeleted |= DeleteElement(element);

            if (somethingWasDeleted)
                JustNotify();
        }

        public bool CopySelection()
        {
            ClearCopyBuffer();

            if (m_Selection.isEmpty)
                return false;

            // UXML
            var veas = new List<VisualElementAsset>();
            foreach (var element in m_Selection.selection)
            {
                var vea = element.GetVisualElementAsset();
                if (vea == null)
                {
                    veas.Clear();
                    break; // Mixed type selections are not supported.
                }

                // Check if current element is a child of another selected element.
                if (element.HasAnyAncestorInList(m_Selection.selection))
                    continue;

                veas.Add(vea);
            }
            if (veas.Count > 0)
            {
                BuilderEditorUtility.systemCopyBuffer =
                    VisualTreeAssetToUXML.GenerateUXML(m_PaneWindow.document.visualTreeAsset, null, veas);
                return true;
            }

            // USS
            var ussSnippetBuilder = new StringBuilder();
            foreach (var element in m_Selection.selection)
            {
                var selector = element.GetStyleComplexSelector();
                if (selector == null)
                {
                    ussSnippetBuilder.Length = 0;
                    break; // Mixed type selections are not supported.
                }

                // Check if current element is a child of another selected element.
                if (element.HasAnyAncestorInList(m_Selection.selection))
                    continue;

                var styleSheet = element.GetClosestStyleSheet();
                StyleSheetToUss.ToUssString(styleSheet, selector, ussSnippetBuilder);
            }
            if (ussSnippetBuilder.Length > 0)
            {
                BuilderEditorUtility.systemCopyBuffer = ussSnippetBuilder.ToString();
                return true;
            }

            return false;
        }

        public void CutSelection()
        {
            m_CutElements.Clear();

            if (!CopySelection())
                return;

            foreach (var element in m_Selection.selection)
                m_CutElements.Add(element);

            JustNotify();
        }

        public void DuplicateSelection()
        {
            if (CopySelection())
                Paste();
        }

        public void RenameSelection()
        {
            if (m_Selection.isEmpty)
                return;

            var element = m_Selection.selection.First();
            var explorerItemElement = element.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
            explorerItemElement?.ActivateRenameElementMode();
        }

        void PasteUXML(string copyBuffer)
        {
            var importer = new BuilderVisualTreeAssetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.ImportXmlFromString(copyBuffer, out var pasteVta);

            /* If the current parent element is linked to a VisualTreeAsset, it could mean
            that our parent is the TemplateContainer belonging to our parent document and the
            current open document is a sub-document opened in-place. In such a case, we don't
            want to use our parent's VisualElementAsset, as that belongs to our parent document.
            So instead, we just use no parent, indicating that we are adding this new element
            to the root of our document. */
            VisualElementAsset parent = null;
            if (!m_Selection.isEmpty)
            {
                var selectionParent = m_Selection.selection.First().parent;
                parent = selectionParent?.GetVisualElementAsset();

                if (selectionParent?.GetVisualTreeAsset() == m_PaneWindow.document.visualTreeAsset)
                    parent = null;

                m_Selection.ClearSelection(null);
            }

            // Select all pasted elements.
            foreach (var templateAsset in pasteVta.templateAssets)
                if (pasteVta.IsRootElement(templateAsset))
                    templateAsset.Select();
            foreach (var vea in pasteVta.visualElementAssets)
                if (pasteVta.IsRootElement(vea))
                    vea.Select();

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, parent, pasteVta);
            m_PaneWindow.document.AddStyleSheetsToAllRootElements();

            ScriptableObject.DestroyImmediate(pasteVta);
        }

        void PasteUSS(string copyBuffer)
        {
            // Paste does nothing if document has no stylesheets.
            var mainStyleSheet = m_PaneWindow.document.activeStyleSheet;
            if (mainStyleSheet == null)
                return;

            var pasteStyleSheet = StyleSheetUtilities.CreateInstance();
            var importer = new BuilderStyleSheetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.Import(pasteStyleSheet, copyBuffer);

            // Select all pasted selectors.
            m_Selection.ClearSelection(null);
            foreach (var selector in pasteStyleSheet.complexSelectors)
                BuilderAssetUtilities.AddStyleComplexSelectorToSelection(pasteStyleSheet, selector);

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, mainStyleSheet, pasteStyleSheet);

            ScriptableObject.DestroyImmediate(pasteStyleSheet);
        }

        public void Paste()
        {
            var focused = m_PaneWindow.rootVisualElement.focusController.focusedElement as VisualElement;
            if (!BuilderEditorUtility.CopyBufferMatchesTarget(focused))
                return;

            var copyBuffer = BuilderEditorUtility.systemCopyBuffer;

            if (BuilderEditorUtility.IsUxml(copyBuffer))
                PasteUXML(copyBuffer);
            else if (BuilderEditorUtility.IsUss(copyBuffer))
                PasteUSS(copyBuffer);
            else // Unknown string.
                return;

            if (m_CutElements.Count > 0)
            {
                foreach (var elementToCut in m_CutElements)
                    DeleteElement(elementToCut);

                m_CutElements.Clear();
                BuilderEditorUtility.systemCopyBuffer = null;
            }

            m_PaneWindow.OnEnableAfterAllSerialization();

            // TODO: ListView bug. Does not refresh selection pseudo states after a
            // call to Refresh().
            m_PaneWindow.rootVisualElement.schedule.Execute(() =>
            {
                if (m_Selection.isEmpty)
                    return;
                m_Selection.ForceReselection();
            }).ExecuteLater(200);

            m_Selection.NotifyOfHierarchyChange();
        }

        bool DeleteElement(VisualElement element)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(element) ||
                BuilderSharedStyles.IsStyleSheetElement(element) ||
                BuilderSharedStyles.IsDocumentElement(element) ||
                !element.IsLinkedToAsset())
                return false;

            if (BuilderSharedStyles.IsSelectorElement(element))
            {
                var styleSheet = element.GetClosestStyleSheet();
                Undo.RegisterCompleteObjectUndo(
                    styleSheet, BuilderConstants.DeleteSelectorUndoMessage);

                var selectorStr = BuilderSharedStyles.GetSelectorString(element);
                styleSheet.RemoveSelector(selectorStr);

                element.RemoveFromHierarchy();
                m_Selection.NotifyOfHierarchyChange();

                return true;
            }

            return DeleteElementFromVisualTreeAsset(element);
        }

        bool DeleteElementFromVisualTreeAsset(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea == null)
                return false;

            {
                BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, element);

                element.RemoveFromHierarchy();
                m_Selection.NotifyOfHierarchyChange();
            }

            return true;
        }

        public void ClearCopyBuffer()
        {
            BuilderEditorUtility.systemCopyBuffer = null;
        }

        public void ClearSelectionNotify()
        {
            m_Selection.ClearSelection(null);
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        public void JustNotify()
        {
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }
    }
}
