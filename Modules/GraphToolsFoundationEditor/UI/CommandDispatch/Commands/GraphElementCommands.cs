// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A command to reset the color of some graph element models
    /// </summary>
    class ResetElementColorCommand : ModelCommand<GraphElementModel>
    {
        const string k_UndoStringSingular = "Reset Element Color";
        const string k_UndoStringPlural = "Reset Elements Color";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetElementColorCommand" /> class.
        /// </summary>
        public ResetElementColorCommand()
            : base(k_UndoStringSingular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetElementColorCommand" /> class.
        /// </summary>
        /// <param name="models">Element models to reset</param>
        public ResetElementColorCommand(IReadOnlyList<GraphElementModel> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetElementColorCommand" /> class.
        /// </summary>
        /// <param name="models">Element models to reset</param>
        public ResetElementColorCommand(params GraphElementModel[] models)
            : this((IReadOnlyList<GraphElementModel>)models)
        {
        }

        /// <summary>
        /// Default command handler
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ResetElementColorCommand command)
        {
            if (command.Models != null)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var updater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    var colorableModels = command.Models.Where(c => c.IsColorable());
                    foreach (var model in colorableModels)
                    {
                        model.ResetColor();
                    }
                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the color of some graph element models
    /// </summary>
    class ChangeElementColorCommand : ModelCommand<GraphElementModel, Color>
    {
        const string k_UndoStringSingular = "Change Element Color";
        const string k_UndoStringPlural = "Change Elements Color";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementColorCommand" /> class.
        /// </summary>
        public ChangeElementColorCommand()
            : base(k_UndoStringSingular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementColorCommand" /> class.
        /// </summary>
        /// <param name="color">The color to set</param>
        /// <param name="elementModels">Element models to affect</param>
        public ChangeElementColorCommand(Color color, IReadOnlyList<GraphElementModel> elementModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, color, elementModels)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementColorCommand" /> class.
        /// </summary>
        /// <param name="color">The color to set</param>
        /// <param name="elementModels">Element models to affect</param>
        public ChangeElementColorCommand(Color color, IEnumerable<Model> elementModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, color, elementModels?.OfType<GraphElementModel>().ToList() )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementColorCommand" /> class.
        /// </summary>
        /// <param name="color">The color to set</param>
        /// <param name="elementModels">Element models to affect</param>
        public ChangeElementColorCommand(Color color, params GraphElementModel[] elementModels)
            : this(color, (IReadOnlyList<GraphElementModel>)elementModels)
        {
        }

        /// <summary>
        /// Default command handler
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeElementColorCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            if (command.Models != null)
            {
                using (var updater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    var colorableModels = command.Models.Where(c => c.IsColorable()).ToList();
                    foreach (var model in colorableModels)
                    {
                        model.Color = command.Value;
                    }
                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to align nodes hierarchies in a graph view.
    /// </summary>
    class AlignNodesCommand : UndoableCommand
    {
        /// <summary>
        /// The GraphView in charge of aligning the nodes.
        /// </summary>
        public readonly GraphView GraphView;
        /// <summary>
        /// A list of nodes to align.
        /// </summary>
        public readonly IReadOnlyList<GraphElementModel> Nodes;
        /// <summary>
        /// True if hierarchies should be aligned. Otherwise, only the nodes in <cref name="Nodes"/> are aligned.
        /// </summary>
        public readonly bool Follow;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AlignNodesCommand()
        {
            UndoString = "Align Items";
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="graphView">The GraphView in charge of aligning the nodes.</param>
        /// <param name="follow">True if hierarchies should be aligned. Otherwise, only the nodes in <paramref name="nodes"/> are aligned.</param>
        /// <param name="nodes">A list of nodes to align.</param>
        public AlignNodesCommand(GraphView graphView, bool follow, IReadOnlyList<GraphElementModel> nodes) : this()
        {
            GraphView = graphView;
            Nodes = nodes;
            Follow = follow;

            if (follow)
            {
                UndoString = "Align Hierarchies";
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="graphView">The GraphView in charge of aligning the nodes.</param>
        /// <param name="nodes">A list of nodes to align.</param>
        /// <param name="follow">True if hierarchies should be aligned. Otherwise, only the nodes in <paramref name="nodes"/> are aligned.</param>
        public AlignNodesCommand(GraphView graphView, bool follow, params GraphElementModel[] nodes)
            : this(graphView, follow, (IReadOnlyList<GraphElementModel>)nodes)
        {
        }

        /// <summary>
        /// Default handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AlignNodesCommand command)
        {
            if (command.Nodes.Any())
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var stateUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    command.GraphView.PositionDependenciesManager_Internal.AlignNodes(command.Follow, command.Nodes);
                    stateUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// A command to select a graph element models.
    /// </summary>
    class SelectElementsCommand : ModelCommand<GraphElementModel>
    {
        /// <summary>
        /// Selection mode.
        /// </summary>
        public enum SelectionMode
        {
            /// <summary>
            /// Replace the selection.
            /// </summary>
            Replace,
            /// <summary>
            /// Add to the selection.
            /// </summary>
            Add,
            /// <summary>
            /// Remove from the selection.
            /// </summary>
            Remove,
            /// <summary>
            /// If the element is not currently selected,
            /// add it to the selection. Otherwise remove it from the selection.
            /// </summary>
            Toggle,
        }

        const string k_UndoStringSingular = "Select Element";
        const string k_UndoStringPlural = "Select Elements";

        /// <summary>
        /// The selection mode.
        /// </summary>
        public SelectionMode Mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        public SelectElementsCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, IReadOnlyList<GraphElementModel> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, params GraphElementModel[] models)
            : this(mode, (IReadOnlyList<GraphElementModel>)models)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="mainSelectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent mainSelectionState, SelectElementsCommand command)
        {
            if ((command.Mode == SelectionMode.Add || command.Mode == SelectionMode.Remove || command.Mode == SelectionMode.Toggle) &&
                command.Models.Count == 0)
            {
                return;
            }

            if (command.Mode == SelectionMode.Replace)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(mainSelectionState);

                var currentSelection = new HashSet<GraphElementModel>();
                foreach (var selectionStateComponent in selectionHelper.SelectionStates)
                {
                    currentSelection.UnionWith(selectionStateComponent.GetSelection(graphModelState.GraphModel));
                }

                currentSelection.SymmetricExceptWith(command.Models);
                if (currentSelection.Count == 0)
                {
                    // currentSelection is already the same as command.Models
                    return;
                }

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
                }

                using (var updaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in updaters)
                    {
                        updater.ClearSelection();
                    }

                    updaters.MainUpdateScope.SelectElements(command.Models, true);
                }
            }
            else
            {
                switch (command.Mode)
                {
                    case SelectionMode.Add:
                        if (command.Models.All(mainSelectionState.IsSelected))
                        {
                            return;
                        }
                        break;

                    case SelectionMode.Remove:
                        if (command.Models.All(m => !mainSelectionState.IsSelected(m)))
                        {
                            return;
                        }
                        break;
                }

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(mainSelectionState);
                }

                using (var mainUpdater = mainSelectionState.UpdateScope)
                {
                    switch (command.Mode)
                    {
                        case SelectionMode.Add:
                            mainUpdater.SelectElements(command.Models, true);
                            break;
                        case SelectionMode.Remove:
                            mainUpdater.SelectElements(command.Models, false);
                            break;
                        case SelectionMode.Toggle:
                            var toSelect = command.Models.Where(m => !mainSelectionState.IsSelected(m)).ToList();
                            mainUpdater.SelectElements(command.Models, false);
                            mainUpdater.SelectElements(toSelect, true);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// A command to clear the selection.
    /// </summary>
    class ClearSelectionCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Clear Selection";

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearSelectionCommand" /> class.
        /// </summary>
        public ClearSelectionCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, ClearSelectionCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            if (selectionHelper.SelectionStates.All(s => s.IsSelectionEmpty))
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(selectionHelper.SelectionStates);
            }

            using (var updaters = selectionHelper.UpdateScopes)
            {
                foreach (var updater in updaters)
                {
                    updater.ClearSelection();
                }
            }
        }
    }

    /// <summary>
    /// A command to change the position and size of an element.
    /// </summary>
    class ChangeElementLayoutCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Resize Element";

        /// <summary>
        /// The model to resize.
        /// </summary>
        public IResizable Model;
        /// <summary>
        /// The new layout.
        /// </summary>
        public Rect Layout;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementLayoutCommand" /> class.
        /// </summary>
        public ChangeElementLayoutCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeElementLayoutCommand" /> class.
        /// </summary>
        /// <param name="resizableModel">The model to resize.</param>
        /// <param name="newLayout">The new position and size.</param>
        public ChangeElementLayoutCommand(IResizable resizableModel, Rect newLayout)
            : this()
        {
            Model = resizableModel;
            Layout = newLayout;
        }

        /// <summary>
        /// Default command handler for ChangeElementLayoutCommand.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangeElementLayoutCommand command)
        {
            if (command.Model.PositionAndSize == command.Layout)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
            {
                command.Model.PositionAndSize = command.Layout;
                graphUpdater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
