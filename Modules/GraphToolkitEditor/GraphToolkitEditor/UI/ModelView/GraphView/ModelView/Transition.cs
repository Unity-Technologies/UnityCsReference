// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class for transition UI.
    /// </summary>
    [UnityRestricted]
    internal class Transition : AbstractTransition
    {
        /// <summary>
        /// The USS class name added to a <see cref="Transition"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-transition";

        /// <summary>
        /// The USS class name added to ghost transitions.
        /// </summary>
        public static readonly string ghostUssClassName = ussClassName.WithUssModifier(ghostUssModifier);

        /// <summary>
        /// The USS class name added to local transitions.
        /// </summary>
        public static readonly string localTransitionUssClassName = ussClassName.WithUssModifier("local");

        /// <summary>
        /// The USS class name added to self transitions.
        /// </summary>
        public static readonly string selfTransitionUssClassName = ussClassName.WithUssModifier("self");

        /// <summary>
        /// The USS class name added to transitions triggered when entering a state machine.
        /// </summary>
        public static readonly string onEnterSelectorUssClassName = ussClassName.WithUssModifier("on-enter");

        /// <summary>
        /// The USS class name added to transitions between two states.
        /// </summary>
        public static readonly string stateToStateSelectorUssClassName = ussClassName.WithUssModifier("state-to-state");

        /// <summary>
        /// The name used for the <see cref="ModelViewPart"/> of the transition arrow.
        /// </summary>
        public static readonly string transitionArrowPartName = "transition-arrow";

        /// <summary>
        /// The name used for the <see cref="ModelViewPart"/> of the transition control.
        /// </summary>
        public static readonly string transitionControlPartName = "transition-control";

        /// <summary>
        /// The name for the "paste transitions as new" command.
        /// </summary>
        public static readonly string pasteTransitionsAsNewCommandName = "Paste Transitions as New";

        // Dependency tracking
#pragma warning disable CS0649
        ChildView m_LastUsedFromPort;
        ChildView m_LastUsedToPort;
#pragma warning restore CS0649
        Hash128 m_LastUsedFromNodeModelGuid;
        Hash128 m_LastUsedToNodeModelGuid;

        TransitionHoverDetector m_TransitionHoverDetector;
        TransitionSupportAnchorManipulator m_TransitionAnchorManipulator;

        TransitionControl m_TransitionControl;
        TransitionArrow m_TransitionArrow;

        bool m_ShowConnectors;

        /// <inheritdoc />
        public override bool Hovered
        {
            get => base.Hovered;
            set
            {
                base.Hovered = value;
                ShowHideConnectors();
            }
        }

        /// <summary>
        /// The transition hover detector.
        /// </summary>
        protected TransitionHoverDetector TransitionHoverDetector
        {
            get => m_TransitionHoverDetector;
            set => this.ReplaceManipulator(ref m_TransitionHoverDetector, value);
        }

        /// <summary>
        /// The transition anchor manipulator.
        /// </summary>
        protected TransitionSupportAnchorManipulator TransitionSupportAnchorManipulator
        {
            get => m_TransitionAnchorManipulator;
            set => this.ReplaceManipulator(ref m_TransitionAnchorManipulator, value);
        }

        /// <summary>
        /// The transition control.
        /// </summary>
        public TransitionControl TransitionControl
        {
            get
            {
                if (m_TransitionControl == null)
                {
                    var wireControlPart = PartList.GetPart(transitionControlPartName);
                    m_TransitionControl = wireControlPart?.Root as TransitionControl;
                }

                return m_TransitionControl;
            }
        }

        /// <summary>
        /// The transition arrow.
        /// </summary>
        protected TransitionArrow TransitionArrow
        {
            get
            {
                if (m_TransitionArrow == null)
                {
                    var wireControlPart = PartList.GetPart(transitionArrowPartName);
                    m_TransitionArrow = wireControlPart?.Root as TransitionArrow;
                }

                return m_TransitionArrow;
            }
        }

        /// <inheritdoc />
        public override Vector2 GetFrom()
        {
            var p = Vector2.zero;

            var port = WireModel.FromPort;
            if (port == null)
            {
                if (WireModel is IGhostWireModel ghostWireModel)
                {
                    p = ghostWireModel.FromWorldPoint;
                }
            }
            else
            {
                var ui = port.NodeModel.GetView<State>(RootView);
                if (ui == null)
                    return Vector2.zero;

                p = ui.GetFromPositionForTransition(TransitionModel);
            }

            return this.WorldToLocal(p);
        }

        /// <inheritdoc />
        public override Vector2 GetTo()
        {
            var p = Vector2.zero;

            var port = WireModel.ToPort;
            if (port == null)
            {
                if (WireModel is IGhostWireModel ghostWireModel)
                {
                    p = ghostWireModel.ToWorldPoint;
                }
            }
            else
            {
                var ui = port.NodeModel.GetView<State>(RootView);
                if (ui == null)
                    return Vector2.zero;

                p = ui.GetToPositionForTransition(TransitionModel);
            }

            return this.WorldToLocal(p);
        }

        /// <inheritdoc />
        internal override VisualElement SizeElement => TransitionModel.IsSingleStateTransition ? TransitionArrow : TransitionControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> class.
        /// </summary>
        public Transition()
        {
            Layer = -1;
            TransitionHoverDetector = new TransitionHoverDetector();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            if (TransitionModel.IsSingleStateTransition)
            {
                PartList.AppendPart(TransitionArrowPart.Create(transitionArrowPartName, WireModel, this, ussClassName));
            }
            else
            {
                PartList.AppendPart(TransitionControlPart.Create(transitionControlPartName, Model, this, ussClassName));
            }
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
            EnableInClassList(ghostUssClassName, Model is IGhostWireModel);
            EnableInClassList(selfTransitionUssClassName, TransitionModel.TransitionSupportKind == TransitionSupportKind.Self);
            EnableInClassList(localTransitionUssClassName, TransitionModel.TransitionSupportKind == TransitionSupportKind.Local);
            EnableInClassList(onEnterSelectorUssClassName, TransitionModel.TransitionSupportKind == TransitionSupportKind.OnEnter);
            EnableInClassList(stateToStateSelectorUssClassName, TransitionModel.TransitionSupportKind == TransitionSupportKind.StateToState);
            this.AddPackageStylesheet("Wire.uss");

            if (TransitionModel.IsSingleStateTransition)
            {
                m_TransitionAnchorManipulator = new TransitionSupportAnchorManipulator();
                this.AddManipulator(m_TransitionHoverDetector);
            }
        }

        /// <inheritdoc />
        public override void UpdateUISelection(UpdateSelectionVisitor visitor)
        {
            base.UpdateUISelection(visitor);
            ShowHideConnectors(true);
        }

        void ShowHideConnectors(bool forceUpdate = false)
        {
            if (!TransitionModel.IsSingleStateTransition)
            {
                var showConnectors = IsSelected() || Hovered;
                if (showConnectors && (forceUpdate || !m_ShowConnectors))
                {
                    var fromStateGuid = TransitionModel.FromNodeGuid;
                    var fromStateUI = fromStateGuid.GetView<GraphElement>(GraphView);
                    (fromStateUI as INodeWithConnector)?.ShowConnector(this);

                    var toStateGuid = TransitionModel.ToNodeGuid;
                    var toStateUI = toStateGuid.GetView<GraphElement>(GraphView);
                    (toStateUI as INodeWithConnector)?.ShowConnector(this);

                    m_ShowConnectors = true;
                }
                else if (!showConnectors && (forceUpdate || m_ShowConnectors))
                {
                    var fromStateGuid = TransitionModel.FromNodeGuid;
                    var fromStateUI = fromStateGuid.GetView<GraphElement>(GraphView);
                    (fromStateUI as INodeWithConnector)?.HideConnector(this);

                    var toStateGuid = TransitionModel.ToNodeGuid;
                    var toStateUI = toStateGuid.GetView<GraphElement>(GraphView);
                    (toStateUI as INodeWithConnector)?.HideConnector(this);

                    m_ShowConnectors = false;
                }
            }
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            var fromStateGuid = TransitionModel.FromNodeGuid;
            var fromStateUI = fromStateGuid.GetView<GraphElement>(GraphView);
            (fromStateUI as INodeWithConnector)?.HideConnector(this);

            var toStateGuid = TransitionModel.ToNodeGuid;
            var toStateUI = toStateGuid.GetView<GraphElement>(GraphView);
            (toStateUI as INodeWithConnector)?.HideConnector(this);

            base.RemoveFromRootView();
        }

        /// <inheritdoc />
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            var selection = GraphView.GetSelection();
            if (selection.Count == 0)
                return false;

            var destinationTransitionSupportModels = new List<TransitionSupportModel>();
            foreach (var element in selection)
            {
                if( element is TransitionSupportModel tsm)
                    destinationTransitionSupportModels.Add(tsm);
                else
                    return false;
            }

            return PasteOn(destinationTransitionSupportModels, operationName == ShortCutPasteWithoutWires.id);
        }

        internal static bool CanPasteTransitionsAsNew(CopyPasteData copyPaste)
        {
            if (copyPaste == null || copyPaste.Wires.Count == 0 || copyPaste.Nodes.Count != 0 || copyPaste.Placemats.Count != 0 || copyPaste.StickyNotes.Count != 0 || copyPaste.StickyNotes.Count != 0 || copyPaste.VariableDeclarations.Count != 0)
                return false;

            foreach (var wire in copyPaste.Wires)
            {
                if (wire is not TransitionSupportModel)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool PasteAsNew()
        {
            return PasteOn(new []{TransitionModel}, true);
        }

        bool PasteOn(IReadOnlyList<TransitionSupportModel> transitionSupportModels, bool additivePaste)
        {
            using var copyPasteData = GraphView.GraphTool.ClipboardProvider.DeserializeDataFromClipboard();
            if (!CanPasteTransitionsAsNew(copyPasteData))
                return false;

            var sourceTransitionSupportModels = new List<TransitionSupportModel>();
            foreach (var wire in copyPasteData.Wires)
            {
                if( wire is TransitionSupportModel tsm)
                    sourceTransitionSupportModels.Add(tsm);
            }

            GraphView.Dispatch(new PasteTransitionSupportsCommand(pasteTransitionsAsNewCommandName, transitionSupportModels, sourceTransitionSupportModels, additivePaste));

            return true;
        }

        /// <inheritdoc />
        public override bool HasForwardsDependenciesChanged()
        {
            return m_LastUsedFromNodeModelGuid != WireModel.FromNodeGuid || m_LastUsedToNodeModelGuid != WireModel.ToNodeGuid;
        }

        /// <inheritdoc />
        public override void AddForwardDependencies()
        {
            base.AddForwardDependencies();

            m_LastUsedFromNodeModelGuid = WireModel.FromNodeGuid;
            m_LastUsedToNodeModelGuid = WireModel.ToNodeGuid;

            var uiList = new List<ChildView>();
            m_LastUsedFromNodeModelGuid.AppendAllViews(GraphView, null, uiList);
            m_LastUsedToNodeModelGuid.AppendAllViews(GraphView, null, uiList);
            foreach (var childView in uiList)
            {
                Dependencies.AddForwardDependency(childView, DependencyTypes.Geometry);
                Dependencies.AddForwardDependency(childView, DependencyTypes.Style);
            }
        }

        /// <inheritdoc />
        public override bool HasBackwardsDependenciesChanged()
        {
            return m_LastUsedFromPort != WireModel.FromNodeGuid.GetView(RootView) || m_LastUsedToPort != WireModel.ToNodeGuid.GetView(RootView);
        }

        /// <inheritdoc />
        public override void AddBackwardDependencies()
        {
            base.AddBackwardDependencies();

            // When the ports move, the wire should be redrawn.
            AddDependencies(WireModel.FromNodeGuid);
            AddDependencies(WireModel.ToNodeGuid);

            return;

            void AddDependencies(Hash128 nodeModelGuid)
            {
                if (nodeModelGuid == default)
                    return;

                var ui = nodeModelGuid.GetView(RootView);
                if (ui != null)
                {
                    // Show connectors on node.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);
                    // Wire position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }

                if (WireModel.GraphModel.TryGetModelFromGuid(nodeModelGuid, out var model))
                {
                    ui = (model.Container as GraphElementModel)?.GetView(GraphView);
                    if (ui != null)
                    {
                        // Show connectors on node.
                        Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);
                        // Wire position changes with container's position.
                        Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            if (SizeElement != null)
            {
                if (SizeElement.Overlaps(this.ChangeCoordinatesTo(SizeElement, rectangle)))
                    return true;
            }

            return base.Overlaps(rectangle);
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (SizeElement != null)
            {
                if (SizeElement.ContainsPoint(this.ChangeCoordinatesTo(SizeElement, localPoint)))
                    return true;
            }

            return base.ContainsPoint(localPoint);
        }

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (TransitionControl != null)
                TransitionControl.Zoom = zoom;
        }

        /// <inheritdoc />
        public override bool CanBePartitioned()
        {
            return Model is not GhostTransitionSupportModel && base.CanBePartitioned();
        }

        /// <inheritdoc />
        public override Rect GetBoundingBox()
        {
            return SizeElement.layout;
        }
    }
}
