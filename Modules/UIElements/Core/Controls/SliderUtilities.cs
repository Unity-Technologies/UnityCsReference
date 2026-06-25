// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // Non-generic slider layout helpers, shared by every BaseSlider<T> value type so the
    // geometry/fill code is emitted once instead of specialized per value type by IL2CPP.
    static class SliderUtilities
    {
        internal const string k_FillElementName = "unity-fill";

        static bool SameValues(float a, float b, float epsilon)
        {
            return Mathf.Abs(b - a) < epsilon;
        }

        internal static void UpdateDragElementPosition(
            VisualElement slider, VisualElement dragContainer, VisualElement dragElement, VisualElement dragBorderElement,
            VisualElement trackElement, ref VisualElement fillElement, UniqueStyleString fillUssClassNameUnique,
            float normalizedPosition, SliderDirection direction, bool inverted, bool fill, ref float adjustedPageSizeFromClick)
        {
            float directionalNormalizedPosition = inverted ? 1f - normalizedPosition : normalizedPosition;
            float halfPixel = slider.scaledPixelsPerPoint * 0.5f;

            if (direction == SliderDirection.Horizontal)
            {
                float dragElementWidth = dragElement.resolvedStyle.width;

                float offsetForThumbFullWidth = -dragElement.resolvedStyle.marginLeft - dragElement.resolvedStyle.marginRight;
                float totalWidth = dragContainer.layoutSize.x - dragElementWidth + offsetForThumbFullWidth;
                float newLeft = directionalNormalizedPosition * totalWidth;

                if (float.IsNaN(newLeft))
                    return;

                float currentLeft = dragElement.resolvedStyle.translate.x;

                if (!SameValues(currentLeft, newLeft, halfPixel))
                {
                    var newPos = new Vector3(newLeft, 0, 0);
                    dragElement.style.translate = newPos;
                    dragBorderElement.style.translate = newPos;
                    adjustedPageSizeFromClick = 0;
                }
            }
            else
            {
                float dragElementHeight = dragElement.resolvedStyle.height;

                float totalHeight = dragContainer.resolvedStyle.height - dragElementHeight;

                // Vertical scrollbar default starts from the bottom, so we invert the normalized position.
                float newTop = (1 - directionalNormalizedPosition) * totalHeight;

                if (float.IsNaN(newTop))
                    return;

                float currentTop = dragElement.resolvedStyle.translate.y;
                if (!SameValues(currentTop, newTop, halfPixel))
                {
                    var newPos = new Vector3(0, newTop, 0);
                    dragElement.style.translate = newPos;
                    dragBorderElement.style.translate = newPos;
                    adjustedPageSizeFromClick = 0;
                }
            }

            UpdateFill(ref fillElement, trackElement, normalizedPosition, direction, inverted, fill, fillUssClassNameUnique);
        }

        static void UpdateFill(ref VisualElement fillElement, VisualElement trackElement, float normalizedValue,
            SliderDirection direction, bool inverted, bool fill, UniqueStyleString fillUssClassNameUnique)
        {
            if (!fill)
                return;

            if (fillElement == null)
            {
                fillElement = new VisualElement { name = k_FillElementName, usageHints = UsageHints.DynamicColor };
                fillElement.AddToClassList(fillUssClassNameUnique);
                trackElement.Add(fillElement);
            }

            float inverseNormalizedValue = 1.0f - normalizedValue;
            var valuePercent = Length.Percent(inverseNormalizedValue * 100.0f);
            if (direction == SliderDirection.Vertical)
            {
                fillElement.style.right = 0;
                fillElement.style.left = 0;
                fillElement.style.bottom = inverted ? valuePercent : 0;
                fillElement.style.top = inverted ? 0 : valuePercent;
            }
            else
            {
                fillElement.style.top = 0;
                fillElement.style.bottom = 0;
                fillElement.style.left = inverted ? valuePercent : 0;
                fillElement.style.right = inverted ? 0 : valuePercent;
            }
        }

        internal static void AdjustDragElement(VisualElement slider, VisualElement dragContainer, VisualElement dragElement,
            VisualElement dragBorderElement, float factor, SliderDirection direction)
        {
            bool needsElement = factor < 1f;
            if (needsElement)
            {
                dragElement.style.visibility = new StyleEnum<Visibility>(Visibility.Visible, StyleKeyword.Null);

                IStyle inlineStyles = dragElement.style;

                if (direction == SliderDirection.Horizontal)
                {
                    float elemMinWidth = slider.resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : slider.resolvedStyle.minWidth.value;
                    inlineStyles.width = Mathf.Round(Mathf.Max(dragContainer.layoutSize.x * factor, elemMinWidth));
                }
                else
                {
                    float elemMinHeight = slider.resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : slider.resolvedStyle.minHeight.value;
                    inlineStyles.height = Mathf.Round(Mathf.Max(dragContainer.layoutSize.y * factor, elemMinHeight));
                }
            }
            else
            {
                dragElement.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden, StyleKeyword.Undefined);
            }

            dragBorderElement.visible = dragElement.visible;
        }
    }
}
