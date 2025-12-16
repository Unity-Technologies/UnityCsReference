// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements.HierarchyV2
{
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class ScrollContainer : VisualElement
    {
        const float k_MouseScrollFactor = 18f;
        VisualElement m_Container;
        VisualElement m_Viewport;

        /// <summary>
        /// The main content container of this control.
        /// </summary>
        public override VisualElement contentContainer
        {
            get => m_Container;
        }

        /// <summary>
        /// Represents the visible part of container.
        /// </summary>
        public VisualElement viewport
        {
            get => m_Viewport;
        }

        CollectionViewScroller m_VerticalScroller;

        /// <summary>
        /// The vertical scrollbar of the container.
        /// </summary>
        public CollectionViewScroller verticalScroller
        {
            get => m_VerticalScroller;
            private set => m_VerticalScroller = value;
        }

        CollectionViewScroller m_HorizontalScroller;

        /// <summary>
        /// The horizontal scrollbar of the container.
        /// </summary>
        public CollectionViewScroller horizontalScroller
        {
            get => m_HorizontalScroller;
            private set => m_HorizontalScroller = value;
        }

        Vector2 m_ContainerOffset;

        /// <summary>
        /// The offset of the container.
        /// </summary>
        public Vector2 containerOffset
        {
            get => m_ContainerOffset;
            set
            {
                if (!Mathf.Approximately(m_ContainerOffset.x, value.x) || !Mathf.Approximately(m_ContainerOffset.y, value.y))
                {
                    m_ContainerOffset.x = horizontalScroller.highValue > 0 && value.x >= 0 ? value.x : 0;
                    m_ContainerOffset.y = verticalScroller.highValue > 0 && value.y >= 0 ? value.y : 0;
                    m_Container.style.translate = new Vector3(-m_ContainerOffset.x, -m_ContainerOffset.y, 0);
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-collection-view-scroll-view";
        /// <summary>
        /// USS class name of CollectionView scroll container in elements of this type.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__content-container";
        /// <summary>
        /// USS class name of the vertical scroller in elements of this type.
        /// </summary>
        public static readonly string verticalScrollerUssClassName = ussClassName + "__vertical-scroller";
        /// <summary>
        /// USS class name of the horizontal scroller in elements of this type.
        /// </summary>
        public static readonly string horizontalScrollerUssClassName = ussClassName + "__horizontal-scroller";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string contentAndHorizontalScrollUssClassName = ussClassName + "__content-and-horizontal-scroll-container";
        /// <summary>
        /// USS class name of content viewport in elements of this type.
        /// </summary>
        public static readonly string contentViewportUssClassName = ussClassName + "__content-viewport";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScrollContainer()
        {
            AddToClassList(ussClassName);
            m_Viewport = new VisualElement();
            m_Viewport.AddToClassList(contentViewportUssClassName);
            m_Viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Container = new VisualElement();
            m_Container.AddToClassList(containerUssClassName);
            m_Container.RegisterCallback<WheelEvent>(OnScrollWheel);
            m_Container.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            verticalScroller = new CollectionViewScroller();
            verticalScroller.AddToClassList(verticalScrollerUssClassName);

            horizontalScroller = new CollectionViewScroller { direction = SliderDirection.Horizontal };
            horizontalScroller.AddToClassList(horizontalScrollerUssClassName);
            horizontalScroller.RegisterValueChangedCallback(evt =>
            {
                var offset = containerOffset;
                offset.x = (float)evt.newValue;
                containerOffset = offset;
            });

            m_Viewport.Add(m_Container);

            var containerWithHorizontalScroller = new VisualElement();
            containerWithHorizontalScroller.AddToClassList(contentAndHorizontalScrollUssClassName);
            containerWithHorizontalScroller.Add(m_Viewport);
            containerWithHorizontalScroller.Add(horizontalScroller);
            hierarchy.Add(containerWithHorizontalScroller);
            hierarchy.Add(verticalScroller);
        }

        void OnScrollWheel(WheelEvent evt)
        {
            verticalScroller.value += evt.delta.y * (verticalScroller.lowValue < verticalScroller.highValue ? 1f : -1f) * k_MouseScrollFactor;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            AdjustScroller();
        }

        void AdjustScroller()
        {
            horizontalScroller.Adjust();
            verticalScroller.Adjust();
        }
    }
}
