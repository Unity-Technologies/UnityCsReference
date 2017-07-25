// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public class ScrollView : VisualContainer
    {
        public Vector2 horizontalScrollerValues { get; set; }
        public Vector2 verticalScrollerValues { get; set; }

        public static readonly Vector2 kDefaultScrollerValues = new Vector2(0, 100);

        public bool showHorizontal { get; set; }
        public bool showVertical { get; set; }

        public bool needsHorizontal
        {
            get { return showHorizontal || (contentView.layout.width - layout.width > 0); }
        }

        public bool needsVertical
        {
            get { return showVertical || (contentView.layout.height - layout.height > 0); }
        }

        Vector2 m_ScrollOffset;
        public Vector2 scrollOffset
        {
            get { return m_ScrollOffset; }
            set
            {
                m_ScrollOffset = value;
                UpdateContentViewTransform();
            }
        }

        void UpdateContentViewTransform()
        {
            // [0..1]
            Vector2 normalizedOffset = m_ScrollOffset;
            normalizedOffset.x -= horizontalScroller.lowValue;
            normalizedOffset.x /= (horizontalScroller.highValue - horizontalScroller.lowValue);
            normalizedOffset.y -= verticalScroller.lowValue;
            normalizedOffset.y /= (verticalScroller.highValue - verticalScroller.lowValue);

            // Adjust contentView's position
            float scrollableWidth = contentView.layout.width - contentViewport.layout.width;
            float scrollableHeight = contentView.layout.height - contentViewport.layout.height;

            var t = contentView.transform.position;
            t.x = -(normalizedOffset.x * scrollableWidth);
            t.y = -(normalizedOffset.y * scrollableHeight);
            contentView.transform.position = t;

            this.Dirty(ChangeType.Repaint);
        }

        public VisualContainer contentView { get; private set; }        // Contains full content, potentially partially visible
        public VisualContainer contentViewport { get; private set; }    // Represents the visible part of contentView
        public Scroller horizontalScroller { get; private set; }
        public Scroller verticalScroller { get; private set; }

        public ScrollView() : this(kDefaultScrollerValues, kDefaultScrollerValues)
        {
        }

        public ScrollView(Vector2 horizontalScrollerValues, Vector2 verticalScrollerValues)
        {
            this.horizontalScrollerValues = horizontalScrollerValues;
            this.verticalScrollerValues = verticalScrollerValues;

            // Basic content container; its constraints should be defined in the USS file
            contentView = new VisualContainer() {name = "ContentView"};
            contentViewport = new VisualContainer() {name = "ContentViewport"};
            contentViewport.clipChildren = true;
            contentViewport.AddChild(contentView);
            AddChild(contentViewport);

            horizontalScroller = new Scroller(horizontalScrollerValues.x, horizontalScrollerValues.y,
                    (value) =>
                {
                    scrollOffset = new Vector2(value, scrollOffset.y);
                }, Slider.Direction.Horizontal)
            {name = "HorizontalScroller"};
            AddChild(horizontalScroller);

            verticalScroller = new Scroller(verticalScrollerValues.x, verticalScrollerValues.y,
                    (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                }, Slider.Direction.Vertical)
            {name = "VerticalScroller"};
            AddChild(verticalScroller);

            RegisterCallback<WheelEvent>(OnScrollWheel);
        }

        protected internal override void OnPostLayout(bool hasNewLayout)
        {
            if (!hasNewLayout)
                return;

            if (contentView.layout.width > Mathf.Epsilon)
                horizontalScroller.Adjust(contentViewport.layout.width / contentView.layout.width);
            if (contentView.layout.height > Mathf.Epsilon)
                verticalScroller.Adjust(contentViewport.layout.height / contentView.layout.height);

            // Set availability
            horizontalScroller.enabled = (contentView.layout.width - layout.width > 0);
            verticalScroller.enabled = (contentView.layout.height - layout.height > 0);

            // Set visibility
            horizontalScroller.visible = needsHorizontal;
            verticalScroller.visible = needsVertical;

            UpdateContentViewTransform();
        }

        // TODO: Same behaviour as IMGUI Scroll view; it would probably be nice to show same behaviour
        // as Web browsers, which give back event to parent if not consumed
        void OnScrollWheel(WheelEvent evt)
        {
            if (contentView.layout.height - layout.height > 0)
            {
                if (evt.delta.y < 0)
                    verticalScroller.ScrollPageUp();
                else if (evt.delta.y > 0)
                    verticalScroller.ScrollPageDown();
            }

            evt.StopPropagation();
        }
    }
}
