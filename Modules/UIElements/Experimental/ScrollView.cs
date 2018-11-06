// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements
{
    public class ScrollView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ScrollView, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_ShowHorizontal = new UxmlBoolAttributeDescription { name = "show-horizontal-scroller" };
            UxmlBoolAttributeDescription m_ShowVertical = new UxmlBoolAttributeDescription { name = "show-vertical-scroller" };

            UxmlFloatAttributeDescription m_HorizontalPageSize = new UxmlFloatAttributeDescription { name = "horizontal-page-size", defaultValue = Scroller.kDefaultPageSize };
            UxmlFloatAttributeDescription m_VerticalPageSize = new UxmlFloatAttributeDescription { name = "vertical-page-size", defaultValue = Scroller.kDefaultPageSize };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ScrollView scrollView = (ScrollView)ve;
                scrollView.showHorizontal = m_ShowHorizontal.GetValueFromBag(bag, cc);
                scrollView.showVertical = m_ShowVertical.GetValueFromBag(bag, cc);
                scrollView.horizontalPageSize = m_HorizontalPageSize.GetValueFromBag(bag, cc);
                scrollView.verticalPageSize = m_VerticalPageSize.GetValueFromBag(bag, cc);
            }
        }

        public bool showHorizontal { get; set; }
        public bool showVertical { get; set; }

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
            t.x = -offset.x;
            t.y = -offset.y;
            contentContainer.transform.position = t;

            this.IncrementVersion(VersionChangeType.Repaint);
        }

        public void ScrollTo(VisualElement child)
        {
            // Child not in content view, no need to continue.
            if (!contentContainer.Contains(child))
                throw new ArgumentException("Cannot scroll to null child");

            float yTransform = contentContainer.transform.position.y * -1;
            float viewMin = contentViewport.layout.yMin + yTransform;
            float viewMax = contentViewport.layout.yMax + yTransform;

            float childBoundaryMin = child.layout.yMin;
            float childBoundaryMax = child.layout.yMax;
            if ((childBoundaryMin >= viewMin && childBoundaryMax <= viewMax) || float.IsNaN(childBoundaryMin) || float.IsNaN(childBoundaryMax))
                return;

            bool scrollUpward = false;
            float deltaDistance = childBoundaryMax - viewMax;
            if (deltaDistance < -1)
            {
                // Direction = upward
                deltaDistance = viewMin - childBoundaryMin;
                scrollUpward = true;
            }

            float deltaOffset = deltaDistance * verticalScroller.highValue / scrollableHeight;

            verticalScroller.value = scrollOffset.y + (scrollUpward ? -deltaOffset : deltaOffset);
            UpdateContentViewTransform();
        }

        public VisualElement contentViewport { get; private set; } // Represents the visible part of contentContainer

        public Scroller horizontalScroller { get; private set; }
        public Scroller verticalScroller { get; private set; }

        private VisualElement m_ContentContainer;
        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }

        public ScrollView() : this(FlexDirection.Column) {}

        public ScrollView(FlexDirection contentDirection)
        {
            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            contentViewport = new VisualElement();
            contentViewport.AddToClassList("unity-scrollview-content-viewport");
            contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            shadow.Add(contentViewport);

            // Basic content container; its constraints should be defined in the USS file
            m_ContentContainer = new VisualElement();
            m_ContentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            switch (contentDirection)
            {
                case FlexDirection.Column:
                    m_ContentContainer.AddToClassList("unity-scrollview-vertical");
                    break;
                case FlexDirection.ColumnReverse:
                    m_ContentContainer.AddToClassList("unity-scrollview-vertical-reverse");
                    break;
                case FlexDirection.Row:
                    m_ContentContainer.AddToClassList("unity-scrollview-horizontal");
                    break;
                case FlexDirection.RowReverse:
                    m_ContentContainer.AddToClassList("unity-scrollview-horizontal-reverse");
                    break;
            }

            m_ContentContainer.AddToClassList("unity-scrollview-content-container");
            contentViewport.Add(m_ContentContainer);

            const int defaultMinScrollValue = 0;
            const int defaultMaxScrollValue = 100;

            horizontalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(value, scrollOffset.y);
                    UpdateContentViewTransform();
                }, SliderDirection.Horizontal)
            { persistenceKey = "HorizontalScroller", visible = false };
            horizontalScroller.AddToClassList("unity-scrollview-horizontal-scroller");
            shadow.Add(horizontalScroller);

            verticalScroller = new Scroller(defaultMinScrollValue, defaultMaxScrollValue,
                (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                    UpdateContentViewTransform();
                }, SliderDirection.Vertical)
            { persistenceKey = "VerticalScroller", visible = false };
            verticalScroller.AddToClassList("unity-scrollview-vertical-scroller");
            shadow.Add(verticalScroller);

            RegisterCallback<WheelEvent>(OnScrollWheel);
            scrollOffset = Vector2.zero;
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            // If the ScrollView is collapsed (no height/width set) automatically adds flex-grow for the user
            // This way it behave almost like if the ScrollView content was relative and will always take space
            if (layout.height <= 0f || layout.width <= 0f)
            {
                AddToClassList("unity-flex-grow");
            }
            UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Only affected by dimension changes
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            if (contentContainer.layout.width > Mathf.Epsilon)
                horizontalScroller.Adjust(contentViewport.layout.width / contentContainer.layout.width);
            if (contentContainer.layout.height > Mathf.Epsilon)
                verticalScroller.Adjust(contentViewport.layout.height / contentContainer.layout.height);

            // Set availability
            horizontalScroller.SetEnabled(contentContainer.layout.width - contentViewport.layout.width > 0);
            verticalScroller.SetEnabled(contentContainer.layout.height - contentViewport.layout.height > 0);

            // Expand content if scrollbars are hidden
            contentViewport.style.marginRight = needsVertical ? verticalScroller.layout.width : 0;
            horizontalScroller.style.positionRight = needsVertical ? verticalScroller.layout.width : 0;
            contentViewport.style.marginBottom = needsHorizontal ? horizontalScroller.layout.height : 0;
            verticalScroller.style.positionBottom = needsHorizontal ? horizontalScroller.layout.height : 0;

            // Set min width/height on the content container to make its items stretch by default
            contentContainer.style.minWidth = contentViewport.layout.width;
            contentContainer.style.minHeight = contentViewport.layout.height;

            if (needsHorizontal && scrollableWidth > 0f)
            {
                horizontalScroller.lowValue = 0f;
                horizontalScroller.highValue = scrollableWidth;
            }
            else
            {
                horizontalScroller.value = 0f;
            }

            if (needsVertical && scrollableHeight > 0f)
            {
                verticalScroller.lowValue = 0f;
                verticalScroller.highValue = scrollableHeight;
            }
            else
            {
                verticalScroller.value = 0f;
            }

            // Set visibility and remove/add content viewport margin as necessary
            if (horizontalScroller.visible != needsHorizontal)
            {
                horizontalScroller.visible = needsHorizontal;
            }

            if (verticalScroller.visible != needsVertical)
            {
                verticalScroller.visible = needsVertical;
            }

            UpdateContentViewTransform();
        }

        // TODO: Same behaviour as IMGUI Scroll view; it would probably be nice to show same behaviour
        // as Web browsers, which give back event to parent if not consumed
        void OnScrollWheel(WheelEvent evt)
        {
            if (contentContainer.layout.height - layout.height > 0)
            {
                if (evt.delta.y < 0)
                    verticalScroller.ScrollPageUp(Mathf.Abs(evt.delta.y));
                else if (evt.delta.y > 0)
                    verticalScroller.ScrollPageDown(Mathf.Abs(evt.delta.y));
            }

            evt.StopPropagation();
        }
    }
}
