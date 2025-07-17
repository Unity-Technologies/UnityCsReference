// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class ToolbarOverlayContainer : OverlayContainer
    {
        [Serializable]
        public new class UxmlSerializedData : OverlayContainer.UxmlSerializedData
        {
            public override object CreateInstance() => new ToolbarOverlayContainer();
        }

        const string k_ToolbarClassName = "overlay-toolbar-area";
        public const string k_RightToolbarName = "overlay-toolbar__right";
        public const string k_LeftToolbarName = "overlay-toolbar__left";

        readonly Dictionary<Overlay, Action<bool>> m_OverlayToDisplayCallback = new Dictionary<Overlay, Action<bool>>();
        readonly VisualElement m_ContentContainer;
        protected readonly ScrollView scrollView;
        protected readonly VisualElement dockArea;
        protected readonly ContainerSection beforeSection;
        protected readonly ContainerSection afterSection;
        protected readonly OverlayContainerInsertDropZone beforeDropZone;
        protected readonly OverlayContainerInsertDropZone afterDropZone;

        float m_ScrollOffsetRequestedValue;

        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        public override Layout preferredLayout => isHorizontal ? Layout.HorizontalToolbar : Layout.VerticalToolbar;

        internal bool canAssignScrollOffset => isHorizontal ? HasValidScrollerValues(scrollView.horizontalScroller) : HasValidScrollerValues(scrollView.verticalScroller);

        public float scrollOffset
        {
            get => isHorizontal ? scrollView.scrollOffset.x : scrollView.scrollOffset.y;
            set
            {
                if (canAssignScrollOffset)
                    scrollView.scrollOffset = isHorizontal ? new Vector2(value, 0) : new Vector2(0, value);
                else
                    m_ScrollOffsetRequestedValue = value;
            }
        }

        public ToolbarOverlayContainer()
        {
            scrollView = new ScrollView(ScrollViewMode.Horizontal);
            scrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            hierarchy.Add(scrollView);
            scrollView.RegisterCallback<GeometryChangedEvent>(DelayScrollViewInit);

            AddToClassList(k_ToolbarClassName);

            m_ContentContainer = scrollView.contentContainer;
            CreateDefaultSections(out beforeSection, out afterSection);

            //Force the current direction because scroll view was just created
            if (isHorizontal)
                SetHorizontal();
            else
                SetVertical();

            dockArea = new VisualElement { name = "DockArea", pickingMode = PickingMode.Ignore };
            dockArea.StretchToParentSize();
            Add(dockArea);

            dockArea.Add(beforeDropZone = new OverlayContainerInsertDropZone(this, OverlayContainerSection.BeforeSpacer, OverlayContainerDropZone.Placement.End));
            dockArea.Add(afterDropZone = new OverlayContainerInsertDropZone(this, OverlayContainerSection.AfterSpacer, OverlayContainerDropZone.Placement.End));
            beforeSection.RegisterCallback<GeometryChangedEvent>((evt) => UpdateDockArea());
            afterSection.RegisterCallback<GeometryChangedEvent>((evt) => UpdateDockArea());

            beforeSection.overlayInserted += OnOverlayInserted;
            afterSection.overlayInserted += OnOverlayInserted;

            beforeSection.overlayRemoved += OnOverlayRemoved;
            afterSection.overlayRemoved += OnOverlayRemoved;
        }

        void OnOverlayInserted(Overlay overlay, int index, DockingHint hint)
        {
            Action<bool> handler = (_) => UpdateDynamicPanelOverlayContainerBorderStyle();
            m_OverlayToDisplayCallback.Add(overlay, handler);
            overlay.displayedChanged += handler;

            UpdateDynamicPanelOverlayContainerBorderStyle();
        }

        void OnOverlayRemoved(Overlay overlay, int index)
        {
            if (m_OverlayToDisplayCallback.TryGetValue(overlay, out var handler))
            {
                overlay.displayedChanged -= handler;
                m_OverlayToDisplayCallback.Remove(overlay);
            }

            UpdateDynamicPanelOverlayContainerBorderStyle();
        }

        void UpdateDynamicPanelOverlayContainerBorderStyle()
        {
            if (canvas.dynamicPanelBehavior == OverlayCanvas.DynamicPanelBehavior.None || isHorizontal)
                return;

            var isRight = name == k_RightToolbarName;
            var index = parent.IndexOf(this);
            var dynamicPanelOverlayContainerIndex = isRight ? index - 1 : index + 1;

            if (dynamicPanelOverlayContainerIndex >= parent.childCount || dynamicPanelOverlayContainerIndex < 0)
                return;

            var ve = parent.ElementAt(dynamicPanelOverlayContainerIndex);
            if (ve is DynamicPanelOverlayContainer dynamicPanelOverlayContainer)
                dynamicPanelOverlayContainer.UpdateBorderStyle(this);
        }

        // Flex containers have some weird interactions with scroll views so we manually updated the "empty space" between before and after sections
        void UpdateDockArea()
        {
            var containerRect = layout;
            var beforeSectionRect = beforeSection.layout;
            var afterSectionRect = afterSection.layout;

            dockArea.style.left = isHorizontal ? beforeSectionRect.width : 0;
            dockArea.style.top = !isHorizontal ? beforeSectionRect.height : 0;
            dockArea.style.width = isHorizontal ? Mathf.Max(containerRect.width - beforeSectionRect.width - afterSectionRect.width, 0) : new StyleLength(StyleKeyword.Auto);
            dockArea.style.height = !isHorizontal ? Mathf.Max(containerRect.height - beforeSectionRect.height - afterSectionRect.height, 0) : new StyleLength(StyleKeyword.Auto);
        }

        internal override IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            yield return beforeDropZone;
            yield return afterDropZone;
        }

        void DelayScrollViewInit(GeometryChangedEvent evt)
        {
            scrollView.UnregisterCallback<GeometryChangedEvent>(DelayScrollViewInit);
            scrollView.horizontalScrollerVisibility = scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            if (!Mathf.Approximately(m_ScrollOffsetRequestedValue, 0))
                scrollOffset = m_ScrollOffsetRequestedValue;
        }

        protected override void SetHorizontal()
        {
            base.SetHorizontal();

            if (scrollView != null)
            {
                scrollView.mode = ScrollViewMode.Horizontal;
                scrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                scrollView.style.height = new StyleLength(StyleKeyword.Auto);
            }
        }

        protected override void SetVertical()
        {
            base.SetVertical();

            if (scrollView != null)
            {
                scrollView.mode = ScrollViewMode.Vertical;
                scrollView.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                scrollView.style.width = new StyleLength(StyleKeyword.Auto);
            }
        }

        public override bool IsOverlayLayoutSupported(Layout requested)
        {
            if (isHorizontal)
                return (requested & Layout.HorizontalToolbar) > 0;
            return (requested & Layout.VerticalToolbar) > 0;
        }

        bool HasValidScrollerValues(Scroller scroller)
        {
            return !float.IsNaN(scroller.lowValue) && !float.IsNaN(scroller.highValue);
        }
    }
}
