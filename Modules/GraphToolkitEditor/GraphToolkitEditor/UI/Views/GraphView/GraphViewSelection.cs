// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class that manages the selection, including standard copy and paste operations, on a <see cref="SelectionStateComponent"/> for a graph view.
    /// </summary>
    /// <remarks>
    /// It is responsible for managing the selection and supporting typical operations like copying and pasting graph elements,
    /// enabling users to efficiently duplicate or move elements within the <see cref="GraphView"/>. The 'GraphViewSelection' is
    /// created in <see cref="GraphViewEditorWindow.CreateGraphViewSelection"/> and is then associated with the <see cref="GraphView"/>.
    /// It can be accessed via <see cref="GraphView.ViewSelection"/>, providing an easy way to interact with the selection within the graph view context.
    /// </remarks>
    [UnityRestricted]
    internal class GraphViewSelection : ViewSelection
    {
        protected readonly GraphModelStateComponent m_GraphModelState;

        // Internal for tests
        internal const int pasteOffset = 30;

        protected GraphModel GraphModel => m_GraphModelState.GraphModel;

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> SelectableModels =>
            GraphModel.GetGraphElementModels().Where(t => t is not VariableDeclarationModelBase && t.IsSelectable());

        /// <inheritdoc cref="ViewSelection(SelectionStateComponent, ClipboardProvider)"/>
        public GraphViewSelection(SelectionStateComponent selectionState, GraphModelStateComponent graphModelState, ClipboardProvider clipboardProvider)
            : base(selectionState, clipboardProvider)
        {
            m_GraphModelState = graphModelState;
        }

        /// <inheritdoc />
        protected override Vector2 GetPasteDelta(CopyPasteData data, PasteOperation operation)
        {
            var targetPosition = Vector2.zero;

            if (View is not GraphView graphView)
                return targetPosition;

            if (operation == PasteOperation.Paste)
            {
                // Paste from the contextual menu: Always at mouse location
                targetPosition = graphView.ContentViewContainer.WorldToLocal(EngineBridge.GetMousePosition());

                // Paste from keyboard shortcut CTR+V: Add an offset if target is the same as origin, otherwise = mouse location
                if (Event.current is not null && Event.current.type == EventType.ExecuteCommand)
                {
                    var originPosition = data.TopLeftNodePosition;

                    if (Mathf.Abs(targetPosition.x - originPosition.x) < EditorGUIUtility.pixelsPerPoint && Mathf.Abs(targetPosition.y - originPosition.y) < EditorGUIUtility.pixelsPerPoint)
                    {
                        targetPosition = new Vector2(targetPosition.x + pasteOffset, targetPosition.y + pasteOffset);
                    }
                    else
                    {
                        // In case there were multiple succeeding paste actions: Add an offset if the target position is the same as the position of the data that was just pasted.
                        var topLeftPosition = Vector2.positiveInfinity;
                        var selection = graphView.GetSelection();
                        for (var i = 0; i < selection.Count; i++)
                        {
                            var childView = selection[i].GetView(graphView);
                            if (childView is not null && selection[i].IsMovable())
                                topLeftPosition = Vector2.Min(topLeftPosition, childView.layout.position);
                        }
                        if (Mathf.Abs(targetPosition.x - topLeftPosition.x) < EditorGUIUtility.pixelsPerPoint && Mathf.Abs(targetPosition.y - topLeftPosition.y) < EditorGUIUtility.pixelsPerPoint)
                        {
                            targetPosition = new Vector2(targetPosition.x + pasteOffset, targetPosition.y + pasteOffset);
                        }
                    }
                }
            }
            else if (operation == PasteOperation.Duplicate)
            {
                // Duplicate : Always add a slight offset from the origin, not dependent on mouse location
                var topLeftPosition = Vector2.positiveInfinity;
                var elements = new List<GraphElementModel>();
                elements.AddRange(data.Nodes);
                elements.AddRange(data.Placemats);
                elements.AddRange(data.StickyNotes);
                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is not IMovable)
                        continue;
                    var childView = elements[i].GetView(graphView);
                    if (childView is not null)
                        topLeftPosition = Vector2.Min(topLeftPosition, childView.layout.position);
                }
                targetPosition = new Vector2(topLeftPosition.x + pasteOffset, topLeftPosition.y + pasteOffset);
            }

            return targetPosition - data.TopLeftNodePosition;
        }

        /// <inheritdoc />
        public override void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (View != null && ((GraphView)View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnValidateCommand(evt);
            }
        }

        /// <inheritdoc />
        public override void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (View != null && ((GraphView)View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnExecuteCommand(evt);
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<GraphElementModel> GetSelection()
        {
            return m_SelectionState?.GetSelection(GraphModel) ?? s_EmptyList;
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet)
        {
            var copyPaste = new CopyPasteData(null, elementsToCopySet.ToList());
            return copyPaste;
        }

        /// <inheritdoc />
        protected override HashSet<GraphElementModel> CollectCopyableGraphElements(IEnumerable<GraphElementModel> elements)
        {
            var elementsToCopySet = new HashSet<GraphElementModel>();
            var elementList = elements.ToList();
            FilterElements(elementList, elementsToCopySet, IsCopiable);

            var nodesInPlacemat = new HashSet<AbstractNodeModel>();
            // Also collect hovering list of nodes
            foreach (var placemat in elementList.OfType<PlacematModel>())
            {
                var placematUI = placemat.GetView<Placemat>(View);
                placematUI?.ActOnGraphElementsInside(
                    el =>
                    {
                        if (el.Model is AbstractNodeModel node)
                            nodesInPlacemat.Add(node);
                        FilterElements(new[] { el.GraphElementModel },
                            elementsToCopySet,
                            IsCopiable);
                        return false;
                    });
            }

            // copying wires between nodes in placemats
            foreach (var wire in GraphModel.WireModels)
            {
                if (nodesInPlacemat.Contains(wire.FromPort?.NodeModel) && nodesInPlacemat.Contains(wire.ToPort?.NodeModel))
                    elementsToCopySet.Add(wire);
            }

            return elementsToCopySet;
        }
    }
}
