// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class StyleValueHandleExtensions
    {
        public static int SwallowStyleValue(this StyleSheet to, StyleSheet from, StyleValueHandle valueHandle)
        {
            var index = 0;
            switch (valueHandle.valueType)
            {
                case StyleValueType.Keyword:
                    index = to.AddValueToArray(from.GetKeyword(valueHandle)); break;
                case StyleValueType.Float:
                    index = to.AddValueToArray(from.GetFloat(valueHandle)); break;
                case StyleValueType.Dimension:
                    index = to.AddValueToArray(from.GetDimension(valueHandle)); break;
                case StyleValueType.Color:
                    index = to.AddValueToArray(from.GetColor(valueHandle)); break;
                case StyleValueType.String:
                    index = to.AddValueToArray(from.GetString(valueHandle)); break;
                case StyleValueType.AssetReference:
                    index = to.AddValueToArray(from.GetAsset(valueHandle)); break;
                case StyleValueType.ResourcePath:
                    index = to.AddValueToArray(from.GetString(valueHandle)); break;
                case StyleValueType.Enum:
                    index = to.AddValueToArray(from.GetEnum(valueHandle)); break;
            }

            return index;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, StyleValueKeyword value)
        {
            styleSheet.UpdateContentHash();
            return (int)value;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, float value)
        {
            var floats = styleSheet.floats.ToList();
            floats.Add(value);
            styleSheet.floats = floats.ToArray();
            styleSheet.UpdateContentHash();

            return floats.Count - 1;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, Dimension value)
        {
            var dimensions = styleSheet.dimensions.ToList();
            dimensions.Add(value);
            styleSheet.dimensions = dimensions.ToArray();
            styleSheet.UpdateContentHash();

            return dimensions.Count - 1;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, Color value)
        {
            var colors = styleSheet.colors.ToList();
            colors.Add(value);
            styleSheet.colors = colors.ToArray();
            styleSheet.UpdateContentHash();

            return colors.Count - 1;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, string value)
        {
            var strings = styleSheet.strings.ToList();
            strings.Add(value);
            styleSheet.strings = strings.ToArray();
            styleSheet.UpdateContentHash();

            return strings.Count - 1;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, Object value)
        {
            var assets = styleSheet.assets.ToList();
            assets.Add(value);
            styleSheet.assets = assets.ToArray();
            styleSheet.UpdateContentHash();

            return assets.Count - 1;
        }

        public static int AddValueToArray(this StyleSheet styleSheet, Enum value)
        {
            var newEnumStr = value.ToString();
            var strValue = BuilderNameUtilities.ConvertCamelToDash(newEnumStr);

            // Add value data to data array.
            var values = styleSheet.strings.ToList();
            values.Add(strValue);
            styleSheet.strings = values.ToArray();
            styleSheet.UpdateContentHash();

            return values.Count - 1;
        }

        public static StyleValueKeyword GetKeyword(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadKeyword(valueHandle);
        }

        public static int GetInt(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return (int)styleSheet.floats[valueHandle.valueIndex];
        }

        public static float GetFloat(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadFloat(valueHandle);
        }

        public static Dimension GetDimension(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadDimension(valueHandle);
        }

        public static Color GetColor(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            if (valueHandle.valueType == StyleValueType.Enum)
            {
                var colorName = styleSheet.ReadAsString(valueHandle);
                StyleSheetColor.TryGetColor(colorName.ToLower(), out var value);
                return value;
            }
            return styleSheet.ReadColor(valueHandle);
        }

        public static string GetString(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.strings[valueHandle.valueIndex];
        }

        public static BuilderTransformOrigin GetTransformOrigin(this StyleSheet styleSheet, StyleProperty styleProperty)
        {
            int valCount = styleProperty.values.Length;

            if (valCount > 0)
            {
                var val1 = new StylePropertyValue() { handle = styleProperty.values[0], sheet = styleSheet };
                var val2 = valCount > 1 ? new StylePropertyValue() { handle = styleProperty.values[1], sheet = styleSheet } : new StylePropertyValue();
                var zval = valCount > 2 ? new StylePropertyValue() { handle = styleProperty.values[2], sheet = styleSheet } : new StylePropertyValue();

                return new BuilderTransformOrigin(StylePropertyReader.ReadTransformOrigin(valCount, val1, val2, zval));
            }
            else
            {

                return new BuilderTransformOrigin
                {
                    x = new Dimension(0f, Dimension.Unit.Percent),
                    y = new Dimension(0f, Dimension.Unit.Percent)
                };
            }
        }

        public static BuilderTranslate GetTranslate(this StyleSheet styleSheet, StyleProperty styleProperty)
        {
            int valCount = styleProperty.values.Length;

            if (valCount > 0)
            {
                var xval = new StylePropertyValue() { handle = styleProperty.values[0], sheet = styleSheet };
                var yval = valCount > 1 ? new StylePropertyValue() { handle = styleProperty.values[1], sheet = styleSheet } : default;
                var zval = valCount > 2 ? new StylePropertyValue() { handle = styleProperty.values[2], sheet = styleSheet } : default;

                return new BuilderTranslate(StylePropertyReader.ReadTranslate(valCount, xval, yval, zval));
            }
            else
            {

                return new BuilderTranslate
                {
                    x = new Dimension(0f, Dimension.Unit.Percent),
                    y = new Dimension(0f, Dimension.Unit.Percent)
                };
            }
        }

        public static BuilderRotate GetRotate(this StyleSheet styleSheet, StyleProperty styleProperty)
        {
            int valCount = styleProperty.values.Length;

            if (valCount > 0)
            {
                var val1 = new StylePropertyValue() { handle = styleProperty.values[0], sheet = styleSheet };
                var val2 = valCount > 1 ? new StylePropertyValue() { handle = styleProperty.values[1], sheet = styleSheet } : default;
                var val3 = valCount > 2 ? new StylePropertyValue() { handle = styleProperty.values[2], sheet = styleSheet } : default;
                var val4 = valCount > 3 ? new StylePropertyValue() { handle = styleProperty.values[3], sheet = styleSheet } : default;

                return new BuilderRotate(StylePropertyReader.ReadRotate(valCount, val1, val2, val3, val4));
            }
            else
            {

                return new BuilderRotate
                {
                    x = new Dimension(0f, Dimension.Unit.Degree)
                };
            }
        }

        public static BuilderScale GetScale(this StyleSheet styleSheet, StyleProperty styleProperty)
        {
            int valCount = styleProperty.values.Length;

            if (valCount > 0)
            {
                var val1 = new StylePropertyValue() { handle = styleProperty.values[0], sheet = styleSheet };
                var val2 = valCount > 1 ? new StylePropertyValue() { handle = styleProperty.values[1], sheet = styleSheet } : default;
                var zval = valCount > 2 ? new StylePropertyValue() { handle = styleProperty.values[2], sheet = styleSheet } : default;

                return new BuilderScale(StylePropertyReader.ReadScale(valCount, val1, val2, zval));
            }
            else
            {

                return new BuilderScale
                {
                    value = Vector3.one
                };
            }
        }

        public static BuilderTextShadow GetTextShadow(this StyleSheet styleSheet, StyleProperty styleProperty)
        {
            Dimension offsetX = new Dimension(0f, Dimension.Unit.Pixel);
            Dimension offsetY = new Dimension(0f, Dimension.Unit.Pixel);
            Dimension blurRadius = new Dimension(0f, Dimension.Unit.Pixel);
            var color = Color.clear;

            var valueCount = styleProperty.values.Length;

            if (valueCount >= 2)
            {
                int i = 0;
                var valueType = styleProperty.values[i].valueType;
                bool wasColorFound = false;
                if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                {
                    color = styleSheet.GetColor(styleProperty.values[i++]);
                    wasColorFound = true;
                }

                if (i + 1 < valueCount)
                {
                    valueType = styleProperty.values[i].valueType;
                    var valueType2 = styleProperty.values[i + 1].valueType;
                    if ((valueType == StyleValueType.Dimension || valueType == StyleValueType.Float) &&
                        (valueType2 == StyleValueType.Dimension || valueType2 == StyleValueType.Float))
                    {
                        var valueX = styleProperty.values[i++];
                        var valueY = styleProperty.values[i++];

                        offsetX = styleSheet.GetDimension(valueX);
                        offsetY = styleSheet.GetDimension(valueY);
                    }
                }

                if (i < valueCount)
                {
                    valueType = styleProperty.values[i].valueType;
                    if (valueType == StyleValueType.Dimension || valueType == StyleValueType.Float)
                    {
                        var valueBlur = styleProperty.values[i++];
                        blurRadius = styleSheet.GetDimension(valueBlur);
                    }
                    else if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        if (!wasColorFound)
                            color = styleSheet.GetColor(styleProperty.values[i]);
                    }
                }

                if (i < valueCount)
                {
                    valueType = styleProperty.values[i].valueType;
                    if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        if (!wasColorFound)
                            color = styleSheet.GetColor(styleProperty.values[i]);
                    }
                }
            }

            return new BuilderTextShadow
            {
                offsetX = offsetX,
                offsetY = offsetY,
                blurRadius = blurRadius,
                color = color
            };
        }

        public static Object GetAsset(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            switch (valueHandle.valueType)
            {
                case StyleValueType.ResourcePath:
                    var resourcePath = styleSheet.strings[valueHandle.valueIndex];
                    var asset = Resources.Load<Object>(resourcePath);
                    return asset;
                case StyleValueType.Keyword:
                    return null;
                default:
                    return styleSheet.assets[valueHandle.valueIndex];
            }
        }

        public static StyleValueFunction GetFunction(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadFunction(valueHandle);
        }

        public static string GetVariable(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadVariable(valueHandle);
        }

        public static string GetEnum(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            return styleSheet.ReadEnum(valueHandle);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, StyleValueKeyword value)
        {
            throw new InvalidOperationException("Style value cannot be set if its a keyword!");
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, float value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.floats[valueHandle.valueIndex] = value;
            styleSheet.UpdateContentHash();
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Dimension value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.dimensions[valueHandle.valueIndex] = value;
            styleSheet.UpdateContentHash();
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Color value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.colors[valueHandle.valueIndex] = value;
            styleSheet.UpdateContentHash();
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            styleSheet.strings[valueHandle.valueIndex] = value;
            styleSheet.UpdateContentHash();
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Object value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (valueHandle.valueType == StyleValueType.ResourcePath)
            {
                var resourcesPath = BuilderAssetUtilities.GetResourcesPathForAsset(value);
                styleSheet.strings[valueHandle.valueIndex] = resourcesPath;
            }
            else
            {
                styleSheet.assets[valueHandle.valueIndex] = value;
            }
            styleSheet.UpdateContentHash();
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Enum value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var newEnumStr = value.ToString();
            var strValue = BuilderNameUtilities.ConvertCamelToDash(newEnumStr);
            styleSheet.strings[valueHandle.valueIndex] = strValue;
            styleSheet.UpdateContentHash();
        }
    }
}
