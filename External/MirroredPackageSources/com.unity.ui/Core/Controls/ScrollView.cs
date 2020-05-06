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
    /// Mode configuring the <see cref="ScrollView"/> for the intended usage.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="ScrollViewMode.Vertical"/>.
    /// </remarks>
    public enum ScrollViewMode
    {
        /// <summary>
        /// Configure <see cref="ScrollView"/> for vertical scrolling.
        /// </summary>
        /// <remarks>
        /// Require elements with an height.
        /// </remarks>
        Vertical,
        /// <summary>
        /// Configure <see cref="ScrollView"/> for horizontal scrolling.
        /// </summary>
        /// <remarks>
        /// Require elements with a width.
        /// If <see cref="ScrollView"/> is set to flex-grow or if it's parent is set to <see cref="FlexDirection.Row"/> elements height stretch else they require a height.
        /// </remarks>
        Horizontal,
        /// <summary>
        /// Configure <see cref="ScrollView"/> for vertical and horizontal scrolling.
        /// </summary>
        /// <remarks>
        /// Require elements with an height.
        /// The difference with the vertical mode is that content will not wrap.
        /// </remarks>
        VerticalAndHorizontal
    }

    /// <summary>
    /// Displays its contents inside a scrollable frame.
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
            UxmlEnumAttributeDescription<ScrollViewMode> m_ScrollViewMode =
                new UxmlEnumAttributeDescription<ScrollViewMode>
            {name = "mode", defaultValue = ScrollViewMode.Vertical};

            UxmlBoolAttributeDescription m_ShowHorizontal = new UxmlBoolAttributeDescription
            {name = "show-horizontal-scroller"};

            UxmlBoolAttributeDescription m_ShowVertical = new UxmlBoolAttributeDescription
            {name = "show-vertical-scroller"};

            UxmlFloatAttributeDescription m_HorizontalPageSize = new UxmlFloatAttributeDescription
            {name = "horizontal-page-size", defaultValue = Scroller.kDefaultPageSize};

            UxmlFloatAttributeDescription m_VerticalPageSize = new UxmlFloatAttributeDescription
            {name = "vertical-page-size", defaultValue = Scroller.kDefaultPageSize};

            UxmlEnumAttributeDescription<TouchScrollBehavior> m_TouchScrollBehavior =
                new UxmlEnumAttributeDescription<TouchScrollBehavior>
            {
                name = "touch-scroll-type", defaultValue = TouchScrollBehavior.Clamped
            };

            UxmlFloatAttributeDescription m_ScrollDecelerationRate = new UxmlFloatAttributeDescription
            {name = "scroll-deceleration-rate", defaultValue = k_DefaultScrollDecelerationRate};

            UxmlFloatAttributeDescription m_Elasticity = new UxmlFloatAttributeDescription
            {name = "elasticity", defaultValue = k_DefaultElasticity};


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
                scrollView.SetScrollViewMode(m_ScrollViewMode.GetValueFromBag(bag, cc));
                scrollView.showHorizontal = m_ShowHorizontal.GetValueFromBag(bag, cc);
                scrollView.showVertical = m_ShowVertical.GetValueFromBag(bag, cc);
                scrollView.horizontalPageSize = m_HorizontalPageSize.GetValueFromBag(bag, cc);
                scrollView.verticalPageSize = m_VerticalPageSize.GetValueFromBag(bag, cc);
                scrollView.scrollDecelerationRate = m_ScrollDecelerationRate.GetValueFromBag(bag, cc);
                scrollView.touchScrollBehavior = m_TouchScrollBehavior.GetValueFromBag(bag, cc);
                scrollView.elasticity = m_Elasticity.GetValueFromBag(bag, cc);
            }
        }

        private bool m_ShowHorizontal;

        /// <summary>
        /// Should the horizontal scroller be visible.
        /// </summary>
        public bool showHorizontal
        {
            get { return m_ShowHorizontal; }
            set
            {
                m_ShowHorizontal = value;
                UpdateScrollers(m_ShowHorizontal, m_ShowVertical);
            }
        }

        private bool m_ShowVertical;

        /// <summary>
        /// Should the vertical scroller be visible.
        /// </summary>
        public bool showVertical
        {
            get { return m_ShowVertical; }
            set
            {
                m_ShowVertical = value;
                UpdateScrollers(m_ShowHorizontal, m_ShowVertical);
            }
        }

        internal bool needsHorizontal
        {
            get
            {
                return showHorizontal || (contentContainer.layout.width - layout.width > 0);
            }
        }

        internal bool needsVertical
        {
            get
            {
                return showVertical || (contentContainer.layout.height - layout.height > 0);
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

        /// <summary>
        /// This property is controlling the scrolling speed of the horizontal scroller.
        /// </summary>
        public float horizontalPageSize
        {
            get { return horizontalScroller.slider.pageSize; }
            set { horizontalScroller.slider.pageSize = value; }
        }

        /// <summary>
        /// This property is controlling the scrolling speed of the vertical scroller.
        /// </summary>
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

        /// <summary>
        /// Scroll to a specific child element.
        /// </summary>
        /// <param name="child">The child to scroll to.</param>
        public void ScrollTo(VisualElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

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
        public static readonly string contentUssClassName = ussClassName + "__content-container";
        /// <summary>
        /// USS class name of horizontal scrollers in elements of this type.
        /// </summary>
        public static readonly string hScrollerUssClassName = ussClassName + "__horizontal-scroller";
        /// <summary>
        /// USS class name of vertical scrollers in elements of this type.
        /// </summary>
        public static readonly string vScrollerUssClassName = ussClassName + "__vertical-scroller";
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        public static readonly string verticalHorizontalVariantUssClassName = ussClassName + "--vertical-horizontal";
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

            contentViewport = new VisualElement() {name = "unity-content-viewport"};
            contentViewport.AddToClassList(viewportUssClassName);
            contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            contentViewport.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            contentViewport.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            hierarchy.Add(contentViewport);

            m_ContentContainer = new VisualElement() {name = "unity-content-container"};
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
            {viewDataKey = "HorizontalScroller", visible = false};
            horizontalScroller.AddToClassList(hScrollerUssClassName);
            hierarchy.Add(horizontalScroller);

            verticalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                    UpdateContentViewTransform();
                }, SliderDirection.Vertical)
            {viewDataKey = "VerticalScroller", visible = false};
            verticalScroller.AddToClassList(vScrollerUssClassName);
            hierarchy.Add(verticalScroller);

            touchScrollBehavior = TouchScrollBehavior.Clamped;

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

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
            {
                return;
            }

            if (evt.destinationPanel.contextType == ContextType.Player)
            {
                contentViewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
                contentViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                contentViewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
            {
                return;
            }

            if (evt.originPanel.contextType == ContextType.Player)
            {
                contentViewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                contentViewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                contentViewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
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

        private int m_ScrollingPointerId = PointerId.invalidPointerId;
        private Vector2 m_StartPosition;
        private Vector2 m_PointerStartPosition;
        private Vector2 m_Velocity;
        private Vector2 m_SpringBackVelocity;
        private Vector2 m_LowBounds;
        private Vector2 m_HighBounds;
        private IVisualElementScheduledItem m_PostPointerUpAnimation;

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

            if (Mathf.Abs(delta) < Mathf.Epsilon)
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

        private void ApplyScrollInertia()
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
            if (evt.pointerType != PointerType.mouse && evt.isPrimary && m_ScrollingPointerId == PointerId.invalidPointerId)
            {
                m_PostPointerUpAnimation?.Pause();

                m_ScrollingPointerId = evt.pointerId;
                m_PointerStartPosition = evt.position;
                m_StartPosition = scrollOffset;
                m_Velocity = Vector2.zero;
                m_SpringBackVelocity = Vector2.zero;

                m_LowBounds = new Vector2(
                    Mathf.Min(horizontalScroller.lowValue, horizontalScroller.highValue),
                    Mathf.Min(verticalScroller.lowValue, verticalScroller.highValue));
                m_HighBounds = new Vector2(
                    Mathf.Max(horizontalScroller.lowValue, horizontalScroller.highValue),
                    Mathf.Max(verticalScroller.lowValue, verticalScroller.highValue));

                evt.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId == m_ScrollingPointerId)
            {
                Vector2 newScrollOffset;
                if (touchScrollBehavior == TouchScrollBehavior.Clamped)
                {
                    newScrollOffset = m_StartPosition - (new Vector2(evt.position.x, evt.position.y) - m_PointerStartPosition);
                    newScrollOffset = Vector2.Max(newScrollOffset, m_LowBounds);
                    newScrollOffset = Vector2.Min(newScrollOffset, m_HighBounds);
                }
                else if (touchScrollBehavior == TouchScrollBehavior.Elastic)
                {
                    Vector2 deltaPointer = new Vector2(evt.position.x, evt.position.y) - m_PointerStartPosition;
                    newScrollOffset.x = ComputeElasticOffset(deltaPointer.x, m_StartPosition.x,
                        m_LowBounds.x, m_LowBounds.x - contentViewport.resolvedStyle.width,
                        m_HighBounds.x, m_HighBounds.x + contentViewport.resolvedStyle.width);
                    newScrollOffset.y = ComputeElasticOffset(deltaPointer.y, m_StartPosition.y,
                        m_LowBounds.y, m_LowBounds.y - contentViewport.resolvedStyle.height,
                        m_HighBounds.y, m_HighBounds.y + contentViewport.resolvedStyle.height);
                }
                else
                {
                    newScrollOffset = m_StartPosition - (new Vector2(evt.position.x, evt.position.y) - m_PointerStartPosition);
                }

                if (hasInertia)
                {
                    float deltaTime = Time.unscaledDeltaTime;
                    var newVelocity = (newScrollOffset - scrollOffset) / deltaTime;
                    m_Velocity = Vector2.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }

                scrollOffset = newScrollOffset;

                evt.currentTarget.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == m_ScrollingPointerId)
            {
                evt.currentTarget.ReleasePointer(evt.pointerId);
                evt.StopPropagation();

                if (touchScrollBehavior == TouchScrollBehavior.Elastic || hasInertia)
                {
                    ComputeInitialSpringBackVelocity();

                    if (m_PostPointerUpAnimation == null)
                    {
                        m_PostPointerUpAnimation = schedule.Execute(PostPointerUpAnimation).Every(30);
                    }
                    else
                    {
                        m_PostPointerUpAnimation.Resume();
                    }
                }

                m_ScrollingPointerId = PointerId.invalidPointerId;
            }
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
