// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlUtility
    {
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

        public static void MoveListItem(IList list, int src, int dst)
        {
            var item = list[src];
            list.RemoveAt(src);
            list.Insert(dst, item);
        }

        public static float ParseFloat(string value, float defaultValue = default)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : defaultValue;
        }

        public static int ParseInt(string value, int defaultValue = default)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : defaultValue;
        }

        public static uint ParseUint(string value, uint defaultValue = default)
        {
            return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : defaultValue;
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

        public static string ValueToString(Bounds value) => $"{value.center.x},{value.center.y},{value.center.z},{value.size.x},{value.size.y},{value.size.z}";
        public static string ValueToString(BoundsInt value) => $"{value.position.x},{value.position.y},{value.position.z},{value.size.x},{value.size.y},{value.size.z}";
        public static string ValueToString(Rect value) => $"{value.x},{value.y},{value.width},{value.height}";
        public static string ValueToString(RectInt value) => $"{value.x},{value.y},{value.width},{value.height}";
        public static string ValueToString(Vector2 value) => $"{value.x},{value.y}";
        public static string ValueToString(Vector2Int value) => $"{value.x},{value.y}";
        public static string ValueToString(Vector3 value) => $"{value.x},{value.y},{value.z}";
        public static string ValueToString(Vector3Int value) => $"{value.x},{value.y},{value.z}";
        public static string ValueToString(Vector4 value) => $"{value.x},{value.y},{value.z},{value.w}";
              
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
    }
}
