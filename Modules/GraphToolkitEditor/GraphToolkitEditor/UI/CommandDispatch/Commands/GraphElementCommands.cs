// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A command to reset the color of some graph element models
    /// </summary>
    [UnityRestricted]
    internal class ResetElementColorCommand : ModelCommand<GraphElementModel>
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
        [UsedImplicitly]
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
                    foreach (var model in command.Models)
                    {
                        if (model is IHasElementColor { ElementColor: { HasUserColor: true } } hasElementColor)
                            hasElementColor.SetColor(hasElementColor.DefaultColor);
                    }
                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the color of some graph element models
    /// </summary>
    [UnityRestricted]
    internal class ChangeElementColorCommand : ModelCommand<GraphElementModel, Color>
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            : base(k_UndoStringSingular, k_UndoStringPlural, color, elementModels?.OfType<GraphElementModel>().ToList())
#pragma warning restore UA2001
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
        [UsedImplicitly]
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
                    foreach (var model in command.Models)
                    {
                        if (model.IsColorable() && model is IHasElementColor hasElementColor)
                            hasElementColor.SetColor(command.Value);
                    }
                    updater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// Command to align nodes hierarchies in a graph view.
    /// </summary>
    [UnityRestricted]
    internal class AlignNodesCommand : UndoableCommand
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
        /// True if hierarchies should be aligned. Otherwise, only the nodes in <see cref="Nodes"/> are aligned.
        /// </summary>
        public readonly bool Follow;

        public readonly bool Reverse;

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
        /// <param name="reverse">True if it is the input node (instead of the output node) that should be repositioned for alignment.</param>
        /// <param name="nodes">A list of nodes to align.</param>
        public AlignNodesCommand(GraphView graphView, bool follow, bool reverse, IReadOnlyList<GraphElementModel> nodes) : this()
        {
            GraphView = graphView;
            Nodes = nodes;
            Follow = follow;
            Reverse = reverse;

            if (follow)
            {
                UndoString = "Align Hierarchies";
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="graphView">The GraphView in charge of aligning the nodes.</param>
        /// <param name="reverse">True if it is the input node (instead of the output node) that should be repositioned for alignment.</param>
        /// <param name="nodes">A list of nodes to align.</param>
        /// <param name="follow">True if hierarchies should be aligned. Otherwise, only the nodes in <paramref name="nodes"/> are aligned.</param>
        public AlignNodesCommand(GraphView graphView, bool follow, bool reverse, params GraphElementModel[] nodes)
            : this(graphView, follow, reverse, (IReadOnlyList<GraphElementModel>)nodes)
        {
        }

        /// <summary>
        /// Default handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AlignNodesCommand command)
        {
            if (command.Nodes.Count > 0)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveState(graphModelState);
                }

                using (var stateUpdater = graphModelState.UpdateScope)
                using (var changeScope = graphModelState.GraphModel.ChangeDescriptionScope)
                {
                    command.GraphView.PositionDependenciesManager.AlignNodes(command.Follow, command.Reverse, command.Nodes);
                    stateUpdater.MarkUpdated(changeScope.ChangeDescription);
                }
            }
        }
    }

    /// <summary>
    /// A command to trigger a display of the selection in the inspector.
    /// </summary>
    [UnityRestricted]
    internal class DisplayInInspectorCommand : ICommand
    {
        [UsedImplicitly]
        public static void DefaultCommandHandler(SelectionStateComponent mainSelectionState, DisplayInInspectorCommand command)
        {
            using (var mainUpdater = mainSelectionState.UpdateScope)
            {
                mainUpdater.DisplayInInspector();
            }
        }
    }

    /// <summary>
    /// A command to select a graph element models.
    /// </summary>
    [UnityRestricted]
    internal class SelectElementsCommand : ModelCommand<GraphElementModel>
    {
        /// <summary>
        /// Selection mode.
        /// </summary>
        [UnityRestricted]
        internal enum SelectionMode
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
        /// Whether the inspector should update itself to display the selection.
        /// </summary>
        public readonly bool DisplayInInspector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        public SelectElementsCommand()
            : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="displayInInspector">Whether the command should trigger an inspector refresh.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, bool displayInInspector, IReadOnlyList<GraphElementModel> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Mode = mode;
            DisplayInInspector = displayInInspector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="displayInInspector">Whether the command should trigger an inspector refresh.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, bool displayInInspector, params GraphElementModel[] models)
            : this(mode, displayInInspector, (IReadOnlyList<GraphElementModel>)models)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, IReadOnlyList<GraphElementModel> models)
            : this(mode, true, models)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectElementsCommand" /> class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, params GraphElementModel[] models)
            : this(mode, true, (IReadOnlyList<GraphElementModel>)models)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="mainSelectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
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

                    updaters.MainUpdateScope.SelectElements(command.Models, true, command.DisplayInInspector);
                }
            }
            else
            {
                switch (command.Mode)
                {
                    case SelectionMode.Add:
                        if (command.Models.TrueForAll(mainSelectionState.IsSelected))
                            return;
                        break;

                    case SelectionMode.Remove:
                        if (command.Models.TrueForAll(m => !mainSelectionState.IsSelected(m)))
                            return;
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
                            mainUpdater.SelectElements(command.Models, true, command.DisplayInInspector);
                            break;
                        case SelectionMode.Remove:
                            mainUpdater.SelectElements(command.Models, false, command.DisplayInInspector);
                            break;
                        case SelectionMode.Toggle:
                            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            var toSelect = command.Models.Where(m => !mainSelectionState.IsSelected(m)).ToList();
#pragma warning restore UA2001
                            mainUpdater.SelectElements(command.Models, false, false);
                            mainUpdater.SelectElements(toSelect, true, command.DisplayInInspector);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// A command to clear the selection.
    /// </summary>
    [UnityRestricted]
    internal class ClearSelectionCommand : UndoableCommand
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
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, SelectionStateComponent selectionState, ClearSelectionCommand command)
        {
            var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

            if (selectionHelper.SelectionStates.TrueForAll(s => s.IsSelectionEmpty))
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
                    updater.DisplayInInspector();
                }
            }
        }
    }

    /// <summary>
    /// A command to change the position and size of an element.
    /// </summary>
    [UnityRestricted]
    internal class ChangeElementLayoutCommand : UndoableCommand
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
        [UsedImplicitly]
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

    /// <summary>
    /// A command to change a placemat layout and bring placemats to front at the same time.
    /// </summary>
    [UnityRestricted]
    internal class ChangePlacematLayoutAndBringPlacematToFrontCommand : ChangeElementLayoutCommand
    {
        IReadOnlyList<(PlacematModel, PlacematModel)> m_PlacematModelsToBringInFrontOf;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePlacematLayoutAndBringPlacematToFrontCommand"/> class.
        /// </summary>
        /// <param name="resizableModel">The model to resize.</param>
        /// <param name="newLayout">The new position and size.</param>
        /// <param name="placematModelsToBringInFrontOf">The placemats to bring in front.</param>
        public ChangePlacematLayoutAndBringPlacematToFrontCommand(IResizable resizableModel, Rect newLayout, IReadOnlyList<(PlacematModel, PlacematModel)> placematModelsToBringInFrontOf)
            : base(resizableModel, newLayout)
        {
            m_PlacematModelsToBringInFrontOf = placematModelsToBringInFrontOf;
        }

        /// <summary>
        /// Default command handler for <see cref="ChangePlacematLayoutAndBringPlacematToFrontCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        [UsedImplicitly]
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, ChangePlacematLayoutAndBringPlacematToFrontCommand command)
        {
            if (command.Model is not PlacematModel || command.Model.PositionAndSize == command.Layout)
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

                MoveElementsAndBringPlacematToFrontCommand.BringPlacematsToFront(command.m_PlacematModelsToBringInFrontOf);
            }
        }
    }
}
