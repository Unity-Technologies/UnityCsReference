using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        internal InlineStyleAccess inlineStyleAccess;
        public IStyle style
        {
            get
            {
                if (inlineStyleAccess == null)
                    inlineStyleAccess = new InlineStyleAccess(this);

                return inlineStyleAccess;
            }
        }

        public ICustomStyle customStyle => computedStyle;

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

        private StyleFloat ResolveLengthValue(StyleLength styleLength, bool isRow)
        {
            if (styleLength.keyword != StyleKeyword.Undefined)
                return styleLength.ToStyleFloat();

            var length = styleLength.value;
            if (length.unit != LengthUnit.Percent)
                return styleLength.ToStyleFloat();

            var parent = hierarchy.parent;
            if (parent == null)
                return 0f;

            float parentSize = isRow ? parent.resolvedStyle.width : parent.resolvedStyle.height;
            return length.value * parentSize / 100;
        }
    }
}
