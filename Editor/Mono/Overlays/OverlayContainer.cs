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
        public const string spacerClassName = "overlay-container__spacer";
        const string k_HorizontalClassName = className + "-horizontal";
        const string k_VerticalClassName = className + "-vertical";
        public static readonly Overlay spacerMarker = null;

        readonly List<Overlay> m_BeforeOverlays = new List<Overlay>();
        readonly List<Overlay> m_AfterOverlays = new List<Overlay>();
        readonly VisualElement m_Spacer;

        // This is set by querying the stylesheet for 'vertical' and 'horizontal'
        Layout m_SupportedOverlayLayouts = 0; //Used as a flag in this case
        bool m_IsHorizontal;

        public OverlayCanvas canvas { get; internal set; }

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

        protected VisualElement spacer => m_Spacer;
        public bool isSpacerVisible => !Mathf.Approximately(spacer.rect.width, 0) && !Mathf.Approximately(spacer.rect.height, 0);

        public OverlayContainer()
        {
            AddToClassList(className);
            name = className;

            Add(m_Spacer = new VisualElement());
            m_Spacer.AddToClassList(spacerClassName);

            m_Spacer.Add(new OverlayContainerDropZone(this, OverlayContainerDropZone.Placement.Start));
            var dropZoneSpacer = new VisualElement { name = "DropZonesSpacer" };
            m_Spacer.Add(dropZoneSpacer);
            m_Spacer.Add(new OverlayContainerDropZone(this, OverlayContainerDropZone.Placement.End));

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

            int realIndex = -1;

            //Insert relative to another element in case other visual elements are added to hierarchy
            if (index < list.Count)
            {
                realIndex = IndexOf(list[index].rootVisualElement);

                // Section after spacer is listed from bottom to spacer instead of spacer to bottom.
                // So before an overlay after the spacer is actually after the overlay element in the container hierarchy.
                if (section == OverlayContainerSection.AfterSpacer)
                    ++realIndex;

            }

            if (realIndex < 0)
            {
                switch (section)
                {
                    case OverlayContainerSection.BeforeSpacer: realIndex = IndexOf(m_Spacer); break;
                    case OverlayContainerSection.AfterSpacer: realIndex = IndexOf(m_Spacer) + 1; break;
                }
            }


            Insert(realIndex, overlay.rootVisualElement);
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

        public bool HasVisibleOverlays()
        {
            if (GetFirstVisible(OverlayContainerSection.BeforeSpacer) != null)
                return true;

            if (GetFirstVisible(OverlayContainerSection.AfterSpacer) != null)
                return true;

            return false;
        }

        public int GetSectionCount(OverlayContainerSection section)
        {
            return GetSectionInternal(section).Count;
        }

        public ReadOnlyCollection<Overlay> GetSection(OverlayContainerSection section)
        {
            return GetSectionInternal(section).AsReadOnly();
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

        public virtual bool IsOverlayLayoutSupported(Layout requested)
        {
            return (m_SupportedOverlayLayouts & requested) > 0;
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

        readonly OverlayDropZoneBase m_NoElementDropZone;
        readonly VisualElement m_ContentContainer;

        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        public override Layout preferredLayout => isHorizontal ? Layout.HorizontalToolbar : Layout.VerticalToolbar;

        public ToolbarOverlayContainer()
        {
            AddToClassList(k_ToolbarClassName);
            m_NoElementDropZone = new HiddenToolbarDropZone(this) { name = "NoElementToolbarDropZone" };
            hierarchy.Add(m_NoElementDropZone);

            hierarchy.Add(m_ContentContainer = new VisualElement());
            m_ContentContainer.style.flexGrow = 1;
            m_ContentContainer.style.flexDirection = isHorizontal ? FlexDirection.Row : FlexDirection.Column;
            m_ContentContainer.pickingMode = PickingMode.Ignore;
            m_ContentContainer.Add(spacer);
        }

        protected override void SetHorizontal()
        {
            base.SetHorizontal();

            if (contentContainer != null)
                contentContainer.style.flexDirection = FlexDirection.Row;
        }

        protected override void SetVertical()
        {
            base.SetVertical();

            if (contentContainer != null)
                contentContainer.style.flexDirection = FlexDirection.Column;
        }

        public override bool IsOverlayLayoutSupported(Layout requested)
        {
            if (isHorizontal)
                return (requested & Layout.HorizontalToolbar) > 0;
            return (requested & Layout.VerticalToolbar) > 0;
        }
    }
}
