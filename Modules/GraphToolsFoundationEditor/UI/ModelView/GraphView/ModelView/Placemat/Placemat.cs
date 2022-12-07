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

        Vector2 m_LastModelSize;

        /// <summary>
        /// The uss class name added to <see cref="Placemat"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-placemat";

        /// <summary>
        /// The name of the <see cref="SelectionBorder"/>.
        /// </summary>
        public static readonly string selectionBorderElementName = "selection-border";

        /// <summary>
        /// The name of the <see cref="VisualElement"/> for the title container.
        /// </summary>
        public static readonly string titleContainerPartName = "title-container";

        /// <summary>
        /// The name for the <see cref="ResizableElement"/>
        /// </summary>
        public static readonly string resizerPartName = "resizer";

        protected internal static readonly float k_Bounds_Internal = 9.0f;
        protected internal static readonly float k_BoundTop_Internal = 64.0f; // Current height of Title

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        protected static readonly float k_MinWidth = 200;
        protected static readonly float k_MinHeight = 100;

        /// <summary>
        /// The container for the content of the <see cref="Placemat"/>.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <summary>
        /// The model that this <see cref="Placemat"/> displays.
        /// </summary>
        public PlacematModel PlacematModel => Model as PlacematModel;

        /// <summary>
        /// The container for the content of the <see cref="Placemat"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// The container for the title of the <see cref="Placemat"/>.
        /// </summary>
        public VisualElement TitleContainer { get; private set; }

        /// <summary>
        /// Creates an instance of the <see cref="Placemat"/> class.
        /// </summary>
        public Placemat()
        {
            focusable = true;
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            var editableTitlePart = PlacematTitlePart.Create(titleContainerPartName, Model, this, ussClassName, false, true, false);
            PartList.AppendPart(editableTitlePart);
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (TitleContainer != null)
                return TitleContainer.rect.Contains(this.ChangeCoordinatesTo(TitleContainer, localPoint));

            return base.ContainsPoint(localPoint);
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            if (TitleContainer != null)
            {
                Rect localRect = TitleContainer.ChangeCoordinatesTo(this, TitleContainer.rect);
                return localRect.Overlaps(rectangle, true);
            }

            return base.Overlaps(rectangle);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            TitleContainer = PartList.GetPart(titleContainerPartName)?.Root;

            this.AddStylesheet_Internal("Placemat.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            Color color = PlacematModel.Color;
            color.a = 0.25f;
            style.backgroundColor = color;

            SetPositionAndSize(PlacematModel.PositionAndSize);
        }

        // PF FIXME: we can probably improve the performance of this.
        // Idea: build a bounding box of placemats affected by currentPlacemat and use this BB to intersect with nodes.
        // PF TODO: also revisit Placemat other recursive functions for perf improvements.
        protected static void GatherDependencies(Placemat currentPlacemat, IList<ModelView> graphElements, ICollection<ModelView> dependencies)
        {
            // We want gathering dependencies to work even if the placemat layout is not up to date, so we use the
            // currentPlacemat.PlacematModel.PositionAndSize to do our overlap test.
            var currentActivePlacematRect = currentPlacemat.PlacematModel.PositionAndSize;

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
        }


        /// <summary>
        /// Sets the position and the size of the placemat.
        /// </summary>
        /// <param name="positionAndSize">The position and size.</param>
        public void SetPositionAndSize(Rect positionAndSize)
        {
            SetPosition(positionAndSize.position);
            if (!PositionIsOverriddenByManipulator)
            {
                if (m_LastModelSize.x != positionAndSize.width || m_LastModelSize.y != positionAndSize.height)
                {
                    GraphView.SelectionDragger.SetSelectionDirty();
                    m_LastModelSize = new Vector2(positionAndSize.width, positionAndSize.height);
                }
                style.height = positionAndSize.height;
                style.width = positionAndSize.width;
            }
        }

        /// <inheritdoc />
        [EventInterest(typeof(PointerDownEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            if (evt is PointerDownEvent mde)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                {
                    var models = new List<GraphElementModel>();
                    ActOnGraphElementsInside_Internal(e =>
                    {
                        models.Add(e.GraphElementModel);
                        return true;
                    },true);
                    GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, models));
                }
        }

        static readonly List<ModelView> k_ActOnGraphElementsOverAllUIs = new List<ModelView>();

        /// <summary>
        /// Executes an <see cref="Action"/> on each <see cref="GraphElement"/> within this <see cref="Placemat"/>.
        /// </summary>
        /// <param name="act">The action that will be executed.</param>
        protected internal void ActOnGraphElementsOver_Internal(Action<GraphElement> act)
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
        protected internal bool ActOnGraphElementsInside_Internal(Func<GraphElement, bool> act, bool includePlacemats)
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is WireModel) && ge is not IPlaceholder)
                .GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOver2AllUIs);

            var retVal = RecurseActOnGraphElementsOver(this);
            k_ActOnGraphElementsOver2AllUIs.Clear();
            return retVal;

            bool RecurseActOnGraphElementsOver(Placemat currentPlacemat)
            {
                var currentActivePlacematRect = currentPlacemat.layout;

                var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                foreach (var elem in k_ActOnGraphElementsOver2AllUIs.OfType<GraphElement>())
                {
                    var elemLayout = elem.layout;
                    if (currentActivePlacematRect.Contains(elemLayout.position) && currentActivePlacematRect.Contains(elemLayout.position + elemLayout.size))
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

                return false;
            }
        }

        protected internal bool WillDragNode_Internal(GraphElement node)
        {
            return ActOnGraphElementsInside_Internal(t => node == t, true);
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

        internal void GetElementsToMove_Internal(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsInside_Internal(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                }, true);
            }
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Placemat placemat))
                return;
            evt.menu.AppendSeparator();

            var selectedPlacemats = GraphView.GetSelection().OfType<PlacematModel>().ToList();

            if (!selectedPlacemats.Skip(1).Any()) // If there is only one placemat selected
            {
                evt.menu.AppendAction("Select Placemat Contents",
                    _ =>
                    {
                        var elements = new List<GraphElementModel>();
                        ActOnGraphElementsInside_Internal(element =>
                        {
                            elements.Add(element.GraphElementModel);
                            return true;
                        },true);
                        GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elements));
                    });
            }

            var placemats = GraphView.GraphModel.PlacematModels;

            // JOCE TODO: Check that *ALL* placemats are at the top or bottom. We should be able to do something otherwise.
            var placematIsTop = placemats.Last() == placemat.PlacematModel;
            var placematIsBottom = placemats.First() == placemat.PlacematModel;
            var canBeReordered = placemats.Count > 1;


            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Bring to Front",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToFront, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Bring Forward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Forward, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Send Backward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Backward, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Send to Back",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToBack, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();
            // Gather nodes here so that we don't recycle this code in the resize functions.
            List<GraphElement> hoveringNodes = placemat.GetNodesOverThisPlacemat();

            if (selectedPlacemats.Count == 1)
            {
                evt.menu.AppendAction("Smart Resize",
                    _ =>
                    {
                        var newRect = placemat.ComputeShrinkToFitElementsRect_Internal();
                        if (newRect != Rect.zero)
                            GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                    },
                    hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        /// <summary>
        /// Gets the list of <see cref="Node"/>s that are within this <see cref="Placemat"/>.
        /// </summary>
        /// <returns>The list of <see cref="Node"/>s that are within this <see cref="Placemat"/>.</returns>
        protected List<GraphElement> GetNodesOverThisPlacemat()
        {
            var potentialElements = new List<ModelView>();
            ActOnGraphElementsOver_Internal(e => potentialElements.Add(e));

            return potentialElements.OfType<GraphElement>().Where(e => e.Model is AbstractNodeModel).ToList();
        }

        protected internal bool GetPortCenterOverride_Internal(PortModel port, out Vector2 overriddenPosition)
        {
                overriddenPosition = Vector2.zero;
                return false;
            }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        protected internal static bool ComputeElementBounds_Internal(ref Rect pos, IEnumerable<GraphElement> elements)
        {
            return ComputeElementBounds(ref pos, elements, MinSizePolicy.EnsureMinSize);
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        static bool ComputeElementBounds(ref Rect pos, IEnumerable<GraphElement> elements, MinSizePolicy ensureMinSize)
        {
            if (elements == null || !elements.Any())
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

        /// <inheritdoc />
        public override void ActivateRename()
        {
            (PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.BeginEditing();
        }
    }
}
