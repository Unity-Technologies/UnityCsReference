// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderPlacementIndicator : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-placement-indicator";

        const int k_IndicatorSize = 4;
        const int k_IndicatorHalfSize = 2;
        const int k_DistanceFromElementEdge = 10;

        public VisualElement parentElement { get; private set; }
        public int indexWithinParent { get; private set; }
        public VisualElement documentRootElement { get; set; }

        public new class UxmlFactory : UxmlFactory<BuilderPlacementIndicator, UxmlTraits> {}

        public BuilderPlacementIndicator()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderPlacementIndicator.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);
        }

        public void Activate(VisualElement hierarchyParentElement, int hierarchyIndex)
        {
            Reset();

            if (hierarchyParentElement.childCount == 0 || hierarchyParentElement == documentRootElement)
                return;

            var mouseOverElement = hierarchyIndex > -1 && hierarchyIndex < hierarchyParentElement.childCount - 1
                ? hierarchyParentElement.ElementAt(hierarchyIndex)
                : hierarchyParentElement.ElementAt(hierarchyParentElement.childCount - 1);

            var mouseOverElementCanvasRect = BuilderTracker.GetRelativeRectFromTargetElement(mouseOverElement, this.hierarchy.parent);

            var shouldGoLast = hierarchyIndex >= hierarchyParentElement.childCount;
            var isRow =
                hierarchyParentElement.resolvedStyle.flexDirection == FlexDirection.Row ||
                hierarchyParentElement.resolvedStyle.flexDirection == FlexDirection.RowReverse;
            var reverseOrder =
                hierarchyParentElement.resolvedStyle.flexDirection == FlexDirection.ColumnReverse ||
                hierarchyParentElement.resolvedStyle.flexDirection == FlexDirection.RowReverse;

            if (isRow)
            {
                if (!reverseOrder && !shouldGoLast)
                {
                    style.top = mouseOverElementCanvasRect.y;
                    style.left = mouseOverElementCanvasRect.x - k_IndicatorHalfSize;
                    style.width = k_IndicatorSize;
                    style.height = mouseOverElementCanvasRect.height;
                }
                else if (reverseOrder || shouldGoLast)
                {
                    style.top = mouseOverElementCanvasRect.y;
                    style.left = mouseOverElementCanvasRect.xMax - k_IndicatorHalfSize;
                    style.width = k_IndicatorSize;
                    style.height = mouseOverElementCanvasRect.height;
                }
            }
            else if (!isRow)
            {
                if (!reverseOrder && !shouldGoLast)
                {
                    style.top = mouseOverElementCanvasRect.y - k_IndicatorHalfSize;
                    style.left = mouseOverElementCanvasRect.x;
                    style.width = mouseOverElementCanvasRect.width;
                    style.height = k_IndicatorSize;
                }
                else if (reverseOrder || shouldGoLast)
                {
                    style.top = mouseOverElementCanvasRect.yMax - k_IndicatorHalfSize;
                    style.left = mouseOverElementCanvasRect.x;
                    style.width = mouseOverElementCanvasRect.width;
                    style.height = k_IndicatorSize;
                }
            }
            else
            {
                return;
            }

            style.display = DisplayStyle.Flex;
        }

        public void Activate(VisualElement mouseOverElement, Vector2 mousePosition)
        {
            Reset();

            parentElement = mouseOverElement;

            if (mouseOverElement == documentRootElement)
                return;

            var mouseOverElementMouse = mouseOverElement.WorldToLocal(mousePosition);
            var mouseOverElementRect = mouseOverElement.rect;
            var mouseOverElementCanvasRect = BuilderTracker.GetRelativeRectFromTargetElement(mouseOverElement, this.hierarchy.parent);

            var isCloseToLeftEdge = mouseOverElementMouse.x < k_DistanceFromElementEdge;
            var isCloseToRightEdge = mouseOverElementMouse.x > mouseOverElementRect.xMax - k_DistanceFromElementEdge;
            var isCloseToTopEdge = mouseOverElementMouse.y < k_DistanceFromElementEdge;
            var isCloseToBottomEdge = mouseOverElementMouse.y > mouseOverElementRect.yMax - k_DistanceFromElementEdge;

            var reverseOrder =
                mouseOverElement.parent.resolvedStyle.flexDirection == FlexDirection.ColumnReverse ||
                mouseOverElement.parent.resolvedStyle.flexDirection == FlexDirection.RowReverse;

            int indexOffset;
            if (isCloseToLeftEdge)
            {
                style.top = mouseOverElementCanvasRect.y;
                style.left = mouseOverElementCanvasRect.x - k_IndicatorHalfSize;
                style.width = k_IndicatorSize;
                style.height = mouseOverElementCanvasRect.height;
                indexOffset = reverseOrder ? 1 : 0;
            }
            else if (isCloseToRightEdge)
            {
                style.top = mouseOverElementCanvasRect.y;
                style.left = mouseOverElementCanvasRect.xMax - k_IndicatorHalfSize;
                style.width = k_IndicatorSize;
                style.height = mouseOverElementCanvasRect.height;
                indexOffset = reverseOrder ? 0 : 1;
            }
            else if (isCloseToTopEdge)
            {
                style.top = mouseOverElementCanvasRect.y - k_IndicatorHalfSize;
                style.left = mouseOverElementCanvasRect.x;
                style.width = mouseOverElementCanvasRect.width;
                style.height = k_IndicatorSize;
                indexOffset = reverseOrder ? 1 : 0;
            }
            else if (isCloseToBottomEdge)
            {
                style.top = mouseOverElementCanvasRect.yMax - k_IndicatorHalfSize;
                style.left = mouseOverElementCanvasRect.x;
                style.width = mouseOverElementCanvasRect.width;
                style.height = k_IndicatorSize;
                indexOffset = reverseOrder ? 0 : 1;
            }
            else
            {
                return;
            }

            style.display = DisplayStyle.Flex;
            parentElement = mouseOverElement.parent;

            // We don't want to store the old parent into parentElement, otherwise there will be some consequences when
            // overriding contentContainer. We instead store it in a local copy and fetch it around when needed.
            var correctedParentElement = BuilderHierarchyUtilities.GetToggleButtonGroupContentContainer(parentElement) ?? parentElement;
            indexWithinParent = correctedParentElement.IndexOf(mouseOverElement) + indexOffset;
        }

        public void Deactivate()
        {
            Reset();
        }

        void Reset()
        {
            style.display = DisplayStyle.None;
            parentElement = null;
            indexWithinParent = -1;
        }
    }
}
