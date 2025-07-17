// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class MainToolbarOverlayContainer : ToolbarOverlayContainer
    {
        [Serializable]
        public new class UxmlSerializedData : OverlayContainer.UxmlSerializedData
        {
            public override object CreateInstance() => new MainToolbarOverlayContainer();
        }

        public override Layout preferredLayout => Layout.HorizontalToolbar;


        readonly OverlayContainerInsertDropZone m_MiddleDropZoneL;
        readonly OverlayContainerInsertDropZone m_MiddleDropZoneR;

        readonly ContainerSection m_LeftSection;
        readonly ContainerSection m_RightSection;
        readonly ContainerSection m_MiddleSection;

        // MainToolbarOverlayContainer has two DockAreas, one between L and M container, and one between M and R containers
        readonly VisualElement m_DockAreaSecond;

        const string k_MiddleClassName = className + "__middle-container";
        const string k_SpacingContainerClassName = className + "__spacing-container";
        const string k_ContentClassName = className + "__content";


        public MainToolbarOverlayContainer()
        {
            m_LeftSection = beforeSection;
            m_RightSection = afterSection;
            m_MiddleSection = CreateSection();
            Insert(IndexOf(m_RightSection), m_MiddleSection); //Move the middle section between left and right
            m_MiddleSection.AddToClassList(k_MiddleClassName);
            m_MiddleSection.AddToClassList(k_SpacingContainerClassName);
            m_MiddleSection.contentContainer.AddToClassList(k_ContentClassName);

            Add(dockArea);

            m_DockAreaSecond = new VisualElement { name = "DockArea", pickingMode = PickingMode.Ignore };
            m_DockAreaSecond.StretchToParentSize();
            Add(m_DockAreaSecond);

            dockArea.Add(beforeDropZone);
            dockArea.Add(m_MiddleDropZoneL = new OverlayContainerInsertDropZone(this, OverlayContainerSection.Middle, OverlayContainerDropZone.Placement.Start));
            m_DockAreaSecond.Add(m_MiddleDropZoneR = new OverlayContainerInsertDropZone(this, OverlayContainerSection.Middle, OverlayContainerDropZone.Placement.End));
            m_DockAreaSecond.Add(afterDropZone);
            m_MiddleSection.RegisterCallback<GeometryChangedEvent>((evt) => UpdateDockArea());

            scrollView.style.minHeight = Toolbar.ToolbarHeight;
        }

        // Flex containers have some weird interactions with scroll views so we manually updated the "empty space" between before and after sections
        void UpdateDockArea()
        {
            var containerRect = layout;
            var beforeSectionRect = m_LeftSection.layout;
            var afterSectionRect = m_RightSection.layout;
            var middleSectionRect = m_MiddleSection.layout;

            float dockAreaW = Mathf.Max(containerRect.width - beforeSectionRect.width - middleSectionRect.width - afterSectionRect.width, 0) / 2f;

            dockArea.style.left = beforeSectionRect.width;
            dockArea.style.top = 0;
            dockArea.style.width = dockAreaW;
            dockArea.style.maxWidth = dockAreaW;
            dockArea.style.height = new StyleLength(StyleKeyword.Auto);

            m_DockAreaSecond.style.left = middleSectionRect.xMax;
            m_DockAreaSecond.style.top = 0;
            m_DockAreaSecond.style.width = dockAreaW;
            m_DockAreaSecond.style.maxWidth = dockAreaW;
            m_DockAreaSecond.style.height = new StyleLength(StyleKeyword.Auto);
        }

        internal override IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            yield return beforeDropZone;
            yield return m_MiddleDropZoneL;
            yield return m_MiddleDropZoneR;
            yield return afterDropZone;
        }

        protected override void SetVertical() { }

        public override bool IsOverlayLayoutSupported(Layout requested)
        {
            return (requested & Layout.HorizontalToolbar) > 0;
        }
    }
}
