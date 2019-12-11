// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    // Assuming a ScrollView parent with a flex-direction column.
    // The modes follow these rules :
    //
    // Vertical
    // ---------------------
    // Require elements with an height, width will stretch.
    // If the ScrollView parent is set to flex-direction row the elements height will not stretch.
    // How measure works :
    // Width is restricted, height is not. content-container is set to overflow: scroll
    //
    // Horizontal
    // ---------------------
    // Require elements with a width. If ScrollView is set to flex-grow elements height stretch else they require a height.
    // If the ScrollView parent is set to flex-direction row the elements height will stretch.
    // How measure works :
    // Height is restricted, width is not. content-container is set to overflow: scroll
    //
    // VerticalAndHorizontal
    // ---------------------
    // Require elements with an height, width will stretch.
    // The difference with the Vertical type is that content will not wrap (white-space has no effect).
    // How measure works :
    // Nothing is restricted, the content-container will stop shrinking so that all the content fit and scrollers will appear.
    // To achieve this content-viewport is set to overflow: scroll and flex-direction: row.
    // content-container is set to flex-direction: column, flex-grow: 1 and align-self:flex-start.
    //
    // This type is more tricky, it requires the content-viewport and content-container to have a different flex-direction.
    // "flex-grow:1" is to make elements stretch horizontally.
    // "align-self:flex-start" prevent the content-container from shrinking below the content size vertically.
    // "overflow:scroll" on the content-viewport and content-container is to not restrict measured elements in any direction.
    public enum ScrollViewMode
    {
        Vertical,
        Horizontal,
        VerticalAndHorizontal
    }

    public class ScrollView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ScrollView, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlEnumAttributeDescription<ScrollViewMode> m_ScrollViewMode = new UxmlEnumAttributeDescription<ScrollViewMode> { name = "mode", defaultValue = ScrollViewMode.Vertical};
            UxmlBoolAttributeDescription m_ShowHorizontal = new UxmlBoolAttributeDescription { name = "show-horizontal-scroller" };
            UxmlBoolAttributeDescription m_ShowVertical = new UxmlBoolAttributeDescription { name = "show-vertical-scroller" };

            UxmlFloatAttributeDescription m_HorizontalPageSize = new UxmlFloatAttributeDescription { name = "horizontal-page-size", defaultValue = Scroller.kDefaultPageSize };
            UxmlFloatAttributeDescription m_VerticalPageSize = new UxmlFloatAttributeDescription { name = "vertical-page-size", defaultValue = Scroller.kDefaultPageSize };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ScrollView scrollView = (ScrollView)ve;
                scrollView.SetScrollViewMode(m_ScrollViewMode.GetValueFromBag(bag, cc));
                scrollView.showHorizontal = m_ShowHorizontal.GetValueFromBag(bag, cc);
                scrollView.showVertical = m_ShowVertical.GetValueFromBag(bag, cc);
                scrollView.horizontalPageSize = m_HorizontalPageSize.GetValueFromBag(bag, cc);
                scrollView.verticalPageSize = m_VerticalPageSize.GetValueFromBag(bag, cc);
            }
        }

        private bool m_ShowHorizontal;
        public bool showHorizontal
        {
            get { return m_ShowHorizontal;}
            set
            {
                m_ShowHorizontal = value;
                UpdateScrollers(m_ShowHorizontal, m_ShowVertical);
            }
        }

        private bool m_ShowVertical;
        public bool showVertical
        {
            get { return m_ShowVertical;}
            set
            {
                m_ShowVertical = value;
                UpdateScrollers(m_ShowHorizontal, m_ShowVertical);
            }
        }

        internal bool needsHorizontal
        {
            get { return showHorizontal || (contentContainer.layout.width - layout.width > 0); }
        }

        internal bool needsVertical
        {
            get { return showVertical || (contentContainer.layout.height - layout.height > 0); }
        }

        public Vector2 scrollOffset
        {
            get { return new Vector2(horizontalScroller.value, verticalScroller.value); }
            set
            {
                if (value != scrollOffset)
                {
                    horizontalScroller.value = value.x;
                    verticalScroller.value = value.y;
                    UpdateContentViewTransform();
                }
            }
        }

        public float horizontalPageSize
        {
            get { return horizontalScroller.slider.pageSize; }
            set { horizontalScroller.slider.pageSize = value; }
        }

        public float verticalPageSize
        {
            get { return verticalScroller.slider.pageSize; }
            set { verticalScroller.slider.pageSize = value; }
        }

        private float scrollableWidth
        {
            get { return contentContainer.layout.width - contentViewport.layout.width; }
        }

        private float scrollableHeight
        {
            get { return contentContainer.layout.height - contentViewport.layout.height; }
        }

        void UpdateContentViewTransform()
        {
            // Adjust contentContainer's position
            var t = contentContainer.transform.position;

            var offset = scrollOffset;
            t.x = GUIUtility.RoundToPixelGrid(-offset.x);
            t.y = GUIUtility.RoundToPixelGrid(-offset.y);
            contentContainer.transform.position = t;

            // TODO: Can we get rid of this?
            this.IncrementVersion(VersionChangeType.Repaint);
        }

        public void ScrollTo(VisualElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            // Child not in content view, no need to continue.
            if (!contentContainer.Contains(child))
                throw new ArgumentException("Cannot scroll to a VisualElement that is not a child of the ScrollView content-container.");

            float yDeltaOffset = 0, xDeltaOffset = 0;

            if (scrollableHeight > 0)
            {
                yDeltaOffset = GetYDeltaOffset(child);
                verticalScroller.value = scrollOffset.y + yDeltaOffset;
            }
            if (scrollableWidth > 0)
            {
                xDeltaOffset = GetXDeltaOffset(child);
                horizontalScroller.value = scrollOffset.x + xDeltaOffset;
            }

            if (yDeltaOffset == 0 && xDeltaOffset == 0)
                return;

            UpdateContentViewTransform();
        }

        private float GetXDeltaOffset(VisualElement child)
        {
            float xTransform = contentContainer.transform.position.x * -1;

            var contentWB = contentViewport.worldBound;
            float viewMin = contentWB.xMin + xTransform;
            float viewMax = contentWB.xMax + xTransform;

            var childWB = child.worldBound;
            float childBoundaryMin = childWB.xMin + xTransform;
            float childBoundaryMax = childWB.xMax + xTransform;

            if ((childBoundaryMin >= viewMin && childBoundaryMax <= viewMax) || float.IsNaN(childBoundaryMin) || float.IsNaN(childBoundaryMax))
                return 0;

            float deltaDistance = GetDeltaDistance(viewMin, viewMax, childBoundaryMin, childBoundaryMax);

            return deltaDistance * horizontalScroller.highValue / scrollableWidth;
        }

        private float GetYDeltaOffset(VisualElement child)
        {
            float yTransform = contentContainer.transform.position.y * -1;

            var contentWB = contentViewport.worldBound;
            float viewMin = contentWB.yMin + yTransform;
            float viewMax = contentWB.yMax + yTransform;

            var childWB = child.worldBound;
            float childBoundaryMin = childWB.yMin + yTransform;
            float childBoundaryMax = childWB.yMax + yTransform;

            if ((childBoundaryMin >= viewMin && childBoundaryMax <= viewMax) || float.IsNaN(childBoundaryMin) || float.IsNaN(childBoundaryMax))
                return 0;

            float deltaDistance = GetDeltaDistance(viewMin, viewMax, childBoundaryMin, childBoundaryMax);

            return deltaDistance * verticalScroller.highValue / scrollableHeight;
        }

        private float GetDeltaDistance(float viewMin, float viewMax, float childBoundaryMin, float childBoundaryMax)
        {
            float deltaDistance = childBoundaryMax - viewMax;
            if (deltaDistance < -1)
            {
                deltaDistance = childBoundaryMin - viewMin;
            }

            return deltaDistance;
        }

        public VisualElement contentViewport { get; private set; } // Represents the visible part of contentContainer

        public Scroller horizontalScroller { get; private set; }
        public Scroller verticalScroller { get; private set; }

        private VisualElement m_ContentContainer;
        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }

        public static readonly string ussClassName = "unity-scroll-view";
        public static readonly string viewportUssClassName = ussClassName + "__content-viewport";
        public static readonly string contentUssClassName = ussClassName + "__content-container";
        public static readonly string hScrollerUssClassName = ussClassName + "__horizontal-scroller";
        public static readonly string vScrollerUssClassName = ussClassName + "__vertical-scroller";
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        public static readonly string verticalHorizontalVariantUssClassName = ussClassName + "--vertical-horizontal";
        public static readonly string scrollVariantUssClassName = ussClassName + "--scroll";

        public ScrollView() : this(ScrollViewMode.Vertical) {}

        public ScrollView(ScrollViewMode scrollViewMode)
        {
            AddToClassList(ussClassName);

            contentViewport = new VisualElement() { name = "unity-content-viewport" };
            contentViewport.AddToClassList(viewportUssClassName);
            contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            hierarchy.Add(contentViewport);

            m_ContentContainer = new VisualElement() { name = "unity-content-container" };
            m_ContentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_ContentContainer.AddToClassList(contentUssClassName);
            m_ContentContainer.usageHints = UsageHints.GroupTransform;
            contentViewport.Add(m_ContentContainer);

            SetScrollViewMode(scrollViewMode);

            const int defaultMinScrollValue = 0;
            const int defaultMaxScrollValue = 100;

            horizontalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(value, scrollOffset.y);
                    UpdateContentViewTransform();
                }, SliderDirection.Horizontal)
            { viewDataKey = "HorizontalScroller", visible = false };
            horizontalScroller.AddToClassList(hScrollerUssClassName);
            hierarchy.Add(horizontalScroller);

            verticalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                    UpdateContentViewTransform();
                }, SliderDirection.Vertical)
            { viewDataKey = "VerticalScroller", visible = false };
            verticalScroller.AddToClassList(vScrollerUssClassName);
            hierarchy.Add(verticalScroller);

            RegisterCallback<WheelEvent>(OnScrollWheel);
            scrollOffset = Vector2.zero;
        }

        internal void SetScrollViewMode(ScrollViewMode scrollViewMode)
        {
            RemoveFromClassList(verticalVariantUssClassName);
            RemoveFromClassList(horizontalVariantUssClassName);
            RemoveFromClassList(verticalHorizontalVariantUssClassName);
            RemoveFromClassList(scrollVariantUssClassName);

            switch (scrollViewMode)
            {
                case ScrollViewMode.Vertical:
                    AddToClassList(verticalVariantUssClassName);
                    AddToClassList(scrollVariantUssClassName);
                    break;
                case ScrollViewMode.Horizontal:
                    AddToClassList(horizontalVariantUssClassName);
                    AddToClassList(scrollVariantUssClassName);
                    break;
                case ScrollViewMode.VerticalAndHorizontal:
                    AddToClassList(scrollVariantUssClassName);
                    AddToClassList(verticalHorizontalVariantUssClassName);
                    break;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Only affected by dimension changes
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            // Get the initial information on the necessity of the scrollbars
            bool needsVerticalCached = needsVertical;
            bool needsHorizontalCached = needsHorizontal;

            // Here, we allow the removal of the scrollbar only in the first layout pass.
            // Addition is always allowed.
            if (evt.layoutPass > 0)
            {
                needsVerticalCached = needsVerticalCached || verticalScroller.visible;
                needsHorizontalCached = needsHorizontalCached || horizontalScroller.visible;
            }

            UpdateScrollers(needsHorizontalCached, needsVerticalCached);
            UpdateContentViewTransform();
        }

        void UpdateScrollers(bool displayHorizontal, bool displayVertical)
        {
            float horizontalFactor = contentContainer.layout.width > Mathf.Epsilon ? contentViewport.layout.width / contentContainer.layout.width : 1f;
            float verticalFactor = contentContainer.layout.height > Mathf.Epsilon ? contentViewport.layout.height / contentContainer.layout.height : 1f;

            horizontalScroller.Adjust(horizontalFactor);
            verticalScroller.Adjust(verticalFactor);

            // Set availability
            horizontalScroller.SetEnabled(contentContainer.layout.width - contentViewport.layout.width > 0);
            verticalScroller.SetEnabled(contentContainer.layout.height - contentViewport.layout.height > 0);

            // Expand content if scrollbars are hidden
            contentViewport.style.marginRight = displayVertical ? verticalScroller.layout.width : 0;
            horizontalScroller.style.right = displayVertical ? verticalScroller.layout.width : 0;
            contentViewport.style.marginBottom = displayHorizontal ? horizontalScroller.layout.height : 0;
            verticalScroller.style.bottom = displayHorizontal ? horizontalScroller.layout.height : 0;

            if (displayHorizontal && scrollableWidth > 0f)
            {
                horizontalScroller.lowValue = 0f;
                horizontalScroller.highValue = scrollableWidth;
            }
            else
            {
                horizontalScroller.value = 0f;
            }

            if (displayVertical && scrollableHeight > 0f)
            {
                verticalScroller.lowValue = 0f;
                verticalScroller.highValue = scrollableHeight;
            }
            else
            {
                verticalScroller.value = 0f;
            }

            // Set visibility and remove/add content viewport margin as necessary
            if (horizontalScroller.visible != displayHorizontal)
            {
                horizontalScroller.visible = displayHorizontal;
            }
            if (verticalScroller.visible != displayVertical)
            {
                verticalScroller.visible = displayVertical;
            }
        }

        // TODO: Same behaviour as IMGUI Scroll view
        void OnScrollWheel(WheelEvent evt)
        {
            var oldValue = verticalScroller.value;
            if (contentContainer.layout.height - layout.height > 0)
            {
                if (evt.delta.y < 0)
                    verticalScroller.ScrollPageUp(Mathf.Abs(evt.delta.y));
                else if (evt.delta.y > 0)
                    verticalScroller.ScrollPageDown(Mathf.Abs(evt.delta.y));
            }

            if (verticalScroller.value != oldValue)
            {
                evt.StopPropagation();
            }
        }
    }
}
