// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor
{
    internal static class PopupLocationHelper
    {
        public static Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow)
        {
            return GetDropDownRect(buttonRect, minSize, maxSize, popupContainerWindow, null);
        }

        public static Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow, PopupLocation[] locationPriorityOrder)
        {
            if (locationPriorityOrder == null)
                locationPriorityOrder = new[]
                {
                    PopupLocation.Below,
                    PopupLocation.Above,
                    PopupLocation.Left,
                    PopupLocation.Right
                }; // Default priority order

            List<Rect> croppedRects = new List<Rect>();
            foreach (PopupLocation location in locationPriorityOrder)
            {
                Rect resultRect;
                switch (location)
                {
                    case PopupLocation.BelowAlignLeft:
                        if (PopupBelowAlignLeft(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.BelowAlignRight:
                        if (PopupBelowAlignRight(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.AboveAlignLeft:
                        if (PopupAboveAlignLeft(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.AboveAlignRight:
                        if (PopupAboveAlignRight(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.Left:
                        if (PopupLeft(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.Right:
                        if (PopupRight(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                    case PopupLocation.Overlay:
                        if (PopupOverlay(buttonRect, minSize, maxSize, popupContainerWindow, out resultRect))
                            return resultRect;
                        croppedRects.Add(resultRect);
                        break;
                }
            }

            // Popup did not fit any of the wanted locations. Now find and return the largest cropped rect instead
            return GetLargestRect(croppedRects);
        }

        private static Rect FitRect(Rect rect, Vector2 uiPositionToSelectScreen, ContainerWindow popupContainerWindow)
        {
            return ContainerWindow.FitRectToScreen(rect, uiPositionToSelectScreen, true, popupContainerWindow);
        }

        private static bool PopupRight(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow, out Rect resultRect)
        {
            // Calculate how much space is available for the window right of the button
            Rect dropDownRectRight = new Rect(buttonRect.xMax, buttonRect.y, maxSize.x, maxSize.y);
            float spaceFromRight = 0;
            dropDownRectRight.xMax += spaceFromRight;
            dropDownRectRight.height += k_SpaceFromBottom;

            dropDownRectRight = FitRect(dropDownRectRight, buttonRect.center, popupContainerWindow);
            float availableWidthRight = Mathf.Max(dropDownRectRight.xMax - buttonRect.xMax - spaceFromRight, 0);
            float windowWidth = Mathf.Min(availableWidthRight, maxSize.x);
            resultRect = new Rect(dropDownRectRight.x, dropDownRectRight.y, windowWidth, dropDownRectRight.height - k_SpaceFromBottom);

            if (Mathf.Ceil(availableWidthRight) >= minSize.x)
                return true;
            return false;
        }

        private static bool PopupLeft(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            // Calculate how much space is available for the window left of the button
            Rect dropDownRectLeft = new Rect(buttonRect.x - maxSize.x, buttonRect.y, maxSize.x, maxSize.y);
            float spaceFromLeft = 0;
            dropDownRectLeft.xMin -= spaceFromLeft;
            dropDownRectLeft.height += k_SpaceFromBottom;

            dropDownRectLeft = FitRect(dropDownRectLeft, buttonRect.center, popupContainerWindow);
            float availableWidthLeft = Mathf.Max(buttonRect.x - dropDownRectLeft.x - spaceFromLeft, 0);
            float windowWidth = Mathf.Min(availableWidthLeft, maxSize.x);
            resultRect = new Rect(dropDownRectLeft.x, dropDownRectLeft.y, windowWidth, dropDownRectLeft.height - k_SpaceFromBottom);

            if (Mathf.Ceil(availableWidthLeft) >= minSize.x)
                return true;
            return false;
        }

        private static bool PopupAboveAlignLeft(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            Rect dropDownRectAbove = new Rect(buttonRect.x, buttonRect.y - maxSize.y, maxSize.x, maxSize.y);
            return PopupAbove(dropDownRectAbove, buttonRect, minSize, maxSize, popupContainerWindow, out resultRect);
        }

        private static bool PopupAboveAlignRight(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            Rect dropDownRectAbove = new Rect(buttonRect.xMax - maxSize.x, buttonRect.y - maxSize.y, maxSize.x, maxSize.y);
            return PopupAbove(dropDownRectAbove, buttonRect, minSize, maxSize, popupContainerWindow, out resultRect);
        }

        private static bool PopupAbove(Rect dropDownRectAbove, Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            float spaceFromTop = 0;

            // Expand dropdown height to include empty space above
            dropDownRectAbove.yMin -= spaceFromTop;
            // Fit rect to screen
            dropDownRectAbove = FitRect(dropDownRectAbove, buttonRect.center, popupContainerWindow);

            // Calculate how much space is available for the window above the button
            float availableHeightAbove = Mathf.Max(buttonRect.y - dropDownRectAbove.y - spaceFromTop, 0);
            // If there's room for the window at its minimum size above the button, then place it there
            if (Mathf.Ceil(availableHeightAbove) >= minSize.y)
            {
                float windowHeight = Mathf.Min(availableHeightAbove, maxSize.y);
                {
                    resultRect = new Rect(dropDownRectAbove.x, buttonRect.y - windowHeight, dropDownRectAbove.width, windowHeight);
                    return true;
                }
            }
            resultRect = new Rect(dropDownRectAbove.x, buttonRect.y - availableHeightAbove, dropDownRectAbove.width, availableHeightAbove);
            return false;
        }

        private static bool PopupBelowAlignLeft(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            Rect dropDownRectBelow = new Rect(buttonRect.x, buttonRect.yMax, maxSize.x, maxSize.y);
            return PopupBelow(dropDownRectBelow, buttonRect, minSize, maxSize, popupContainerWindow, out resultRect);
        }

        private static bool PopupBelowAlignRight(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            Rect dropDownRectBelow = new Rect(buttonRect.xMax - maxSize.x, buttonRect.yMax, maxSize.x, maxSize.y);
            return PopupBelow(dropDownRectBelow, buttonRect, minSize, maxSize, popupContainerWindow, out resultRect);
        }

        private static bool PopupBelow(Rect dropDownRectBelow, Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            // Expand dropdown height to include empty space below
            dropDownRectBelow.height += k_SpaceFromBottom;
            // Fit rect to screen
            dropDownRectBelow = FitRect(dropDownRectBelow, buttonRect.center, popupContainerWindow);

            // Calculate how much space is available for the window below the button
            float availableHeightBelow = Mathf.Max(dropDownRectBelow.yMax - buttonRect.yMax - k_SpaceFromBottom, 0);
            // If there's room for the window at its minimum size below the button, then place it there
            if (Mathf.Ceil(availableHeightBelow) >= minSize.y)
            {
                float windowHeight = Mathf.Min(availableHeightBelow, maxSize.y);
                resultRect = new Rect(dropDownRectBelow.x, buttonRect.yMax, dropDownRectBelow.width, windowHeight);
                return true;
            }
            if (dropDownRectBelow.yMax - buttonRect.yMax - k_SpaceFromBottom < 0)
            {
                resultRect = new Rect(dropDownRectBelow.x, (dropDownRectBelow.yMax + dropDownRectBelow.yMin) / 2 - dropDownRectBelow.height, dropDownRectBelow.width, dropDownRectBelow.height);
                return true;
            }
            resultRect = new Rect(dropDownRectBelow.x, buttonRect.yMax, dropDownRectBelow.width, availableHeightBelow);
            return false;
        }

        private static bool PopupOverlay(Rect buttonRect, Vector2 minSize, Vector2 maxSize, ContainerWindow popupContainerWindow,
            out Rect resultRect)
        {
            //Stretch the popup over the button Rect if it doesn't fit
            Rect dropDownRectBelow = new Rect(buttonRect.x, buttonRect.yMax, maxSize.x, maxSize.y);
            resultRect = FitRect(dropDownRectBelow, buttonRect.center, popupContainerWindow);
            return true;
        }

        private static Rect GetLargestRect(List<Rect> rects)
        {
            Rect max = new Rect();
            foreach (Rect rect in rects)
                if (rect.height * rect.width > max.height * max.width)
                    max = rect;
            return max;
        }

        // How much empty space there should be at minimum at the bottom of the screen below the popup
        private static float k_SpaceFromBottom
        {
            get
            {
                if (Application.platform == RuntimePlatform.OSXEditor)
                    return 10f;
                return 0;
            }
        }
    }
}
