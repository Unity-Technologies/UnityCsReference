// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Text;
using Unity.UIToolkit.Editor;
using UnityEditor.UIElements;
using UnityEngine.Pool;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        const CommandCategory k_ExternalChangesTrackedCategories =
            CommandCategory.Hierarchy |
            CommandCategory.Attributes |
            CommandCategory.Styling |
            CommandCategory.StylingContext |
            CommandCategory.Variables;

        BuilderPaneWindow m_PaneWindow;
        BuilderToolbar m_Toolbar;
        BuilderSelection m_Selection;

        List<VisualElement> m_CutElements = new List<VisualElement>();

        List<BuilderPaneContent> m_Panes = new List<BuilderPaneContent>();
        private bool m_PendingExternalRefresh;
        private bool m_ExternalChangeAffectsStyleSheets;
        private bool m_ExternalChangeAffectsVisualTree;
        CommandCategory m_ExternalChangesCategories;
        readonly HashSet<UnityEngine.Object> m_ExternalChangeAssets = new HashSet<UnityEngine.Object>();

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
            UICommandQueue.RegisterHandlerForCategory(k_ExternalChangesTrackedCategories, SyncExternalChanges);
            UICommandQueue.GroupEnded += OnGroupEnded;
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
            UICommandQueue.UnregisterHandlerForCategory(k_ExternalChangesTrackedCategories, SyncExternalChanges);
            UICommandQueue.GroupEnded -= OnGroupEnded;

            // A refresh may already be scheduled via EditorApplication.delayCall, which survives this window's
            // teardown. Clear the pending flag so the deferred call no-ops (its guard returns early) rather than
            // dereferencing the now-destroyed pane window.
            m_PendingExternalRefresh = false;
            m_ExternalChangeAffectsStyleSheets = false;
            m_ExternalChangeAffectsVisualTree = false;
            m_ExternalChangesCategories = CommandCategory.None;
            m_ExternalChangeAssets.Clear();
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
            var selectionCopy = new List<VisualElement>(m_Selection.selection);
            m_Selection.ClearSelection(null, true);

            foreach (var element in selectionCopy)
                DeleteElement(element);
        }

        public bool CopySelection()
        {
            ClearCopyBuffer();

            if (m_Selection.isEmpty)
                return false;

            // UXML
            var veas = new List<UxmlAsset>();
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
                    VisualTreeAssetExporter.Default.ToUxmlString(
                        m_PaneWindow.document.visualTreeAsset,
                        veas
                    );

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
                ussSnippetBuilder.AppendLine(BuilderStyleSheetExporter.ExportSelectorAsRule(styleSheet, selector));
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

            var element = m_Selection.selection[0];
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
                var selectionParent = m_Selection.selection[0].parent;
                parent = selectionParent?.GetVisualElementAsset();

                if (selectionParent?.GetVisualTreeAsset() == m_PaneWindow.document.visualTreeAsset)
                    parent = null;

                m_Selection.ClearSelection(null);
            }

            // Select all pasted elements.
            foreach (var vea in pasteVta.DepthFirstTraversalOfType<VisualElementAsset>())
                if (pasteVta.IsRootElement(vea))
                    SelectionUtility.AddToSelection(vea);

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

            var pasteStyleSheet = StyleSheetUtility.CreateInstanceWithHideFlags();
            var importer = new BuilderStyleSheetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.Import(pasteStyleSheet, copyBuffer);

            // Select all pasted selectors.
            m_Selection.ClearSelection(null);
            foreach(var rule in pasteStyleSheet.rules)
            foreach (var selector in rule.complexSelectors)
                SelectionUtility.AddToSelection(pasteStyleSheet, selector);

            BuilderAssetUtilities.TransferAssetToAsset(mainStyleSheet, pasteStyleSheet);

            pasteStyleSheet.Destroy();
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

                var complexSelector = BuilderSharedStyles.GetSelectorProperty(element);
                styleSheet.RemoveSelector(complexSelector);

                // Selection changes on delete, we need to update preview here
                UpdateStyleSheetUssPreview(styleSheet);

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

                m_Selection.NotifyOfStylingChange();
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

            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document.visualTreeAsset, element);
            element.RemoveFromHierarchy();
            m_Selection.NotifyOfHierarchyChange();

            return true;
        }

        public void CreateTemplateFromHierarchy(VisualElement ve, VisualTreeAsset vta, string path = "")
        {
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

            var vea = ve.GetVisualElementAsset();

            using var _ = ListPool<UxmlNamespaceDefinition>.Get(out var nsDefinitions);
            vta.GatherUxmlNamespaceDefinitions(vea.parentAsset, nsDefinitions);

            var newVta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            var newRootVea = newVta.visualTree;

            foreach (var property in vta.visualTree.properties)
            {
                newRootVea.SetAttribute(property.name, property.value);
            }
            newRootVea.xmlNamespace = vta.visualTree.xmlNamespace;
            foreach (var ns in vta.visualTree.namespaceDefinitions)
            {
                newRootVea.namespaceDefinitions.Add(ns);
            }

            using var setHandle = HashSetPool<UxmlNamespaceDefinition>.Get(out var previousSet);
            foreach (var def in newVta.visualTree.namespaceDefinitions)
                previousSet.Add(def);

            foreach (var def in nsDefinitions)
            {
                if (!previousSet.Add(def))
                    continue;
                newRootVea.namespaceDefinitions.Add(def);
            }

            Undo.RegisterCompleteObjectUndo(vta, BuilderConstants.DeleteUIElementUndoMessage);

            newRootVea.Add(vea);

            var uxml = VisualTreeAssetExporter.Default.ToUxmlString(newVta);

            if (!m_PaneWindow.document.SaveNewTemplateFileFromHierarchy(path, uxml))
            {
                // New template wasn't saved
                return;
            }

            var parent = ve.parent;
            var parentVEA = parent.GetVisualElementAsset();
            var index = parent.IndexOf(ve);

            // Delete old element
            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document.visualTreeAsset, ve);
            ve.RemoveFromHierarchy();

            // Replace with new template
            newVta = EditorGUIUtility.Load(path) as VisualTreeAsset;

            var newTemplateContainer = newVta.CloneTree();
            newTemplateContainer.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, path);
            newTemplateContainer.name = newVta.name;

            parent.Insert(index, newTemplateContainer);

            BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document.visualTreeAsset, newTemplateContainer, (inVta, inParent, ve) =>
            {
                var vea = inVta.AddTemplateInstance(inParent, path) as VisualElementAsset;
                vea.SetAttribute("name", newVta.name);
                ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, newVta);
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
                BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document.visualTreeAsset, unpackedVE, templateContainerIndex + 1);

                var linkedInstancedVTA = elementToUnpack.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                var linkedTA = elementToUnpack.GetVisualElementAsset() as TemplateAsset;
                var linkedVTACopy = linkedInstancedVTA.DeepCopy();
                var unpackedVEA = unpackedVE.GetVisualElementAsset();

                using var listHandle = ListPool<UxmlNamespaceDefinition>.Get(out var definitions);
                m_PaneWindow.document.visualTreeAsset.GatherUxmlNamespaceDefinitions(unpackedVEA, definitions);
                using var setHandle = HashSetPool<UxmlNamespaceDefinition>.Get(out var definitionsSet);
                foreach (var def in definitions)
                    definitionsSet.Add(def);

                var definitionsToTransfer = linkedVTACopy.visualTree.namespaceDefinitions;
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
                    if (unpackedVEA.serializedData != null)
                    {
                        UxmlSerializer.TryParseSerializedAttribute(attribute.Key, attribute.Value,
                            unpackedVEA.serializedData,
                            new CreationContext(m_PaneWindow.document.visualTreeAsset));
                    }
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
                unpackedVEA.AddStyleSheets(linkedInstancedVTA.stylesheets);
                unpackedVEA.ruleIndex = linkedTA.ruleIndex;

                BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, unpackedVEA, linkedVTACopy, false);

                // Sync serialized data because attribute overrides have been updated
                UxmlSerializer.CreateSerializedDataOverrides(linkedVTACopy);

                elementsToUnpack.Remove(elementToUnpack);

                if (elementToUnpack != templateContainer)
                {
                    BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document.visualTreeAsset, elementToUnpack, false);
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

            // Sync serialized data because attribute overrides have been updated
            UxmlSerializer.CreateSerializedDataOverrides(m_PaneWindow.document.visualTreeAsset);

            m_Selection.NotifyOfHierarchyChange();
            m_PaneWindow.OnEnableAfterAllSerialization();

            // Keep hierarchy tree state in the new unpacked element
            var hierarchy = (m_PaneWindow as Builder)?.hierarchy;
            hierarchy?.elementHierarchyView.CopyTreeViewItemStates(rootVea, rootUnpackedVEA);

            // Delete old template element
            BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document.visualTreeAsset, templateContainer, false);
            templateContainer.RemoveFromHierarchy();

            m_Selection.ClearSelection(null);
            SelectionUtility.AddToSelection(rootUnpackedVEA);

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

        public void UpdateStyleSheetUssPreview(StyleSheet styleSheet)
        {
            var ussFile = m_PaneWindow.document.activeOpenUXMLFile.GetUssFileFromSheet(styleSheet);
            ussFile?.GeneratePreview();
        }

        private void SyncExternalChanges(in CommandContext context)
        {
            // Only track commands that were not sent by this window. The actual reload decision is deferred to
            // OnGroupEnded, once the full set of modified objects for the group is known.
            if (context.Status != CommandExecutionStatus.Success)
                return;

            if (context.Source == CommandSources.Builder)
            {
                // A Builder-sourced command is only external here when a sibling Builder window sent it. Its
                // asset payload stands in for the group's undo objects, which a Builder edit never records.
                if (context.Command is not BuilderSyncCommand syncCommand || syncCommand.SenderWindow == m_PaneWindow)
                    return;

                // A sibling's undo/redo refresh is not an edit: every window already refreshes itself from
                // Undo.undoRedoEvent, and relaying it would re-mark this window unsaved right after a save.
                if (syncCommand.IsUndoRedoRefresh)
                    return;

                foreach (var asset in syncCommand.Assets)
                    m_ExternalChangeAssets.Add(asset);
            }

            m_ExternalChangesCategories |= context.Command.Category;
        }

        // Only the window that owns the viewport drives the document-wide refresh. Several BuilderPaneWindows can
        // be alive at once (a popped-out inspector preview), each with its own handler registered on the
        // process-wide command queue: letting a secondary one run would refresh nothing (its
        // OnEnableAfterAllSerialization is the no-op base) while re-arming the panes' one-shot unsaved-mark
        // suppression with nothing left to consume it — silently swallowing the "*" for the user's next real edit.
        bool IsPrimaryViewportWindow
            => !ReferenceEquals(m_PaneWindow.document.primaryViewportWindow, null)
            && ReferenceEquals(m_PaneWindow.document.primaryViewportWindow, m_PaneWindow);

        void OnGroupEnded(in GroupEndedContext context)
        {
            // Nothing external was recorded during this group, so it's safe to skip any reload.
            if (m_ExternalChangesCategories == CommandCategory.None)
            {
                m_ExternalChangeAssets.Clear();
                return;
            }

            m_ExternalChangesCategories = CommandCategory.None;

            if (!IsPrimaryViewportWindow)
            {
                m_ExternalChangeAssets.Clear();
                return;
            }

            // Classify the group's undo objects together with any sibling-window sync payloads.
            if (context.UndoObjects != null)
                m_ExternalChangeAssets.UnionWith(context.UndoObjects);

            var affectsDocument = ClassifyExternalChanges(m_ExternalChangeAssets, out var affectsStyleSheets, out var affectsVisualTree);
            m_ExternalChangeAssets.Clear();

            if (!affectsDocument)
                return;

            if (affectsStyleSheets)
                m_ExternalChangeAffectsStyleSheets = true;
            if (affectsVisualTree)
                m_ExternalChangeAffectsVisualTree = true;

            // Coalesce multiple groups within the same frame into a single reload.
            if (m_PendingExternalRefresh)
                return;

            m_PendingExternalRefresh = true;
            EditorApplication.delayCall += SyncExternalChangesDeferred;
        }

        bool ClassifyExternalChanges(IReadOnlyCollection<UnityEngine.Object> objects, out bool affectsStyleSheets, out bool affectsVisualTree)
        {
            affectsStyleSheets = false;
            affectsVisualTree = false;

            if (objects == null || objects.Count == 0)
                return false;

            using var documentAssetsHandle = HashSetPool<UnityEngine.Object>.Get(out var documentAssets);
            using var styleSheetAssetsHandle = HashSetPool<UnityEngine.Object>.Get(out var styleSheetAssets);
            using var dependencyAssetsHandle = HashSetPool<UnityEngine.Object>.Get(out var dependencyAssets);
            CollectDocumentAssets(documentAssets, styleSheetAssets, dependencyAssets);

            var affectsDocument = false;
            foreach (var obj in objects)
            {
                if (obj == null)
                    continue;

                if (!documentAssets.Contains(obj))
                {
                    // A change to a nested template (or a sheet only it uses) is not a change to our document,
                    // but the canvas instantiates it, so the view still has to be rebuilt. Request the refresh
                    // without claiming any of our own assets became unsaved.
                    if (dependencyAssets.Contains(obj))
                        affectsDocument = true;
                    continue;
                }

                affectsDocument = true;
                // documentAssets holds the VisualTreeAsset, its inline sheet, and its style sheets;
                // styleSheetAssets holds only the style sheets. So an object in the document set that is not a
                // style sheet is the VisualTreeAsset or its inline sheet.
                if (styleSheetAssets.Contains(obj))
                    affectsStyleSheets = true;
                else
                    affectsVisualTree = true;
            }

            return affectsDocument;
        }

        void CollectDocumentAssets(HashSet<UnityEngine.Object> assets, HashSet<UnityEngine.Object> styleSheetAssets,
            HashSet<UnityEngine.Object> dependencyAssets)
        {
            foreach (var openUXMLFile in m_PaneWindow.document.openUXMLFiles)
            {
                var visualTreeAsset = openUXMLFile.visualTreeAsset;
                if (visualTreeAsset == null)
                    continue;

                assets.Add(visualTreeAsset);
                if (visualTreeAsset.inlineSheet != null)
                    assets.Add(visualTreeAsset.inlineSheet);

                foreach (var openUSSFile in openUXMLFile.openUSSFiles)
                {
                    if (openUSSFile.styleSheet != null)
                    {
                        assets.Add(openUSSFile.styleSheet);
                        styleSheetAssets.Add(openUSSFile.styleSheet);
                    }
                }

                foreach (var template in visualTreeAsset.templateDependencies)
                    CollectTemplateDependencies(template, dependencyAssets);
            }
        }

        // The assets a document instantiates but does not own: nested templates, recursively, plus their inline
        // and referenced style sheets.
        static void CollectTemplateDependencies(VisualTreeAsset template, HashSet<UnityEngine.Object> dependencyAssets)
        {
            if (template == null || !dependencyAssets.Add(template))
                return;

            if (template.inlineSheet != null)
                dependencyAssets.Add(template.inlineSheet);

            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            template.GetAllReferencedStyleSheets(sheets);
            foreach (var sheet in sheets)
                if (sheet != null)
                    dependencyAssets.Add(sheet);

            foreach (var nested in template.templateDependencies)
                CollectTemplateDependencies(nested, dependencyAssets);
        }

        // The user is typing in a field or dragging (pointer captured) in this window, which has focus.
        bool IsUserInteractingWithWindow()
        {
            if (EditorWindow.focusedWindow != m_PaneWindow)
                return false;

            // The leaf, not the retargeted focusedElement: a focused field retargets to the field itself,
            // while the element actually being typed in is its inner TextElement.
            var root = m_PaneWindow.rootVisualElement;
            if (root.focusController?.GetLeafFocusedElement() is TextElement)
                return true;

            return root.panel?.GetCapturingElement(PointerId.mousePointerId) != null;
        }

        void SyncExternalChangesDeferred()
        {
            if (!m_PendingExternalRefresh)
                return;

            // A change arriving while the user is typing or dragging here is an echo of their own edit relayed
            // by another window; refreshing now would rebuild the inspector and steal the field focus. Hold
            // the refresh until the interaction ends; it re-arms once per editor tick.
            if (IsUserInteractingWithWindow())
            {
                EditorApplication.delayCall += SyncExternalChangesDeferred;
                return;
            }

            var selection = m_PaneWindow.primarySelection;
            if (selection == null)
            {
                m_PendingExternalRefresh = false;
                m_ExternalChangeAffectsStyleSheets = false;
                m_ExternalChangeAffectsVisualTree = false;
                return;
            }

            try
            {
                // Clear only the in-memory selection: the markers live in the shared asset, so stripping them
                // would delete the EDITING window's selection. The refresh below re-resolves ours from them.
                selection.ClearSelection(null, false);
                selection.isApplyingExternalCommand = true;
                selection.suppressStyleSheetsPaneUnsavedMark = !m_ExternalChangeAffectsStyleSheets;
                selection.suppressHierarchyPaneUnsavedMark = m_ExternalChangeAffectsStyleSheets && !m_ExternalChangeAffectsVisualTree;

                // Mark only the active open file (the BuilderDocument setter fans out to sub-documents, which
                // are never cleared again). Gate on the registry: a sibling window also notifies on load/init
                // and right after a save, when nothing is actually unsaved.
                if ((m_ExternalChangeAffectsStyleSheets || m_ExternalChangeAffectsVisualTree)
                    && m_PaneWindow.document.AreOpenAssetsDirtyInRegistry())
                    m_PaneWindow.document.activeOpenUXMLFile.hasUnsavedChanges = true;

                m_PaneWindow.OnEnableAfterAllSerialization();
            }
            finally
            {
                // The suppress flags are intentionally left set here: they are consumed later by the deferred
                // styling / hierarchy notifications the panes receive from the refresh above.
                m_ExternalChangeAffectsStyleSheets = false;
                m_ExternalChangeAffectsVisualTree = false;
                m_PendingExternalRefresh = false;
                selection.isApplyingExternalCommand = false;
            }
        }
    }
}
