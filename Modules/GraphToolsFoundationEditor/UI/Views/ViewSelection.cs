// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The kind of paste.
    /// </summary>
    enum PasteOperation
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
    abstract class ViewSelection
    {
        static IReadOnlyList<GraphElementModel> s_EmptyList = new List<GraphElementModel>();

        protected readonly RootView m_View;
        protected readonly GraphModelStateComponent m_GraphModelState;
        protected readonly SelectionStateComponent m_SelectionState;
        protected readonly ClipboardProvider m_ClipboardProvider;


        /// <summary>
        /// All the models that can be selected in this view.
        /// </summary>
        public abstract IEnumerable<GraphElementModel> SelectableModels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewSelection"/> class.
        /// </summary>
        /// <param name="view">The view used to dispatch commands.</param>
        /// <param name="graphModelState">The graph model state.</param>
        /// <param name="selectionState">The selection state.</param>
        public ViewSelection(RootView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
        {
            m_View = view;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
            m_ClipboardProvider = m_View.GraphTool.ClipboardProvider;
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> start processing copy paste commands.
        /// </summary>
        public void AttachToView()
        {
            m_View.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_View.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> stop processing copy paste commands.
        /// </summary>
        public void DetachFromView()
        {
            m_View.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_View.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }

        /// <summary>
        /// Handles the <see cref="ValidateCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (m_View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNames.Copy && CanCopySelection)
                || (evt.commandName == EventCommandNames.Paste && CanPaste)
                || (evt.commandName == EventCommandNames.Duplicate && CanDuplicateSelection)
                || (evt.commandName == EventCommandNames.Cut && CanCutSelection)
                || evt.commandName == EventCommandNames.SelectAll
                || evt.commandName == EventCommandNames.DeselectAll
                || evt.commandName == EventCommandNames.InvertSelection
                || ((evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete) && CanDeleteSelection))
            {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Handles the <see cref="ExecuteCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (m_View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNames.Copy)
            {
                CopySelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Paste)
            {
                Paste();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Duplicate)
            {
                DuplicateSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Cut)
            {
                CutSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Delete)
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.SoftDelete)
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.SelectAll)
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, SelectableModels.ToList()));
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.DeselectAll)
            {
                m_View.Dispatch(new ClearSelectionCommand());
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.InvertSelection)
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, SelectableModels.ToList()));
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Gets the selected models.
        /// </summary>
        /// <returns>The selected models.</returns>
        public IReadOnlyList<GraphElementModel> GetSelection()
        {
            return m_SelectionState?.GetSelection(m_GraphModelState.GraphModel) ?? s_EmptyList;
        }

        /// <summary>
        /// Returns true if the selection can be copied.
        /// </summary>
        protected virtual bool CanCopySelection => m_ClipboardProvider != null && GetSelection().Any(ge => ge.IsCopiable());

        /// <summary>
        /// Returns true if the selection can be cut (copied and deleted).
        /// </summary>
        protected virtual bool CanCutSelection => m_ClipboardProvider != null && GetSelection().Any(ge => ge.IsCopiable() && ge.IsDeletable());

        /// <summary>
        /// Returns true if the clipboard content can be pasted.
        /// </summary>
        protected virtual bool CanPaste => m_ClipboardProvider?.CanDeserializeDataFromClipboard() ?? false;

        /// <summary>
        /// Returns true if the selection can be duplicated.
        /// </summary>
        protected virtual bool CanDuplicateSelection => CanCopySelection;

        /// <summary>
        /// Returns true if the selection can be deleted.
        /// </summary>
        protected virtual bool CanDeleteSelection => GetSelection().Any(ge => ge.IsDeletable());

        /// <summary>
        /// Serializes the selection and related elements to the clipboard.
        /// <returns>The copied elements</returns>
        /// </summary>
        protected virtual IEnumerable<GraphElementModel> CopySelection()
        {
            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            m_ClipboardProvider.SerializeDataToClipboard(copyPasteData);
            return elementsToCopySet;
        }

        /// <summary>
        /// Serializes the selection and related elements to the clipboard, then deletes the selection.
        /// </summary>
        protected virtual void CutSelection()
        {
            var copiedElements = CopySelection();
            m_View.Dispatch(new DeleteElementsCommand(copiedElements.ToList()) { UndoString = "Cut" });
        }

        /// <summary>
        /// Pastes the clipboard content into the graph.
        /// </summary>
        protected virtual void Paste()
        {
            var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            PasteData(PasteOperation.Paste, "Paste", copyPasteData);
        }

        /// <summary>
        /// Duplicates the selection and related elements.
        /// </summary>
        protected virtual void DuplicateSelection()
        {
            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            var duplicatedData = m_ClipboardProvider.Duplicate(copyPasteData);
            PasteData(PasteOperation.Duplicate, "Duplicate", duplicatedData);
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
        /// <returns>The newly created <see cref="CopyPasteData"/>.</returns>.
        protected abstract CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet);

        /// <summary>
        /// Gets the offset at which data should be pasted.
        /// </summary>
        /// <remarks>
        /// Often, pasted nodes should not be pasted at their original position so they
        /// do not hide the original nodes. This method gives the offset to apply on the pasted nodes.
        /// </remarks>
        /// <param name="data">The data to paste.</param>
        /// <returns>The offset to apply to the pasted elements.</returns>
        protected virtual Vector2 GetPasteDelta(CopyPasteData data)
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
            if (data == null)
                return;

            var delta = GetPasteDelta(data);
            var selection = GetSelection();
            foreach (var selected in selection.Reverse())
            {
                var ui = selected.GetView_Internal(m_View);
                if (ui != null && ui.HandlePasteOperation(operation, operationName, delta, data))
                    return;
            }

            m_View.Dispatch(new PasteDataCommand(operation, operationName, delta, data));
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
        /// Adds items related to the selection to the contextual menu.
        /// </summary>
        /// <param name="evt">The contextual menu event.</param>
        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Cut", _ => { CutSelection(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", _ => { CopySelection(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Paste", _ => { Paste(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Duplicate", _ => { DuplicateSelection(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection().ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Select All", _ =>
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, SelectableModels.ToList()));
            }, _ => DropdownMenuAction.Status.Normal);
        }
    }
}
