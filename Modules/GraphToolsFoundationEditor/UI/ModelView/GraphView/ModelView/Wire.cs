// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The UI for an <see cref="WireModel"/>.
    /// </summary>
    class Wire : GraphElement, IShowItemLibraryUI_Internal
    {
        public new static readonly string ussClassName = "ge-wire";
        public static readonly string ghostModifierUssClassName = ussClassName.WithUssModifier("ghost");

        public static readonly string wireControlPartName = "wire-control";
        public static readonly string wireBubblePartName = "wire-bubble";

        WireManipulator m_WireManipulator;

        WireControl m_WireControl;

        ModelView m_LastUsedFromPort;
        ModelView m_LastUsedToPort;
        WireModel m_LastUsedWireModel;

        protected WireManipulator WireManipulator
        {
            get => m_WireManipulator;
            set => this.ReplaceManipulator(ref m_WireManipulator, value);
        }

        public WireModel WireModel => Model as WireModel;

        public bool IsGhostWire => WireModel is IGhostWire;

        public Vector2 From
        {
            get
            {
                var p = Vector2.zero;

                var port = WireModel.FromPort;
                if (port == null)
                {
                    if (WireModel is IGhostWire ghostWire)
                    {
                        p = ghostWire.EndPoint;
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
        }

        public Vector2 To
        {
            get
            {
                var p = Vector2.zero;

                var port = WireModel.ToPort;
                if (port == null)
                {
                    if (WireModel is GhostWireModel ghostWireModel)
                    {
                        p = ghostWireModel.EndPoint;
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
        }

        public WireControl WireControl
        {
            get
            {
                if (m_WireControl == null)
                {
                    var wireControlPart = PartList.GetPart(wireControlPartName);
                    m_WireControl = wireControlPart?.Root as WireControl;
                }

                return m_WireControl;
            }
        }

        public PortModel Output => WireModel.FromPort;

        public PortModel Input => WireModel.ToPort;

        /// <inheritdoc />
        public override bool ShowInMiniMap => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wire"/> class.
        /// </summary>
        public Wire()
        {
            Layer = -1;

            WireManipulator = new WireManipulator();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(WireControlPart.Create(wireControlPartName, Model, this, ussClassName));
            PartList.AppendPart(WireBubblePart.Create(wireBubblePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            EnableInClassList(ghostModifierUssClassName, IsGhostWire);
            this.AddStylesheet_Internal("Wire.uss");
        }

        /// <inheritdoc />
        public override bool HasBackwardsDependenciesChanged()
        {
            return m_LastUsedFromPort != WireModel.FromPort?.GetView_Internal(RootView) || m_LastUsedToPort != WireModel.ToPort?.GetView_Internal(RootView);
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

            m_LastUsedFromPort = WireModel.FromPort.GetView_Internal(RootView);
            m_LastUsedToPort = WireModel.ToPort.GetView_Internal(RootView);

            void AddDependencies(PortModel portModel)
            {
                if (portModel == null)
                    return;

                var ui = portModel.GetView_Internal(RootView);
                if (ui != null)
                {
                    // Wire color changes with port color.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);
                }

                ui = portModel.NodeModel.GetView_Internal(RootView);
                if (ui != null)
                {
                    // Wire position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }

                ui = (portModel.NodeModel.Container as GraphElementModel)?.GetView_Internal(GraphView);
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
            return WireControl.Overlaps(this.ChangeCoordinatesTo(WireControl, rectangle));
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return WireControl.ContainsPoint(this.ChangeCoordinatesTo(WireControl, localPoint));
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Wire wire))
                return;

            var selection = GraphView.GetSelection().ToList();

            // If any element in the selection is not a wire, the graph view context menu is opened instead.
            if (selection.Any(ge => !ge.GetType().IsAssignableFrom(typeof(WireModel))))
                return;

            evt.menu.AppendAction("Insert Node on Wire", menuAction =>
            {
                var mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                ShowItemLibrary(mousePosition);
            });

            // TODO OYT (GTF-918) : When Junction Points are implemented, a menu item needs to be added to create a Junction Point. The menu item name will be "Add Junction Point".
            var wires = selection.OfType<WireModel>().ToList();
            if (wires.Count > 0)
            {
                var wireData = wires.Select(
                    wireModel =>
                    {
                        var outputPort = wireModel.FromPort.GetView<Port>(GraphView);
                        var inputPort = wireModel.ToPort.GetView<Port>(GraphView);
                        var outputNode = wireModel.FromPort.NodeModel.GetView<Node>(GraphView);
                        var inputNode = wireModel.ToPort.NodeModel.GetView<Node>(GraphView);

                        if (outputNode == null || inputNode == null || outputPort == null || inputPort == null)
                            return (null, Vector2.zero, Vector2.zero);

                        return (wireModel,
                            outputPort.ChangeCoordinatesTo(contentContainer, outputPort.layout.center),
                            inputPort.ChangeCoordinatesTo(contentContainer, inputPort.layout.center));
                    }
                ).Where(tuple => tuple.Item1 != null).ToList();

                evt.menu.AppendAction("Add Portals", _ =>
                {
                    GraphView.Dispatch(new ConvertWiresToPortalsCommand(wireData));
                });
            }

            if (wire.WireModel.FromPort?.HasReorderableWires ?? false)
            {
                var initialMenuItemCount = evt.menu.MenuItems().Count;

                if (initialMenuItemCount > 0)
                    evt.menu.AppendSeparator();

                var siblingWires = wire.WireModel.FromPort.GetConnectedWires().ToList();
                var siblingWiresCount = siblingWires.Count;

                var index = siblingWires.IndexOf(wire.WireModel);
                evt.menu.AppendAction("Reorder Wire/Move First",
                    _ => ReorderWires(ReorderType.MoveFirst),
                    siblingWiresCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Reorder Wire/Move Up",
                    _ => ReorderWires(ReorderType.MoveUp),
                    siblingWiresCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Reorder Wire/Move Down",
                    _ => ReorderWires(ReorderType.MoveDown),
                    siblingWiresCount > 1 && index < siblingWiresCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Reorder Wire/Move Last",
                    _ => ReorderWires(ReorderType.MoveLast),
                    siblingWiresCount > 1 && index < siblingWiresCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                void ReorderWires(ReorderType reorderType)
                {
                    GraphView.Dispatch(new ReorderWireCommand(wire.WireModel, reorderType));
                }
            }

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Delete", _ =>
            {
                RootView.Dispatch(new DeleteElementsCommand(selection.ToList()));
            }, selection.Any(ge => ge.IsDeletable()) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.StopPropagation();
        }

        /// <inheritdoc/>
        public virtual bool ShowItemLibrary(Vector2 mousePosition)
        {
            var graphPosition = GraphView.ContentViewContainer.WorldToLocal(mousePosition);
            var stencil = GraphView.GraphModel.Stencil as Stencil;
            ItemLibraryService.ShowNodesForWire(stencil, GraphView, WireModel, mousePosition, item =>
            {
                GraphView.Dispatch(CreateNodeCommand.OnWire(item, WireModel, graphPosition));
            });

            return true;
        }

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if( WireControl != null)
                WireControl.Zoom = zoom;
        }
    }
}
