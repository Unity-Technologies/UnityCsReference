// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for placemats.
    /// </summary>
    [UnityRestricted]
    internal class Placemat : GraphElement, IResizeListener
    {
        Vector2 m_LastModelSize;

        /// <summary>
        /// The USS class name added to <see cref="Placemat"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-placemat";

        /// <summary>
        /// The USS class name added to <see cref="PlacematGradientBorder"/>.
        /// </summary>
        public static readonly string gradientUssClassName = ussClassName.WithUssElement("gradient");

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the title container.
        /// </summary>
        public static readonly string titleContainerPartName = GraphElementHelper.titleContainerName;

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the resizer.
        /// </summary>
        public static readonly string resizerPartName = "resizer";

        internal static readonly float k_SmartResizeMargin = 37.0f;

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        protected static readonly float k_MinWidth = 200;
        protected static readonly float k_MinHeight = 100;

        PlacematGradientBorder m_PlacematGradientBorder;

        /// <summary>
        /// The default placemat size.
        /// </summary>
        public static readonly Vector2 DefaultPlacematSize = new Vector2(600, 300);

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
        public Placemat() : base(true)
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
        protected override void BuildUI()
        {
            base.BuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);

            m_PlacematGradientBorder = new PlacematGradientBorder();
            Add(m_PlacematGradientBorder);
            m_PlacematGradientBorder.AddToClassList(gradientUssClassName);
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (TitleContainer != null)
                return TitleContainer.layout.Contains(this.ChangeCoordinatesTo(TitleContainer.parent, localPoint));

            return base.ContainsPoint(localPoint);
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            if (TitleContainer != null)
            {
                Rect localRect = TitleContainer.parent.ChangeCoordinatesTo(this, TitleContainer.layout);
                return localRect.Overlaps(rectangle, true);
            }

            return base.Overlaps(rectangle);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            TitleContainer = PartList.GetPart(titleContainerPartName)?.Root;
            TitleContainer?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            this.AddPackageStylesheet("Placemat.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                UpdateColor();

                Color.RGBToHSV(PlacematModel.ElementColor.Color, out var H, out var _, out var __);

                var lightColor = EditorGUIUtility.isProSkin ? Color.HSVToRGB(H, 34 / 100.0f, 77 / 100.0f) : Color.HSVToRGB(H, 58 / 100.0f, 59 / 100.0f);
                var darkColor = EditorGUIUtility.isProSkin ? Color.HSVToRGB(H, 48 / 100.0f, 71 / 100.0f) : Color.HSVToRGB(H, 58 / 100.0f, 42 / 100.0f);

                m_PlacematGradientBorder.LightColor = PlayerSettings.colorSpace == ColorSpace.Linear ? lightColor.gamma : lightColor;
                m_PlacematGradientBorder.DarkColor = PlayerSettings.colorSpace == ColorSpace.Linear ? darkColor.gamma : darkColor;
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                SetPositionAndSize(PlacematModel.PositionAndSize);
            }
        }

        Color UpdateColor()
        {
            Color color = PlacematModel.ElementColor.Color;
            color.a = MoveOnly ? 0.2f : 0.8f;
            style.backgroundColor = color;
            return color;
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
                if (GraphView?.GraphModel?.GetGraphElementModels() != null)
                {
                    foreach (var ge in GraphView.GraphModel.GetGraphElementModels())
                    {
                        if (ge.IsSelectable() && ge is not WireModel)
                        {
                            list.Add(ge);
                        }
                    }

                    list.GetAllViews(GraphView, e => e.parent is GraphView.Layer, k_AddForwardDependenciesAllUIs);
                }
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
                    m_LastModelSize = new Vector2(positionAndSize.width, positionAndSize.height);
                }
                style.height = positionAndSize.height;
                style.width = positionAndSize.width;
            }
        }

        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBorder(this);
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
                    ActOnGraphElementsInside(e =>
                    {
                        models.Add(e.GraphElementModel);
                        return true;
                    });
                    GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, models));
                }
        }

        static readonly List<GraphElement> k_ActOnGraphElementsOver2AllUIs = new();
        protected internal bool ActOnGraphElementsInside(Func<GraphElement, bool> act)
        {
            var currentActivePlacematRect = layout;

            var placematsNotWithinThisPlacemat = new List<PlacematModel>();
            var placematsWithinThisPlacemat = new List<PlacematModel>();

            // First jump all the placemat below.
            using (var placematEnum = GraphView.GraphModel.PlacematModels.GetEnumerator())
            {
                while (placematEnum.MoveNext() && placematEnum.Current != PlacematModel) {}

                while (placematEnum.MoveNext())
                {
                    var placemat = placematEnum.Current;

                    var elemLayout = placemat.PositionAndSize;
                    if (!currentActivePlacematRect.Contains(elemLayout.position) || !currentActivePlacematRect.Contains(elemLayout.position + elemLayout.size))
                    {
                        placematsNotWithinThisPlacemat.Add(placemat);
                    }
                    else
                    {
                        placematsWithinThisPlacemat.Add(placemat);
                    }
                }
            }

            foreach (var placemat in placematsWithinThisPlacemat)
            {
                var placematView = placemat.GetView<Placemat>(GraphView);
                if (act(placematView))
                    return true;
            }

            GraphView.GetGraphElementsInRegion(currentActivePlacematRect, k_ActOnGraphElementsOver2AllUIs, GraphView.PartitioningMode.PlacematBody, false);

            foreach (var elem in k_ActOnGraphElementsOver2AllUIs)
            {
                var elemLayout = elem.layout;

                if (elem.resolvedStyle.visibility == Visibility.Hidden)
                    continue;

                if (!elem.IsMovable())
                    continue;

                if (elem is Placemat)
                    continue;

                var withinNotContainedPlacemat = false;
                foreach (var otherPlacemat in placematsNotWithinThisPlacemat)
                {
                    var otherPlacematRect = otherPlacemat.PositionAndSize;
                    if (otherPlacematRect.Contains(elemLayout.position) && otherPlacematRect.Contains(elemLayout.position + elemLayout.size))
                    {
                        withinNotContainedPlacemat = true;
                        break;
                    }
                }
                if (withinNotContainedPlacemat)
                    continue;
                if (elem.resolvedStyle.visibility != Visibility.Hidden)
                    if (act(elem))
                        return true;
            }

            k_ActOnGraphElementsOver2AllUIs.Clear();

            return false;
        }

        static readonly List<GraphElement> k_ShrinkToFitElements = new List<GraphElement>();

        internal Rect ComputeShrinkToFitElementsRect()
        {
            k_ShrinkToFitElements.Clear();
            ActOnGraphElementsInside(e =>
            {
                k_ShrinkToFitElements.Add(e);
                return false;
            });

            var pos = new Rect();
            ComputeElementBounds(parent, ref pos, k_ShrinkToFitElements);
            return pos;
        }

        public bool HasElementsOverThisPlacemat()
        {
            return ActOnGraphElementsInside(_ => true);
        }

        internal void GetElementsToMove(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (!moveOnlyPlacemat && !MoveOnly)
            {
                ActOnGraphElementsInside(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                });
            }
        }

        /// <summary>
        /// Selects all elements inside the Placemat.
        /// </summary>
        public void SelectAllInside()
        {
            var elements = new List<GraphElementModel>();
            ActOnGraphElementsInside(element =>
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
            var newRect = ComputeShrinkToFitElementsRect();
            if (newRect.width <= k_SmartResizeMargin * 2 || newRect.height <= k_SmartResizeMargin * 2)
            {
                newRect.position = PlacematModel.Position;
                newRect.size = DefaultPlacematSize;
            }

            if (newRect != Rect.zero)
                GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        internal static bool ComputeElementBounds(VisualElement reference, ref Rect pos, List<GraphElement> elements)
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

            var width = maxX - minX + k_SmartResizeMargin * 2.0f;
            var height = maxY - minY + k_SmartResizeMargin * 2.0f;

            pos = new Rect(
                minX - k_SmartResizeMargin,
                minY - k_SmartResizeMargin,
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

        HashSet<GraphElement> m_ElementsHighlightedWhenResizing = new HashSet<GraphElement>();

        void IResizeListener.OnStartResize()
        {
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, GraphElementModel));

            GetElementsToMove(false, m_ElementsHighlightedWhenResizing);

            foreach (var ge in m_ElementsHighlightedWhenResizing)
            {
                ge.OverrideHighlighted = true;
            }
        }

        void IResizeListener.OnResizing()
        {
            foreach (var ge in m_ElementsHighlightedWhenResizing)
            {
                ge.OverrideHighlighted = false;
            }
            m_ElementsHighlightedWhenResizing.Clear();
            GetElementsToMove(false, m_ElementsHighlightedWhenResizing);
            foreach (var ge in m_ElementsHighlightedWhenResizing)
            {
                ge.OverrideHighlighted = true;
            }
        }

        bool m_MoveOnly;

        public bool MoveOnly
        {
            get => m_MoveOnly;
            set
            {
                if (m_MoveOnly != value)
                {
                    m_MoveOnly = value;
                    UpdateColor();
                }
            }
        }

        void IResizeListener.OnStopResize()
        {
            foreach (var ge in m_ElementsHighlightedWhenResizing)
            {
                ge.OverrideHighlighted = false;
            }
            m_ElementsHighlightedWhenResizing.Clear();
        }
    }
}
