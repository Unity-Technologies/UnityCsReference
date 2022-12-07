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
    class GraphViewSelection : ViewSelection
    {
        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> SelectableModels
        {
            get => m_GraphModelState.GraphModel.GraphElementModels.Where(t => !(t is VariableDeclarationModel) && t.IsSelectable());
        }
        /// <inheritdoc />
        public GraphViewSelection(GraphView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
            : base(view, graphModelState, selectionState) { }

        /// <inheritdoc />
        protected override Vector2 GetPasteDelta(CopyPasteData data)
        {
            var mousePosition = PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
            mousePosition = ((GraphView)m_View).ContentViewContainer.WorldToLocal(mousePosition);
            return mousePosition - data.TopLeftNodePosition;
        }

        /// <inheritdoc />
        protected override void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (((GraphView)m_View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnValidateCommand(evt);
            }
        }

        /// <inheritdoc />
        protected override void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (((GraphView)m_View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnExecuteCommand(evt);
            }
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet)
        {
            var copyPaste = CopyPasteData.GatherCopiedElementsData_Internal(null, elementsToCopySet.ToList());
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
                var placematUI = placemat.GetView<Placemat>(m_View);
                placematUI?.ActOnGraphElementsInside_Internal(
                    el =>
                    {
                        if (el.Model is AbstractNodeModel node)
                            nodesInPlacemat.Add(node);
                        FilterElements(new[] { el.GraphElementModel },
                            elementsToCopySet,
                            IsCopiable);
                        return false;
                    },
                    true);
            }

            // copying wires between nodes in placemats
            foreach (var wire in m_GraphModelState.GraphModel.WireModels)
            {
                if (nodesInPlacemat.Contains(wire.FromPort?.NodeModel) && nodesInPlacemat.Contains(wire.ToPort?.NodeModel))
                    elementsToCopySet.Add(wire);
            }

            return elementsToCopySet;
        }
    }
}
