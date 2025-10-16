// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The kind of paste.
    /// </summary>
    [UnityRestricted]
    internal enum PasteOperation
    {
        /// <summary>
        /// The paste is part of a duplicate operation.
        /// </summary>
        Duplicate,

        /// <summary>
        /// Paste the current clipboard content.
        /// </summary>
        Paste
    }

    /// <summary>
    /// Class that provides standard copy paste operations on a <see cref="SelectionStateComponent"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class ViewSelection
    {
        protected static IReadOnlyList<GraphElementModel> s_EmptyList = new List<GraphElementModel>();

        protected readonly SelectionStateComponent m_SelectionState;
        protected readonly ClipboardProvider m_ClipboardProvider;

        /// <summary>
        /// The view to which this <see cref="ViewSelection"/> is attached.
        /// </summary>
        protected RootView View { get; private set; }

        /// <summary>
        /// All the models that can be selected in this view.
        /// </summary>
        public abstract IEnumerable<GraphElementModel> SelectableModels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewSelection"/> class.
        /// </summary>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="clipboardProvider">The clipboard provider.</param>
        public ViewSelection(SelectionStateComponent selectionState, ClipboardProvider clipboardProvider)
        {
            View = null;
            m_SelectionState = selectionState;
            m_ClipboardProvider = clipboardProvider;
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> start processing copy paste commands.
        /// </summary>
        public void AttachToView(RootView view)
        {
            View = view;
            View.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            View.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            View.RegisterCallback<ShortCutPasteWithoutWires>(_ => PasteWithoutWires());
            View.RegisterCallback<ShortCutDuplicateWithoutWires>(_ => DuplicateSelectionWithoutWires());
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> stop processing copy paste commands.
        /// </summary>
        public void DetachFromView()
        {
            View?.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            View?.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            View = null;
        }

        /// <summary>
        /// Handles the <see cref="ValidateCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        public virtual void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (View == null)
                return;

            if (View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNamesBridge.Copy && CanCopySelection())
                || (evt.commandName == EventCommandNamesBridge.Paste && CanPaste())
                || (evt.commandName == EventCommandNamesBridge.Duplicate && CanDuplicateSelection())
                || (evt.commandName == EventCommandNamesBridge.Cut && CanCutSelection())
                || evt.commandName == EventCommandNamesBridge.SelectAll
                || evt.commandName == EventCommandNamesBridge.DeselectAll
                || evt.commandName == EventCommandNamesBridge.InvertSelection
                || ((evt.commandName == EventCommandNamesBridge.Delete || evt.commandName == EventCommandNamesBridge.SoftDelete) && CanDeleteSelection()))
            {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Handles the <see cref="ExecuteCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        public virtual void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (View == null)
                return;

            if (View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNamesBridge.Copy)
            {
                CopySelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Paste)
            {
                Paste();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Duplicate)
            {
                DuplicateSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Cut)
            {
                CutSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.Delete)
            {
                View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.SoftDelete)
            {
                View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.SelectAll)
            {
                View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, SelectableModels.ToList()));
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.DeselectAll)
            {
                View.Dispatch(new ClearSelectionCommand());
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNamesBridge.InvertSelection)
            {
                View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, SelectableModels.ToList()));
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Gets the selected models.
        /// </summary>
        /// <returns>The selected models.</returns>
        public abstract IReadOnlyList<GraphElementModel> GetSelection();

        /// <summary>
        /// Returns the renamable element if the selection can be renamed.
        /// </summary>
        protected virtual GraphElementModel GetRenamableElement()
        {
            var selection = GetSelection();

            // The rename is allowed only when there is one element in the selection.
            return selection.Count == 1 && selection[0].IsRenamable() ? selection[0] : null;
        }

        /// <summary>
        /// Returns true if the selection can be copied.
        /// </summary>
        protected virtual bool CanCopySelection() => m_ClipboardProvider != null && GetSelection().Any(ge => ge.IsCopiable());

        /// <summary>
        /// Returns true if the selection can be cut (copied and deleted).
        /// </summary>
        protected virtual bool CanCutSelection() => m_ClipboardProvider != null && GetSelection().Any(ge => ge.IsCopiable() && ge.IsDeletable());

        /// <summary>
        /// Returns true if the clipboard content can be pasted.
        /// </summary>
        protected virtual bool CanPaste() => m_ClipboardProvider?.CanDeserializeDataFromClipboard() ?? false;

        /// <summary>
        /// Returns true if the selection can be duplicated.
        /// </summary>
        protected virtual bool CanDuplicateSelection() => CanCopySelection();

        /// <summary>
        /// Returns true if the selection can be deleted.
        /// </summary>
        protected virtual bool CanDeleteSelection() => GetSelection().Any(ge => ge.IsDeletable());

        /// <summary>
        /// Renames the selected element.
        /// </summary>
        protected virtual void RenameElement(GraphElementModel elementToRename)
        {
            var view = elementToRename.GetView(View) as ModelView;
            view?.Rename();
        }

        /// <summary>
        /// Serializes the selection and related elements to the clipboard.
        /// </summary>
        /// <returns>The copied elements</returns>
        protected virtual IEnumerable<GraphElementModel> CopySelection()
        {
            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            using var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            m_ClipboardProvider.SerializeDataToClipboard(copyPasteData);
            return elementsToCopySet;
        }

        /// <summary>
        /// Serializes the selection and related elements to the clipboard, then deletes the selection.
        /// </summary>
        protected virtual void CutSelection()
        {
            if (View == null)
                return;

            var copiedElements = CopySelection();
            View.Dispatch(new DeleteElementsCommand(copiedElements.ToList()) { UndoString = "Cut" });
        }

        /// <summary>
        /// Pastes the clipboard content into the graph.
        /// </summary>
        protected virtual void Paste()
        {
            if (View == null)
                return;

            using var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            PasteData(PasteOperation.Paste, "Paste", copyPasteData);
        }

        /// <summary>
        /// Pastes the clipboard content into the graph. Omitting wires.
        /// </summary>
        protected virtual void PasteWithoutWires()
        {
            // TODO: Rename and modify when we have a proper implementation for the Paste/Duplicate with Wires.

            if (View == null)
                return;

            using var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            PasteData(PasteOperation.Paste, ShortCutPasteWithoutWires.id, copyPasteData);
        }

        /// <summary>
        /// Duplicates the selection and related elements.
        /// </summary>
        protected virtual void DuplicateSelection()
        {
            if (View == null)
                return;

            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            var duplicatedData = m_ClipboardProvider.Duplicate(copyPasteData);
            copyPasteData.Dispose();
            PasteData(PasteOperation.Duplicate, "Duplicate", duplicatedData);
        }

        /// <summary>
        /// Duplicates the selection and related elements excluding wires.
        /// </summary>
        protected virtual void DuplicateSelectionWithoutWires()
        {
            // TODO: Rename and modify when we have a proper implementation for the Paste/Duplicate with Wires.

            if (View == null)
                return;

            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            copyPasteData.RemoveWires();
            var duplicatedData = m_ClipboardProvider.Duplicate(copyPasteData);
            PasteData(PasteOperation.Duplicate, ShortCutDuplicateWithoutWires.id, duplicatedData);
        }

        /// <summary>
        /// Builds the set of elements to be copied from an initial set of elements.
        /// </summary>
        /// <param name="elements">The initial set of elements, usually the selection.</param>
        /// <returns>A set of elements to be copied, usually <paramref name="elements"/> plus related elements.</returns>
        protected virtual HashSet<GraphElementModel> CollectCopyableGraphElements(IEnumerable<GraphElementModel> elements)
        {
            var elementsToCopySet = new HashSet<GraphElementModel>();
            FilterElements(elements, elementsToCopySet, IsCopiable);
            return elementsToCopySet;
        }

        /// <summary>
        /// Creates a <see cref="CopyPasteData"/> from a set of elements to copy. This data will eventually be
        /// serialized and saved to the clipboard.
        /// </summary>
        /// <param name="elementsToCopySet">The set of elements to copy.</param>
        /// <returns>The newly created <see cref="CopyPasteData"/>.</returns>
        protected abstract CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet);

        /// <summary>
        /// Gets the offset at which data should be pasted.
        /// </summary>
        /// <remarks>
        /// Often, pasted nodes should not be pasted at their original position so they
        /// do not hide the original nodes. This method gives the offset to apply on the pasted nodes.
        /// </remarks>
        /// <param name="data">The data to paste.</param>
        /// <param name="operation">The kind of operation.</param>
        /// <returns>The offset to apply to the pasted elements.</returns>
        protected virtual Vector2 GetPasteDelta(CopyPasteData data, PasteOperation operation)
        {
            return Vector2.zero;
        }

        /// <summary>
        /// Paste the content of data into the graph.
        /// </summary>
        /// <param name="operation">The kind of operation.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="data">The serialized data.</param>
        protected virtual void PasteData(PasteOperation operation, string operationName, CopyPasteData data)
        {
            if (View == null)
                return;

            if (data == null)
                return;

            var delta = GetPasteDelta(data, operation);
            var selection = GetSelection();
            foreach (var selected in selection.Reverse())
            {
                var ui = selected.GetView(View);
                if (ui is ModelView modelView && modelView.HandlePasteOperation(operation, operationName, delta, data))
                    return;
            }

            if(operationName == ShortCutPasteWithoutWires.id)
                data.RemoveWires();

            View.Dispatch(new PasteDataCommand(operation, operationName, delta, data));
        }

        /// <summary>
        /// Builds a set of unique, non null elements that satisfies the <paramref name="conditionFunc"/>.
        /// </summary>
        /// <param name="elements">The source elements.</param>
        /// <param name="collectedElementSet">The set of elements that satisfies the <paramref name="conditionFunc"/>.</param>
        /// <param name="conditionFunc">The filter to apply.</param>
        protected static void FilterElements(IEnumerable<GraphElementModel> elements, HashSet<GraphElementModel> collectedElementSet, Func<GraphElementModel, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && conditionFunc(e)))
            {
                collectedElementSet.Add(element);
            }
        }

        /// <summary>
        /// Returns true if the model is not null and the model is copiable.
        /// </summary>
        /// <param name="model">The model to check.</param>
        /// <returns>True if the model is not null and the model is copiable</returns>
        protected static bool IsCopiable(GraphElementModel model)
        {
            return model?.IsCopiable() ?? false;
        }

        /// <summary>
        /// Builds the contextual menu from the categorized <see cref="ContextualMenuItem"/>s.
        /// </summary>
        /// <param name="categorizedMenuItems">The <see cref="ContextualMenuItem"/>s used to build the menu, organized by <see cref="ContextualMenuCategory"/>.</param>
        /// <param name="evt">The event to populate the contextual menu.</param>
        /// <param name="menuActionMap">A dictionary mapping each <see cref="ContextualMenuItem"/>'s name to its corresponding action to be invoked when the menu item is selected.</param>
        public static void BuildContextualMenu(Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> categorizedMenuItems, ContextualMenuPopulateEvent evt, Dictionary<string, Action> menuActionMap)
        {
            // If there are no categorized menu items, we can return early.
            if (categorizedMenuItems == null)
                return;

            // Append actions to the contextual menu using the categorized menu items.
            var menuCategories = (ContextualMenuCategory[])Enum.GetValues(typeof(ContextualMenuCategory));
            for (var i = 0; i < menuCategories.Length; i++)
            {
                // Skip empty categories.
                if (!categorizedMenuItems.TryGetValue(menuCategories[i], out var itemsInCategory) || itemsInCategory.Count == 0)
                    continue;

                // Append a separator before each category, except:
                // - Before the first category
                // - Before the OrganizationalElements category
                // - Before the ToolSpecificElements category
                if (evt.menu.MenuItems().Count > 0 &&
                    menuCategories[i] != ContextualMenuCategory.OrganizationalElements &&
                    menuCategories[i] != ContextualMenuCategory.ToolSpecificElements)
                    evt.menu.AppendSeparator();

                // Append the items in the current category to the contextual menu.
                foreach (var item in itemsInCategory)
                {
                    if (menuActionMap.TryGetValue(item.Name, out var action))
                        action.Invoke();
                }
            }
        }

        internal void PopulateMenuActionMap(Dictionary<string, Action> menuActionMap, ContextualMenuPopulateEvent evt)
        {
            if (menuActionMap == null)
                return;

            menuActionMap.Add(ContextualMenuHelpers.cutItem.Name, () => AppendCutMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.copyItem.Name, () => AppendCopyMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.pasteItem.Name, () => AppendPasteMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.renameItem.Name, () => AppendRenameMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.duplicateItem.Name, () => AppendDuplicateMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.deleteItem.Name, () => AppendDeleteMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.selectAllItem.Name, () => AppendSelectAllMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.pasteAsNewMenuItem.Name, () => AppendPasteAsNewMenuItem(evt));
        }

        void AppendCutMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Cut, _ => { CutSelection(); },
                CanCutSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendCopyMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Copy, _ => { CopySelection(); },
                CanCopySelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        internal void AppendCopyPortValueMenuItem(ContextualMenuPopulateEvent evt, PortModel portModel)
        {
            var canCopy = portModel.GetConnectedWires().Count == 0 && portModel.EmbeddedValue != null;
            evt.menu.AppendAction(L10n.Tr("Copy Value"), _ =>
            {
                var copyPasteData = new CopyPasteData(new List<Constant> { portModel.EmbeddedValue });
                m_ClipboardProvider.SerializeDataToClipboard(copyPasteData);
            },
            canCopy ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        internal void AppendPastePortValueMenuItem(ContextualMenuPopulateEvent evt, PortModel portModel)
        {
            using var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            var canPaste = false;
            Constant constant = null;

            if (copyPasteData is { Constants: not null } && copyPasteData.Constants.Count > 0 && portModel.GetConnectedWires().Count == 0)
            {
                constant = copyPasteData.Constants[0];
                canPaste = CanPaste() && portModel.EmbeddedValue != null && portModel.EmbeddedValue.IsAssignableFrom(constant.Type);
            }

            evt.menu.AppendAction(L10n.Tr("Paste Value"), _ =>
            {
                View.Dispatch(new UpdateConstantsValueCommand(new[] { portModel.EmbeddedValue }, constant?.ObjectValue));
            }, canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendPasteMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Paste, menuAction => { Paste(); },
                CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendPasteAsNewMenuItem(ContextualMenuPopulateEvent evt)
        {
            var selection = GetSelection();
            if (selection.Count > 1)
                return;

            var transitionSupportModel = selection[0] as TransitionSupportModel;
            var stateModel = selection[0] as StateModel;

            if (transitionSupportModel == null && stateModel == null)
                return;

            using var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            var enableItemMenu = Transition.CanPasteTransitionsAsNew(copyPasteData);

            Action<DropdownMenuAction> action = null;
            if (transitionSupportModel != null)
            {
                var transition = transitionSupportModel.GetView<Transition>(View);
                if (transition != null)
                    action = _ => transition.PasteAsNew();
            }

            if (stateModel != null)
            {
                var state = stateModel.GetView<State>(View);
                if (state != null)
                    action = _ => state.PasteAsNew();
            }

            if (action == null)
                return;

            evt.menu.AppendMenuItemFromShortcutWithName<ShortCutPasteWithoutWires>(View.GraphTool, L10n.Tr("Paste as New"), action,
                enableItemMenu ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendRenameMenuItem(ContextualMenuPopulateEvent evt)
        {
            var elementToRename = GetRenamableElement();
            if (elementToRename == null)
                return;

            evt.menu.AppendAction("Rename", _ => { RenameElement(elementToRename); });
        }

        void AppendDuplicateMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Duplicate, _ => { DuplicateSelection(); },
                CanDuplicateSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendDeleteMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.Delete, _ =>
            {
                View.Dispatch(new DeleteElementsCommand(GetSelection().ToList()));
            }, CanDeleteSelection() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendSelectAllMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.SelectAll, _ =>
            {
                View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, SelectableModels.ToList()));
            }, _ => DropdownMenuAction.Status.Normal);
        }
    }
}
