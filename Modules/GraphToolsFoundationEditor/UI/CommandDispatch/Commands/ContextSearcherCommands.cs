// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Graph block creation data used by the library.
    /// </summary>
    ///
    readonly struct GraphBlockCreationData : IGraphNodeCreationData
    {
        /// <summary>
        /// The interface to the graph where we want the block to be created in.
        /// </summary>
        public GraphModel GraphModel { get; }

        /// <summary>
        /// Unused for blocks.
        /// </summary>
        public Vector2 Position { get => Vector2.zero; }

        /// <summary>
        /// The flags specifying how the block is to be spawned.
        /// </summary>
        public SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid { get; }

        /// <summary>
        /// The Context in which the Block will be added.
        /// </summary>
        public ContextNodeModel ContextNodeModel { get; }

        /// <summary>
        /// The index of the position at which the Block will be added to the Context.
        /// </summary>
        public int OrderInContext { get; }

        /// <summary>
        /// Initializes a new GraphNodeCreationData.
        /// </summary>
        /// <param name="graphModel">The interface to the graph where we want the node to be created in.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="contextNodeModel">The Context in which the block will be added.</param>
        /// <param name="orderInContext">The index of the position at which the Block is to be added to the Context.</param>
        public GraphBlockCreationData(GraphModel graphModel,
            SpawnFlags spawnFlags = SpawnFlags.Default,
            SerializableGUID guid = default,
            ContextNodeModel contextNodeModel = null,
            int orderInContext = -1)
        {
            GraphModel = graphModel;
            SpawnFlags = spawnFlags;
            Guid = guid;
            ContextNodeModel = contextNodeModel;
            OrderInContext = orderInContext;
        }
    }

    /// <summary>
    /// Command to create a block from a <see cref="GraphNodeModelLibraryItem"/>.
    /// </summary>
    class CreateBlockFromItemLibraryCommand : UndoableCommand
    {
        const string k_UndoString = "Create Block";
        /// <summary>
        /// The <see cref="GraphNodeModelLibraryItem"/> representing the block to create.
        /// </summary>
        public GraphNodeModelLibraryItem SelectedItem;

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The Context in which the block will be added.
        /// </summary>
        public ContextNodeModel ContextNodeModel;

        /// <summary>
        /// The index of the position at which the block will be added to the Context.
        /// </summary>
        public int OrderInContext;

        /// <summary>
        /// Initializes a new <see cref="CreateBlockFromItemLibraryCommand"/>.
        /// </summary>
        CreateBlockFromItemLibraryCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new <see cref="CreateBlockFromItemLibraryCommand"/>.
        /// </summary>
        /// <param name="selectedItem">The <see cref="GraphNodeModelLibraryItem"/> representing the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        /// <param name="contextNodeModel">The context in which to add the block.</param>
        /// <param name="orderInContext">The index of the position at which the Block is to be added to the Context.</param>
        public CreateBlockFromItemLibraryCommand(GraphNodeModelLibraryItem selectedItem,
                                             ContextNodeModel contextNodeModel = null,
                                              int orderInContext = -1,
                                              SerializableGUID guid = default) : this()
        {
            SelectedItem = selectedItem;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
            ContextNodeModel = contextNodeModel;
            OrderInContext = orderInContext;
        }

        /// <summary>
        /// Default command handler for <see cref="CreateBlockFromItemLibraryCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, CreateBlockFromItemLibraryCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                var undoableStates = selectionHelper.SelectionStates.Append<IUndoableStateComponent>(graphModelState);
                undoStateUpdater.SaveStates(undoableStates);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var selectionUpdaters = selectionHelper.UpdateScopes)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                foreach (var selectionUpdater in selectionUpdaters)
                {
                    selectionUpdater.ClearSelection();
                }

                var newModel = command.SelectedItem.CreateElement.Invoke(
                    new GraphBlockCreationData(graphModelState.GraphModel, guid: command.Guid, contextNodeModel: command.ContextNodeModel, orderInContext: command.OrderInContext));

                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
                selectionUpdaters.MainUpdateScope.SelectElement(newModel, true);
            }
        }
    }
}
