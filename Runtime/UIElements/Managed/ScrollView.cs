// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public class ScrollView : VisualElement
    {
        public Vector2 horizontalScrollerValues { get; set; }
        public Vector2 verticalScrollerValues { get; set; }

        public static readonly Vector2 kDefaultScrollerValues = new Vector2(0, 100);

        public bool showHorizontal { get; set; }
        public bool showVertical { get; set; }

        public bool needsHorizontal
        {
            get { return showHorizontal || (contentContainer.layout.width - layout.width > 0); }
        }

        public bool needsVertical
        {
            get { return showVertical || (contentContainer.layout.height - layout.height > 0); }
        }

        private VisualElement m_ContentContainer;

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

        private float scrollableWidth { get { return contentContainer.layout.width - contentViewport.layout.width; } }
        private float scrollableHeight { get { return contentContainer.layout.height - contentViewport.layout.height; } }

        void UpdateContentViewTransform()
        {
            // Adjust contentContainer's position
            var t = contentContainer.transform.position;

            var offset = scrollOffset;
            t.x = -offset.x;
            t.y = -offset.y;
            contentContainer.transform.position = t;

            this.Dirty(ChangeType.Repaint);
        }

        public VisualElement contentViewport { get; private set; }    // Represents the visible part of contentContainer

        [Obsolete("Please use contentContainer instead", false)]
        public VisualElement contentView { get { return contentContainer; } }
        public Scroller horizontalScroller { get; private set; }
        public Scroller verticalScroller { get; private set; }

        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }

        public ScrollView() : this(kDefaultScrollerValues, kDefaultScrollerValues)
        {
        }

        public ScrollView(Vector2 horizontalScrollerValues, Vector2 verticalScrollerValues)
        {
            this.horizontalScrollerValues = horizontalScrollerValues;
            this.verticalScrollerValues = verticalScrollerValues;

            contentViewport = new VisualElement() { name = "ContentViewport" };
            contentViewport.clippingOptions = ClippingOptions.ClipContents;
            shadow.Add(contentViewport);

            // Basic content container; its constraints should be defined in the USS file
            m_ContentContainer = new VisualElement() {name = "ContentView"};
            contentViewport.Add(m_ContentContainer);

            horizontalScroller = new Scroller(horizontalScrollerValues.x, horizontalScrollerValues.y,
                    (value) =>
                {
                    scrollOffset = new Vector2(value, scrollOffset.y);
                }, Slider.Direction.Horizontal)
            {name = "HorizontalScroller", persistenceKey = "HorizontalScroller"};
            shadow.Add(horizontalScroller);

            verticalScroller = new Scroller(verticalScrollerValues.x, verticalScrollerValues.y,
                    (value) =>
                {
                    scrollOffset = new Vector2(scrollOffset.x, value);
                }, Slider.Direction.Vertical)
            {name = "VerticalScroller", persistenceKey = "VerticalScroller"};
            shadow.Add(verticalScroller);

            RegisterCallback<WheelEvent>(OnScrollWheel);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == PostLayoutEvent.TypeId())
            {
                var postLayoutEvt = (PostLayoutEvent)evt;
                OnPostLayout(postLayoutEvt.hasNewLayout);
            }
        }

        private void OnPostLayout(bool hasNewLayout)
        {
            if (!hasNewLayout)
                return;

            if (contentContainer.layout.width > Mathf.Epsilon)
                horizontalScroller.Adjust(contentViewport.layout.width / contentContainer.layout.width);
            if (contentContainer.layout.height > Mathf.Epsilon)
                verticalScroller.Adjust(contentViewport.layout.height / contentContainer.layout.height);

            // Set availability
            horizontalScroller.SetEnabled(contentContainer.layout.width - layout.width > 0);
            verticalScroller.SetEnabled(contentContainer.layout.height - layout.height > 0);

            // Expand content if scrollbars are hidden
            contentViewport.style.positionRight = needsVertical ? verticalScroller.layout.width : 0;
            horizontalScroller.style.positionRight = needsVertical ? verticalScroller.layout.width : 0;
            contentViewport.style.positionBottom = needsHorizontal ? horizontalScroller.layout.height : 0;
            verticalScroller.style.positionBottom = needsHorizontal ? horizontalScroller.layout.height : 0;

            if (needsHorizontal)
            {
                horizontalScroller.lowValue = 0.0f;
                horizontalScroller.highValue = scrollableWidth;
            }
            else
            {
                horizontalScroller.value = 0.0f;
            }

            if (needsVertical)
            {
                verticalScroller.lowValue = 0.0f;
                verticalScroller.highValue = scrollableHeight;
            }
            else
            {
                verticalScroller.value = 0.0f;
            }

            // Set visibility and remove/add content viewport margin as necessary
            if (horizontalScroller.visible != needsHorizontal)
            {
                horizontalScroller.visible = needsHorizontal;
                if (needsHorizontal)
                {
                    contentViewport.AddToClassList("HorizontalScroll");
                }
                else
                {
                    contentViewport.RemoveFromClassList("HorizontalScroll");
                }
            }

            if (verticalScroller.visible != needsVertical)
            {
                verticalScroller.visible = needsVertical;
                if (needsVertical)
                {
                    contentViewport.AddToClassList("VerticalScroll");
                }
                else
                {
                    contentViewport.RemoveFromClassList("VerticalScroll");
                }
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
                    verticalScroller.ScrollPageUp();
                else if (evt.delta.y > 0)
                    verticalScroller.ScrollPageDown();
            }

            evt.StopPropagation();
        }
    }
}
