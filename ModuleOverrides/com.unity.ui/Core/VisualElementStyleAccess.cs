// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        /// Sets the style values on a <see cref="VisualElement"/>.
        /// </summary>
        /// <remarks>
        /// The returned style data, computed from USS files or inline styles written to this object 
        /// in C#, doesn't represent the fully resolved styles, such as the final height and width of 
        /// a VisualElement. 
        /// To access these fully resolved styles, use <see cref="resolvedStyle"/>.
        /// </remarks>
        /// <remarks>
        /// For information about how to use this property and all the supported USS properties, refer to the
        /// [[wiki:UIE-apply-styles-with-csharp|Apply styles in C# scripts]] and
        /// [[wiki:UIE-USS-Properties-Reference|USS properties reference]] manual pages.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// // Set the background color of the element to red.
        /// element.style.backgroundColor = Color.red;
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// SA: [[VisualElement.resolvedStyle]], [[VisualElement.customStyle]], [[StyleSheet]]
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
        /// The custom style properties accessor of a <see cref="VisualElement"/> (RO).
        /// </summary>
        /// <remarks>
        /// To get the custom styles properties of an element, call the <see cref="ICustomStyle.TryGetValue"/> 
        /// method to query the returned object of this property. 
        /// </remarks>
        /// <remarks>
        /// For more information about how to use this property, refer to the
        /// [[wiki:UIE-get-custom-styles|Get custom styles]] manual page.
        /// </remarks>
        /// <remarks>
        /// For a list of all the supported style properties, refer 
        /// to the [[wiki:UIE-USS-Properties-Reference|USS properties reference]] manual page.
        /// </remarks>
        /// <remarks>
        /// SA: [[VisualElement.style]], [[VisualElement.resolvedStyle]]
        /// </remarks>
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

        private Vector3 ResolveTranslate()
        {
            var translationOperation = computedStyle.translate;
            float x;
            var x_cache = translationOperation.x;
            if (x_cache.unit == LengthUnit.Percent)
            {
                var width = resolvedStyle.width;
                x = float.IsNaN(width) ? 0 : width * x_cache.value / 100;
            }
            else // we assume unitless or pixel values
            {
                x = x_cache.value;
                x = float.IsNaN(x) ? 0.0f : x;
            }

            float y;
            var y_cache = translationOperation.y;
            if (y_cache.unit == LengthUnit.Percent)
            {
                var height = resolvedStyle.height;
                y = float.IsNaN(height) ? 0 : height * y_cache.value / 100;
            }
            else // we assume unitless or pixel values
            {
                y = y_cache.value;
                y = float.IsNaN(y) ? 0.0f : y;
            }

            float z = translationOperation.z;
            z = float.IsNaN(z) ? 0.0f : z;

            return new Vector3(x, y, z);
        }

        private Vector3 ResolveTransformOrigin()
        {
            var transformOrigin = computedStyle.transformOrigin;

            float x = float.NaN;
            var x_cache = transformOrigin.x;
            if (x_cache.IsNone())
            {
                var width = resolvedStyle.width;
                x = float.IsNaN(width) ? 0 :  width / 2;
            }
            else if (x_cache.unit == LengthUnit.Percent)
            {
                var width = resolvedStyle.width;
                x = float.IsNaN(width) ? 0 : width * x_cache.value / 100;
            }
            else // we asume unitless or pixel values
            {
                x = x_cache.value;
            }


            float y = float.NaN;
            var y_cache = transformOrigin.y;
            if (y_cache.IsNone())
            {
                var height = resolvedStyle.height;
                y = float.IsNaN(height) ? 0 : height / 2;
            }
            else if (y_cache.unit == LengthUnit.Percent)
            {
                var height = resolvedStyle.height;
                y = float.IsNaN(height) ? 0 : height * y_cache.value  / 100;
            }
            else // we asume unitless or pixel values
            {
                y = y_cache.value;
            }

            float z = transformOrigin.z;

            return new Vector3(x, y, z);
        }

        private Quaternion ResolveRotation()
        {
            var rotate = computedStyle.rotate;
            var axis = rotate.axis;
            if (float.IsNaN(rotate.angle.value) || float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
                rotate = Rotate.Initial();

            return rotate.ToQuaternion();
        }

        private Vector3 ResolveScale()
        {
            Vector3 s = computedStyle.scale.value;
            s = (float.IsNaN(s.x) || float.IsNaN(s.y) || float.IsNaN(s.z)) ? Vector3.one : s;

            return s;
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
                            return StyleSheetColor.TryGetColor(colorName.ToLowerInvariant(), out value);
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

            public bool TryGetValue<T>(CustomStyleProperty<T> property, out T value) where T : Object
            {
                if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
                {
                    if (customProp.sheet.TryReadAssetReference(customProp.handle, out Object objValue))
                    {
                        value = objValue as T;
                        return value != null;
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
