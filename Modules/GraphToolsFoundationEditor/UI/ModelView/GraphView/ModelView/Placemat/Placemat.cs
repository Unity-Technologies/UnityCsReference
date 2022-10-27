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
    /// <summary>
    /// UI for placemats.
    /// </summary>
    class Placemat : GraphElement
    {
        enum MinSizePolicy
        {
            EnsureMinSize,
            DoNotEnsureMinSize
        }

        protected internal static readonly Vector2 k_DefaultCollapsedSize_Internal = new Vector2(200, 42);
        protected static readonly int k_SelectRectOffset = 3;

        public new static readonly string ussClassName = "ge-placemat";
        public static readonly string collapsedModifierUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string resizerPartName = "resizer";

        protected internal static readonly float k_Bounds_Internal = 9.0f;
        protected internal static readonly float k_BoundTop_Internal = 29.0f; // Current height of Title

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        protected static readonly float k_MinWidth = 200;
        protected static readonly float k_MinHeight = 100;

        protected VisualElement m_ContentContainer;

        protected HashSet<GraphElement> m_CollapsedElements = new HashSet<GraphElement>();

        public PlacematModel PlacematModel => Model as PlacematModel;

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// The size of the placemat in its uncollapsed state.
        /// </summary>
        protected Vector2 UncollapsedSize => PlacematModel.PositionAndSize.size;

        /// <summary>
        /// The size of the placemat in its collapsed state.
        /// </summary>
        protected Vector2 CollapsedSize
        {
            get
            {
                var actualCollapsedSize = k_DefaultCollapsedSize_Internal;
                if (UncollapsedSize.x < k_DefaultCollapsedSize_Internal.x)
                    actualCollapsedSize.x = UncollapsedSize.x;

                return actualCollapsedSize;
            }
        }

        protected Rect EffectArea => PlacematModel.Collapsed ? new Rect(layout.position, UncollapsedSize) : layout;

        /// <summary>
        /// The graph elements that are currently being hidden by the placemat.
        /// </summary>
        protected IEnumerable<GraphElement> CollapsedElements
        {
            get => m_CollapsedElements;
            set
            {
                foreach (var collapsedElement in m_CollapsedElements)
                {
                    collapsedElement.style.visibility = StyleKeyword.Null;
                }

                m_CollapsedElements.Clear();

                if (value == null)
                    return;

                foreach (var collapsedElement in value)
                {
                    collapsedElement.style.visibility = Visibility.Hidden;
                    m_CollapsedElements.Add(collapsedElement);
                }
            }
        }

        public Placemat()
        {
            focusable = true;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void BuildPartList()
        {
            var editableTitlePart = EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName, false, true, false);
            PartList.AppendPart(editableTitlePart);
            var collapseButtonPart = CollapseButtonPart.Create(collapseButtonPartName, Model, this, ussClassName);
            editableTitlePart.PartList.AppendPart(collapseButtonPart);
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.SafeQ(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);

            this.AddStylesheet_Internal("Placemat.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            style.backgroundColor = PlacematModel.Color;

            SetPositionAndSize(PlacematModel.PositionAndSize);

            EnableInClassList(collapsedModifierUssClassName, PlacematModel.Collapsed);

            if (PlacematModel.Collapsed)
            {
                var collapsedElements = new List<GraphElement>();
                if (PlacematModel.HiddenElements != null)
                {
                    foreach (var elementModel in PlacematModel.HiddenElements)
                    {
                        var graphElement = elementModel.GetView<GraphElement>(RootView);
                        if (graphElement != null)
                            collapsedElements.Add(graphElement);
                    }
                }

                GatherCollapsedWires(collapsedElements);
                CollapsedElements = collapsedElements;
            }
            else
            {
                CollapsedElements = null;
            }
        }

        // PF FIXME: we can probably improve the performance of this.
        // Idea: build a bounding box of placemats affected by currentPlacemat and use this BB to intersect with nodes.
        // PF TODO: also revisit Placemat other recursive functions for perf improvements.
        protected static void GatherDependencies(Placemat currentPlacemat, IList<ModelView> graphElements, ICollection<ModelView> dependencies)
        {
            if (currentPlacemat.PlacematModel.Collapsed)
            {
                foreach (var cge in currentPlacemat.CollapsedElements)
                {
                    dependencies.Add(cge);
                    if (cge is Placemat placemat)
                        GatherDependencies(placemat, graphElements, dependencies);
                }

                return;
            }

            // We want gathering dependencies to work even if the placemat layout is not up to date, so we use the
            // currentPlacemat.PlacematModel.PositionAndSize to do our overlap test.
            var currRect = currentPlacemat.PlacematModel.PositionAndSize;
            var currentActivePlacematRect = RectUtils_Internal.Inflate(currRect, -k_SelectRectOffset, -k_SelectRectOffset,
                -k_SelectRectOffset, -k_SelectRectOffset);

            var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(currentActivePlacematRect))
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                    {
                        GatherDependencies(placemat, graphElements, dependencies);
                    }

                    if (placemat == null || placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                    {
                        dependencies.Add(elem);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool HasForwardsDependenciesChanged()
        {
            // Would need to look into making this fast... Always re-add the dependencies for now.
            return true;
        }

        static readonly List<ModelView> k_AddForwardDependenciesAllUIs = new List<ModelView>();
        /// <inheritdoc/>
        public override void AddForwardDependencies()
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is WireModel))
                .GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_AddForwardDependenciesAllUIs);

            var dependencies = new List<ModelView>();
            GatherDependencies(this, k_AddForwardDependenciesAllUIs, dependencies);
            k_AddForwardDependenciesAllUIs.Clear();

            var nodeModels = dependencies.Select(e => e.Model).OfType<AbstractNodeModel>();
            foreach (var wireModel in nodeModels.SelectMany(n => n.GetConnectedWires()))
            {
                var ui = wireModel.GetView_Internal(RootView);
                if (ui != null)
                {
                    // Wire models endpoints need to be updated when the placemat is collapsed/uncollapsed.
                    Dependencies.AddForwardDependency(ui, DependencyTypes.Geometry | DependencyTypes.Removal);
                }
            }
        }

        protected void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            this.CollapsePlacemat(evt.newValue);
        }

        /// <summary>
        /// Sets the position and the size of the placemat.
        /// </summary>
        /// <param name="positionAndSize">The position and size.</param>
        public void SetPositionAndSize(Rect positionAndSize)
        {
            if (PlacematModel.Collapsed)
                positionAndSize.size = CollapsedSize;

            SetPosition(positionAndSize.position);
            if (!PositionIsOverriddenByManipulator)
            {
                style.height = positionAndSize.height;
                style.width = positionAndSize.width;
            }
        }

        protected static IEnumerable<GraphElement> AllCollapsedElements(IEnumerable<GraphElement> collapsedElements)
        {
            if (collapsedElements != null)
            {
                foreach (var element in collapsedElements)
                {
                    switch (element)
                    {
                        case Placemat placemat when placemat.PlacematModel.Collapsed:
                        {
                            // TODO: evaluate performance of this recursive call.
                            foreach (var subElement in AllCollapsedElements(placemat.CollapsedElements))
                                yield return subElement;
                            yield return element;
                            break;
                        }
                        case Placemat placemat when !placemat.PlacematModel.Collapsed:
                            yield return element;
                            break;
                        case {} e when e.IsMovable():
                            yield return element;
                            break;
                    }
                }
            }
        }

        protected void GatherCollapsedWires(ICollection<GraphElement> collapsedElements)
        {
            var allCollapsedNodes = AllCollapsedElements(collapsedElements)
                .Select(e => e.Model)
                .OfType<AbstractNodeModel>()
                .ToList();
            foreach (var wireModel in PlacematModel.GraphModel.WireModels)
            {
                if (AnyNodeIsConnectedToPort(allCollapsedNodes, wireModel.ToPort) && AnyNodeIsConnectedToPort(allCollapsedNodes, wireModel.FromPort))
                {
                    var wireView = wireModel.GetView<GraphElement>(RootView);
                    if (!collapsedElements.Contains(wireView))
                    {
                        collapsedElements.Add(wireView);
                    }
                }
            }
        }

        static readonly List<ModelView> k_GatherCollapsedElementsAllUIs = new List<ModelView>();
        protected internal List<GraphElementModel> GatherCollapsedElements_Internal()
        {
            List<GraphElement> collapsedElements = new List<GraphElement>();

            var graphView = GraphView;
            graphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is WireModel))
                .GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_GatherCollapsedElementsAllUIs);

            var collapsedElementsElsewhere = new List<GraphElement>();
            RecurseGatherCollapsedElements(this, k_GatherCollapsedElementsAllUIs.OfType<GraphElement>(), collapsedElementsElsewhere);
            k_GatherCollapsedElementsAllUIs.Clear();

            var nodes = new HashSet<AbstractNodeModel>(AllCollapsedElements(collapsedElements).Select(e => e.Model).OfType<AbstractNodeModel>());

            for (var index = 0; index < graphView.GraphModel.WireModels.Count; index++)
            {
                var wireModel = graphView.GraphModel.WireModels[index];
                if (AnyNodeIsConnectedToPort(nodes, wireModel.ToPort) && AnyNodeIsConnectedToPort(nodes, wireModel.FromPort))
                    collapsedElements.Add(wireModel.GetView<Wire>(graphView));
            }

            foreach (var ge in collapsedElementsElsewhere)
                collapsedElements.Remove(ge);

            return collapsedElements.Select(e => e.GraphElementModel).ToList();

            void RecurseGatherCollapsedElements(Placemat currentPlacemat, IEnumerable<GraphElement> graphElementsParam,
                List<GraphElement> collapsedElementsElsewhereParam)
            {
                var currRect = currentPlacemat.EffectArea;
                var currentActivePlacematRect = RectUtils_Internal.Inflate(currRect, -k_SelectRectOffset, -k_SelectRectOffset, -k_SelectRectOffset, -k_SelectRectOffset);

                var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                foreach (var elem in graphElementsParam)
                {
                    if (elem.layout.Overlaps(currentActivePlacematRect))
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                        {
                            if (placemat.PlacematModel.Collapsed)
                                foreach (var cge in placemat.CollapsedElements)
                                    collapsedElementsElsewhereParam.Add(cge);
                            else
                                RecurseGatherCollapsedElements(placemat, graphElementsParam, collapsedElementsElsewhereParam);
                        }

                        if (placemat == null || placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                            if (elem.resolvedStyle.visibility == Visibility.Visible)
                                collapsedElements.Add(elem);
                    }
                }
            }
        }

        protected static bool AnyNodeIsConnectedToPort(IEnumerable<AbstractNodeModel> nodes, PortModel port)
        {
            if (port.NodeModel == null)
            {
                return false;
            }

            foreach (var node in nodes)
            {
                if (node == port.NodeModel)
                    return true;
            }

            return false;
        }

        [EventInterest(typeof(PointerDownEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            if (evt is PointerDownEvent mde)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                {
                    var models = new List<GraphElementModel>();
                    ActOnGraphElementsOver(e =>
                    {
                        models.Add(e.GraphElementModel);
                    });
                    GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, models));
                }
        }

        static readonly List<ModelView> k_ActOnGraphElementsOverAllUIs = new List<ModelView>();
        protected void ActOnGraphElementsOver(Action<GraphElement> act)
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is WireModel))
                .GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOverAllUIs);

            foreach (var elem in k_ActOnGraphElementsOverAllUIs.OfType<GraphElement>())
            {
                if (elem.layout.Overlaps(layout))
                    act(elem);
            }

            k_ActOnGraphElementsOverAllUIs.Clear();
        }

        static readonly List<ModelView> k_ActOnGraphElementsOver2AllUIs = new List<ModelView>();
        protected internal bool ActOnGraphElementsOver_Internal(Func<GraphElement, bool> act, bool includePlacemats)
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is WireModel))
                .GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOver2AllUIs);

            var retVal = RecurseActOnGraphElementsOver(this);
            k_ActOnGraphElementsOver2AllUIs.Clear();
            return retVal;

            bool RecurseActOnGraphElementsOver(Placemat currentPlacemat)
            {
                if (currentPlacemat.PlacematModel.Collapsed)
                {
                    var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                    foreach (var elem in currentPlacemat.CollapsedElements)
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                            if (RecurseActOnGraphElementsOver(placemat))
                                return true;

                        if (placemat == null || (includePlacemats && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder))
                            if (act(elem))
                                return true;
                    }
                }
                else
                {
                    var currRect = currentPlacemat.EffectArea;
                    var currentActivePlacematRect = new Rect(
                        currRect.x + k_SelectRectOffset,
                        currRect.y + k_SelectRectOffset,
                        currRect.width - 2 * k_SelectRectOffset,
                        currRect.height - 2 * k_SelectRectOffset);

                    var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                    foreach (var elem in k_ActOnGraphElementsOver2AllUIs.OfType<GraphElement>())
                    {
                        if (elem.layout.Overlaps(currentActivePlacematRect))
                        {
                            var placemat = elem as Placemat;
                            if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                                if (RecurseActOnGraphElementsOver(placemat))
                                    return true;

                            if (placemat == null || (includePlacemats && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder))
                                if (elem.resolvedStyle.visibility != Visibility.Hidden)
                                    if (act(elem))
                                        return true;
                        }
                    }
                }

                return false;
            }
        }

        protected internal bool WillDragNode_Internal(GraphElement node)
        {
            if (PlacematModel.Collapsed)
                return AllCollapsedElements(CollapsedElements).Contains(node);

            return ActOnGraphElementsOver_Internal(t => node == t, true);
        }

        internal Rect ComputeGrowToFitElementsRect_Internal(List<GraphElement> elements = null)
        {
            if (elements == null)
                elements = GetNodesOverThisPlacemat();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug. In other words: we don't ever decrease in size.
                Rect currentRect = layout;
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
            }

            return pos;
        }

        internal Rect ComputeShrinkToFitElementsRect_Internal()
        {
            var elements = GetNodesOverThisPlacemat();
            var pos = new Rect();
            ComputeElementBounds_Internal(ref pos, elements);
            return pos;
        }

        Rect ComputeResizeToIncludeSelectedNodesRect()
        {
            var nodes = GraphView.GetSelection().
                OfType<AbstractNodeModel>().
                Select(n => n.GetView<GraphElement>(RootView)).
                ToList();

            // Now include the selected nodes
            var pos = new Rect();
            if (ComputeElementBounds(ref pos, nodes, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug: we only resize enough to contain the selected nodes.
                var currentRect = layout;
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
            }

            return pos;
        }

        internal void GetElementsToMove_Internal(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (PlacematModel.Collapsed)
            {
                var collapsedElements = AllCollapsedElements(CollapsedElements);
                foreach (var element in collapsedElements)
                {
                    collectedElementsToMove.Add(element);
                }
            }
            else if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsOver_Internal(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                }, true);
            }
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Placemat placemat))
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction(placemat.PlacematModel.Collapsed ? "Expand Placemat" : "Collapse Placemat",
                _ => placemat.CollapsePlacemat(!placemat.PlacematModel.Collapsed));

            // Gather nodes here so that we don't recycle this code in the resize functions.
            List<GraphElement> hoveringNodes = placemat.GetNodesOverThisPlacemat();

            evt.menu.AppendAction("Resize Placemat/Grow to Fit Nodes",
                _ =>
                {
                    var newRect = placemat.ComputeGrowToFitElementsRect_Internal();
                    if (newRect != Rect.zero)
                        GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Shrink to Fit Nodes",
                _ =>
                {
                    var newRect = placemat.ComputeShrinkToFitElementsRect_Internal();
                    if (newRect != Rect.zero)
                        GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Grow to Fit Selected Nodes",
                _ =>
                {
                    var newRect = placemat.ComputeResizeToIncludeSelectedNodesRect();
                    GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                _ =>
                {
                    var selectedNodes = placemat.GraphView.GetSelection().Where(e => e is AbstractNodeModel);
                    return selectedNodes.Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                });

            var placemats = GraphView.GraphModel.PlacematModels;

            // JOCE TODO: Check that *ALL* placemats are at the top or bottom. We should be able to do something otherwise.
            var placematIsTop = placemats.Last() == placemat.PlacematModel;
            var placematIsBottom = placemats.First() == placemat.PlacematModel;
            var canBeReordered = placemats.Count > 1;

            var selectedPlacemats = GraphView.GetSelection().OfType<PlacematModel>().ToList();

            evt.menu.AppendAction("Reorder Placemats/Bring to Front",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToFront, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Bring Forward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Forward, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Send Backward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Backward, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Send to Back",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToBack, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        protected List<GraphElement> GetNodesOverThisPlacemat()
        {
            var potentialElements = new List<ModelView>();
            ActOnGraphElementsOver(e => potentialElements.Add(e));

            return potentialElements.OfType<GraphElement>().Where(e => e.Model is AbstractNodeModel).ToList();
        }

        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            CollapsedElements = null;
        }

        protected internal bool GetPortCenterOverride_Internal(PortModel port, out Vector2 overriddenPosition)
        {
            if (!PlacematModel.Collapsed || parent == null)
            {
                overriddenPosition = Vector2.zero;
                return false;
            }

            const int xOffset = 6;
            const int yOffset = 3;
            var halfSize = CollapsedSize * 0.5f;
            var offset = port.Orientation == PortOrientation.Horizontal
                ? new Vector2(port.Direction == PortDirection.Input ? -halfSize.x + xOffset : halfSize.x - xOffset, 0)
                : new Vector2(0, port.Direction == PortDirection.Input ? -halfSize.y + yOffset : halfSize.y - yOffset);

            overriddenPosition = parent.LocalToWorld(layout.center + offset);
            return true;
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        protected internal static bool ComputeElementBounds_Internal(ref Rect pos, List<GraphElement> elements)
        {
            return ComputeElementBounds(ref pos, elements, MinSizePolicy.EnsureMinSize);
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        static bool ComputeElementBounds(ref Rect pos, List<GraphElement> elements, MinSizePolicy ensureMinSize)
        {
            if (elements == null || elements.Count == 0)
                return false;

            float minX = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxY = -Mathf.Infinity;

            foreach (var r in elements.Where(t => t.GraphElementModel.IsMovable()).Select(n => n.layout))
            {
                if (r.xMin < minX)
                    minX = r.xMin;

                if (r.xMax > maxX)
                    maxX = r.xMax;

                if (r.yMin < minY)
                    minY = r.yMin;

                if (r.yMax > maxY)
                    maxY = r.yMax;
            }

            var width = maxX - minX + k_Bounds_Internal * 2.0f;
            var height = maxY - minY + k_Bounds_Internal * 2.0f + k_BoundTop_Internal;

            pos = new Rect(
                minX - k_Bounds_Internal,
                minY - (k_BoundTop_Internal + k_Bounds_Internal),
                width,
                height);

            if (ensureMinSize == MinSizePolicy.EnsureMinSize)
                MakeRectAtLeastMinimalSize(ref pos);

            return true;
        }

        static void MakeRectAtLeastMinimalSize(ref Rect r)
        {
            if (r.width < k_MinWidth)
                r.width = k_MinWidth;

            if (r.height < k_MinHeight)
                r.height = k_MinHeight;
        }

        public override void ActivateRename()
        {
            (PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.BeginEditing();
        }
    }
}
