using UnityEngine;
using UnityEngine.UIElements;

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
            BuilderAssetUtilities.AddElementToAsset(paneWindow.document, absoluteIslandContainer);

            bool isTop = localMousePosition.y < documentRootElement.resolvedStyle.height / 2;
            bool isBottom = !isTop;
            bool isLeft = localMousePosition.x < documentRootElement.resolvedStyle.width / 2;
            bool isRight = !isLeft;

            // Set Absolute position.
            BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "position", Position.Absolute);

            if (isTop && isLeft)
            {
                var left = localMousePosition.x;
                var top = localMousePosition.y;
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "left", left);
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "top", top);
            }
            else if (isTop && isRight)
            {
                var right = documentRootElement.resolvedStyle.width - localMousePosition.x;
                var top = localMousePosition.y;
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "right", right);
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "top", top);
            }
            else if (isBottom && isLeft)
            {
                var left = localMousePosition.x;
                var bottom = documentRootElement.resolvedStyle.height - localMousePosition.y;
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "left", left);
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "bottom", bottom);
            }
            else if (isBottom && isRight)
            {
                var right = documentRootElement.resolvedStyle.width - localMousePosition.x;
                var bottom = documentRootElement.resolvedStyle.height - localMousePosition.y;
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "right", right);
                BuilderStyleUtilities.SetInlineStyleValue(vta, absoluteIslandContainer, "bottom", bottom);
            }

            // Need to explicitly update inline styles from asset.
            selection.NotifyOfHierarchyChange(null, absoluteIslandContainer, BuilderHierarchyChangeType.InlineStyle);

            return absoluteIslandContainer;
        }
    }
}
