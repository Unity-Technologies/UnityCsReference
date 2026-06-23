// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // Non-generic helpers for the text input layout/scroll logic. These never depend on the field
    // value type, so hoisting them here emits the bodies once instead of specializing them for every
    // TextInputBaseField<T> value type. The owning TextInputBase passes its existing child elements
    // and mutable scroll state by reference.
    internal static class TextInputUtilities
    {
        internal static void UpdateScrollOffset(VisualElement self, TextElement textElement, ScrollView scrollView,
            ref Vector2 scrollOffset, ref bool scrollViewWasClamped, ref Vector2 lastCursorPos, bool isBackspace, bool widthChanged)
        {
            var selection = textElement.selection;
            if (selection.cursorIndex < 0 || (selection.cursorIndex <= 0 && selection.selectIndex <= 0 && scrollOffset == Vector2.zero))
                return;

            if (scrollView != null)
            {
                scrollOffset = GetScrollOffset(self, textElement, scrollView, scrollOffset, scrollViewWasClamped, ref lastCursorPos,
                    scrollView.scrollOffset.x, scrollView.scrollOffset.y, scrollView.contentViewport.layout.width, isBackspace, widthChanged);
                scrollView.scrollOffset = scrollOffset;

                scrollViewWasClamped = scrollOffset.x > scrollView.scrollOffset.x || scrollOffset.y > scrollView.scrollOffset.y;
            }
            else
            {
                var t = textElement.resolvedStyle.translate;

                scrollOffset = GetScrollOffset(self, textElement, scrollView, scrollOffset, scrollViewWasClamped, ref lastCursorPos,
                    scrollOffset.x, scrollOffset.y, self.contentRect.width, isBackspace, widthChanged);

                t.y = -Mathf.Min(scrollOffset.y, Mathf.Abs(textElement.contentRect.height - self.contentRect.height));
                t.x = -scrollOffset.x;

                if (!t.Equals(textElement.resolvedStyle.translate))
                    textElement.style.translate = t;
            }
        }

        static Vector2 GetScrollOffset(VisualElement self, TextElement textElement, ScrollView scrollView,
            Vector2 scrollOffset, bool scrollViewWasClamped, ref Vector2 lastCursorPos,
            float xOffset, float yOffset, float contentViewportWidth, bool isBackspace, bool widthChanged)
        {
            // Scroll to the beginning when focus is lost (UUM-73005)
            if (!textElement.hasFocus)
                return Vector2.zero;

            var selection = textElement.selection;
            var cursorPos = selection.cursorPosition;
            var cursorWidth = selection.cursorWidth;

            var newXOffset = xOffset;
            var newYOffset = yOffset;

            const int leftScrollOffsetPadding = 5;
            const float epsilon = 0.05f;

            // Related to: UUM-2057
            // To uncomment once TXT-301 is fixed.
            // if (isBackspace && xOffset > 0.0f)
            // {
            //     newXOffset = xOffset + cursorPos.x - lastCursorPos.x;
            // }

            if (Mathf.Abs(lastCursorPos.x - cursorPos.x) > epsilon || scrollViewWasClamped || widthChanged)
            {
                // Update scrollOffset when cursor moves right or when the offset is not needed anymore.
                if (cursorPos.x > xOffset + contentViewportWidth - cursorWidth
                    || xOffset > 0 && widthChanged)
                {
                    var roundedValue = Mathf.Ceil(cursorPos.x + cursorWidth - contentViewportWidth);
                    newXOffset = Mathf.Max(roundedValue, 0);
                }
                // Update scrollOffset when cursor moves left.
                else if (cursorPos.x < xOffset + leftScrollOffsetPadding)
                {
                    newXOffset = Mathf.Max(cursorPos.x - leftScrollOffsetPadding, 0);
                }
            }

            if (textElement.edition.multiline && (Mathf.Abs(lastCursorPos.y - cursorPos.y) > epsilon || scrollViewWasClamped))
            {
                // Update scrollOffset when cursor moves down.
                if (cursorPos.y > self.contentRect.height + yOffset)
                    newYOffset = cursorPos.y - self.contentRect.height;
                // Update scrollOffset when cursor moves up.
                else if (cursorPos.y < selection.lineHeightAtCursorPosition + yOffset + epsilon)
                    newYOffset = cursorPos.y - selection.lineHeightAtCursorPosition;
            }

            lastCursorPos = cursorPos;

            if (Mathf.Abs(xOffset - newXOffset) > epsilon || Mathf.Abs(yOffset - newYOffset) > epsilon)
            {
                return new Vector2(newXOffset, newYOffset);
            }

            return scrollView != null ? scrollView.scrollOffset : scrollOffset;
        }

        internal static void SetScrollViewMode(VisualElement self, TextElement textElement, ScrollView scrollView,
            UniqueStyleString verticalVariant, UniqueStyleString verticalHorizontalVariant, UniqueStyleString horizontalVariant)
        {
            if (scrollView == null)
                return;

            textElement.RemoveFromClassList(verticalVariant);
            textElement.RemoveFromClassList(verticalHorizontalVariant);
            textElement.RemoveFromClassList(horizontalVariant);

            if (textElement.edition.multiline && (self.computedStyle.whiteSpace == WhiteSpace.Normal || self.computedStyle.whiteSpace == WhiteSpace.PreWrap))
            {
                textElement.AddToClassList(verticalVariant);
                scrollView.mode = ScrollViewMode.Vertical;
            }
            else if (textElement.edition.multiline)
            {
                textElement.AddToClassList(verticalHorizontalVariant);
                scrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            }
            else
            {
                textElement.AddToClassList(horizontalVariant);
                scrollView.mode = ScrollViewMode.Horizontal;
            }
        }

        internal static void SetMultilineContainerStyle(VisualElement self, VisualElement multilineContainer)
        {
            if (multilineContainer != null)
            {
                if (self.computedStyle.whiteSpace == WhiteSpace.Normal || self.computedStyle.whiteSpace == WhiteSpace.PreWrap)
                {
                    self.style.overflow = Overflow.Hidden;
                    multilineContainer.style.alignSelf = Align.Auto;
                }
                else
                    self.style.overflow = (Overflow)OverflowInternal.Scroll;
            }
        }
    }
}
