// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Helpers for defining the menu items to show in a contextual menu.
    /// </summary>
    static class ContextualMenuHelpers
    {
        internal static Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> GetMenuItemsForSelection(IReadOnlyList<GraphElementModel> selection)
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
                if (elementModel is IHasContextualMenuItems hasContextualMenuItems && (!uniqueSelectedTypes.Contains(type) || elementModel is ISubgraphNodeInternal))
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
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem cutItem = new(ContextualMenuCategory.CutCopyPaste, "Cut");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem copyItem = new(ContextualMenuCategory.CutCopyPaste, "Copy");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem pasteItem = new(ContextualMenuCategory.CutCopyPaste, "Paste");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem renameItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Rename");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem duplicateItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Duplicate");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem deleteItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem selectUnusedItem = new(ContextualMenuCategory.Organization, "Select Unused");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem pasteAsNewMenuItem = new(ContextualMenuCategory.CutCopyPaste, "Paste as New");

        // Common graph element menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createPlacematItem = new(ContextualMenuCategory.OrganizationalElements, "Create Placemat");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createLocalSubgraphFromSelectionItem = new(ContextualMenuCategory.Conversions, "Create Local Subgraph from Selection");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem frameSelectionItem = new(ContextualMenuCategory.Modifications, "Frame Selection");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem colorItem = new(ContextualMenuCategory.Modifications, "Color");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem alignAndDistributeElementsItem = new(ContextualMenuCategory.Organization, "Align and Distribute Elements");

        // GraphView menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem addNodeItem = new(ContextualMenuCategory.FunctionalElements, "Add Node");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createStickyNoteItem = new(ContextualMenuCategory.OrganizationalElements, "Create Sticky Note");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createEmptyLocalSubgraphItem = new(ContextualMenuCategory.OrganizationalElements, "Create Empty Local Subgraph");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem selectAllItem = new(ContextualMenuCategory.Organization, "Select All");

        // Node menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem editSubtitleItem = new(ContextualMenuCategory.Modifications, "Edit Subtitle");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem bypassNodeItem = new(ContextualMenuCategory.Modifications, "Bypass Node");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem disconnectAllWiresItem = new(ContextualMenuCategory.Modifications, "Disconnect All Wires");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem toggleCollapseItem = new(ContextualMenuCategory.Modifications, "Toggle Collapse");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem deleteAndReconnectItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete and reconnect");

        // State menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create Transition");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createSelfTransitionMenuItem = new(ContextualMenuCategory.FunctionalElements, "Create Self Transition");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem disconnectAllTransitionsItem = new(ContextualMenuCategory.Modifications, "Disconnect All Transitions");

        // Subgraph menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem extractContentsToPlacematItem = new(ContextualMenuCategory.Conversions, "Extract Contents to Placemat");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem openLocalSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Open Local Subgraph");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem openAssetSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Open Asset Subgraph");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem unpackToLocalSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Unpack to Local Subgraph");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem findAssetInProjectItem = new(ContextualMenuCategory.AssetManagement, "Find Asset in Project");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem convertToAssetSubgraphItem = new(ContextualMenuCategory.AssetManagement, "Convert to Asset Subgraph");

        // Variable and constant menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem itemizeItem = new(ContextualMenuCategory.Modifications, "Itemize");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem convertToConstantItem = new(ContextualMenuCategory.Conversions, "Convert to Constant");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem convertToVariableItem = new(ContextualMenuCategory.Conversions, "Convert to Variable");

        // Blackboard menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createVariableItem = new(ContextualMenuCategory.FunctionalElements, "Create Variable");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createGroupItem = new(ContextualMenuCategory.FunctionalElements, "Create Group");

        // Ports menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem addNodeFromPortItem = new(ContextualMenuCategory.FunctionalElements, "Add Node from port");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createVariableFromPortItem = new(ContextualMenuCategory.FunctionalElements, "Create Variable from port");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem copyValueItem = new(ContextualMenuCategory.CutCopyPaste, "Copy Value");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem pasteValueItem = new(ContextualMenuCategory.CutCopyPaste, "Paste Value");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem expandPortItem = new(ContextualMenuCategory.Modifications, "Expand Port");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem collapsePortItem = new(ContextualMenuCategory.Modifications, "Collapse Port");

        // Wire menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem insertNodeItem = new(ContextualMenuCategory.FunctionalElements, "Insert Node");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem insertJunctionPointItem = new(ContextualMenuCategory.FunctionalElements, "Insert Junction Point");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem convertToPortalsItem = new(ContextualMenuCategory.Conversions, "Convert to Portals");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem reorderWireItem = new(ContextualMenuCategory.Modifications, "Reorder Wire");

        // Context and block menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem addBlockItem = new(ContextualMenuCategory.FunctionalElements, "Add Block");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem insertBlockAboveItem = new(ContextualMenuCategory.FunctionalElements, "Insert Block Above");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem insertBlockBelowItem = new(ContextualMenuCategory.FunctionalElements, "Insert Block Below");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem convertToBlockSubgraphItem = new(ContextualMenuCategory.Conversions, "Convert to Block Subgraph");

        // Sticky Note menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem fitToTextItem = new(ContextualMenuCategory.Modifications, "Fit to Text");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem fontSizeAndThemeItem = new(ContextualMenuCategory.Modifications, "Font Size");

        // Placemat menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem deleteAndSelectContentsItem = new(ContextualMenuCategory.RenameDuplicateDelete, "Delete and Select Contents");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem smartResizeItem = new(ContextualMenuCategory.Modifications, "Smart Resize");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem reorderPlacematItem = new(ContextualMenuCategory.Modifications, "Reorder Placemat");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem selectAllPlacematContentsItem = new(ContextualMenuCategory.Organization, "Select All Placemat Contents");

        // Portals menu items:
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem createOppositePortalItem = new(ContextualMenuCategory.Conversions, "Create Opposite Portal");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem revertToWireItem = new(ContextualMenuCategory.Conversions, "Revert to Wire");
        [NoAutoStaticsCleanup] // fixed menu item descriptor; category and label are compile-time constants
        internal static ContextualMenuItem revertAllToWiresItem = new(ContextualMenuCategory.Conversions, "Revert All to Wire");
    }
}
