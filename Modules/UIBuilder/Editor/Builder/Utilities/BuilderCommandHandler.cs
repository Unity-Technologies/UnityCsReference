// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine.Pool;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderToolbar m_Toolbar;
        BuilderSelection m_Selection;

        List<VisualElement> m_CutElements = new List<VisualElement>();

        List<BuilderPaneContent> m_Panes = new List<BuilderPaneContent>();

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

            // Undo/Redo
            Undo.undoRedoEvent += OnUndoRedo;
        }

        public void OnDisable()
        {
            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                pane.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);
            }

            // Undo/Redo
            Undo.undoRedoEvent -= OnUndoRedo;
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

        void OnUndoRedo(in UndoRedoInfo info)
        {
            m_PaneWindow.OnUndoRedo();
        }

        void OnDelete(KeyDownEvent evt)
        {
            // HACK: This must be a bug. TextField leaks its key events to everyone!
            if (evt.target is TextElement)
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
                BuilderSharedStyles.IsDocumentElement(element) ||
                !element.IsLinkedToAsset() ||
                (!BuilderSharedStyles.IsSelectorElement(element) && !element.IsPartOfActiveVisualTreeAsset(m_PaneWindow.document) && !BuilderSharedStyles.IsStyleSheetElement(element)) ||
                BuilderSharedStyles.IsStyleSheetElement(element) && !string.IsNullOrEmpty(element?.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string))
                return false;

            if (BuilderSharedStyles.IsSelectorElement(element))
            {
                var styleSheet = element.GetClosestStyleSheet();
                Undo.RegisterCompleteObjectUndo(
                    styleSheet, BuilderConstants.DeleteSelectorUndoMessage);

                var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
                styleSheet.RemoveSelector(complexSelector);

                // If we are deleting multiple items then its possible that a previous
                // delete recreated the explorer panel and this element is no longer valid.
                // In that case, we force an update with OnEnableAfterAllSerialization.
                if (element.panel == null)
                {
                    m_PaneWindow.OnEnableAfterAllSerialization();
                }
                else
                {
                    element.RemoveFromHierarchy();
                }
                m_Selection.NotifyOfHierarchyChange();

                return true;
            }
            else if (BuilderSharedStyles.IsStyleSheetElement(element))
            {
                BuilderStyleSheetsUtilities.RemoveUSSFromAsset(m_PaneWindow, m_Selection, element);
                return true;
            }

            return DeleteElementFromVisualTreeAsset(element);
        }

        bool DeleteElementFromVisualTreeAsset(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea == null)
                return false;

            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, element);
            element.RemoveFromHierarchy();
            m_Selection.NotifyOfHierarchyChange();

            return true;
        }

        public void CreateTemplateFromHierarchy(VisualElement ve, VisualTreeAsset vta, string path = "")
        {
            var veas = new List<VisualElementAsset>();
            var vea = ve.GetVisualElementAsset();

            using var _ = ListPool<UxmlNamespaceDefinition>.Get(out var nsDefinitions);
            vta.GatherUxmlNamespaceDefinitions(vta.GetParentAsset(vea), nsDefinitions);

            veas.Add(vea);

            if (string.IsNullOrEmpty(path))
            {
                path = BuilderDialogsUtility.DisplaySaveFileDialog("Save UXML", null, ve.name, "uxml");

                if (string.IsNullOrEmpty(path))
                {
                    // Save dialog cancelled
                    return;
                }
            }

            if (path == m_PaneWindow.document.activeOpenUXMLFile.uxmlPath)
            {
                // Path is the same as the active open uxml file. Abort!
                BuilderDialogsUtility.DisplayDialog(
                    BuilderConstants.InvalidCreateTemplatePathTitle,
                    BuilderConstants.InvalidCreateTemplatePathMessage,
                    BuilderConstants.DialogOkOption);

                return;
            }

            var uxml = VisualTreeAssetToUXML.GenerateUXML(vta, null, veas);

            if (!m_PaneWindow.document.SaveNewTemplateFileFromHierarchy(path, uxml))
            {
                // New template wasn't saved
                return;
            }

            var parent = ve.parent;
            var parentVEA = parent.GetVisualElementAsset();
            var index = parent.IndexOf(ve);

            // Delete old element
            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, ve);
            ve.RemoveFromHierarchy();

            // Replace with new template
            var newTemplateVTA = EditorGUIUtility.Load(path) as VisualTreeAsset;
            var rootVea = newTemplateVTA.GetRootUxmlElement();
            using var setHandle = HashSetPool<UxmlNamespaceDefinition>.Get(out var definitionsSet);
            foreach (var def in rootVea.namespaceDefinitions)
                definitionsSet.Add(def);

            foreach (var def in nsDefinitions)
            {
                if (!definitionsSet.Add(def))
                    continue;
                rootVea.namespaceDefinitions.Add(def);
            }

            var newTemplateContainer = newTemplateVTA.CloneTree();
            newTemplateContainer.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, path);
            newTemplateContainer.name = newTemplateVTA.name;

            parent.Insert(index, newTemplateContainer);

            BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document, newTemplateContainer, (inVta, inParent, ve) =>
            {
                var vea = inVta.AddTemplateInstance(inParent, path) as VisualElementAsset;
                vea.SetAttribute("name", newTemplateVTA.name);
                ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, newTemplateVTA);
                return vea;
            }, index);

            m_Selection.Select(null, newTemplateContainer);

            // Refresh
            m_Selection.NotifyOfHierarchyChange();
            m_PaneWindow.OnEnableAfterAllSerialization();
        }

        public void UnpackTemplateContainer(VisualElement templateContainer, bool unpackCompletely = false)
        {
            if (templateContainer == null)
            {
                Debug.LogError("Template to unpack is null");
                return;
            }

            var elementsToUnpack = new List<VisualElement>();
            var rootVea = templateContainer.GetVisualElementAsset();

            var isRootElement = true;
            VisualElementAsset rootUnpackedVEA = null;
            elementsToUnpack.Add(templateContainer);

            while (elementsToUnpack.Count > 0)
            {
                var elementToUnpack = elementsToUnpack[0];
                var unpackedVE = new VisualElement();
                var templateContainerParent = elementToUnpack.parent;
                var templateContainerIndex = templateContainerParent.IndexOf(elementToUnpack);

                // Create new unpacked element and add it in the hierarchy
                templateContainerParent.Add(unpackedVE);
                BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document, unpackedVE, templateContainerIndex + 1);

                var linkedInstancedVTA = elementToUnpack.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                var linkedTA = elementToUnpack.GetVisualElementAsset() as TemplateAsset;
                var linkedVTACopy = linkedInstancedVTA.DeepCopy();
                var unpackedVEA = unpackedVE.GetVisualElementAsset();

                using var listHandle = ListPool<UxmlNamespaceDefinition>.Get(out var definitions);
                m_PaneWindow.document.visualTreeAsset.GatherUxmlNamespaceDefinitions(unpackedVEA, definitions);
                using var setHandle = HashSetPool<UxmlNamespaceDefinition>.Get(out var definitionsSet);
                foreach (var def in definitions)
                    definitionsSet.Add(def);

                var definitionsToTransfer = linkedVTACopy.GetRootUxmlElement().namespaceDefinitions;
                for (var i = 0; i < definitionsToTransfer.Count; ++i)
                {
                    var definitionToTransfer = definitionsToTransfer[i];
                    if (definitionsSet.Contains(definitionToTransfer))
                        continue;
                    unpackedVEA.namespaceDefinitions.Add(definitionToTransfer);
                }

                var templateContainerVEA = elementToUnpack.GetVisualElementAsset();
                var attributeOverrides = linkedTA.attributeOverrides;

                var attributes = elementToUnpack.GetOverriddenAttributes();
                foreach (var attribute in attributes)
                {
                    unpackedVEA.SetAttribute(attribute.Key, attribute.Value);
                }

                if (isRootElement)
                {
                    rootUnpackedVEA = unpackedVEA;
                }

                // Apply attribute overrides to elements in the unpacked element
                BuilderAssetUtilities.ApplyAttributeOverridesToTreeAsset(attributeOverrides, linkedVTACopy);

                // Move attribute overrides to new template containers
                BuilderAssetUtilities.CopyAttributeOverridesToChildTemplateAssets(elementToUnpack as TemplateContainer, attributeOverrides, linkedVTACopy);

                // Apply stylesheets to new element + inline rules
                BuilderAssetUtilities.AddStyleSheetsFromTreeAsset(unpackedVEA, linkedInstancedVTA);
                unpackedVEA.ruleIndex = templateContainerVEA.ruleIndex;

                BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, unpackedVEA, linkedVTACopy, false);

                // Sync serialized data because attribute overrides have been updated
                UxmlSerializer.SyncVisualTreeAssetSerializedData(new CreationContext(linkedVTACopy), false);

                elementsToUnpack.Remove(elementToUnpack);

                if (elementToUnpack != templateContainer)
                {
                    BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, elementToUnpack, false);
                    elementToUnpack.RemoveFromHierarchy();
                }

                if (unpackCompletely && elementsToUnpack.Count == 0)
                {
                    VisualElement tree = new VisualElement();
                    m_PaneWindow.document.activeOpenUXMLFile.visualTreeAsset.LinkedCloneTree(tree);
                    var newElement = tree.Query<VisualElement>().Where(x => x.GetVisualElementAsset() == rootUnpackedVEA).First();
                    var newTemplates = newElement.Query<TemplateContainer>().Where(x => x.GetVisualElementAsset() != null).Build();
                    elementsToUnpack.AddRange(newTemplates);
                    isRootElement = false;
                }
            }

            m_Selection.NotifyOfHierarchyChange();
            m_PaneWindow.OnEnableAfterAllSerialization();

            // Keep hierarchy tree state in the new unpacked element
            var hierarchy = Builder.ActiveWindow.hierarchy;
            hierarchy.elementHierarchyView.CopyTreeViewItemStates(rootVea, rootUnpackedVEA);

            // Delete old template element
            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, templateContainer, false);
            templateContainer.RemoveFromHierarchy();

            m_Selection.ClearSelection(null);
            rootUnpackedVEA.Select();

            m_Selection.NotifyOfHierarchyChange();
            m_PaneWindow.OnEnableAfterAllSerialization();
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

        public void CreateTargetedSelector(VisualElement ve)
        {
            // populates the new selector field with a selector that targets the current element
            var newSelectorField = m_PaneWindow.rootVisualElement.Q<BuilderStyleSheets>().newSelectorField;
            newSelectorField.value = BuilderStyleUtilities.GenerateElementTargetedSelector(ve);
            newSelectorField.Focus();
        }
    }
}
