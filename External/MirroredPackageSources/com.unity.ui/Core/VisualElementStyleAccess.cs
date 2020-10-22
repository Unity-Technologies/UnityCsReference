using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement
    {
        internal InlineStyleAccess inlineStyleAccess;
        /// <summary>
        /// Reference to the style object of this element.
        /// </summary>
        /// <remarks>
        /// Contains data computed from USS files or inline styles written to this object in C#.
        /// </remarks>
        public IStyle style
        {
            get
            {
                if (inlineStyleAccess == null)
                    inlineStyleAccess = new InlineStyleAccess(this);

                return inlineStyleAccess;
            }
        }

        /// <summary>
        /// Returns the custom style properties accessor for this element.
        /// </summary>
        public ICustomStyle customStyle => computedStyle;

        /// <summary>
        /// Returns a <see cref="VisualElementStyleSheetSet"/> that manipulates style sheets attached to this element.
        /// </summary>
        public VisualElementStyleSheetSet styleSheets => new VisualElementStyleSheetSet(this);

        internal List<StyleSheet> styleSheetList;

        private static readonly Regex s_InternalStyleSheetPath = new Regex("^instanceId:[-0-9]+$", RegexOptions.Compiled);

        internal void AddStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet), scaledPixelsPerPoint) as StyleSheet;

            if (sheetAsset == null)
            {
                if (!s_InternalStyleSheetPath.IsMatch(sheetPath))
                {
                    Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                }
                return;
            }

            styleSheets.Add(sheetAsset);
        }

        internal bool HasStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet), scaledPixelsPerPoint) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                return false;
            }

            return styleSheets.Contains(sheetAsset);
        }

        internal void RemoveStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet), scaledPixelsPerPoint) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                return;
            }
            styleSheets.Remove(sheetAsset);
        }

        private StyleFloat ResolveLengthValue(Length length, bool isRow)
        {
            if (length.IsAuto())
                return new StyleFloat(StyleKeyword.Auto);

            if (length.IsNone())
                return new StyleFloat(StyleKeyword.None);

            if (length.unit != LengthUnit.Percent)
                return new StyleFloat(length.value);

            var parent = hierarchy.parent;
            if (parent == null)
                return 0f;

            float parentSize = isRow ? parent.resolvedStyle.width : parent.resolvedStyle.height;
            return length.value * parentSize / 100;
        }
    }
}
