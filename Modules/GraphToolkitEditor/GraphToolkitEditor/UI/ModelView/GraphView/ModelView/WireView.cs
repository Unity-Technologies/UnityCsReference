// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.GraphVisualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The UI for an <see cref="WireModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class WireView : AbstractWire, IShowItemLibraryUI, IAnimatableView
    {
        /// <summary>
        /// The USS class name added to a <see cref="WireView"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-wire";

        /// <summary>
        /// The USS class name added to ghost wires.
        /// </summary>
        public static readonly string ghostUssClassName = ussClassName.WithUssModifier(ghostUssModifier);

        /// <summary>
        /// The name used for the <see cref="ModelViewPart"/> of the wire control.
        /// </summary>
        public static readonly string wireControlName = "wire-control";

        /// <summary>
        /// The name used for the <see cref="ModelViewPart"/> of the wire bubble.
        /// </summary>
        public static readonly string wireBubblePartName = "wire-bubble";

        WireManipulator m_WireManipulator;

        WireControl m_WireControl;

        ChildView m_LastUsedFromPort;
        ChildView m_LastUsedToPort;
        WireModel m_LastUsedWireModel;

        bool m_VisuallySelected;

        protected WireManipulator WireManipulator
        {
            get => m_WireManipulator;
            set => this.ReplaceManipulator(ref m_WireManipulator, value);
        }

        /// <inheritdoc />
        public override Vector2 GetFrom()
        {
            var p = Vector2.zero;

            var port = WireModel.FromPort;
            if (port == null)
            {
                if (WireModel is IGhostWireModel ghostWire)
                {
                    p = ghostWire.FromWorldPoint;
                }
            }
            else
            {
                var ui = port.GetView<Port>(RootView);
                if (ui == null)
                    return Vector2.zero;

                p = ui.GetGlobalCenter();
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
                var ui = port.GetView<Port>(RootView);
                if (ui == null)
                    return Vector2.zero;

                p = ui.GetGlobalCenter();
            }

            return this.WorldToLocal(p);
        }

        /// <summary>
        /// The WireControl that represents the wire.
        /// </summary>
        public WireControl WireControl => m_WireControl;

        public PortModel Output => WireModel.FromPort;

        public PortModel Input => WireModel.ToPort;

        internal override VisualElement SizeElement => WireControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireView"/> class.
        /// </summary>
        public WireView()
        {
            Layer = -1;

            WireManipulator = new WireManipulator();
        }

        /// <inheritdoc />
        public override void BuildUITree()
        {
            base.BuildUITree();

            m_WireControl = new WireControl(this) { name = wireControlName };
            m_WireControl.AddToClassList(ussClassName.WithUssElement(wireControlName));

            m_WireControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveWire);
            m_WireControl.RegisterCallback<MouseDownEvent>(OnMouseDownWire);
            m_WireControl.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            Insert(0, m_WireControl);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(WireBubblePart.Create(wireBubblePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            WireControl?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            AddToClassList(ussClassName);
            EnableInClassList(ghostUssClassName, Model is IGhostWireModel);
            this.AddPackageStylesheet("Wire.uss");
        }

        /// <inheritdoc />
        public override bool HasBackwardsDependenciesChanged()
        {
            return m_LastUsedFromPort != WireModel.FromPort?.GetView(RootView) || m_LastUsedToPort != WireModel.ToPort?.GetView(RootView);
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged() => m_LastUsedWireModel != WireModel;

        /// <inheritdoc/>
        public override void AddBackwardDependencies()
        {
            base.AddBackwardDependencies();

            // When the ports move, the wire should be redrawn.
            AddDependencies(WireModel.FromPort);
            AddDependencies(WireModel.ToPort);

            m_LastUsedFromPort = WireModel.FromPort.GetView(RootView);
            m_LastUsedToPort = WireModel.ToPort.GetView(RootView);

            void AddDependencies(PortModel portModel)
            {
                if (portModel == null)
                    return;

                var ui = portModel.GetView(RootView);
                if (ui != null)
                {
                    // Wire color changes with port color.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);

                    // When port geometry changes, the wire should follow.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }

                ui = portModel.NodeModel.GetView(RootView);
                if (ui != null)
                {
                    // Wire position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }

                ui = (portModel.NodeModel.Container as GraphElementModel)?.GetView(GraphView);
                if (ui != null)
                {
                    // Wire position changes with container's position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }
            }
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            var ui = WireModel.FromPort?.GetView<Port>(RootView);
            ui?.AddDependencyToWireModel(WireModel);

            ui = WireModel.ToPort?.GetView<Port>(RootView);
            ui?.AddDependencyToWireModel(WireModel);

            m_LastUsedWireModel = WireModel;
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            return WireControl.RectIntersectsLine(rectangle);
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return WireControl.IsPointOnLine(localPoint);
        }

        /// <summary>
        /// Gets the wire data required to create portals.
        /// </summary>
        /// <param name="wires">The wires that will be converted to portals.</param>
        /// <param name="rootView">The <see cref="RootView"/> that contains the portals.</param>
        internal static List<(WireModel, Vector2, Vector2)> GetPortalsWireData(IEnumerable<WireModel> wires, RootView rootView)
        {
            List<(WireModel, Vector2, Vector2)> wireData = new();

            foreach (var wireModel in wires)
            {
                var outputPort = wireModel.FromPort.GetView<Port>(rootView);
                var inputPort = wireModel.ToPort.GetView<Port>(rootView);
                var outputNode = wireModel.FromPort.NodeModel.GetView<NodeView>(rootView);
                var inputNode = wireModel.ToPort.NodeModel.GetView<NodeView>(rootView);
                var wire = wireModel.GetView<WireView>(rootView);

                if (outputNode == null || inputNode == null || outputPort == null || inputPort == null || wire == null)
                    continue;

                wireData.Add((wireModel,
                    outputPort.ChangeCoordinatesTo(wire.contentContainer, outputPort.layout.center),
                    inputPort.ChangeCoordinatesTo(wire.contentContainer, inputPort.layout.center)));
            }

            return wireData;
        }

        /// <inheritdoc/>
        public virtual bool ShowItemLibrary(Vector2 mousePosition)
        {
            var graphPosition = GraphView.ContentViewContainer.WorldToLocal(mousePosition);
            ItemLibraryService.ShowNodesForWire(GraphView, WireModel, mousePosition, item =>
            {
                if (item is GraphNodeModelLibraryItem nodeItem)
                    GraphView.Dispatch(CreateNodeCommand.OnWire(nodeItem, WireModel, graphPosition));
            });

            return true;
        }

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (WireControl != null)
                WireControl.Zoom = zoom;
        }

        /// <inheritdoc/>
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);
            if (WireModel?.GraphModel is not null )
            {
                m_WireControl.FromOrientation = WireModel.FromPort?.Orientation ?? (WireModel.ToPort?.Orientation ?? PortOrientation.Horizontal);
                m_WireControl.ToOrientation = WireModel.ToPort?.Orientation ?? (WireModel.FromPort?.Orientation ?? PortOrientation.Horizontal);
                m_WireControl.FromDirection = WireModel.FromPort?.Direction ?? GetReverseDirection(WireModel.ToPort?.Direction ?? PortDirection.Output);
                m_WireControl.ToDirection = WireModel.ToPort?.Direction ?? GetReverseDirection(WireModel.FromPort?.Direction ?? PortDirection.Input);

                if ((WireModel.ToPort is null || WireModel.FromPort is null))
                {
                    var(inputResult, outputResult) = WireModel.AddMissingPorts(out var inputNode, out var outputNode);

                    if (inputResult == PortMigrationResult.MissingPortAdded && inputNode != null)
                    {
                        var inputNodeUi = inputNode.GetView(GraphView);
                        inputNodeUi?.UpdateView(visitor);
                    }

                    if (outputResult == PortMigrationResult.MissingPortAdded && outputNode != null)
                    {
                        var outputNodeUi = outputNode.GetView(GraphView);
                        outputNodeUi?.UpdateView(visitor);
                    }
                }
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                m_WireControl.UpdateLayout();
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Animation) && IsAnimatable())
            {
                if (WireModel is { IsAnimating: true })
                    GraphView.Animator.Play(this, WireModel.AnimationSpeed);
                else
                    GraphView.Animator.Stop(this);
            }

            UpdateWireControlColors();
            m_WireControl.MarkDirtyRepaint();
        }

        public void SetAppearance(WireVisualData appearance)
        {
            if (appearance == null)
            {
                GraphView.Animator.Stop(this);
                m_WireControl.ResetIsDashed();
                m_WireControl.ResetLineWidth();
                m_WireControl.OpacityMultiplier = 1f;
            }
            else
            {
                if (appearance.IsAnimating)
                    GraphView.Animator.Play(this, appearance.AnimationSpeed);
                else
                    GraphView.Animator.Stop(this);

                m_WireControl.IsDashed = appearance.IsDashed;

                if (appearance.WidthOverride == 0)
                    m_WireControl.ResetLineWidth();
                else
                    m_WireControl.LineWidth = appearance.WidthOverride;

                m_WireControl.OpacityMultiplier = appearance.Opacity;
            }

            m_WireControl.MarkDirtyRepaint();
        }

        /// <inheritdoc />
        public override void UpdateSelectionVisuals(bool selected)
        {
            m_VisuallySelected = selected;
            base.UpdateSelectionVisuals(selected);
            UpdateWireControlColors();
        }

        /// <inheritdoc />
        public override bool CanBePartitioned()
        {
            return Model is not GhostWireModel && base.CanBePartitioned();
        }

        /// <inheritdoc />
        public override Rect GetBoundingBox()
        {
            return WireControl.layout;
        }

        static PortDirection GetReverseDirection(PortDirection direction)
        {
            switch (direction)
            {
                case PortDirection.Input:
                    return PortDirection.Output;
                default:
                    return PortDirection.Input;
            }
        }

        /// <summary>
        /// Allow updating the color of the wire.
        /// </summary>
        /// <param name="from">The color of the from side.</param>
        /// <param name="to">The color of the to side</param>
        /// <returns>True if the custom color should be used.</returns>
        protected virtual bool GetWireCustomColor(out Color from, out Color to)
        {
            from = Color.clear;
            to = Color.clear;
            return false;
        }

        /// <summary>
        /// Update the wire color base on the selection state, the port color, or a custom wire color.
        /// </summary>
        protected void UpdateWireControlColors()
        {
            if (m_VisuallySelected)
            {
                m_WireControl.ResetColor();
            }
            else if (GetWireCustomColor(out Color input, out Color output))
            {
                m_WireControl.SetColor(input, output);
            }
            else if (WireModel is IPlaceholder)
            {
                m_WireControl.SetColor(Color.red, Color.red);
            }
            else
            {
                var inputColor = Color.white;
                var outputColor = Color.white;

                if (WireModel?.ToPort != null)
                    inputColor = WireModel.ToPort.GetView<Port>(RootView)?.PortColor ?? Color.white;
                else if (WireModel?.FromPort != null)
                    inputColor = WireModel.FromPort.GetView<Port>(RootView)?.PortColor ?? Color.white;

                if (WireModel?.FromPort != null)
                    outputColor = WireModel.FromPort.GetView<Port>(RootView)?.PortColor ?? Color.white;
                else if (WireModel?.ToPort != null)
                    outputColor = WireModel.ToPort.GetView<Port>(RootView)?.PortColor ?? Color.white;

                if (WireModel is IGhostWireModel)
                {
                    inputColor = new Color(inputColor.r, inputColor.g, inputColor.b, 0.5f);
                    outputColor = new Color(outputColor.r, outputColor.g, outputColor.b, 0.5f);
                }

                m_WireControl.SetColor(inputColor, outputColor);
            }
        }

        /// <inheritdoc />
        public void BeginAnimating(float animationSpeed)
        {
            m_WireControl?.BeginAnimating(animationSpeed);
        }

        /// <inheritdoc />
        public void StopAnimating()
        {
            m_WireControl?.StopAnimating();
        }

        /// <inheritdoc />
        public void AnimationUpdate(double deltaTime)
        {
            m_WireControl?.AnimationUpdate(deltaTime);
        }

        void OnMouseDownWire(MouseDownEvent e)
        {
            if (e.target == m_WireControl)
            {
                m_WireControl.ResetColor();
            }
        }

        void OnMouseLeaveWire(MouseLeaveEvent e)
        {
            if (e.target == m_WireControl)
            {
                UpdateWireControlColors();
            }
        }
    }
}
