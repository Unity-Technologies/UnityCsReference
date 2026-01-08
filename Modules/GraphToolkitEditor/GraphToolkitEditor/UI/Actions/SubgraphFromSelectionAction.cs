// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Collects and prepares shared data from the given context for Subgraph related commands.
    /// Useful to parse the data once instead of many times within a frame.
    /// </summary>
    class SubgraphFromSelectionAction
    {
        static internal readonly SubgraphFromSelectionActionData InvalidData
            = new SubgraphFromSelectionActionData() { IsValid = false };

        internal struct SubgraphFromSelectionActionData
        {
            public bool IsValid;

            public List<GraphElementModel> elementsToInclude;
            public List<GraphElementModel> elementsToDelete;
            public List<SubgraphNodeModel> assetSubgraphNodes;
            public List<SubgraphNodeModel> localSubgraphNodes;

            public string defaultName;
            public bool shouldConvertToPlacemat;
        }

        internal static SubgraphFromSelectionActionData CollectData(GraphView view, Type graphObjectType, Type graphModelType)
        {
            if (!view.GraphModel.AllowSubgraphCreation)
                return InvalidData;

            var selection = view.GetSelection();

            if (selection.HasAny(e => e is IPlaceholder || e is IHasDeclarationModel hasDeclarationModel && hasDeclarationModel.DeclarationModel is IPlaceholder)
                || !selection.HasAny(e => e is AbstractNodeModel || e is PlacematModel || e is StickyNoteModel))
                return InvalidData;

            if (!selection.HasAny(e => e is not BlockNodeModel))
                return InvalidData;

            var transferredModels = new HashSet<GraphElementModel>();
            List<SubgraphNodeModel> assetSubgraphNodes = null;
            List<SubgraphNodeModel> localSubgraphNodes = null;

            var placematCount = 0;
            PlacematModel encompassingPlacemat = null;
            var encompassingPlacematRect = Rect.zero;
            var isInEncompassingPlacemat = true;

            var allStickyNotes = true;
            foreach (var model in selection)
            {
                if (model is not StickyNoteModel)
                    allStickyNotes = false;

                if (model is SubgraphNodeModel subgraphNode && graphModelType != null && graphModelType.IsInstanceOfType(subgraphNode.GetSubgraphModel()))
                {
                    if (subgraphNode.GetSubgraphModel()?.GraphObject == null || !subgraphNode.IsReferencingLocalSubgraph)
                    {
                        assetSubgraphNodes ??= new List<SubgraphNodeModel>();
                        assetSubgraphNodes.Add(subgraphNode);
                    }
                    else
                    {
                        localSubgraphNodes ??= new List<SubgraphNodeModel>();
                        localSubgraphNodes.Add(subgraphNode);
                    }
                }

                if (model is not PlacematModel placematModel)
                    continue;

                var placemat = placematModel.GetView<Placemat>(view);
                if (placemat is null)
                    continue;

                placemat.ActOnGraphElementsInside(ge =>
                {
                    transferredModels.Add(ge.GraphElementModel);
                    return false;
                });

                placematCount++;

                if (placematCount == 1)
                {
                    encompassingPlacemat = placematModel;
                    encompassingPlacematRect = placemat.layout;
                }
                else if (isInEncompassingPlacemat && placematCount > 1)
                {
                    // Verify that in case of multiple selected placemats, they are contained inside the encompassing placemat.
                    var placematIsInEncompassingRect = encompassingPlacematRect.Contains(placemat.layout.min) && encompassingPlacematRect.Contains(placemat.layout.max);
                    var encompassingRectIsInPlacemat = placemat.layout.Contains(encompassingPlacematRect.min) && placemat.layout.Contains(encompassingPlacematRect.max);

                    if (encompassingRectIsInPlacemat)
                    {
                        // The placemat contains the current encompassing placemat, it becomes the new encompassing placemat.
                        encompassingPlacemat = placematModel;
                        encompassingPlacematRect = placemat.layout;
                    }

                    if (!encompassingRectIsInPlacemat && !placematIsInEncompassingRect)
                        isInEncompassingPlacemat = false;
                }
            }

            // If all selected elements are sticky notes, we don't allow the creation of a subgraph.
            if (allStickyNotes)
                return InvalidData;

            // The Convert to Subgraph option should only be displayed if there is ONE selected placemat OR if there are multiple selected placemats, they must be contained inside the encompassing placemat.
            // and all other selected elements are inside the encompassing placemat.
            var shouldConvertToPlacemat = placematCount == 1 || isInEncompassingPlacemat;
            foreach (var model in selection)
            {
                if (!transferredModels.Add(model))
                    continue;

                if (shouldConvertToPlacemat && model is PlacematModel)
                    continue;

                // A selected model isn't in the placemat(s).
                shouldConvertToPlacemat = false;
            }

            // The Convert to Subgraph option should not include the placemat in the newly created subgraph.
            if (shouldConvertToPlacemat)
                transferredModels.Remove(encompassingPlacemat);

            var data = new SubgraphFromSelectionActionData()
            {
                IsValid = true,
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                elementsToInclude = transferredModels.ToList(),
#pragma warning restore RS0030
                defaultName = shouldConvertToPlacemat ? encompassingPlacemat?.Title : SubgraphCreationHelper.defaultLocalSubgraphName,
                elementsToDelete = shouldConvertToPlacemat ? new List<GraphElementModel> { encompassingPlacemat } : null,
                assetSubgraphNodes = assetSubgraphNodes,
                localSubgraphNodes = localSubgraphNodes,
                shouldConvertToPlacemat = shouldConvertToPlacemat
            };

            return data;
        }
    }
}
