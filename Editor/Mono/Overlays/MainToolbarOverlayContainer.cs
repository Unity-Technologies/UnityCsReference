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

            scrollView.style.minHeight = Toolbar.ToolbarHeight;

            RegisterCallback<GeometryChangedEvent>(evt => UpdateSectionsWidth());
            m_LeftSection.contentContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdateSectionsWidth());
            m_MiddleSection.contentContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdateSectionsWidth());
            m_RightSection.contentContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdateSectionsWidth());
        }

        // Flex containers have some weird interactions with scroll views so we manually updated the "empty space" between before and after sections
        protected override void UpdateDockArea()
        {
            var containerRect = layout;
            // Sections Left/Right take the full remaining space. Their content only takes what space they need.
            var beforeSectionRect = new Rect(m_LeftSection.layout.position, m_LeftSection.contentContainer.rect.size);
            var afterSectionRect = new Rect(m_RightSection.layout.position + m_RightSection.contentContainer.layout.position, m_RightSection.contentContainer.rect.size);
            var middleSectionRect = m_MiddleSection.layout;

            dockArea.style.left = beforeSectionRect.xMax;
            dockArea.style.width = Mathf.Max(middleSectionRect.x - beforeSectionRect.xMax, 0);
            dockArea.style.top = 0;
            dockArea.style.height = new StyleLength(StyleKeyword.Auto);

            m_DockAreaSecond.style.left = middleSectionRect.xMax;
            m_DockAreaSecond.style.width = Mathf.Max(afterSectionRect.x - middleSectionRect.xMax, 0);
            m_DockAreaSecond.style.top = 0;
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

        void UpdateSectionsWidth()
        {
            var totalSize = canvas.rootVisualElement.rect.width;
            var canvasLeftOffset = layout.x;
            var canvasRightOffset = totalSize - layout.xMax;
            var leftContentSize = m_LeftSection.contentContainer.rect.width;
            var middleContentSize = m_MiddleSection.contentContainer.rect.width;
            var rightContentSize = m_RightSection.contentContainer.rect.width;

            // Calculate the size of the left container
            // Centralize the middle content while accounting for unity logo size
            var sideMinExpectedSize = (totalSize - middleContentSize) * 0.5f;
            var leftMinExpectedSize = sideMinExpectedSize - canvasLeftOffset;
            var rightMinExpectedSize = sideMinExpectedSize - canvasRightOffset;
            var rightOverflow = rightContentSize - rightMinExpectedSize;
            var leftOverflow = leftContentSize - leftMinExpectedSize;

            // If both side have overflow, let the layout system do it's thing
            if (leftOverflow > 0 && rightOverflow > 0)
            {
                m_LeftSection.style.width = StyleKeyword.Null;
                m_RightSection.style.width = StyleKeyword.Null;
            }

            else if (leftOverflow > 0)
            {
                m_LeftSection.style.width = StyleKeyword.Null;
                var remainingSpace = rightMinExpectedSize - leftOverflow;
                m_RightSection.style.width = remainingSpace > rightContentSize ? remainingSpace : StyleKeyword.Null;
            }

            else if (rightOverflow > 0)
            {
                var remainingSpace = leftMinExpectedSize - rightOverflow;
                m_LeftSection.style.width = remainingSpace > leftContentSize ? remainingSpace : StyleKeyword.Null;
                m_RightSection.style.width = StyleKeyword.Null;
            }

            else
            {
                m_LeftSection.style.width = leftMinExpectedSize;
                m_RightSection.style.width = rightMinExpectedSize;
            }

            UpdateDockArea();
        }
    }
}
