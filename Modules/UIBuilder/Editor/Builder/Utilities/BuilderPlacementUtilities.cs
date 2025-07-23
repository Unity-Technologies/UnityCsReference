// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class BuilderPlacementUtilities
    {
        public static VisualElement CreateAbsoluteIsland(BuilderPaneWindow paneWindow, VisualElement documentRootElement, Vector2 localMousePosition)
        {
            var vta = paneWindow.document.visualTreeAsset;
            var selection = paneWindow.primarySelection;

            // Create absolute island container.
            var absoluteIslandContainer = new VisualElement();
            absoluteIslandContainer.name = "unity-absolute-island";
            documentRootElement.Add(absoluteIslandContainer);
            BuilderAssetUtilities.AddElementToAsset(paneWindow.document.visualTreeAsset, absoluteIslandContainer);

            bool isTop = localMousePosition.y < documentRootElement.resolvedStyle.height / 2;
            bool isBottom = !isTop;
            bool isLeft = localMousePosition.x < documentRootElement.resolvedStyle.width / 2;
            bool isRight = !isLeft;

            // Set Absolute position.
            BuilderStyleUtilities.SetInlineEnumValue(vta, absoluteIslandContainer, "position", Position.Absolute);

            if (isTop && isLeft)
            {
                var left = localMousePosition.x;
                var top = localMousePosition.y;
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "left", new Dimension(left, Dimension.Unit.Pixel));
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "top", new Dimension(top, Dimension.Unit.Pixel));
            }
            else if (isTop && isRight)
            {
                var right = documentRootElement.resolvedStyle.width - localMousePosition.x;
                var top = localMousePosition.y;
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "right", new Dimension(right, Dimension.Unit.Pixel));
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "top", new Dimension(top, Dimension.Unit.Pixel));
            }
            else if (isBottom && isLeft)
            {
                var left = localMousePosition.x;
                var bottom = documentRootElement.resolvedStyle.height - localMousePosition.y;
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "left", new Dimension(left, Dimension.Unit.Pixel));
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "bottom", new Dimension(bottom, Dimension.Unit.Pixel));
            }
            else if (isBottom && isRight)
            {
                var right = documentRootElement.resolvedStyle.width - localMousePosition.x;
                var bottom = documentRootElement.resolvedStyle.height - localMousePosition.y;
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "right", new Dimension(right, Dimension.Unit.Pixel));
                BuilderStyleUtilities.SetInlineDimensionValue(vta, absoluteIslandContainer, "bottom", new Dimension(bottom, Dimension.Unit.Pixel));
            }

            // Need to explicitly update inline styles from asset.
            selection.NotifyOfHierarchyChange(null, absoluteIslandContainer, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);

            return absoluteIslandContainer;
        }
    }
}
