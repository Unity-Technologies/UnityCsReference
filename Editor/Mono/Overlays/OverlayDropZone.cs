// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class OverlayDestinationMarker : VisualElement
    {
        public const string className = "unity-overlay-destination-marker";
        const string k_LineClassName = "unity-overlay-destination-marker__line";

        readonly List<string> m_DropZoneClasses = new List<string>();
        OverlayDropZoneBase m_Target;

        public OverlayDestinationMarker()
        {
            style.width = 0;
            style.height = 0;
            style.right = float.NaN;
            style.bottom = float.NaN;
            AddToClassList(className);

            var line = new VisualElement();
            line.AddToClassList(k_LineClassName);
            Add(line);
        }

        public void SetTarget(OverlayDropZoneBase target)
        {
            if (m_Target == target)
                return;

            if (m_Target != null)
            {
                foreach (var c in m_DropZoneClasses)
                {
                    RemoveFromClassList(c);
                }
            }

            m_Target = target;
            m_DropZoneClasses.Clear();

            if (m_Target != null)
            {
                m_Target.PopulateDestMarkerClassList(m_DropZoneClasses);

                SetBounds(m_Target.GetTargetWorldBounds());
                style.display = DisplayStyle.Flex;
                foreach (var c in m_DropZoneClasses)
                {
                    AddToClassList(c);
                }
            }
            else
            {
                style.display = DisplayStyle.None;
            }
        }

        void SetBounds(Rect worldBound)
        {
            var rect = parent.WorldToLocal(worldBound);
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }
    }

    class OverlayContainerDropZone : OverlayDropZoneBase
    {
        const string k_InToolbarClassName = OverlayDestinationMarker.className + "--in-toolbar";

        public enum Placement
        {
            Start,
            End
        }

        readonly Placement m_Placement;
        readonly OverlayContainer m_Container;

        public OverlayContainerDropZone(OverlayContainer container, Placement placement)
        {
            m_Placement = placement;
            m_Container = container;
        }

        protected override void OnDropZoneActivated(Overlay draggedOverlay)
        {
            base.OnDropZoneActivated(draggedOverlay);

            //Disable if the drop zone is linked to the overlay being dragged currently
            if (m_Placement == Placement.Start && m_Container.LastTopOverlay() == draggedOverlay
                || m_Placement == Placement.End && m_Container.FirstBottomOverlay() == draggedOverlay)
            {
                SetVisualMode(VisualMode.Disabled);
                return;
            }

            if (m_Container is ToolbarOverlayContainer)
            {
                //Disable if drop zone is reduced to 0 size. Toolbar have container drop zone with flex-grow = 1
                if (Mathf.Approximately(rect.width, 0) || Mathf.Approximately(rect.height, 0))
                {
                    SetVisualMode(VisualMode.Disabled);
                    return;
                }

                switch (m_Placement)
                {
                    case Placement.Start:
                        SetVisualMode(VisualMode.AddToStart);
                        break;
                    case Placement.End:
                        SetVisualMode(VisualMode.AddToEnd);
                        break;
                    default:
                        SetVisualMode(VisualMode.Disabled);
                        break;
                }
            }
            else
            {
                switch (m_Placement)
                {
                    case Placement.Start:
                        SetVisualMode(OverlayVisibleInContainerCount(m_Container, m_Container.topOverlays) == 0
                            ? VisualMode.AddToStart
                            : VisualMode.Insert);
                        break;
                    case Placement.End:
                        SetVisualMode(OverlayVisibleInContainerCount(m_Container, m_Container.bottomOverlays) == 0
                            ? VisualMode.AddToEnd
                            : VisualMode.Insert);
                        break;
                    default:
                        SetVisualMode(VisualMode.Insert);
                        break;
                }
            }
        }

        static int OverlayVisibleInContainerCount(OverlayContainer container, List<Overlay> overlays)
        {
            int count = 0;
            foreach (var overlay in overlays)
            {
                if (container.IsOverlayVisibleInContainer(overlay))
                    ++count;
            }
            return count;
        }

        public override void DropOverlay(Overlay overlay)
        {
            switch (m_Placement)
            {
                case Placement.Start:
                {
                    var target = m_Container.LastTopOverlay();
                    if (target != null)
                        m_Container.AddAfter(overlay, target);
                    else
                        m_Container.AddToTop(overlay);

                    break;
                }

                case Placement.End:
                {
                    var target = m_Container.FirstBottomOverlay();
                    if (target != null)
                        m_Container.InsertBefore(overlay, target);
                    else
                        m_Container.AddToBottom(overlay);

                    break;
                }
            }

            overlay.floating = false;
        }

        public override void PopulateDestMarkerClassList(IList<string> classes)
        {
            base.PopulateDestMarkerClassList(classes);

            if (m_Container is ToolbarOverlayContainer)
                classes.Add(k_InToolbarClassName);

            GetContainerStylingForDestMarker(m_Container, ref classes);
        }
    }

    class OverlayDropZone : OverlayDropZoneBase
    {
        public enum Placement
        {
            Before,
            After
        }

        readonly Overlay m_TargetOverlay;
        readonly Placement m_Placement;

        public OverlayDropZone(Overlay target, Placement placement)
        {
            m_TargetOverlay = target;
            m_Placement = placement;
        }

        protected override void OnDropZoneActivated(Overlay draggedOverlay)
        {
            base.OnDropZoneActivated(draggedOverlay);

            if (m_TargetOverlay.floating
                || m_TargetOverlay == draggedOverlay)
            {
                SetVisualMode(VisualMode.Disabled);
            }
            else
            {
                switch (m_Placement)
                {
                    case Placement.Before:
                        if (m_TargetOverlay.container.FirstTopOverlay() != m_TargetOverlay)
                            goto default;

                        SetVisualMode(VisualMode.AddToStart);
                        break;

                    case Placement.After:
                        if (m_TargetOverlay.container.LastBottomOverlay() != m_TargetOverlay)
                            goto default;

                        SetVisualMode(VisualMode.AddToEnd);
                        break;

                    default:
                        SetVisualMode(VisualMode.Insert);
                        break;
                }
            }
        }

        public override bool CanAcceptTarget(Overlay overlay)
        {
            return m_TargetOverlay != overlay;
        }

        public override void DropOverlay(Overlay overlay)
        {
            var container = m_TargetOverlay.container;

            switch (m_Placement)
            {
                case Placement.Before:
                    container.InsertBefore(overlay, m_TargetOverlay);
                    break;

                case Placement.After:
                    container.AddAfter(overlay, m_TargetOverlay);
                    break;
            }

            overlay.floating = false;
        }

        public override void PopulateDestMarkerClassList(IList<string> classes)
        {
            base.PopulateDestMarkerClassList(classes);

            if (m_TargetOverlay.container != null)
                GetContainerStylingForDestMarker(m_TargetOverlay.container, ref classes);
        }
    }

    sealed class HiddenToolbarDropZone : OverlayContainerDropZone
    {
        const string k_NoElementClassName = className + "-no-element";
        const string k_DestinationClassName = OverlayDestinationMarker.className + "--no-element";

        public HiddenToolbarDropZone(OverlayContainer container) : base(container, Placement.Start)
        {
            AddToClassList(k_NoElementClassName);

            priority = 1;
        }

        public override void PopulateDestMarkerClassList(IList<string> classes)
        {
            base.PopulateDestMarkerClassList(classes);

            classes.Add(k_DestinationClassName);
        }
    }

    abstract class OverlayDropZoneBase : VisualElement
    {
        protected enum VisualMode
        {
            Insert,
            AddToStart,
            AddToEnd,
            Custom,
            Disabled,
        }

        public const string className = "unity-overlay-drop-zone";
        const string k_InsertMode = className + "--insert";
        const string k_AddStartMode = className + "--add-start";
        const string k_AddEndMode = className + "--add-end";
        const string k_DisabledMode = className + "--disabled";
        const string k_DropAreaClassName = className + "__target";
        const string k_HorizontalClassName = OverlayDestinationMarker.className + "--container-horizontal";
        const string k_VerticalClassName = OverlayDestinationMarker.className + "--container-vertical";

        public const string dropAreaName = "DropArea";
        const string k_VisualBoundsClassName = k_DropAreaClassName + "__visual-bounds";

        readonly VisualElement m_DropArea;
        readonly VisualElement m_VisualBounds;

        protected VisualMode visualMode { get; private set; }

        public virtual bool CanAcceptTarget(Overlay overlay) { return true; }
        public int priority { get; set; }
        public abstract void DropOverlay(Overlay overlay);

        public virtual void PopulateDestMarkerClassList(IList<string> classes)
        {
            switch (visualMode)
            {
                case VisualMode.AddToStart:
                    classes.Add(OverlayDestinationMarker.className + "--mode-add-to-start");
                    break;

                case VisualMode.AddToEnd:
                    classes.Add(OverlayDestinationMarker.className + "--mode-add-to-end");
                    break;

                case VisualMode.Insert:
                    classes.Add(OverlayDestinationMarker.className + "--mode-insert");
                    break;
            }
        }

        protected void SetVisualMode(VisualMode mode)
        {
            visualMode = mode;
            EnableInClassList(k_InsertMode, mode == VisualMode.Insert);
            EnableInClassList(k_AddEndMode, mode == VisualMode.AddToEnd);
            EnableInClassList(k_AddStartMode, mode == VisualMode.AddToStart);
            EnableInClassList(k_DisabledMode, mode == VisualMode.Disabled);
        }

        public Rect GetTargetWorldBounds()
        {
            return m_VisualBounds.worldBound;
        }

        protected OverlayDropZoneBase()
        {
            AddToClassList(className);

            m_DropArea = new VisualElement { name = dropAreaName };
            m_DropArea.AddToClassList(k_DropAreaClassName);


            Add(m_DropArea);

            m_VisualBounds = new VisualElement();
            m_VisualBounds.AddToClassList(k_VisualBoundsClassName);
            m_DropArea.Add(m_VisualBounds);

            pickingMode = PickingMode.Ignore;
            m_DropArea.pickingMode = PickingMode.Ignore;
            m_VisualBounds.pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            OverlayDragger.dragStarted += OnDragStarted;
            OverlayDragger.dragEnded += OnDragEnded;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            OverlayDragger.dragStarted -= OnDragStarted;
            OverlayDragger.dragEnded -= OnDragEnded;
        }

        void OnDragStarted(Overlay overlay)
        {
            //TODO check if in same canvas? or we support dragging to different canvas
            m_DropArea.pickingMode = PickingMode.Position;
            OnDropZoneActivated(overlay);
        }

        void OnDragEnded(Overlay overlay)
        {
            m_DropArea.pickingMode = PickingMode.Ignore;
            OnDropZoneDeactivated(overlay);
        }

        protected void GetContainerStylingForDestMarker(OverlayContainer container, ref IList<string> classes)
        {
            classes.Add(container.isHorizontal ? k_HorizontalClassName : k_VerticalClassName);

            switch (container.computedStyle.alignItems)
            {
                case Align.FlexStart:
                    classes.Add(OverlayDestinationMarker.className + "--align-start");
                    break;
                case Align.FlexEnd:
                    classes.Add(OverlayDestinationMarker.className + "--align-end");
                    break;
                case Align.Center:
                    classes.Add(OverlayDestinationMarker.className + "--align-center");
                    break;
            }

            if (container is ToolbarOverlayContainer)
                classes.Add(OverlayDestinationMarker.className + "--in-toolbar");
        }

        protected virtual void OnDropZoneActivated(Overlay draggedOverlay) {}
        protected virtual void OnDropZoneDeactivated(Overlay draggedOverlay) {}
    }
}
