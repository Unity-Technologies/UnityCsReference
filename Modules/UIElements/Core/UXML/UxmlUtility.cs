// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class UxmlUtility
    {
        const string s_CommaEncoded = "%2C";

        public static List<string> ParseStringListAttribute(string itemList)
        {
            if (string.IsNullOrEmpty(itemList?.Trim()))
                return null;

            // Here the choices is comma separated in the string...
            var items = itemList.Split(',');

            if (items.Length != 0)
            {
                var result = new List<string>();
                foreach (var item in items)
                {
                    result.Add(item.Trim());
                }

                return result;
            }

            return null;
        }

        public static string EncodeListItem(string item)
        {
            return item == null ? string.Empty : item.Replace(",", s_CommaEncoded);
        }

        public static string DecodeListItem(string item)
        {
            return item.Replace(s_CommaEncoded, ",");
        }

        public static void MoveListItem(IList list, int src, int dst)
        {
            var item = list[src];
            list.RemoveAt(src);
            list.Insert(dst, item);
        }

        public static float ParseFloat(string value)
        {
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static byte ParseByte(string value)
        {
            return byte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static sbyte ParseSByte(string value)
        {
            return sbyte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static short ParseShort(string value)
        {
            return short.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static ushort ParseUShort(string value)
        {
            return ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static bool TryParse(string value, out int result, out string error)
        {
            try
            {
                result = ParseInt(value);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error =  ex.Message;
                result = default;
                return false;
            }
        }

        public static uint ParseUint(string value)
        {
            return uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static long ParseLong(string value)
        {
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static ulong ParseULong(string value)
        {
            return ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static Angle ParseAngle(string value, Angle defaultValue = default)
        {
            return Angle.TryParseString(value, out var angle) ? angle : defaultValue;
        }

        public static float TryParseFloatAttribute(string attributeName, IUxmlAttributes bag, ref int foundAttributeCounter)
        {
            if (bag.TryGetAttributeValue(attributeName, out var value))
            {
                foundAttributeCounter++;
                return ParseFloat(value);
            }

            return default;
        }

        public static int TryParseIntAttribute(string attributeName, IUxmlAttributes bag, ref int foundAttributeCounter)
        {
            if (bag.TryGetAttributeValue(attributeName, out var value))
            {
                foundAttributeCounter++;
                return ParseInt(value);
            }

            return default;
        }

        public static Type ParseType(string value, Type defaultType = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var type = Type.GetType(value, true);
                    return type;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return defaultType;
        }

        /// <summary>
        /// Checks that the name conforms to the `XML Naming Rules` https://www.w3schools.com/xml/xml_elements.asp
        /// </summary>
        /// <param name="name"></param>
        /// <returns>null or an error message.</returns>
        public static string ValidateUxmlName(string name)
        {
            // Element names must start with a letter or underscore.
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return "Element names must start with a letter or underscore";

            // Element names cannot start with the letters xml (or XML, or Xml, etc).
            if (name.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
                return "Element names cannot start with the letters xml (or XML, or Xml, etc)";

            // Element names cannot contain spaces.
            // Element names can contain letters, digits, hyphens, underscores, and periods.
            // We can skip the first character since we already checked it earlier.
            for(int i = 1; i < name.Length; ++i)
            {
                var c = name[i];
                if (char.IsWhiteSpace(c) ||
                    (!char.IsLetterOrDigit(c) &&
                    c != '-' &&
                    c != '_' &&
                    c != '.'))
                {
                    return $"The character '{c}' is invalid. Element names can contain letters, digits, hyphens, underscores, and periods.";
                }
            }

            return null;
        }

        public static string TypeToString(Type value)
        {
            if (value == null)
                return null;
            return $"{value.FullName}, {value.Assembly.GetName().Name}";
        }

        public static string ValueToString(Bounds value) => FormattableString.Invariant($"{value.center.x},{value.center.y},{value.center.z},{value.size.x},{value.size.y},{value.size.z}");
        public static string ValueToString(BoundsInt value) => FormattableString.Invariant($"{value.position.x},{value.position.y},{value.position.z},{value.size.x},{value.size.y},{value.size.z}");
        public static string ValueToString(Rect value) => FormattableString.Invariant($"{value.x},{value.y},{value.width},{value.height}");
        public static string ValueToString(RectInt value) => FormattableString.Invariant($"{value.x},{value.y},{value.width},{value.height}");
        public static string ValueToString(Vector2 value) => FormattableString.Invariant($"{value.x},{value.y}");
        public static string ValueToString(Vector2Int value) => FormattableString.Invariant($"{value.x},{value.y}");
        public static string ValueToString(Vector3 value) => FormattableString.Invariant($"{value.x},{value.y},{value.z}");
        public static string ValueToString(Vector3Int value) => FormattableString.Invariant($"{value.x},{value.y},{value.z}");
        public static string ValueToString(Vector4 value) => FormattableString.Invariant($"{value.x},{value.y},{value.z},{value.w}");

        public static object CloneObject(object value)
        {
            if (value != null &&
                value is not string &&
                value is not Type &&
                value.GetType().IsClass)
            {
                return UxmlSerializedDataUtility.CopySerialized(value);
            }
            return value;
        }

        public static int SplitValues(ReadOnlySpan<char> spanStr, Span<float> values, char separator)
        {
            var valueCount = 0;
            var lastIndex = 0;

            for (var i = 0; i <= spanStr.Length; i++)
            {
                // Check for space or end of string to parse a float
                if (i == spanStr.Length || spanStr[i] == separator)
                {
                    if (lastIndex < i && valueCount < values.Length) // Avoid empty segments
                    {
                        // Try parsing the float directly from the slice
                        if (float.TryParse(spanStr.Slice(lastIndex, i - lastIndex), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result))
                        {
                            values[valueCount++] = result; // Store the parsed float
                        }
                    }

                    lastIndex = i + 1; // Move to the next part
                }
            }

            return valueCount;
        }
    }
}
