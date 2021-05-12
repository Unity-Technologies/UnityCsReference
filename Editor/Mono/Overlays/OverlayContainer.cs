// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class ToolbarOverlayContainer : OverlayContainer
    {
        public new class UxmlFactory : UxmlFactory<ToolbarOverlayContainer, UxmlTraits> {}
        public new class UxmlTraits : OverlayContainer.UxmlTraits {}

        const string k_ToolbarClassName = "overlay-toolbar-area";

        readonly OverlayDropZoneBase m_NoElementDropZone;

        public ToolbarOverlayContainer()
        {
            AddToClassList(k_ToolbarClassName);
            m_NoElementDropZone = new HiddenToolbarDropZone(this) {name = "NoElementToolbarDropZone"};
            Add(m_NoElementDropZone);
        }

        protected override bool InitSpacer()
        {
            if (base.InitSpacer())
            {
                m_NoElementDropZone.style.display = DisplayStyle.Flex;
                beforeSpacerDropZone.style.display = DisplayStyle.None;
                afterSpacerDropZone.style.display = DisplayStyle.None;
                return true;
            }

            return false;
        }

        protected override void OnStateUnlocked()
        {
            base.OnStateUnlocked();
            UpdateDropZones();
        }

        protected override void OnOverlayAdded(Overlay overlay)
        {
            base.OnOverlayAdded(overlay);
            if (!IsOverlayLayoutSupported(overlay.supportedLayouts))
            {
                overlay.collapsed = true;
            }
            overlay.layout = isHorizontal
                ? Layout.HorizontalToolbar
                : Layout.VerticalToolbar;
        }

        protected override void OnOverlayBecomeVisibleInContainer(Overlay overlay)
        {
            InitSpacer();
            base.OnOverlayBecomeVisibleInContainer(overlay);

            UpdateDropZones();
        }

        protected override void OnOverlayBecomeInvisibleInContainer(Overlay overlay)
        {
            InitSpacer();
            base.OnOverlayBecomeInvisibleInContainer(overlay);

            UpdateDropZones();
        }

        void UpdateDropZones()
        {
            if (stateLocked)
                return;

            if (visibleOverlayCount >= 1)
            {
                m_NoElementDropZone.style.display = DisplayStyle.None;
                beforeSpacerDropZone.style.display = DisplayStyle.Flex;
                afterSpacerDropZone.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_NoElementDropZone.style.display = DisplayStyle.Flex;
                beforeSpacerDropZone.style.display = DisplayStyle.None;
                afterSpacerDropZone.style.display = DisplayStyle.None;
            }
        }

        public override bool IsOverlayLayoutSupported(Layout layout)
        {
            if (isHorizontal)
                return (layout & Layout.HorizontalToolbar) > 0;
            else
                return (layout & Layout.VerticalToolbar) > 0;
        }
    }

    class OverlayContainer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<OverlayContainer, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlBoolAttributeDescription m_IsHorizontal = new UxmlBoolAttributeDescription { name = "horizontal", defaultValue = false };
            readonly UxmlStringAttributeDescription m_SupportedLayout = new UxmlStringAttributeDescription {name = "supported-overlay-layout", defaultValue = ""};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var container = ((OverlayContainer)ve);
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

        List<Overlay> m_TopOverlays = new List<Overlay>();
        List<Overlay> m_BottomOverlaysOverlays = new List<Overlay>();
        public List<Overlay> topOverlays => m_TopOverlays;
        public List<Overlay> bottomOverlays => m_BottomOverlaysOverlays;

        public int overlayCount => topOverlays.Count + bottomOverlays.Count;
        public int visibleOverlayCount => m_VisibleInContainer.Count;
        protected OverlayDropZoneBase beforeSpacerDropZone { get; private set; }
        protected OverlayDropZoneBase afterSpacerDropZone { get; private set; }

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

        Layout m_SupportedOverlayLayouts = Layout.Panel; //Used as a flag in this case

        public const string className = "unity-overlay-container";
        public const string spacerClassName = "overlay-container__spacer";
        const string k_HorizontalClassName = className + "-horizontal";
        const string k_VerticalClassName = className + "-vertical";

        bool m_IsHorizontal;
        VisualElement m_Spacer;
        VisualElement m_Canvas;
        bool m_StateLocked;
        readonly HashSet<Overlay> m_VisibleInContainer = new HashSet<Overlay>();

        internal struct Target
        {
            public VisualElement ve;
            public Rect rect;
            public int index;
            public Vector2 position;
        }

        public OverlayContainer()
        {
            AddToClassList(className);
            name = className;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            SetVertical();
        }

        void SetHorizontal()
        {
            EnableInClassList(k_HorizontalClassName, true);
            EnableInClassList(k_VerticalClassName, false);
        }

        void SetVertical()
        {
            EnableInClassList(k_HorizontalClassName, false);
            EnableInClassList(k_VerticalClassName, true);
        }

        public virtual bool IsOverlayLayoutSupported(Layout layout)
        {
            return (m_SupportedOverlayLayouts & layout) > 0;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_Canvas = parent;
            while (m_Canvas != null && !m_Canvas.ClassListContains(OverlayCanvas.ussClassName))
            {
                m_Canvas = m_Canvas.parent;
            }

            InitSpacer();
        }

        public VisualElement spacer
        {
            get
            {
                InitSpacer();
                return m_Spacer;
            }
        }

        public bool stateLocked
        {
            get => m_StateLocked;
            set
            {
                if (m_StateLocked == value)
                    return;

                m_StateLocked = value;
                if (m_StateLocked)
                    OnStateLocked();
                else
                    OnStateUnlocked();
            }
        }

        protected virtual bool InitSpacer()
        {
            if (m_Spacer == null)
            {
                m_Spacer = this.Q(null, spacerClassName);
                if (m_Spacer == null)
                    return false;

                m_Spacer.Add(beforeSpacerDropZone = new OverlayContainerDropZone(this, OverlayContainerDropZone.Placement.Start));
                var dropZoneSpacer = new VisualElement { name = "DropZonesSpacer" };
                m_Spacer.Add(dropZoneSpacer);
                m_Spacer.Add(afterSpacerDropZone = new OverlayContainerDropZone(this, OverlayContainerDropZone.Placement.End));
                return true;
            }

            return false;
        }

        public void RemoveOverlay(Overlay overlay)
        {
            if (!topOverlays.Remove(overlay)
                && !bottomOverlays.Remove(overlay))
                return;

            overlay.rootVisualElement.RemoveFromHierarchy();
            OnOverlayRemoved(overlay);
        }

        public void InsertBefore(Overlay overlay, Overlay targetOverlay)
        {
            var index = topOverlays.IndexOf(targetOverlay);
            if (index >= 0)
            {
                if (topOverlays.Contains(overlay)) return;
                topOverlays.Insert(index, overlay);
                Insert(IndexOf(targetOverlay.rootVisualElement), overlay.rootVisualElement);
                OnOverlayAdded(overlay);
                return;
            }

            index = bottomOverlays.IndexOf(targetOverlay);
            if (index >= 0)
            {
                if (bottomOverlays.Contains(overlay)) return;
                bottomOverlays.Insert(index, overlay);
                Insert(IndexOf(targetOverlay.rootVisualElement), overlay.rootVisualElement);
                OnOverlayAdded(overlay);
            }
        }

        public void AddAfter(Overlay overlay, Overlay targetOverlay)
        {
            var index = topOverlays.IndexOf(targetOverlay);
            if (index >= 0)
            {
                if (topOverlays.Contains(overlay)) return;
                topOverlays.Insert(index + 1, overlay);
                Insert(IndexOf(targetOverlay.rootVisualElement) + 1, overlay.rootVisualElement);
                OnOverlayAdded(overlay);
                return;
            }

            index = bottomOverlays.IndexOf(targetOverlay);
            if (index >= 0)
            {
                if (bottomOverlays.Contains(overlay)) return;
                bottomOverlays.Insert(index + 1, overlay);
                Insert(IndexOf(targetOverlay.rootVisualElement) + 1, overlay.rootVisualElement);
                OnOverlayAdded(overlay);
            }
        }

        public void AddToTop(Overlay overlay)
        {
            if (topOverlays.Contains(overlay)) return;
            topOverlays.Add(overlay);
            Insert(IndexOf(spacer), overlay.rootVisualElement);
            OnOverlayAdded(overlay);
        }

        public void InsertBottom(Overlay overlay)
        {
            if (bottomOverlays.Contains(overlay)) return;
            bottomOverlays.Insert(0, overlay);
            var spacerIndex = IndexOf(spacer);
            if (spacerIndex == childCount - 1)
                Add(overlay.rootVisualElement);
            else
                Insert(spacerIndex + 1, overlay.rootVisualElement);

            OnOverlayAdded(overlay);
        }

        public void AddToBottom(Overlay overlay)
        {
            if (bottomOverlays.Contains(overlay)) return;
            bottomOverlays.Add(overlay);
            Add(overlay.rootVisualElement);
            OnOverlayAdded(overlay);
        }

        protected virtual void OnOverlayAdded(Overlay overlay)
        {
            overlay.container = this;
            UpdateIsVisibleInContainer(overlay);
        }

        protected virtual void OnOverlayRemoved(Overlay overlay)
        {
            overlay.container = null;
            if (m_VisibleInContainer.Remove(overlay))
                OnOverlayBecomeInvisibleInContainer(overlay);
        }

        internal Overlay FirstTopOverlay()
        {
            return FirstValidOverlay(topOverlays);
        }

        internal Overlay LastTopOverlay()
        {
            return LastValidOverlay(topOverlays);
        }

        internal Overlay FirstBottomOverlay()
        {
            return FirstValidOverlay(bottomOverlays);
        }

        internal Overlay LastBottomOverlay()
        {
            return LastValidOverlay(bottomOverlays);
        }

        Overlay FirstValidOverlay(List<Overlay> overlays)
        {
            for (int i = 0; i < overlays.Count; ++i)
            {
                if (IsOverlayVisibleInContainer(overlays[i]))
                    return overlays[i];
            }
            return null;
        }

        Overlay LastValidOverlay(List<Overlay> overlays)
        {
            for (int i = overlays.Count - 1; i >= 0; --i)
            {
                if (IsOverlayVisibleInContainer(overlays[i]))
                    return overlays[i];
            }
            return null;
        }

        internal bool IsOverlayVisibleInContainer(Overlay overlay)
        {
            return !overlay.floating && overlay.displayed;
        }

        protected virtual void OnOverlayBecomeVisibleInContainer(Overlay overlay) {}
        protected virtual void OnOverlayBecomeInvisibleInContainer(Overlay overlay) {}
        protected virtual void OnStateLocked() {}
        protected virtual void OnStateUnlocked() {}

        internal void UpdateIsVisibleInContainer(Overlay overlay)
        {
            if (overlay.displayed && !overlay.floating)
            {
                if (m_VisibleInContainer.Add(overlay))
                    OnOverlayBecomeVisibleInContainer(overlay);
            }
            else
            {
                if (m_VisibleInContainer.Remove(overlay))
                    OnOverlayBecomeInvisibleInContainer(overlay);
            }
        }
    }
}
