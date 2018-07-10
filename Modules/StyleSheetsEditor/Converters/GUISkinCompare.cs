// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.StyleSheets
{
    internal static class GUISkinCompare
    {
        public static bool CompareTo(Color lhs, Color rhs)
        {
            return StyleSheetToUss.ToUssString(lhs) == StyleSheetToUss.ToUssString(rhs);
        }

        public static bool CompareTo(float lhs, float rhs)
        {
            return Math.Abs(lhs - rhs) <= 0.0001f;
        }

        public static bool CompareTo(this RectOffset lhs, RectOffset rhs)
        {
            // Returns false in the presence of NaN values.
            return rhs != null && (lhs != null && (lhs.left == rhs.left && lhs.right == rhs.right && lhs.top == rhs.top && lhs.bottom == rhs.bottom));
        }

        public static bool CompareTo(this GUIStyleState self, GUIStyleState otherState, List<string> diffs = null, string prefix = "")
        {
            // Compare USS color notation to avoid rounding errors.
            if (!CompareTo(self.textColor, otherState.textColor)) { if (diffs == null) return false; diffs.Add(prefix + "textColor"); }
            if (self.background != otherState.background) { if (diffs == null) return false; diffs.Add(prefix + "background"); }
            if (!self.scaledBackgrounds.SequenceEqual(otherState.scaledBackgrounds)) { if (diffs == null) return false; diffs.Add(prefix + "scaledBackgrounds"); }

            return diffs == null || diffs.Count == 0;
        }

        public static bool CompareTo(this GUISettings self, GUISettings otherSettings, List<string> diffs = null)
        {
            if (!CompareTo(self.cursorColor, otherSettings.cursorColor)) { if (diffs == null) return false; diffs.Add("settings[cursorColor]"); }
            if (!CompareTo(self.selectionColor, otherSettings.selectionColor)) { if (diffs == null) return false; diffs.Add("settings[selectionColor]"); }
            if (!CompareTo(self.cursorFlashSpeed, otherSettings.cursorFlashSpeed)) { if (diffs == null) return false; diffs.Add("settings[cursorFlashSpeed]"); }
            if (self.doubleClickSelectsWord != otherSettings.doubleClickSelectsWord) { if (diffs == null) return false; diffs.Add("settings[doubleClickSelectsWord]"); }
            if (self.tripleClickSelectsLine != otherSettings.tripleClickSelectsLine) { if (diffs == null) return false; diffs.Add("settings[tripleClickSelectsLine]"); }

            return diffs == null || diffs.Count == 0;
        }

        public static bool CompareTo(this GUIStyle self, GUIStyle otherStyle, List<string> diffs = null, int index = -1)
        {
            var name = !String.IsNullOrEmpty(self.name) ? self.name : index.ToString();
            var prefix = string.Format("style[{0}]:", name);
            if (self.imagePosition != otherStyle.imagePosition) { if (diffs == null) return false; diffs.Add(prefix + "imagePosition"); }
            if (self.alignment != otherStyle.alignment) { if (diffs == null) return false; diffs.Add(prefix + "alignment"); }
            if (self.wordWrap != otherStyle.wordWrap) { if (diffs == null) return false; diffs.Add(prefix + "wordWrap"); }
            if (self.clipping != otherStyle.clipping) { if (diffs == null) return false; diffs.Add(prefix + "clipping"); }
            if (self.contentOffset != otherStyle.contentOffset) { if (diffs == null) return false; diffs.Add(prefix + "contentOffset"); }
            if (!CompareTo(self.fixedWidth, otherStyle.fixedWidth)) { if (diffs == null) return false; diffs.Add(prefix + "fixedWidth"); }
            if (!CompareTo(self.fixedHeight, otherStyle.fixedHeight)) { if (diffs == null) return false; diffs.Add(prefix + "fixedHeight"); }
            if (self.stretchWidth != otherStyle.stretchWidth) { if (diffs == null) return false; diffs.Add(prefix + "stretchWidth"); }
            if (self.stretchHeight != otherStyle.stretchHeight) { if (diffs == null) return false; diffs.Add(prefix + "stretchHeight"); }

            if (self.fontSize != otherStyle.fontSize) { if (diffs == null) return false; diffs.Add(prefix + "fontSize"); }
            if (self.fontStyle != otherStyle.fontStyle) { if (diffs == null) return false; diffs.Add(prefix + "fontStyle"); }
            if (self.richText != otherStyle.richText) { if (diffs == null) return false; diffs.Add(prefix + "richText"); }

            if (!self.normal.CompareTo(otherStyle.normal, diffs, prefix + "state[normal]:")) { if (diffs == null) return false; }
            if (!self.hover.CompareTo(otherStyle.hover, diffs, prefix + "state[hover]:")) { if (diffs == null) return false; }
            if (!self.active.CompareTo(otherStyle.active, diffs, prefix + "state[active]:")) { if (diffs == null) return false; }
            if (!self.focused.CompareTo(otherStyle.focused, diffs, prefix + "state[focused]:")) { if (diffs == null) return false; }
            if (!self.onNormal.CompareTo(otherStyle.onNormal, diffs, prefix + "state[onNormal]:")) { if (diffs == null) return false; }
            if (!self.onHover.CompareTo(otherStyle.onHover, diffs, prefix + "state[onHover]:")) { if (diffs == null) return false; }
            if (!self.onActive.CompareTo(otherStyle.onActive, diffs, prefix + "state[onActive]:")) { if (diffs == null) return false; }
            if (!self.onFocused.CompareTo(otherStyle.onFocused, diffs, prefix + "state[onFocused]:")) { if (diffs == null) return false; }

            if (!self.border.CompareTo(otherStyle.border)) { if (diffs == null) return false; diffs.Add(prefix + "border"); }
            if (!self.margin.CompareTo(otherStyle.margin)) { if (diffs == null) return false; diffs.Add(prefix + "margin"); }
            if (!self.padding.CompareTo(otherStyle.padding)) { if (diffs == null) return false; diffs.Add(prefix + "padding"); }
            if (!self.overflow.CompareTo(otherStyle.overflow)) { if (diffs == null) return false; diffs.Add(prefix + "overflow"); }

            if (self.font != otherStyle.font) { if (diffs == null) return false; diffs.Add(prefix + "font"); }
            if (!CompareTo(self.lineHeight, otherStyle.lineHeight)) { if (diffs == null) return false; diffs.Add(prefix + "lineHeight"); }
            if (self.isHeightDependantOnWidth != otherStyle.isHeightDependantOnWidth) { if (diffs == null) return false; diffs.Add(prefix + "isHeightDependantOnWidth"); }

            return diffs == null || diffs.Count == 0;
        }

        public static bool CompareTo(this GUISkin self, GUISkin otherSkin, List<string> diffs = null)
        {
            if (self.font != otherSkin.font) { if (diffs == null) return false; diffs.Add("font"); }

            if (!self.box.CompareTo(otherSkin.box, diffs) && diffs == null) return false;
            if (!self.label.CompareTo(otherSkin.label, diffs) && diffs == null) return false;
            if (!self.textField.CompareTo(otherSkin.textField, diffs) && diffs == null) return false;
            if (!self.textArea.CompareTo(otherSkin.textArea, diffs) && diffs == null) return false;
            if (!self.button.CompareTo(otherSkin.button, diffs) && diffs == null) return false;
            if (!self.toggle.CompareTo(otherSkin.toggle, diffs) && diffs == null) return false;
            if (!self.window.CompareTo(otherSkin.window, diffs) && diffs == null) return false;
            if (!self.horizontalSlider.CompareTo(otherSkin.horizontalSlider, diffs) && diffs == null) return false;
            if (!self.horizontalSliderThumb.CompareTo(otherSkin.horizontalSliderThumb, diffs) && diffs == null) return false;
            if (!self.verticalSlider.CompareTo(otherSkin.verticalSlider, diffs) && diffs == null) return false;
            if (!self.verticalSliderThumb.CompareTo(otherSkin.verticalSliderThumb, diffs) && diffs == null) return false;
            if (!self.horizontalScrollbar.CompareTo(otherSkin.horizontalScrollbar, diffs) && diffs == null) return false;
            if (!self.horizontalScrollbarThumb.CompareTo(otherSkin.horizontalScrollbarThumb, diffs) && diffs == null) return false;
            if (!self.horizontalScrollbarLeftButton.CompareTo(otherSkin.horizontalScrollbarLeftButton, diffs) && diffs == null) return false;
            if (!self.horizontalScrollbarRightButton.CompareTo(otherSkin.horizontalScrollbarRightButton, diffs) && diffs == null) return false;
            if (!self.verticalScrollbar.CompareTo(otherSkin.verticalScrollbar, diffs) && diffs == null) return false;
            if (!self.verticalScrollbarThumb.CompareTo(otherSkin.verticalScrollbarThumb, diffs) && diffs == null) return false;
            if (!self.verticalScrollbarUpButton.CompareTo(otherSkin.verticalScrollbarUpButton, diffs) && diffs == null) return false;
            if (!self.verticalScrollbarDownButton.CompareTo(otherSkin.verticalScrollbarDownButton, diffs) && diffs == null) return false;
            if (!self.scrollView.CompareTo(otherSkin.scrollView, diffs) && diffs == null) return false;

            // Check custom styles
            int i = 0;
            bool areCustomStylesDifferent = self.customStyles.Length != otherSkin.customStyles.Length;
            foreach (var customStyle in self.customStyles)
            {
                var matchedStyle = String.IsNullOrEmpty(customStyle.name) ?
                    otherSkin.customStyles.ElementAtOrDefault(i) :
                    otherSkin.customStyles.FirstOrDefault(style => style.name == customStyle.name);
                if (matchedStyle == null)
                {
                    if (diffs == null)
                        return false;
                    diffs.Add("customStyles");
                    areCustomStylesDifferent = true;
                }
                else
                {
                    if (!customStyle.CompareTo(matchedStyle, diffs, i))
                    {
                        if (diffs == null)
                            return false;
                        areCustomStylesDifferent = true;
                    }
                }

                i++;
            }
            if (areCustomStylesDifferent) { if (diffs == null) return false; diffs.Add("customStyles"); }

            if (!self.settings.CompareTo(otherSkin.settings, diffs) && diffs == null) return false;

            return diffs == null || diffs.Count == 0;
        }
    }
}
