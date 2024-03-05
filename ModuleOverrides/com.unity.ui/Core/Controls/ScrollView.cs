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

    /// <summary>
    /// Configurations of the <see cref="ScrollView"/> to influence the layout of its contents and how scrollbars appear.
    /// <see cref="ScrollView.mode"/>
    /// </summary>
    /// <remarks>
    /// The default is <see cref="ScrollViewMode.Vertical"/>.
    /// 
    /// For more information, refer to [[wiki:UIE-uxml-element-ScrollView|UXML element ScrollView]].
    /// </remarks>
    public enum ScrollViewMode
    {
        /// <summary>
        /// Configure <see cref="ScrollView"/> for vertical scrolling.
        /// </summary>
        /// <remarks>
        /// Requires elements with the height property explicitly defined. A ScrollView configured with this mode has the
        /// <see cref="ScrollView.verticalVariantUssClassName"/> class in its class list.
        /// </remarks>
        Vertical,
        /// <summary>
        /// Configure <see cref="ScrollView"/> for horizontal scrolling.
        /// </summary>
        /// <remarks>
        /// Requires elements with the width property explicitly defined. A ScrollView configured with this mode has the
        /// <see cref="ScrollView.horizontalVariantUssClassName"/> class in its class list.
        /// If <see cref="ScrollView"/> is set to flex-grow or if it's parent is set to <see cref="FlexDirection.Row"/>
        /// elements height stretch else they require a height.
        /// </remarks>
        Horizontal,
        /// <summary>
        /// Configure <see cref="ScrollView"/> for vertical and horizontal scrolling.
        /// </summary>
        /// <remarks>
        /// Requires elements with the height property explicitly defined. A ScrollView configured with this mode has the
        /// <see cref="ScrollView.verticalHorizontalVariantUssClassName"/> class in its class list.
        /// The difference with the vertical mode is that content will not wrap.
        /// </remarks>
        VerticalAndHorizontal
    }

    /// <summary>
    /// Options for controlling the visibility of scroll bars in the <see cref="ScrollView"/>.
    /// </summary>
    public enum ScrollerVisibility
    {
        /// <summary>
        /// Displays a scroll bar only if the content does not fit in the scroll view. Otherwise, hides the scroll bar.
        /// </summary>
        Auto,
        /// <summary>
        /// The scroll bar is always visible.
        /// </summary>
        AlwaysVisible,
        /// <summary>
        /// The scroll bar is always hidden.
        /// </summary>
        Hidden
    }

    /// <summary>
    /// Displays its contents inside a scrollable frame. For more information, see [[wiki:UIE-uxml-element-ScrollView|ScrollView]].
    /// </summary>
    public class ScrollView : VisualElement
    {
        /// <summary>
        /// Instantiates a <see cref="ScrollView"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ScrollView, UxmlTraits>
        {
        }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ScrollView"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlEnumAttributeDescription<ScrollViewMode> m_ScrollViewMode = new UxmlEnumAttributeDescription<ScrollViewMode>
            { name = "mode", defaultValue = ScrollViewMode.Vertical };

            UxmlEnumAttributeDescription<NestedInteractionKind> m_NestedInteractionKind = new UxmlEnumAttributeDescription<NestedInteractionKind>
            { name = "nested-interaction-kind", defaultValue = NestedInteractionKind.Default };

            UxmlBoolAttributeDescription m_ShowHorizontal = new UxmlBoolAttributeDescription
            { name = "show-horizontal-scroller" };

            UxmlBoolAttributeDescription m_ShowVertical = new UxmlBoolAttributeDescription
            { name = "show-vertical-scroller" };

            UxmlEnumAttributeDescription<ScrollerVisibility> m_HorizontalScrollerVisibility = new UxmlEnumAttributeDescription<ScrollerVisibility>
            { name = "horizontal-scroller-visibility"};

            UxmlEnumAttributeDescription<ScrollerVisibility> m_VerticalScrollerVisibility = new UxmlEnumAttributeDescription<ScrollerVisibility>
            { name = "vertical-scroller-visibility" };

            UxmlFloatAttributeDescription m_HorizontalPageSize = new UxmlFloatAttributeDescription
            { name = "horizontal-page-size", defaultValue = k_UnsetPageSizeValue };

            UxmlFloatAttributeDescription m_VerticalPageSize = new UxmlFloatAttributeDescription
            { name = "vertical-page-size", defaultValue = k_UnsetPageSizeValue };

            UxmlFloatAttributeDescription m_MouseWheelScrollSize = new UxmlFloatAttributeDescription
            { name = "mouse-wheel-scroll-size", defaultValue = k_MouseWheelScrollSizeDefaultValue };

            UxmlEnumAttributeDescription<TouchScrollBehavior> m_TouchScrollBehavior = new UxmlEnumAttributeDescription<TouchScrollBehavior>
            { name = "touch-scroll-type", defaultValue = TouchScrollBehavior.Clamped };

            UxmlFloatAttributeDescription m_ScrollDecelerationRate = new UxmlFloatAttributeDescription
            { name = "scroll-deceleration-rate", defaultValue = k_DefaultScrollDecelerationRate };

            UxmlFloatAttributeDescription m_Elasticity = new UxmlFloatAttributeDescription
            { name = "elasticity", defaultValue = k_DefaultElasticity };

            UxmlLongAttributeDescription m_ElasticAnimationIntervalMs = new UxmlLongAttributeDescription
                { name = "elastic-animation-interval-ms", defaultValue = k_DefaultElasticAnimationInterval };

            /// <summary>
            /// Initialize <see cref="ScrollView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ScrollView scrollView = (ScrollView)ve;
                scrollView.mode = m_ScrollViewMode.GetValueFromBag(bag, cc);

                // Remove once showHorizontal and showVertical are fully deprecated.
#pragma warning disable 618
                var horizontalVisibility = ScrollerVisibility.Auto;
                if (m_HorizontalScrollerVisibility.TryGetValueFromBag(bag, cc, ref horizontalVisibility))
                    scrollView.horizontalScrollerVisibility = horizontalVisibility;
                else
                    scrollView.showHorizontal = m_ShowHorizontal.GetValueFromBag(bag, cc);

                var verticalVisibility = ScrollerVisibility.Auto;
                if (m_VerticalScrollerVisibility.TryGetValueFromBag(bag, cc, ref verticalVisibility))
                    scrollView.verticalScrollerVisibility = verticalVisibility;
                else
                    scrollView.showVertical = m_ShowVertical.GetValueFromBag(bag, cc);
#pragma warning restore 618

                scrollView.nestedInteractionKind = m_NestedInteractionKind.GetValueFromBag(bag, cc);
                scrollView.horizontalPageSize = m_HorizontalPageSize.GetValueFromBag(bag, cc);
                scrollView.verticalPageSize = m_VerticalPageSize.GetValueFromBag(bag, cc);
                scrollView.mouseWheelScrollSize = m_MouseWheelScrollSize.GetValueFromBag(bag, cc);
                scrollView.scrollDecelerationRate = m_ScrollDecelerationRate.GetValueFromBag(bag, cc);
                scrollView.touchScrollBehavior = m_TouchScrollBehavior.GetValueFromBag(bag, cc);
                scrollView.elasticity = m_Elasticity.GetValueFromBag(bag, cc);
                scrollView.elasticAnimationIntervalMs = m_ElasticAnimationIntervalMs.GetValueFromBag(bag, cc);
            }
        }

        // ScrollViews can take more than 3 passes to stabilize. This can be the case when a scrollview contains elements with height bound to their width (e.g label with wrapped text).
        // Beyond 5 passes, we assume that the layout may never be stabilized then we stop updating the visibility of the scrollers.
        private const int k_MaxLocalLayoutPassCount = 5;
        private int m_FirstLayoutPass = -1; // The layout pass when the first geometry changed occurred. It may not be layoutPass = 0, which could occur when you have nested ScrollViews.

        ScrollerVisibility m_HorizontalScrollerVisibility;

        /// <summary>
        /// Specifies whether the horizontal scroll bar is visible.
        /// </summary>
        public ScrollerVisibility horizontalScrollerVisibility
        {
            get { return m_HorizontalScrollerVisibility; }
            set
            {
                m_HorizontalScrollerVisibility = value;
                UpdateScrollers(needsHorizontal, needsVertical);
            }
        }

        ScrollerVisibility m_VerticalScrollerVisibility;

        /// <summary>
        /// Specifies whether the vertical scroll bar is visible.
        /// </summary>
        public ScrollerVisibility verticalScrollerVisibility
        {
            get { return m_VerticalScrollerVisibility; }
            set
            {
                m_VerticalScrollerVisibility = value;
                UpdateScrollers(needsHorizontal, needsVertical);
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="ScrollView.horizontalScrollerVisibility"/> instead.
        /// </summary>
        [Obsolete("showHorizontal is obsolete. Use horizontalScrollerVisibility instead")]
        public bool showHorizontal
        {
            get => horizontalScrollerVisibility == ScrollerVisibility.AlwaysVisible;
            set => m_HorizontalScrollerVisibility = value ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Auto;
        }

        /// <summary>
        /// Obsolete. Use <see cref="ScrollView.verticalScrollerVisibility"/> instead.
        /// </summary>
        [Obsolete("showVertical is obsolete. Use verticalScrollerVisibility instead")]
        public bool showVertical
        {
            get => verticalScrollerVisibility == ScrollerVisibility.AlwaysVisible;
            set => m_VerticalScrollerVisibility = value ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Auto;
        }

        // Case 1297053: ScrollableWidth/Height may contain some numerical imprecisions.
        const float k_SizeThreshold = 0.001f;

        VisualElement m_AttachedRootVisualContainer;
        float m_SingleLineHeight = UIElementsUtility.singleLineHeight;
        const string k_SingleLineHeightPropertyName = "--unity-metrics-single_line-height";

        const float k_ScrollPageOverlapFactor = 0.1f;
        internal const float k_UnsetPageSizeValue = -1.0f;

        internal const float k_MouseWheelScrollSizeDefaultValue = 18.0f;
        internal const float k_MouseWheelScrollSizeUnset = -1.0f;
        internal bool m_MouseWheelScrollSizeIsInline;

        internal bool needsHorizontal
        {
            get
            {
                return horizontalScrollerVisibility == ScrollerVisibility.AlwaysVisible || (horizontalScrollerVisibility == ScrollerVisibility.Auto && scrollableWidth > k_SizeThreshold);
            }
        }

        internal bool needsVertical
        {
            get
            {
                return verticalScrollerVisibility == ScrollerVisibility.AlwaysVisible || (verticalScrollerVisibility == ScrollerVisibility.Auto && scrollableHeight > k_SizeThreshold);
            }
        }

        internal bool isVerticalScrollDisplayed
        {
            get
            {
                return verticalScroller.resolvedStyle.display == DisplayStyle.Flex;
            }
        }

        internal bool isHorizontalScrollDisplayed
        {
            get
            {
                return horizontalScroller.resolvedStyle.display == DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// The current scrolling position.
        /// </summary>
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

        private float m_HorizontalPageSize;

        /// <summary>
        /// This property controls the speed of the horizontal scrolling when using a keyboard or the on-screen scrollbar buttons (arrows and handle), based on the size of the page.
        /// </summary>
        public float horizontalPageSize
        {
            get { return m_HorizontalPageSize; }
            set
            {
                m_HorizontalPageSize = value;
                UpdateHorizontalSliderPageSize();
            }
        }

        private float m_VerticalPageSize;

        /// <summary>
        /// This property controls the speed of the vertical scrolling when using a keyboard or the on-screen scrollbar buttons (arrows and handle), based on the size of the page.
        /// </summary>
        public float verticalPageSize
        {
            get { return m_VerticalPageSize; }
            set
            {
                m_VerticalPageSize = value;
                UpdateVerticalSliderPageSize();
            }
        }

        private float m_MouseWheelScrollSize = k_MouseWheelScrollSizeDefaultValue;

        /// <summary>
        /// This property controls the scrolling speed only when using a mouse scroll wheel, based on the size of the page. It takes precedence over the --unity-metrics-single_line-height USS variable.
        /// </summary>
        public float mouseWheelScrollSize
        {
            get { return m_MouseWheelScrollSize; }
            set
            {
                var previous = m_MouseWheelScrollSize;
                if (Math.Abs(m_MouseWheelScrollSize - value) > float.Epsilon)
                {
                    m_MouseWheelScrollSizeIsInline = true;
                    m_MouseWheelScrollSize = value;
                }
            }
        }

        internal float scrollableWidth
        {
            get { return contentContainer.boundingBox.width - contentViewport.layout.width; }
        }

        internal float scrollableHeight
        {
            get { return contentContainer.boundingBox.height - contentViewport.layout.height; }
        }

        // For inertia: how quickly the scrollView stops from moving after PointerUp.
        private bool hasInertia => scrollDecelerationRate > 0f;
        private static readonly float k_DefaultScrollDecelerationRate = 0.135f;
        private float m_ScrollDecelerationRate = k_DefaultScrollDecelerationRate;
        /// <summary>
        /// Controls the rate at which the scrolling movement slows after a user scrolls using a touch interaction.
        /// </summary>
        /// <remarks>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. A value of 0 stops the scrolling immediately.
        /// </remarks>
        public float scrollDecelerationRate
        {
            get { return m_ScrollDecelerationRate; }
            set { m_ScrollDecelerationRate = Mathf.Max(0f, value); }
        }

        // For elastic behavior: how long it takes to go back to original position.
        private static readonly float k_DefaultElasticity = 0.1f;
        private float m_Elasticity = k_DefaultElasticity;
        /// <summary>
        /// The amount of elasticity to use when a user tries to scroll past the boundaries of the scroll view.
        /// </summary>
        /// <remarks>
        /// Elasticity is only used when <see cref="touchScrollBehavior"/> is set to Elastic.
        /// </remarks>
        public float elasticity
        {
            get { return m_Elasticity;}
            set { m_Elasticity = Mathf.Max(0f, value); }
        }

        /// <summary>
        /// The behavior to use when a user tries to scroll past the end of the ScrollView content using a touch interaction.
        /// </summary>
        public enum TouchScrollBehavior
        {
            /// <summary>
            /// The content position can move past the ScrollView boundaries.
            /// </summary>
            Unrestricted,
            /// <summary>
            /// The content position can overshoot the ScrollView boundaries, but then "snaps" back within them.
            /// </summary>
            Elastic,
            /// <summary>
            /// The content position is clamped to the ScrollView boundaries.
            /// </summary>
            Clamped,
        }

        private TouchScrollBehavior m_TouchScrollBehavior;
        /// <summary>
        /// The behavior to use when a user tries to scroll past the boundaries of the ScrollView content using a touch interaction.
        /// </summary>
        public TouchScrollBehavior touchScrollBehavior
        {
            get { return m_TouchScrollBehavior; }
            set
            {
                m_TouchScrollBehavior = value;
                if (m_TouchScrollBehavior == TouchScrollBehavior.Clamped)
                {
                    horizontalScroller.slider.clamped = true;
                    verticalScroller.slider.clamped = true;
                }
                else
                {
                    horizontalScroller.slider.clamped = false;
                    verticalScroller.slider.clamped = false;
                }
            }
        }

        /// <summary>
        /// Options for controlling how nested <see cref="ScrollView"/> handles scrolling when reaching
        /// the limits of the scrollable area.
        /// </summary>
        public enum NestedInteractionKind
        {
            /// <summary>
            /// Automatically selects the behavior according to the context in which the UI runs. For touch input, typically mobile devices,
            /// NestedInteractionKind.StopScrolling is used. For scroll wheel input, NestedInteractionKind.ForwardScrolling is used.
            /// </summary>
            Default,
            /// <summary>
            /// Scrolling capture will remain in the scroll view if it initiated the drag.
            /// </summary>
            StopScrolling,
            /// <summary>
            /// Scrolling will continue to the parent when no movement is possible in the scrolled direction.
            /// </summary>
            ForwardScrolling
        }

        NestedInteractionKind m_NestedInteractionKind;

        /// <summary>
        /// The behavior to use when scrolling reaches limits of a nested <see cref="ScrollView"/>.
        /// </summary>
        public NestedInteractionKind nestedInteractionKind
        {
            get => m_NestedInteractionKind;
            set => m_NestedInteractionKind = value;
        }

        static readonly long k_DefaultElasticAnimationInterval = 16;
        long m_ElasticAnimationIntervalMs = k_DefaultElasticAnimationInterval;

        /// <summary>
        /// Specifies the minimum amount of time in milliseconds between each elastic spring animation execution.
        /// </summary>
        public long elasticAnimationIntervalMs
        {
            get { return m_ElasticAnimationIntervalMs; }
            set
            {
                var previous = m_ElasticAnimationIntervalMs;
                m_ElasticAnimationIntervalMs = value;
                if (previous != m_ElasticAnimationIntervalMs)
                {
                    m_PostPointerUpAnimation = schedule.Execute(PostPointerUpAnimation).Every(m_ElasticAnimationIntervalMs);
                }
            }
        }

        void OnHorizontalScrollDragElementChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            UpdateHorizontalSliderPageSize();
        }

        void OnVerticalScrollDragElementChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            UpdateVerticalSliderPageSize();
        }

        void UpdateHorizontalSliderPageSize()
        {
            var containerWidth = horizontalScroller.resolvedStyle.width;
            var horizontalSliderPageSize = m_HorizontalPageSize;

            if (containerWidth > 0f)
            {
                if (Mathf.Approximately(m_HorizontalPageSize, k_UnsetPageSizeValue))
                {
                    var sliderDragElementWidth = horizontalScroller.slider.dragElement.resolvedStyle.width;
                    horizontalSliderPageSize = sliderDragElementWidth * (1f - k_ScrollPageOverlapFactor);
                }
            }

            if (horizontalSliderPageSize >= 0)
            {
                horizontalScroller.slider.pageSize = horizontalSliderPageSize;
            }
        }

        void UpdateVerticalSliderPageSize()
        {
            var containerHeight = verticalScroller.resolvedStyle.height;
            var verticalSliderPageSize = m_VerticalPageSize;

            if (containerHeight > 0f)
            {
                if (Mathf.Approximately(m_VerticalPageSize, k_UnsetPageSizeValue))
                {
                    var sliderDragElementHeight = verticalScroller.slider.dragElement.resolvedStyle.height;
                    verticalSliderPageSize = sliderDragElementHeight * (1f - k_ScrollPageOverlapFactor);
                }
            }

            if (verticalSliderPageSize >= 0)
            {
                verticalScroller.slider.pageSize = verticalSliderPageSize;
            }
        }

        internal void UpdateContentViewTransform()
        {
            // Adjust contentContainer's position
            var t = contentContainer.transform.position;

            var offset = scrollOffset;
            if (needsVertical)
                offset.y += contentContainer.resolvedStyle.top;

            t.x = GUIUtility.RoundToPixelGrid(-offset.x);
            t.y = GUIUtility.RoundToPixelGrid(-offset.y);
            contentContainer.transform.position = t;

            // TODO: Can we get rid of this?
            this.IncrementVersion(VersionChangeType.Repaint);
        }

        /// <summary>
        /// Scroll to a specific child element.
        /// </summary>
        /// <param name="child">The child to scroll to.</param>
        public void ScrollTo(VisualElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (!contentContainer.Contains(child))
                throw new ArgumentException("Cannot scroll to a VisualElement that's not a child of the ScrollView content-container.");

            m_Velocity = Vector2.zero;
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
            var viewSize = viewMax - viewMin;
            var childSize = childBoundaryMax - childBoundaryMin;
            if (childSize > viewSize)
            {
                if (viewMin > childBoundaryMin && childBoundaryMax > viewMax)
                    return 0f;

                return childBoundaryMin > viewMin ? childBoundaryMin - viewMin : childBoundaryMax - viewMax;
            }

            float deltaDistance = childBoundaryMax - viewMax;
            if (deltaDistance < -1)
            {
                deltaDistance = childBoundaryMin - viewMin;
            }

            return deltaDistance;
        }

        /// <summary>
        /// Represents the visible part of contentContainer.
        /// </summary>
        public VisualElement contentViewport { get; private set; } // Represents the visible part of contentContainer

        /// <summary>
        /// Horizontal scrollbar.
        /// </summary>
        public Scroller horizontalScroller { get; private set; }
        /// <summary>
        /// Vertical Scrollbar.
        /// </summary>
        public Scroller verticalScroller { get; private set; }

        private VisualElement m_ContentContainer;
        private VisualElement m_ContentAndVerticalScrollContainer;

        /// <summary>
        /// Contains full content, potentially partially visible.
        /// </summary>
        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-scroll-view";
        /// <summary>
        /// USS class name of viewport elements in elements of this type.
        /// </summary>
        public static readonly string viewportUssClassName = ussClassName + "__content-viewport";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string contentAndVerticalScrollUssClassName = ussClassName + "__content-and-vertical-scroll-container";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content-container";
        /// <summary>
        /// USS class name of horizontal scrollers in elements of this type.
        /// </summary>
        public static readonly string hScrollerUssClassName = ussClassName + "__horizontal-scroller";
        /// <summary>
        /// USS class name of vertical scrollers in elements of this type.
        /// </summary>
        public static readonly string vScrollerUssClassName = ussClassName + "__vertical-scroller";
        /// <summary>
        /// USS class name that's added when the ScrollView is in horizontal mode.
        /// <seealso cref="ScrollViewMode.Horizontal"/>
        /// </summary>
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        /// <summary>
        /// USS class name that's added when the ScrollView is in vertical mode.
        /// <seealso cref="ScrollViewMode.Vertical"/>
        /// </summary>
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        /// <summary>
        /// USS class name that's added when the ScrollView is in both horizontal and vertical mode.
        /// <seealso cref="ScrollViewMode.VerticalAndHorizontal"/>
        /// </summary>
        public static readonly string verticalHorizontalVariantUssClassName = ussClassName + "--vertical-horizontal";
        /// <undoc/>
        // TODO why does this exist? It is set in all cases...
        public static readonly string scrollVariantUssClassName = ussClassName + "--scroll";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScrollView() : this(ScrollViewMode.Vertical) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScrollView(ScrollViewMode scrollViewMode)
        {
            AddToClassList(ussClassName);

            m_ContentAndVerticalScrollContainer = new VisualElement() { name = "unity-content-and-vertical-scroll-container" };
            m_ContentAndVerticalScrollContainer.AddToClassList(contentAndVerticalScrollUssClassName);

            hierarchy.Add(m_ContentAndVerticalScrollContainer);

            contentViewport = new VisualElement() {name = "unity-content-viewport"};
            contentViewport.AddToClassList(viewportUssClassName);
            contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            contentViewport.pickingMode = PickingMode.Ignore;

            m_ContentAndVerticalScrollContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ContentAndVerticalScrollContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            m_ContentAndVerticalScrollContainer.Add(contentViewport);

            m_ContentContainer = new VisualElement() {name = "unity-content-container"};
            // Content container overflow is set to scroll which clip but we need to disable clipping in this case
            // or else absolute elements might not be shown. The viewport is in charge of clipping.
            // See case 1247583
            m_ContentContainer.disableClipping = true;
            m_ContentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_ContentContainer.AddToClassList(contentUssClassName);
            m_ContentContainer.usageHints = UsageHints.GroupTransform;
            contentViewport.Add(m_ContentContainer);

            SetScrollViewMode(scrollViewMode);

            const int defaultMinScrollValue = 0;
            const int defaultMaxScrollValue = int.MaxValue;

            horizontalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(value, scrollOffset.y);
                    UpdateContentViewTransform();
                }, SliderDirection.Horizontal)
            { viewDataKey = "HorizontalScroller" };
            horizontalScroller.AddToClassList(hScrollerUssClassName);
            horizontalScroller.style.display = DisplayStyle.None;
            hierarchy.Add(horizontalScroller);

            verticalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                    UpdateContentViewTransform();
                }, SliderDirection.Vertical)
            { viewDataKey = "VerticalScroller" };

            horizontalScroller.slider.clampedDragger.draggingEnded += UpdateElasticBehaviour;
            verticalScroller.slider.clampedDragger.draggingEnded += UpdateElasticBehaviour;

            horizontalScroller.lowButton.AddAction(UpdateElasticBehaviour);
            horizontalScroller.highButton.AddAction(UpdateElasticBehaviour);
            verticalScroller.lowButton.AddAction(UpdateElasticBehaviour);
            verticalScroller.highButton.AddAction(UpdateElasticBehaviour);

            verticalScroller.AddToClassList(vScrollerUssClassName);
            verticalScroller.style.display = DisplayStyle.None;
            m_ContentAndVerticalScrollContainer.Add(verticalScroller);

            touchScrollBehavior = TouchScrollBehavior.Clamped;

            RegisterCallback<WheelEvent>(OnScrollWheel);
            verticalScroller.RegisterCallback<GeometryChangedEvent>(OnScrollersGeometryChanged);
            horizontalScroller.RegisterCallback<GeometryChangedEvent>(OnScrollersGeometryChanged);

            horizontalPageSize = k_UnsetPageSizeValue;
            verticalPageSize = k_UnsetPageSizeValue;

            horizontalScroller.slider.dragElement.RegisterCallback<GeometryChangedEvent>(OnHorizontalScrollDragElementChanged);
            verticalScroller.slider.dragElement.RegisterCallback<GeometryChangedEvent>(OnVerticalScrollDragElementChanged);

            m_CapturedTargetPointerMoveCallback = OnPointerMove;
            m_CapturedTargetPointerUpCallback = OnPointerUp;
            scrollOffset = Vector2.zero;
        }

        private ScrollViewMode m_Mode;

        /// <summary>
        /// Controls how the ScrollView allows the user to scroll the contents.
        /// <seealso cref="ScrollViewMode"/>
        /// </summary>
        /// <remarks>
        /// The default is <see cref="ScrollViewMode.Vertical"/>.
        /// Writing to this property modifies the class list of the ScrollView according to the specified value of
        /// <see cref="ScrollViewMode"/>. When the value changes, the class list matching the old value is removed and
        /// the class list matching the new value is added.
        /// </remarks>
        public ScrollViewMode mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;
                SetScrollViewMode(value);
            }
        }

        private void SetScrollViewMode(ScrollViewMode mode)
        {
            m_Mode = mode;

            RemoveFromClassList(verticalVariantUssClassName);
            RemoveFromClassList(horizontalVariantUssClassName);
            RemoveFromClassList(verticalHorizontalVariantUssClassName);
            RemoveFromClassList(scrollVariantUssClassName);

            switch (mode)
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

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
            {
                return;
            }

            m_AttachedRootVisualContainer = GetRootVisualContainer();
            m_AttachedRootVisualContainer?.RegisterCallback<CustomStyleResolvedEvent>(OnRootCustomStyleResolved);
            ReadSingleLineHeight();

            if (evt.destinationPanel.contextType == ContextType.Player)
            {
                m_ContentAndVerticalScrollContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);

                contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                contentContainer.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
                contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                contentContainer.RegisterCallback<PointerCaptureEvent>(OnPointerCapture);
                contentContainer.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_ScheduledLayoutPassResetItem?.Pause();
            ResetLayoutPass();

            if (evt.originPanel == null)
            {
                return;
            }

            m_AttachedRootVisualContainer?.UnregisterCallback<CustomStyleResolvedEvent>(OnRootCustomStyleResolved);
            m_AttachedRootVisualContainer = null;

            if (evt.originPanel.contextType == ContextType.Player)
            {
                m_ContentAndVerticalScrollContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                m_ContentAndVerticalScrollContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                m_ContentAndVerticalScrollContainer.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
                m_ContentAndVerticalScrollContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

                contentContainer.UnregisterCallback<PointerCaptureEvent>(OnPointerCapture);
                contentContainer.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            }
        }

        void OnPointerCapture(PointerCaptureEvent evt)
        {
            m_CapturedTarget = evt.target as VisualElement;

            if (m_CapturedTarget == null)
                return;

            m_ScrollingPointerId = evt.pointerId;
            m_CapturedTarget.RegisterCallback(m_CapturedTargetPointerMoveCallback);
            m_CapturedTarget.RegisterCallback(m_CapturedTargetPointerUpCallback);
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            ReleaseScrolling(evt.pointerId, evt.target);

            if (m_CapturedTarget == null)
                return;

            m_CapturedTarget.UnregisterCallback(m_CapturedTargetPointerMoveCallback);
            m_CapturedTarget.UnregisterCallback(m_CapturedTargetPointerUpCallback);
            m_CapturedTarget = null;
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

            if (m_FirstLayoutPass == -1)
                m_FirstLayoutPass = evt.layoutPass;
            else
            {
                // Here, we update the visibility of the scrollbars for only few layout pass.
                // Exceeding this limit could suggest that the layout will never be stabilized if we keep showing/hiding the scrollbars.
                if ((evt.layoutPass - m_FirstLayoutPass) > k_MaxLocalLayoutPassCount)
                {
                    needsVerticalCached = needsVerticalCached || isVerticalScrollDisplayed;
                    needsHorizontalCached = needsHorizontalCached || isHorizontalScrollDisplayed;
                }
            }

            UpdateScrollers(needsHorizontalCached, needsVerticalCached);
            UpdateContentViewTransform();
            ScheduleResetLayoutPass();
        }

        private IVisualElementScheduledItem m_ScheduledLayoutPassResetItem;

        void ScheduleResetLayoutPass()
        {
            // Reset the cached layout pass information in the next frame.
            if (m_ScheduledLayoutPassResetItem == null)
            {
                m_ScheduledLayoutPassResetItem = schedule.Execute(ResetLayoutPass);
            }
            else
            {
                m_ScheduledLayoutPassResetItem.Pause();
                m_ScheduledLayoutPassResetItem.Resume();
            }
        }

        void ResetLayoutPass()
        {
            m_FirstLayoutPass = -1;
        }

        private int m_ScrollingPointerId = PointerId.invalidPointerId;
        private const float k_VelocityLerpTimeFactor = 10;
        internal const float ScrollThresholdSquared = 100;
        private Vector2 m_StartPosition;
        private Vector2 m_PointerStartPosition;
        private Vector2 m_Velocity;
        private Vector2 m_SpringBackVelocity;
        private Vector2 m_LowBounds;
        private Vector2 m_HighBounds;
        private float m_LastVelocityLerpTime;
        private bool m_StartedMoving;
        private bool m_TouchStoppedVelocity;
        VisualElement m_CapturedTarget;
        EventCallback<PointerMoveEvent> m_CapturedTargetPointerMoveCallback;
        EventCallback<PointerUpEvent> m_CapturedTargetPointerUpCallback;

        // Internal for tests
        internal IVisualElementScheduledItem m_PostPointerUpAnimation;

        // Compute the new scroll view offset from a pointer delta, taking elasticity into account.
        // Low and high limits are the values beyond which the scrollview starts to show resistance to scrolling (elasticity).
        // Low and high hard limits are the values beyond which it is infinitely hard to scroll.
        // The mapping between the normalized pointer delta and normalized scroll view offset delta in the
        // elastic zone is: offsetDelta = 1 - 1 / (pointerDelta + 1)
        private static float ComputeElasticOffset(float deltaPointer, float initialScrollOffset, float lowLimit,
            float hardLowLimit, float highLimit, float hardHighLimit)
        {
            // initialScrollOffset should be between hardLowLimit and hardHighLimit.
            // Add safety margin to avoid division by zero in code below.
            initialScrollOffset = Mathf.Max(initialScrollOffset, hardLowLimit * .95f);
            initialScrollOffset = Mathf.Min(initialScrollOffset, hardHighLimit * .95f);

            float delta;
            float scaleFactor;

            if (initialScrollOffset < lowLimit && hardLowLimit < lowLimit)
            {
                scaleFactor = lowLimit - hardLowLimit;
                // Find the current potential energy of current scroll offset
                var currentEnergy = (lowLimit - initialScrollOffset) / scaleFactor;
                // Find the cursor displacement that was needed to get there.
                // Because initialScrollOffset > hardLowLimit, we have currentEnergy < 1
                delta = currentEnergy * scaleFactor / (1 - currentEnergy);

                // Merge with deltaPointer
                delta += deltaPointer;
                // Now it is as if the initial offset was at low limit and the pointer delta was delta.
                initialScrollOffset = lowLimit;
            }
            else if (initialScrollOffset > highLimit && hardHighLimit > highLimit)
            {
                scaleFactor = hardHighLimit - highLimit;
                // Find the current potential energy of current scroll offset
                var currentEnergy = (initialScrollOffset - highLimit) / scaleFactor;
                // Find the cursor displacement that was needed to get there.
                // Because initialScrollOffset > hardLowLimit, we have currentEnergy < 1
                delta = -1 * currentEnergy * scaleFactor / (1 - currentEnergy);

                // Merge with deltaPointer
                delta += deltaPointer;
                // Now it is as if the initial offset was at high limit and the pointer delta was delta.
                initialScrollOffset = highLimit;
            }
            else
            {
                delta = deltaPointer;
            }

            var newOffset = initialScrollOffset - delta;
            float direction;
            if (newOffset < lowLimit)
            {
                // Apply elasticity on the portion below lowLimit
                delta = lowLimit - newOffset;
                initialScrollOffset = lowLimit;
                scaleFactor = lowLimit - hardLowLimit;
                direction = 1f;
            }
            else if (newOffset <= highLimit)
            {
                return newOffset;
            }
            else
            {
                // Apply elasticity on the portion beyond highLimit
                delta = newOffset - highLimit;
                initialScrollOffset = highLimit;
                scaleFactor = hardHighLimit - highLimit;
                direction = -1f;
            }

            if (Mathf.Abs(delta) < UIRUtility.k_Epsilon)
            {
                return initialScrollOffset;
            }

            // Compute energy given by the pointer displacement
            // normalizedDelta = delta / scaleFactor;
            // energy = 1 - 1 / (normalizedDelta + 1) = delta / (delta + scaleFactor)
            var energy = delta / (delta + scaleFactor);
            // Scale energy and use energy to do work on the offset
            energy *= scaleFactor;
            energy *= direction;
            newOffset = initialScrollOffset - energy;
            return newOffset;
        }

        private void ComputeInitialSpringBackVelocity()
        {
            if (touchScrollBehavior != TouchScrollBehavior.Elastic)
            {
                m_SpringBackVelocity = Vector2.zero;
                return;
            }

            if (scrollOffset.x < m_LowBounds.x)
            {
                m_SpringBackVelocity.x = m_LowBounds.x - scrollOffset.x;
            }
            else if (scrollOffset.x > m_HighBounds.x)
            {
                m_SpringBackVelocity.x = m_HighBounds.x - scrollOffset.x;
            }
            else
            {
                m_SpringBackVelocity.x = 0;
            }

            if (scrollOffset.y < m_LowBounds.y)
            {
                m_SpringBackVelocity.y = m_LowBounds.y - scrollOffset.y;
            }
            else if (scrollOffset.y > m_HighBounds.y)
            {
                m_SpringBackVelocity.y = m_HighBounds.y - scrollOffset.y;
            }
            else
            {
                m_SpringBackVelocity.y = 0;
            }
        }

        private void SpringBack()
        {
            if (touchScrollBehavior != TouchScrollBehavior.Elastic)
            {
                m_SpringBackVelocity = Vector2.zero;
                return;
            }

            var newOffset = scrollOffset;

            if (newOffset.x < m_LowBounds.x)
            {
                newOffset.x = Mathf.SmoothDamp(newOffset.x, m_LowBounds.x, ref m_SpringBackVelocity.x, elasticity,
                    Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(m_SpringBackVelocity.x) < 1)
                {
                    m_SpringBackVelocity.x = 0;
                }
            }
            else if (newOffset.x > m_HighBounds.x)
            {
                newOffset.x = Mathf.SmoothDamp(newOffset.x, m_HighBounds.x, ref m_SpringBackVelocity.x, elasticity,
                    Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(m_SpringBackVelocity.x) < 1)
                {
                    m_SpringBackVelocity.x = 0;
                }
            }
            else
            {
                m_SpringBackVelocity.x = 0;
            }

            if (newOffset.y < m_LowBounds.y)
            {
                newOffset.y = Mathf.SmoothDamp(newOffset.y, m_LowBounds.y, ref m_SpringBackVelocity.y, elasticity,
                    Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(m_SpringBackVelocity.y) < 1)
                {
                    m_SpringBackVelocity.y = 0;
                }
            }
            else if (newOffset.y > m_HighBounds.y)
            {
                newOffset.y = Mathf.SmoothDamp(newOffset.y, m_HighBounds.y, ref m_SpringBackVelocity.y, elasticity,
                    Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(m_SpringBackVelocity.y) < 1)
                {
                    m_SpringBackVelocity.y = 0;
                }
            }
            else
            {
                m_SpringBackVelocity.y = 0;
            }

            scrollOffset = newOffset;
        }

        // Internal for tests.
        internal void ApplyScrollInertia()
        {
            if (hasInertia && m_Velocity != Vector2.zero)
            {
                m_Velocity *= Mathf.Pow(scrollDecelerationRate, Time.unscaledDeltaTime);

                if (Mathf.Abs(m_Velocity.x) < 1 ||
                    touchScrollBehavior == TouchScrollBehavior.Elastic && (scrollOffset.x < m_LowBounds.x || scrollOffset.x > m_HighBounds.x))
                {
                    m_Velocity.x = 0;
                }

                if (Mathf.Abs(m_Velocity.y) < 1 ||
                    touchScrollBehavior == TouchScrollBehavior.Elastic && (scrollOffset.y < m_LowBounds.y || scrollOffset.y > m_HighBounds.y))
                {
                    m_Velocity.y = 0;
                }

                scrollOffset += m_Velocity * Time.unscaledDeltaTime;
            }
            else
            {
                m_Velocity = Vector2.zero;
            }
        }

        private void PostPointerUpAnimation()
        {
            ApplyScrollInertia();
            SpringBack();

            // This compares with epsilon.
            if (m_SpringBackVelocity == Vector2.zero && m_Velocity == Vector2.zero)
            {
                m_PostPointerUpAnimation.Pause();
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.pointerType == PointerType.mouse || !evt.isPrimary)
                return;

            if (m_ScrollingPointerId != PointerId.invalidPointerId)
            {
                ReleaseScrolling(m_ScrollingPointerId, evt.target);
            }

            m_PostPointerUpAnimation?.Pause();

            var touchStopsVelocityOnly = Mathf.Abs(m_Velocity.x) > 10 || Mathf.Abs(m_Velocity.y) > 10;

            m_ScrollingPointerId = evt.pointerId;
            m_StartedMoving = false;
            InitTouchScrolling(evt.position);

            if (touchStopsVelocityOnly)
            {
                contentContainer.CapturePointer(evt.pointerId);
                contentContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
                evt.StopPropagation();
                m_TouchStoppedVelocity = true;
            }
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerType == PointerType.mouse || !evt.isPrimary || evt.pointerId != m_ScrollingPointerId)
                return;

            if (evt.isHandledByDraggable)
            {
                m_PointerStartPosition = evt.position;
                m_StartPosition = scrollOffset;
                return;
            }

            Vector2 position = evt.position;
            var delta = position - m_PointerStartPosition;
            if (mode == ScrollViewMode.Horizontal)
                delta.y = 0;
            else if (mode == ScrollViewMode.Vertical)
                delta.x = 0;

            if (!m_TouchStoppedVelocity && !m_StartedMoving && delta.sqrMagnitude < ScrollThresholdSquared)
                return;

            var scrollResult = ComputeTouchScrolling(evt.position);

            if (scrollResult != TouchScrollingResult.Forward)
            {
                evt.isHandledByDraggable = true;
                evt.StopPropagation();

                if (!contentContainer.HasPointerCapture(evt.pointerId))
                    contentContainer.CapturePointer(evt.pointerId);
            }
            else
            {
                m_Velocity = Vector2.zero;
            }
        }

        void OnPointerCancel(PointerCancelEvent evt)
        {
            ReleaseScrolling(evt.pointerId, evt.target);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (ReleaseScrolling(evt.pointerId, evt.target))
            {
                contentContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
                evt.StopPropagation();
            }
        }

        // Internal for tests.
        internal enum TouchScrollingResult
        {
            Apply,
            Forward,
            Block
        }

        // Internal for tests.
        internal void InitTouchScrolling(Vector2 position)
        {
            m_PointerStartPosition = position;
            m_StartPosition = scrollOffset;
            m_Velocity = Vector2.zero;
            m_SpringBackVelocity = Vector2.zero;

            m_LowBounds = new Vector2(
                Mathf.Min(horizontalScroller.lowValue, horizontalScroller.highValue),
                Mathf.Min(verticalScroller.lowValue, verticalScroller.highValue));
            m_HighBounds = new Vector2(
                Mathf.Max(horizontalScroller.lowValue, horizontalScroller.highValue),
                Mathf.Max(verticalScroller.lowValue, verticalScroller.highValue));
        }

        // Internal for tests.
        internal TouchScrollingResult ComputeTouchScrolling(Vector2 position)
        {
            // Calculate offset based on touch scroll behavior.
            Vector2 newScrollOffset;
            if (touchScrollBehavior == TouchScrollBehavior.Clamped)
            {
                newScrollOffset = m_StartPosition - (position - m_PointerStartPosition);
                newScrollOffset = Vector2.Max(newScrollOffset, m_LowBounds);
                newScrollOffset = Vector2.Min(newScrollOffset, m_HighBounds);
            }
            else if (touchScrollBehavior == TouchScrollBehavior.Elastic)
            {
                Vector2 deltaPointer = position - m_PointerStartPosition;
                newScrollOffset.x = ComputeElasticOffset(deltaPointer.x, m_StartPosition.x,
                    m_LowBounds.x, m_LowBounds.x - contentViewport.resolvedStyle.width,
                    m_HighBounds.x, m_HighBounds.x + contentViewport.resolvedStyle.width);
                newScrollOffset.y = ComputeElasticOffset(deltaPointer.y, m_StartPosition.y,
                    m_LowBounds.y, m_LowBounds.y - contentViewport.resolvedStyle.height,
                    m_HighBounds.y, m_HighBounds.y + contentViewport.resolvedStyle.height);
            }
            else
            {
                newScrollOffset = m_StartPosition - (position - m_PointerStartPosition);
            }

            // Cancel opposite axis if mode is set to only a single direction.
            if (mode == ScrollViewMode.Vertical)
                newScrollOffset.x = m_LowBounds.x;
            else if (mode == ScrollViewMode.Horizontal)
                newScrollOffset.y = m_LowBounds.y;

            var shouldScrollOffsetChange = scrollOffset != newScrollOffset;
            if (shouldScrollOffsetChange)
            {
                return ApplyTouchScrolling(newScrollOffset) ? TouchScrollingResult.Apply : TouchScrollingResult.Forward;
            }

            var shouldBlock = m_StartedMoving && nestedInteractionKind != NestedInteractionKind.ForwardScrolling;
            return shouldBlock ? TouchScrollingResult.Block : TouchScrollingResult.Forward;
        }

        bool ApplyTouchScrolling(Vector2 newScrollOffset)
        {
            m_StartedMoving = true;

            if (hasInertia)
            {
                // Reset velocity if we reached bounds.
                if (newScrollOffset == m_LowBounds || newScrollOffset == m_HighBounds)
                {
                    m_Velocity = Vector2.zero;
                    scrollOffset = newScrollOffset;
                    return false;
                }

                // Account for idle pointer time.
                if (m_LastVelocityLerpTime > 0)
                {
                    var deltaTimeSinceLastLerp = Time.unscaledTime - m_LastVelocityLerpTime;
                    m_Velocity = Vector2.Lerp(m_Velocity, Vector2.zero, deltaTimeSinceLastLerp * k_VelocityLerpTimeFactor);
                }

                m_LastVelocityLerpTime = Time.unscaledTime;

                var deltaTime = Time.unscaledDeltaTime;
                var newVelocity = (newScrollOffset - scrollOffset) / deltaTime;
                m_Velocity = Vector2.Lerp(m_Velocity, newVelocity, deltaTime * k_VelocityLerpTimeFactor);
            }

            var scrollOffsetChanged = scrollOffset != newScrollOffset;
            scrollOffset = newScrollOffset;
            return scrollOffsetChanged;
        }

        bool ReleaseScrolling(int pointerId, IEventHandler target)
        {
            if (pointerId != m_ScrollingPointerId)
                return false;

            m_ScrollingPointerId = PointerId.invalidPointerId;

            m_TouchStoppedVelocity = false;
            m_StartedMoving = false;

            if (target != contentContainer || !contentContainer.HasPointerCapture(pointerId))
                return false;

            if (touchScrollBehavior == TouchScrollBehavior.Elastic || hasInertia)
            {
                ExecuteElasticSpringAnimation();
            }

            contentContainer.ReleasePointer(pointerId);
            return true;
        }

        void ExecuteElasticSpringAnimation()
        {
            ComputeInitialSpringBackVelocity();

            if (m_PostPointerUpAnimation == null)
            {
                m_PostPointerUpAnimation = schedule.Execute(PostPointerUpAnimation).Every(m_ElasticAnimationIntervalMs);
            }
            else
            {
                m_PostPointerUpAnimation.Resume();
            }
        }

        void AdjustScrollers()
        {
            float horizontalFactor = contentContainer.boundingBox.width > UIRUtility.k_Epsilon ? contentViewport.layout.width / contentContainer.boundingBox.width : 1f;
            float verticalFactor = contentContainer.boundingBox.height > UIRUtility.k_Epsilon ? contentViewport.layout.height / contentContainer.boundingBox.height : 1f;

            horizontalScroller.Adjust(horizontalFactor);
            verticalScroller.Adjust(verticalFactor);
        }

        internal void UpdateScrollers(bool displayHorizontal, bool displayVertical)
        {
            AdjustScrollers();

            // Set availability
            horizontalScroller.SetEnabled(contentContainer.boundingBox.width - contentViewport.layout.width > 0);
            verticalScroller.SetEnabled(contentContainer.boundingBox.height - contentViewport.layout.height > 0);

            var newShowHorizontal = displayHorizontal && m_HorizontalScrollerVisibility != ScrollerVisibility.Hidden;
            var newShowVertical = displayVertical && m_VerticalScrollerVisibility != ScrollerVisibility.Hidden;
            var newHorizontalDisplay = newShowHorizontal ? DisplayStyle.Flex : DisplayStyle.None;
            var newVerticalDisplay = newShowVertical ? DisplayStyle.Flex : DisplayStyle.None;

            // Set display as necessary
            if (newHorizontalDisplay != horizontalScroller.style.display)
            {
                horizontalScroller.style.display = newHorizontalDisplay;
            }
            if (newVerticalDisplay != verticalScroller.style.display)
            {
                verticalScroller.style.display = newVerticalDisplay;
            }

            // Need to set always, for touch scrolling.
            verticalScroller.lowValue = 0f;
            verticalScroller.highValue = scrollableHeight;
            horizontalScroller.lowValue = 0f;
            horizontalScroller.highValue = scrollableWidth;
        }

        private void OnScrollersGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            var newShowHorizontal = needsHorizontal && m_HorizontalScrollerVisibility != ScrollerVisibility.Hidden;

            // Align the right side of the horizontal scroller with the left side of the vertical scroller.
            if (newShowHorizontal)
            {
                horizontalScroller.style.marginRight = verticalScroller.layout.width;
            }

            AdjustScrollers();
        }

        // TODO: Same behaviour as IMGUI Scroll view
        void OnScrollWheel(WheelEvent evt)
        {
            var updateContentViewTransform = false;
            var canUseVerticalScroll = contentContainer.boundingBox.height - layout.height > 0;
            var canUseHorizontalScroll = contentContainer.boundingBox.width - layout.width > 0;
            var horizontalScrollDelta = canUseHorizontalScroll && !canUseVerticalScroll ? evt.delta.y : evt.delta.x;
            var mouseScrollFactor = m_MouseWheelScrollSizeIsInline ? mouseWheelScrollSize : m_SingleLineHeight;

            if (canUseVerticalScroll)
            {
                var oldVerticalValue = verticalScroller.value;
                verticalScroller.value += evt.delta.y * (verticalScroller.lowValue < verticalScroller.highValue ? 1f : -1f) * mouseScrollFactor;

                if (nestedInteractionKind == NestedInteractionKind.StopScrolling || !Mathf.Approximately(verticalScroller.value, oldVerticalValue))
                {
                    evt.StopPropagation();
                    updateContentViewTransform = true;
                }
            }

            if (canUseHorizontalScroll)
            {
                var oldHorizontalValue = horizontalScroller.value;
                horizontalScroller.value += horizontalScrollDelta * (horizontalScroller.lowValue < horizontalScroller.highValue ? 1f : -1f) * mouseScrollFactor;

                if (nestedInteractionKind == NestedInteractionKind.StopScrolling || !Mathf.Approximately(horizontalScroller.value, oldHorizontalValue))
                {
                    evt.StopPropagation();
                    updateContentViewTransform = true;
                }
            }

            if (updateContentViewTransform)
            {
                UpdateElasticBehaviour();
                UpdateContentViewTransform();
            }
        }

        void OnRootCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ReadSingleLineHeight();
        }

        void ReadSingleLineHeight()
        {
            if (m_AttachedRootVisualContainer?.computedStyle.customProperties != null &&
                m_AttachedRootVisualContainer.computedStyle.customProperties.TryGetValue(k_SingleLineHeightPropertyName, out var customProp))
            {
                if (customProp.sheet.TryReadDimension(customProp.handle, out var dimension))
                {
                    m_SingleLineHeight = dimension.value;
                }
            }
            else
            {
                m_SingleLineHeight = UIElementsUtility.singleLineHeight;
            }
        }

        void UpdateElasticBehaviour()
        {
            if (touchScrollBehavior == TouchScrollBehavior.Elastic)
            {
                m_LowBounds = new Vector2(
                    Mathf.Min(horizontalScroller.lowValue, horizontalScroller.highValue),
                    Mathf.Min(verticalScroller.lowValue, verticalScroller.highValue));
                m_HighBounds = new Vector2(
                    Mathf.Max(horizontalScroller.lowValue, horizontalScroller.highValue),
                    Mathf.Max(verticalScroller.lowValue, verticalScroller.highValue));

                ExecuteElasticSpringAnimation();
            }
        }
    }
}
