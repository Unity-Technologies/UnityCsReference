using System;
using System.Globalization;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    static class StyleSheetExtensions
    {
        public static string ReadAsString(this StyleSheet sheet, StyleValueHandle handle)
        {
            string value = string.Empty;

            switch (handle.valueType)
            {
                case StyleValueType.Float:
                    value = sheet.ReadFloat(handle).ToString(CultureInfo.InvariantCulture.NumberFormat);
                    break;
                case StyleValueType.Dimension:
                    value = sheet.ReadDimension(handle).ToString();
                    break;
                case StyleValueType.Color:
                    value = sheet.ReadColor(handle).ToString();
                    break;
                case StyleValueType.ResourcePath:
                    value = sheet.ReadResourcePath(handle);
                    break;
                case StyleValueType.String:
                    value = sheet.ReadString(handle);
                    break;
                case StyleValueType.Enum:
                    value = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.Variable:
                    value = sheet.ReadVariable(handle);
                    break;
                case StyleValueType.Keyword:
                    value = sheet.ReadKeyword(handle).ToUssString();
                    break;
                case StyleValueType.AssetReference:
                    value = sheet.ReadAssetReference(handle).ToString();
                    break;
                case StyleValueType.Function:
                    value = sheet.ReadFunctionName(handle);
                    break;
                case StyleValueType.FunctionSeparator:
                    value = ",";
                    break;
                case StyleValueType.ScalableImage:
                    value = sheet.ReadScalableImage(handle).ToString();
                    break;
                default:
                    value = "Error reading value type (" + handle.valueType + ") at index " + handle.valueIndex;
                    break;
            }

            return value;
        }

        public static bool IsVarFunction(this StyleValueHandle handle)
        {
            return handle.valueType == StyleValueType.Function && (StyleValueFunction)handle.valueIndex == StyleValueFunction.Var;
        }
    }
}
