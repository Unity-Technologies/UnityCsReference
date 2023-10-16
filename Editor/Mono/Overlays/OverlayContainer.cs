// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    enum OverlayContainerSection
    {
        BeforeSpacer,
        AfterSpacer
    }

    class OverlayContainer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<OverlayContainer, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlBoolAttributeDescription m_IsHorizontal = new UxmlBoolAttributeDescription
                {name = "horizontal", defaultValue = false};

            readonly UxmlStringAttributeDescription m_SupportedLayout = new UxmlStringAttributeDescription
                {name = "supported-overlay-layout", defaultValue = ""};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var container = ((OverlayContainer) ve);
                container.isHorizontal = m_IsHorizontal.GetValueFromBag(bag, cc);

                container.m_SupportedOverlayLayouts = Layout.Panel;
                foreach (var layout in m_SupportedLayout.GetValueFromBag(bag, cc).Split(' '))
                {
                    switch (layout.ToLower())
                    {
                        case "horizontal":
                            container.m_SupportedOverlayLayouts |= Layout.HorizontalToolbar;
                            break;

                        case "vertical":
                            container.m_SupportedOverlayLayouts |= Layout.VerticalToolbar;
                            break;
                    }
                }
            }
        }

        public const string className = "unity-overlay-container";
        const string k_HorizontalClassName = className + "-horizontal";
        const string k_VerticalClassName = className + "-vertical";
        const string k_ContentClassName = className + "__content";
        const string k_BeforeClassName = className + "__before-spacer-container";
        const string k_AfterClassName = className + "__after-spacer-container";
        const string k_SpacingContainerClassName = className + "__spacing-container";
        public static readonly Overlay spacerMarker = null;

        readonly List<Overlay> m_BeforeOverlays = new List<Overlay>();
        readonly List<Overlay> m_AfterOverlays = new List<Overlay>();
        readonly VisualElement m_BeforeSectionContent;
        readonly VisualElement m_AfterSectionContent;

        // This is set by querying the stylesheet for 'vertical' and 'horizontal'
        Layout m_SupportedOverlayLayouts = 0; //Used as a flag in this case
        bool m_IsHorizontal;

        public OverlayCanvas canvas { get; internal set; }

        protected readonly VisualElement beforeSectionContainer;
        protected readonly VisualElement afterSectionContainer;

        public int overlayCount => m_BeforeOverlays.Count + m_AfterOverlays.Count;
        public virtual Layout preferredLayout => Layout.Panel;

        public bool isHorizontal
        {
            get => m_IsHorizontal;
            set
            {
                if (m_IsHorizontal == value)
                    return;

                m_IsHorizontal = value;
                if (m_IsHorizontal)
                    SetHorizontal();
                else
                    SetVertical();
            }
        }

        public float spacerSize => isHorizontal
                ? layout.width - (beforeSectionContainer.layout.width + afterSectionContainer.layout.width)
                : layout.height - (beforeSectionContainer.layout.height + afterSectionContainer.layout.height);

        public bool isSpacerVisible => !Mathf.Approximately(spacerSize, 0);

        public OverlayContainer()
        {
            AddToClassList(className);
            name = className;

            beforeSectionContainer = new VisualElement();
            Add(beforeSectionContainer);
            beforeSectionContainer.Add(m_BeforeSectionContent = new VisualElement());
            beforeSectionContainer.AddToClassList(k_BeforeClassName);
            beforeSectionContainer.AddToClassList(k_SpacingContainerClassName);
            m_BeforeSectionContent.AddToClassList(k_ContentClassName);

            afterSectionContainer = new VisualElement();
            Add(afterSectionContainer);
            afterSectionContainer.Add(m_AfterSectionContent = new VisualElement());
            afterSectionContainer.AddToClassList(k_AfterClassName);
            afterSectionContainer.AddToClassList(k_SpacingContainerClassName);
            m_AfterSectionContent.AddToClassList(k_ContentClassName);

            SetVertical();
        }

        protected virtual void SetHorizontal()
        {
            EnableInClassList(k_HorizontalClassName, true);
            EnableInClassList(k_VerticalClassName, false);
        }

        protected virtual void SetVertical()
        {
            EnableInClassList(k_HorizontalClassName, false);
            EnableInClassList(k_VerticalClassName, true);
        }

        public bool ContainsOverlay(Overlay overlay, OverlayContainerSection section)
        {
            return GetSectionInternal(section).Contains(overlay);
        }

        public bool ContainsOverlay(Overlay overlay)
        {
            return ContainsOverlay(overlay, OverlayContainerSection.BeforeSpacer)
                   || ContainsOverlay(overlay, OverlayContainerSection.AfterSpacer);
        }

        public void InsertOverlay(Overlay overlay, OverlayContainerSection section, int index)
        {
            if (overlay == null)
                return;

            var list = GetSectionInternal(section);
            var element = GetSectionElement(section);
            int realIndex = -1;

            //Insert relative to another element in case other visual elements are added to hierarchy
            if (index < list.Count)
            {
                realIndex = element.IndexOf(list[index].rootVisualElement);
            }
            else if (index == list.Count)
            {
                realIndex = element.childCount;
            }

            realIndex = Mathf.Max(realIndex, 0);

            element.Insert(realIndex, overlay.rootVisualElement);
            list.Insert(index, overlay);
        }

        public bool RemoveOverlay(Overlay overlay)
        {
            if (overlay == spacerMarker)
                return false;

            bool found = m_BeforeOverlays.Remove(overlay);
            found |= m_AfterOverlays.Remove(overlay);
            if (found)
               overlay.rootVisualElement.RemoveFromHierarchy();

            return found;
        }

        public bool GetOverlayIndex(Overlay overlay, out OverlayContainerSection section, out int index)
        {
            index = m_BeforeOverlays.IndexOf(overlay);
            if (index >= 0)
            {
                section = OverlayContainerSection.BeforeSpacer;
                return true;
            }

            index = m_AfterOverlays.IndexOf(overlay);
            if (index >= 0)
            {
                section = OverlayContainerSection.AfterSpacer;
                return true;
            }

            section = (OverlayContainerSection)(-1);
            index = -1;
            return false;
        }

        public bool HasVisibleOverlays(OverlayContainerSection section)
        {
            return GetFirstVisible(section) != null;
        }

        public bool HasVisibleOverlays()
        {
            return HasVisibleOverlays(OverlayContainerSection.BeforeSpacer) || HasVisibleOverlays(OverlayContainerSection.AfterSpacer);
        }

        public int GetSectionCount(OverlayContainerSection section)
        {
            return GetSectionInternal(section).Count;
        }

        public ReadOnlyCollection<Overlay> GetSection(OverlayContainerSection section)
        {
            return GetSectionInternal(section).AsReadOnly();
        }

        public VisualElement GetSectionElement(OverlayContainerSection section)
        {
            switch (section)
            {
                case OverlayContainerSection.BeforeSpacer: return m_BeforeSectionContent;
                case OverlayContainerSection.AfterSpacer: return m_AfterSectionContent;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        List<Overlay> GetSectionInternal(OverlayContainerSection section)
        {
            switch (section)
            {
                case OverlayContainerSection.BeforeSpacer: return m_BeforeOverlays;
                case OverlayContainerSection.AfterSpacer: return m_AfterOverlays;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public Overlay GetFirstVisible(OverlayContainerSection section)
        {
            List<Overlay> overlays = GetSectionInternal(section);
            foreach (var overlay in overlays)
            {
                if (overlay != null && overlay.displayed)
                    return overlay;
            }

            return null;
        }

        public Overlay GetLastVisible(OverlayContainerSection section)
        {
            List<Overlay> overlays = GetSectionInternal(section);
            for (int i = overlays.Count - 1; i >= 0; --i)
            {
                var overlay = overlays[i];
                if (overlay != null && overlay.displayed)
                    return overlay;
            }

            return null;
        }

        public Overlay GetOverlay(OverlayContainerSection section, int index)
        {
            return GetSectionInternal(section)[index];
        }

        public virtual bool IsOverlayLayoutSupported(Layout requested)
        {
            return (m_SupportedOverlayLayouts & requested) > 0;
        }

        internal virtual IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            return new OverlayDropZoneBase[0];
        }
    }

    class FloatingOverlayContainer : OverlayContainer
    {
        public FloatingOverlayContainer()
        {
            this.StretchToParentSize();
        }

        public override bool IsOverlayLayoutSupported(Layout requested)
        {
            return true;
        }
    }

    class ToolbarOverlayContainer : OverlayContainer
    {
        public new class UxmlFactory : UxmlFactory<ToolbarOverlayContainer, UxmlTraits> { }
        public new class UxmlTraits : OverlayContainer.UxmlTraits { }

        const string k_ToolbarClassName = "overlay-toolbar-area";

        readonly VisualElement m_ContentContainer;
        readonly ScrollView m_ScrollView;
        readonly VisualElement m_DockArea;
        readonly OverlayContainerInsertDropZone m_BeforeDropZone;
        readonly OverlayContainerInsertDropZone m_AfterDropZone;

        float m_ScrollOffsetRequestedValue;

        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        public override Layout preferredLayout => isHorizontal ? Layout.HorizontalToolbar : Layout.VerticalToolbar;

        internal bool canAssignScrollOffset => isHorizontal ? HasValidScrollerValues(m_ScrollView.horizontalScroller) : HasValidScrollerValues(m_ScrollView.verticalScroller);

        public float scrollOffset
        {
            get => isHorizontal ? m_ScrollView.scrollOffset.x : m_ScrollView.scrollOffset.y;
            set
            {
                if (canAssignScrollOffset)
                    m_ScrollView.scrollOffset = isHorizontal ? new Vector2(value, 0) : new Vector2(0, value);
                else
                    m_ScrollOffsetRequestedValue = value;
            }
        }

        public ToolbarOverlayContainer()
        {
            m_ScrollView = new ScrollView(ScrollViewMode.Horizontal);
            m_ScrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            hierarchy.Add(m_ScrollView);
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(DelayScrollViewInit);

            AddToClassList(k_ToolbarClassName);

            m_ContentContainer = m_ScrollView.contentContainer;
            Add(beforeSectionContainer);
            Add(afterSectionContainer);

            //Force the current direction because scroll view was just created
            if (isHorizontal)
                SetHorizontal();
            else
                SetVertical();

            m_DockArea = new VisualElement { name = "DockArea", pickingMode = PickingMode.Ignore };
            m_DockArea.StretchToParentSize();
            Add(m_DockArea);

            m_DockArea.Add(m_BeforeDropZone = new OverlayContainerInsertDropZone(this, OverlayContainerDropZone.Placement.Start));
            m_DockArea.Add(m_AfterDropZone = new OverlayContainerInsertDropZone(this, OverlayContainerDropZone.Placement.End));
            beforeSectionContainer.RegisterCallback<GeometryChangedEvent>((evt) => UpdateDockArea());
            afterSectionContainer.RegisterCallback<GeometryChangedEvent>((evt) => UpdateDockArea());
        }

        // Flex containers have some weird interactions with scroll views so we manually updated the "empty space" between before and after sections
        void UpdateDockArea()
        {
            var containerRect = layout;
            var beforeSectionRect = beforeSectionContainer.layout;
            var afterSectionRect = afterSectionContainer.layout;

            m_DockArea.style.left = isHorizontal ? beforeSectionRect.width : 0;
            m_DockArea.style.top = !isHorizontal ? beforeSectionRect.height : 0;
            m_DockArea.style.width = isHorizontal ? Mathf.Max(containerRect.width - beforeSectionRect.width - afterSectionRect.width, 0) : new StyleLength(StyleKeyword.Auto);
            m_DockArea.style.height = !isHorizontal ? Mathf.Max(containerRect.height - beforeSectionRect.height - afterSectionRect.height, 0) : new StyleLength(StyleKeyword.Auto);
        }

        internal override IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            yield return m_BeforeDropZone;
            yield return m_AfterDropZone;
        }

        void DelayScrollViewInit(GeometryChangedEvent evt)
        {
            m_ScrollView.UnregisterCallback<GeometryChangedEvent>(DelayScrollViewInit);
            m_ScrollView.horizontalScrollerVisibility = m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            if (!Mathf.Approximately(m_ScrollOffsetRequestedValue, 0))
                scrollOffset = m_ScrollOffsetRequestedValue;
        }

        protected override void SetHorizontal()
        {
            base.SetHorizontal();

            if (m_ScrollView != null)
            {
                m_ScrollView.mode = ScrollViewMode.Horizontal;
                m_ScrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                m_ScrollView.style.height = new StyleLength(StyleKeyword.Auto);
            }
        }

        protected override void SetVertical()
        {
            base.SetVertical();

            if (m_ScrollView != null)
            {
                m_ScrollView.mode = ScrollViewMode.Vertical;
                m_ScrollView.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                m_ScrollView.style.width = new StyleLength(StyleKeyword.Auto);
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
