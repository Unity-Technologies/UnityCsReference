// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Helpers for defining the menu items to show in a contextual menu.
    /// </summary>
    static class ContextualMenuHelpers
    {
        internal static Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> GetMenuItemsForSelection(List<GraphElementModel> selection)
        {
            // If the selection is null or empty, return null.
            if (selection == null || selection.Count == 0)
                return null;

            // Combine the contextual menu items from all selected elements, keeping only the same items between types of selected elements.
            var menuItems = new List<ContextualMenuItem>();
            var uniqueSelectedTypes = new HashSet<Type>();

            foreach (var elementModel in selection)
            {
                // If there are wires in the selection, we don't want to show contextual menu items for them.
                if (elementModel is WireModel)
                    continue;

                var type = elementModel.GetType();
                // If we haven't added menu items for this type yet, or it's a SubgraphNodeModel combine its contextual menu items with the current list.
                // Subgraph nodes are a special case as they could have different menu items based on if they are local or asset subgraphs.
                // It is currently the only model type that can have different contextual menu items based on its data, but this logic can be extended to other model types when needed.
                if (elementModel is IHasContextualMenuItems hasContextualMenuItems && (!uniqueSelectedTypes.Contains(type) || elementModel is SubgraphNodeModel))
                {
                    uniqueSelectedTypes.Add(type);
                    IntersectMenuItems(menuItems, hasContextualMenuItems.ContextualMenuItems);
                }
            }

            return CategorizeMenuItems(menuItems);
        }

        /// <summary>
        /// Organizes the <see cref="ContextualMenuItem"/>s from a given list by <see cref="ContextualMenuCategory"/>.
        /// </summary>
        /// <param name="itemsList">The graph view.</param>
        /// <returns>A dictionary containing lists of <see cref="ContextualMenuItem"/>s paired with their category.</returns>
        internal static Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> CategorizeMenuItems(IReadOnlyList<ContextualMenuItem> itemsList)
        {
            var categoryGroups = new Dictionary<ContextualMenuCategory, List<ContextualMenuItem>>();
            foreach (var contextualMenuItem in itemsList)
            {
                // If the category is not already in the dictionary, create a new list for it.
                if (!categoryGroups.ContainsKey(contextualMenuItem.Category))
                    categoryGroups[contextualMenuItem.Category] = new List<ContextualMenuItem>();

                // If the index is negative or out of bounds, add the item to the end of the list.
                if (contextualMenuItem.IndexInCategory < 0 || contextualMenuItem.IndexInCategory >= categoryGroups[contextualMenuItem.Category].Count)
                    categoryGroups[contextualMenuItem.Category].Add(contextualMenuItem);
                else
                    categoryGroups[contextualMenuItem.Category].Insert(contextualMenuItem.IndexInCategory, contextualMenuItem);
            }

            return categoryGroups;
        }

        /// <summary>
        /// Combines the provided list of <see cref="ContextualMenuItem"/>s with the existing ones in the provided list.
        /// </summary>
        /// <param name="menuItems">The current list of items.</param>
        /// <param name="otherMenuItems">The list of items to combine with the current list.</param>
        static void IntersectMenuItems(List<ContextualMenuItem> menuItems, IReadOnlyList<ContextualMenuItem> otherMenuItems)
        {
            if (menuItems.Count == 0)
            {
                // If the menuItems list is empty, add all items from the provided list.
                menuItems.AddRange(otherMenuItems);
            }
            else
            {
                // Only keep items that are also in the provided list.
                for (var i = menuItems.Count - 1; i >= 0; i--)
                {
                    if (!otherMenuItems.Contains(menuItems[i]))
                        menuItems.RemoveAt(i);
                }
            }
        }

        // Predefined menu items:

        // ViewSelection menu items:
        internal static ContextualMenuItem cutItem = new(ContextualMenuCategory.CutCopyPaste, "Cut");
        internal static ContextualMenuItem copyItem = new(ContextualMenuCategory.CutCopyPaste, "Copy");
        internal static ContextualMenuItem pasteItem = new(ContextualMenuCategory.CutCopyPaste, "Paste");
        internal static ContextualMenuItem renameItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Rename");
        internal static ContextualMenuItem duplicateItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Duplicate");
        internal static ContextualMenuItem deleteItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete");
        internal static ContextualMenuItem selectUnusedItem = new(ContextualMenuCategory.Organization, "Select Unused");
        internal static ContextualMenuItem pasteAsNewMenuItem = new(ContextualMenuCategory.CutCopyPaste, "Paste as New");

        // Common graph element menu items:
        internal static ContextualMenuItem createPlacematItem = new(ContextualMenuCategory.OrganizationalElements, "Create Placemat");
        internal static ContextualMenuItem createLocalSubgraphFromSelectionItem = new(ContextualMenuCategory.Conversions, "Create Local Subgraph from Selection");
        internal static ContextualMenuItem frameSelectionItem = new(ContextualMenuCategory.Modifications, "Frame Selection");
        internal static ContextualMenuItem colorItem = new(ContextualMenuCategory.Modifications, "Color");
        internal static ContextualMenuItem alignAndDistributeElementsItem = new(ContextualMenuCategory.Organization, "Align and Distribute Elements");

        // GraphView menu items:
        internal static ContextualMenuItem addNodeItem = new(ContextualMenuCategory.FunctionalElements, "Add Node");
        internal static ContextualMenuItem createStickyNoteItem = new(ContextualMenuCategory.OrganizationalElements, "Create Sticky Note");
        internal static ContextualMenuItem createEmptyLocalSubgraphItem = new(ContextualMenuCategory.OrganizationalElements, "Create Empty Local Subgraph");
        internal static ContextualMenuItem selectAllItem = new(ContextualMenuCategory.Organization, "Select All");

        // Node menu items:
        internal static ContextualMenuItem editSubtitleItem = new(ContextualMenuCategory.Modifications, "Edit Subtitle");
        internal static ContextualMenuItem bypassNodeItem = new(ContextualMenuCategory.Modifications, "Bypass Node");
        internal static ContextualMenuItem disconnectAllWiresItem = new(ContextualMenuCategory.Modifications, "Disconnect All Wires");
        internal static ContextualMenuItem toggleCollapseItem = new(ContextualMenuCategory.Modifications, "Toggle Collapse");
        internal static ContextualMenuItem deleteAndReconnectItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete and reconnect");

        // State menu items:
        internal static ContextualMenuItem createTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create Transition");
        internal static ContextualMenuItem createLocalTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create Local Transition");
        internal static ContextualMenuItem createOnEnterTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create OnEnter Transition");
        internal static ContextualMenuItem createSelfTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create Self Transition");
        internal static ContextualMenuItem setAsDefaultStateMenuItem = new(ContextualMenuCategory.Modifications, "Set Default State");

        // Subgraph menu items:
        internal static ContextualMenuItem extractContentsToPlacematItem = new(ContextualMenuCategory.Conversions, "Extract Contents to Placemat");
        internal static ContextualMenuItem openLocalSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Open Local Subgraph");
        internal static ContextualMenuItem openAssetSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Open Asset Subgraph");
        internal static ContextualMenuItem unpackToLocalSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Unpack to Local Subgraph");
        internal static ContextualMenuItem findAssetInProjectItem = new(ContextualMenuCategory.AssetManagement, "Find Asset in Project");
        internal static ContextualMenuItem convertToAssetSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Convert to Asset Subgraph");

        // Variable and constant menu items:
        internal static ContextualMenuItem itemizeItem = new(ContextualMenuCategory.Modifications, "Itemize");
        internal static ContextualMenuItem convertToConstantItem = new(ContextualMenuCategory.Conversions, "Convert to Constant");
        internal static ContextualMenuItem convertToVariableItem = new(ContextualMenuCategory.Conversions, "Convert to Variable");

        // Blackboard menu items:
        internal static ContextualMenuItem createVariableItem = new(ContextualMenuCategory.FunctionalElements, "Create Variable");
        internal static ContextualMenuItem createGroupItem = new(ContextualMenuCategory.FunctionalElements, "Create Group");

        // Ports menu items:
        internal static ContextualMenuItem addNodeFromPortItem = new(ContextualMenuCategory.FunctionalElements, "Add Node from port");
        internal static ContextualMenuItem createVariableFromPortItem = new(ContextualMenuCategory.FunctionalElements, "Create Variable from port");
        internal static ContextualMenuItem copyValueItem = new(ContextualMenuCategory.CutCopyPaste, "Copy Value");
        internal static ContextualMenuItem pasteValueItem = new(ContextualMenuCategory.CutCopyPaste, "Paste Value");
        internal static ContextualMenuItem expandPortItem = new(ContextualMenuCategory.Modifications, "Expand Port");
        internal static ContextualMenuItem collapsePortItem = new(ContextualMenuCategory.Modifications, "Collapse Port");

        // Wire menu items:
        internal static ContextualMenuItem insertNodeItem = new(ContextualMenuCategory.FunctionalElements, "Insert Node");
        internal static ContextualMenuItem insertJunctionPointItem = new(ContextualMenuCategory.FunctionalElements, "Insert Junction Point");
        internal static ContextualMenuItem convertToPortalsItem = new(ContextualMenuCategory.Conversions, "Convert to Portals");
        internal static ContextualMenuItem reorderWireItem = new(ContextualMenuCategory.Modifications, "Reorder Wire");

        // Context and block menu items:
        internal static ContextualMenuItem addBlockItem = new(ContextualMenuCategory.FunctionalElements, "Add Block");
        internal static ContextualMenuItem insertBlockAboveItem = new(ContextualMenuCategory.FunctionalElements, "Insert Block Above");
        internal static ContextualMenuItem insertBlockBelowItem = new(ContextualMenuCategory.FunctionalElements, "Insert Block Below");
        internal static ContextualMenuItem convertToBlockSubgraphItem = new(ContextualMenuCategory.Conversions, "Convert to Block Subgraph");

        // Sticky Note menu items:
        internal static ContextualMenuItem fitToTextItem = new(ContextualMenuCategory.Modifications, "Fit to Text");
        internal static ContextualMenuItem fontSizeAndThemeItem = new(ContextualMenuCategory.Modifications, "Font Size");

        // Placemat menu items:
        internal static ContextualMenuItem deleteAndSelectContentsItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete and Select Contents");
        internal static ContextualMenuItem smartResizeItem = new(ContextualMenuCategory.Modifications, "Smart Resize");
        internal static ContextualMenuItem reorderPlacematItem = new(ContextualMenuCategory.Modifications, "Reorder Placemat");
        internal static ContextualMenuItem selectAllPlacematContentsItem = new(ContextualMenuCategory.Organization, "Select All Placemat Contents");

        // Portals menu items:
        internal static ContextualMenuItem createOppositePortalItem = new(ContextualMenuCategory.Conversions, "Create Opposite Portal");
        internal static ContextualMenuItem revertToWireItem = new(ContextualMenuCategory.Conversions, "Revert to Wire");
        internal static ContextualMenuItem revertAllToWiresItem = new(ContextualMenuCategory.Conversions, "Revert All to Wire");
    }
}
