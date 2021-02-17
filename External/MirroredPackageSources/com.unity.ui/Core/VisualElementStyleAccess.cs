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
        internal static CustomStyleAccess s_CustomStyleAccess = new CustomStyleAccess();
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
        public ICustomStyle customStyle
        {
            get
            {
                s_CustomStyleAccess.SetContext(computedStyle.customProperties, computedStyle.dpiScaling);
                return s_CustomStyleAccess;
            }
        }

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

        internal class CustomStyleAccess : ICustomStyle
        {
            private Dictionary<string, StylePropertyValue> m_CustomProperties;
            private float m_DpiScaling;

            public void SetContext(Dictionary<string, StylePropertyValue> customProperties, float dpiScaling)
            {
                m_CustomProperties = customProperties;
                m_DpiScaling = dpiScaling;
            }

            public bool TryGetValue(CustomStyleProperty<float> property, out float value)
            {
                if (TryGetValue(property.name, StyleValueType.Float, out var customProp))
                {
                    if (customProp.sheet.TryReadFloat(customProp.handle, out value))
                        return true;
                }

                value = 0f;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<int> property, out int value)
            {
                if (TryGetValue(property.name, StyleValueType.Float, out var customProp))
                {
                    if (customProp.sheet.TryReadFloat(customProp.handle, out var tmp))
                    {
                        value = (int)tmp;
                        return true;
                    }
                }

                value = 0;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<bool> property, out bool value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    value = customProp.sheet.ReadKeyword(customProp.handle) == StyleValueKeyword.True;
                    return true;
                }

                value = false;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<Color> property, out Color value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    var handle = customProp.handle;
                    switch (handle.valueType)
                    {
                        case StyleValueType.Enum:
                        {
                            var colorName = customProp.sheet.ReadAsString(handle);
                            return StyleSheetColor.TryGetColor(colorName.ToLower(), out value);
                        }
                        case StyleValueType.Color:
                        {
                            if (customProp.sheet.TryReadColor(customProp.handle, out value))
                                return true;
                            break;
                        }
                        default:
                            LogCustomPropertyWarning(property.name, StyleValueType.Color, customProp);
                            break;
                    }
                }

                value = Color.clear;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<Texture2D> property, out Texture2D value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    var source = new ImageSource();
                    if (StylePropertyReader.TryGetImageSourceFromValue(customProp, m_DpiScaling, out source) && source.texture != null)
                    {
                        value = source.texture;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<Sprite> property, out Sprite value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    var source = new ImageSource();
                    if (StylePropertyReader.TryGetImageSourceFromValue(customProp, m_DpiScaling, out source) && source.sprite != null)
                    {
                        value = source.sprite;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<VectorImage> property, out VectorImage value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    var source = new ImageSource();
                    if (StylePropertyReader.TryGetImageSourceFromValue(customProp, m_DpiScaling, out source) && source.vectorImage != null)
                    {
                        value = source.vectorImage;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public bool TryGetValue(CustomStyleProperty<string> property, out string value)
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    value = customProp.sheet.ReadAsString(customProp.handle);
                    return true;
                }

                value = string.Empty;
                return false;
            }

            private bool TryGetValue(string propertyName, StyleValueType valueType, out StylePropertyValue customProp)
            {
                customProp = new StylePropertyValue();
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out customProp))
                {
                    // CustomProperty only support one value
                    var handle = customProp.handle;
                    if (handle.valueType != valueType)
                    {
                        LogCustomPropertyWarning(propertyName, valueType, customProp);
                        return false;
                    }

                    return true;
                }

                return false;
            }

            private static void LogCustomPropertyWarning(string propertyName, StyleValueType valueType, StylePropertyValue customProp)
            {
                Debug.LogWarning($"Trying to read custom property {propertyName} value as {valueType} while parsed type is {customProp.handle.valueType}");
            }
        }
    }
}
