// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for placemats.
    /// </summary>
    class Placemat : GraphElement
    {
        Vector2 m_LastModelSize;

        /// <summary>
        /// The uss class name added to <see cref="Placemat"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-placemat";

        /// <summary>
        /// The name of the <see cref="VisualElement"/> for the title container.
        /// </summary>
        public static readonly string titleContainerPartName = "title-container";

        /// <summary>
        /// The name for the <see cref="ResizableElement"/>
        /// </summary>
        public static readonly string resizerPartName = "resizer";

        protected internal static readonly float k_Bounds_Internal = 9.0f;

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        protected static readonly float k_MinWidth = 200;
        protected static readonly float k_MinHeight = 100;

        /// <summary>
        /// The default placemat size.
        /// </summary>
        public static readonly Vector2 DefaultPlacematSize = new Vector2(600,300);

        /// <summary>
        /// The model that this <see cref="Placemat"/> displays.
        /// </summary>
        public PlacematModel PlacematModel => Model as PlacematModel;

        /// <summary>
        /// The container for the title of the <see cref="Placemat"/>.
        /// </summary>
        public VisualElement TitleContainer { get; private set; }

        /// <summary>
        /// Creates an instance of the <see cref="Placemat"/> class.
        /// </summary>
        public Placemat():base(true)
        {
            focusable = true;
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            var editableTitlePart = PlacematTitlePart.Create(titleContainerPartName, Model, this, ussClassName);
            PartList.AppendPart(editableTitlePart);
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
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
        protected static void GatherDependencies(Placemat currentPlacemat, IList<ChildView> graphElements, ICollection<ChildView> dependencies)
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

        static readonly List<ChildView> k_AddForwardDependenciesAllUIs = new();

        /// <inheritdoc/>
        public override void AddForwardDependencies()
        {
            using (ListPool<GraphElementModel>.Get(out List<GraphElementModel> list))
            {
                foreach (var ge in GraphView.GraphModel.GraphElementModels)
                {
                    if (ge.IsSelectable() && ge is not WireModel)
                    {
                        list.Add(ge);
                    }
                }
                list.GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_AddForwardDependenciesAllUIs);
            }

            var dependencies = new List<ChildView>();
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
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);
            if (evt is PointerDownEvent mde)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                {
                    var models = new List<GraphElementModel>();
                    ActOnGraphElementsInside_Internal(e =>
                    {
                        models.Add(e.GraphElementModel);
                        return true;
                    });
                    GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, models));
                }
        }

        static readonly List<ChildView> k_ActOnGraphElementsOver2AllUIs = new();
        protected internal bool ActOnGraphElementsInside_Internal(Func<GraphElement, bool> act)
        {
            using (ListPool<GraphElementModel>.Get(out List<GraphElementModel> list))
            {
                foreach (var ge in GraphView.GraphModel.GraphElementModels)
                {
                    if (ge.IsSelectable() && ge is not WireModel && ge is not IPlaceholder)
                    {
                        list.Add(ge);
                    }
                }
                list.GetAllViewsInList_Internal(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOver2AllUIs);
            }

            var currentActivePlacematRect = layout;

            foreach (var view in k_ActOnGraphElementsOver2AllUIs)
            {
                if (view is not GraphElement elem)
                    continue;
                var elemLayout = elem.layout;
                if (currentActivePlacematRect.Contains(elemLayout.position) && currentActivePlacematRect.Contains(elemLayout.position + elemLayout.size))
                {
                    if (elem is Placemat placematElem)
                    {
                        if (PlacematModel.GetZOrder() > placematElem.PlacematModel.GetZOrder())
                            continue;
                    }
                    if (elem.resolvedStyle.visibility != Visibility.Hidden)
                        if (act(elem))
                            return true;
                }
            }

            k_ActOnGraphElementsOver2AllUIs.Clear();

            return false;
        }

        static readonly List<GraphElement> k_ShrinkToFitElements = new List<GraphElement>();

        internal Rect ComputeShrinkToFitElementsRect_Internal()
        {
            k_ShrinkToFitElements.Clear();
            ActOnGraphElementsInside_Internal(e =>
            {
                k_ShrinkToFitElements.Add(e);
                return false;
            });

            var pos = new Rect();
            ComputeElementBounds_Internal(parent, ref pos, k_ShrinkToFitElements);
            return pos;
        }

        public bool HasElementsOverThisPlacemat()
        {
            return ActOnGraphElementsInside_Internal(_ => true);
        }

        internal void GetElementsToMove_Internal(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsInside_Internal(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                });
            }
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Placemat placemat))
                return;
            evt.menu.AppendSeparator();

            List<PlacematModel> selectedPlacemats = GraphView.GetSelection().OfTypeToList<PlacematModel,GraphElementModel>();

            if (selectedPlacemats.Count == 1) // If there is only one placemat selected
            {
                evt.menu.AppendAction("Select All Placemat Contents",
                    _ =>
                    {
                        SelectAllInside();
                    });
            }

            var placemats = GraphView.GraphModel.PlacematModels;

            // JOCE TODO: Check that *ALL* placemats are at the top or bottom. We should be able to do something otherwise.
            var placematIsTop = placemats[^1] == placemat.PlacematModel;
            var placematIsBottom = placemats[0] == placemat.PlacematModel;
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
            bool hoveringNodes = placemat.HasElementsOverThisPlacemat();

            if (selectedPlacemats.Count == 1)
            {
                evt.menu.AppendAction("Smart Resize",
                    _ =>
                    {
                        placemat.SmartResize();
                    },
                    hoveringNodes ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        /// <summary>
        /// Selects all elements inside the Placemat.
        /// </summary>
        public void SelectAllInside()
        {
            var elements = new List<GraphElementModel>();
            ActOnGraphElementsInside_Internal(element =>
            {
                elements.Add(element.GraphElementModel);
                return false;
            });
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elements));
        }

        /// <summary>
        /// Resizes the placemat so it is only a big as needed to contain all the included elements.
        /// </summary>
        public void SmartResize()
        {
            var newRect = ComputeShrinkToFitElementsRect_Internal();
            if (newRect.width <= k_Bounds_Internal * 2 || newRect.height <= k_Bounds_Internal * 2)
            {
                newRect.position = PlacematModel.Position;
                newRect.size = DefaultPlacematSize;
            }

            if (newRect != Rect.zero)
                GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        internal static bool ComputeElementBounds_Internal(VisualElement reference, ref Rect pos, List<GraphElement> elements)
        {
            if (elements == null || elements.Count == 0)
                return false;

            float minX = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxY = -Mathf.Infinity;

            bool foundOne = false;
            foreach (var e in elements)
            {
                if (!e.GraphElementModel.IsMovable())
                    continue;

                foundOne = true;
                Rect r = e.parent.ChangeCoordinatesTo(reference, e.layout);

                if (e is Placemat placemat)
                {
                    var placematTitleHeight = placemat.TitleContainer.layout.height * 2;

                    r.y -= placematTitleHeight;
                    r.height += placematTitleHeight;
                }


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
            var height = maxY - minY + k_Bounds_Internal * 2.0f;

            pos = new Rect(
                minX - k_Bounds_Internal,
                minY - k_Bounds_Internal,
                width,
                height);

            MakeRectAtLeastMinimalSize(ref pos);

            return foundOne;
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

        /// <inheritdoc />
        public override Rect GetBoundingBox()
        {
            if (TitleContainer == null)
                return base.GetBoundingBox();

            Rect titleLayout = TitleContainer.ChangeCoordinatesTo(parent, TitleContainer.layout);
            return titleLayout;
        }
    }
}
