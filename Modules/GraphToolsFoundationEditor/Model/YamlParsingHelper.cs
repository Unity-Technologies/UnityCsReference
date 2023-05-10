// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    static class YamlParsingHelper_Internal
    {
        public static bool TryParseVector2(string data, string field, int startingIndex, out Vector2 parsedVector2)
        {
            return TryParseVector2Format1(data, field, startingIndex, out parsedVector2) || TryParseVector2Format2(data, field, startingIndex, out parsedVector2);
        }

        public static bool TryParseLong(string data, string field, int startingIndex, out long parsedLong)
        {
            parsedLong = 0;

            if (TryParseLine(field, data, startingIndex, out var line))
            {
                var whitespace = line.IndexOf("(");

                if (whitespace != -1)
                {
                    line = line[..whitespace];
                }
            }

            return long.TryParse(line.Trim(), out parsedLong);
        }

        public static bool TryParseGUID(string data, string hashGuidFieldName, string obsoleteGuidFieldName, int startingIndex, out Hash128 parsedGuid)
        {
            return TryParseHash128Format1(data, hashGuidFieldName, startingIndex, out parsedGuid) ||
                TryParseHash128Format2(data, hashGuidFieldName, startingIndex, out parsedGuid) ||
                TryParseSerializableGUIDFormat1(data, obsoleteGuidFieldName, startingIndex, out parsedGuid) ||
                TryParseSerializableGUIDFormat2(data, obsoleteGuidFieldName, startingIndex, out parsedGuid);
        }

        public static bool TryParseString(string data, string field, int startingIndex, out string parsedString)
        {
            return TryParseStringFormat1(data, field, startingIndex, out parsedString) || TryParseStringFormat2(data, field, startingIndex, out parsedString);
        }

        public static bool TryParseList(string listField, string elementField, string data, int parentIndex, out List<string> listStr)
        {
            listStr = new List<string>();

            var fieldIndex = data.IndexOf(listField, parentIndex, StringComparison.Ordinal);
            var nextEndOfLineIndex = data.IndexOf("\n", fieldIndex, StringComparison.Ordinal);

            switch (data[nextEndOfLineIndex + 1])
            {
                case '\t':
                {
                    while (data[nextEndOfLineIndex + 1] == '\t')
                    {
                        var elementStr = GetElementStr(nextEndOfLineIndex);
                        listStr.Add(elementStr);
                        var index = data.IndexOf(elementStr, nextEndOfLineIndex, StringComparison.Ordinal);

                        if (index != -1)
                            nextEndOfLineIndex = data.IndexOf("\n", index, StringComparison.Ordinal);
                    }

                    break;
                }
                case '-':
                {
                    while (data[nextEndOfLineIndex + 1] == '-')
                    {
                        var elementStr = GetElementStr(nextEndOfLineIndex);
                        elementStr = elementStr.Replace(":", string.Empty).Trim();
                        listStr.Add(elementStr);
                        var index = data.IndexOf(elementStr, nextEndOfLineIndex, StringComparison.Ordinal);

                        if (index != -1)
                            nextEndOfLineIndex = data.IndexOf("\n", index, StringComparison.Ordinal);
                    }

                    break;
                }
            }

            return true;

            string GetElementStr(int lastLineIndex)
            {
                var dataIndex = data.IndexOf("data", lastLineIndex, StringComparison.Ordinal);
                string elementStr;
                if (dataIndex != -1)
                {
                    var startLineIndex = data.IndexOf(elementField, dataIndex, StringComparison.Ordinal) + elementField.Length;
                    var endLineIndex = data.IndexOf("(", startLineIndex, StringComparison.Ordinal);
                    elementStr = data.Substring(startLineIndex, endLineIndex - startLineIndex).Trim();
                }
                else
                {
                    var elementIndex = data.IndexOf(elementField, lastLineIndex, StringComparison.Ordinal);
                    var twoDotsIndex = data.IndexOf(":", elementIndex, StringComparison.Ordinal);
                    var endLineIndex = data.IndexOf("\n", twoDotsIndex, StringComparison.Ordinal);

                    elementStr = data.Substring(twoDotsIndex, endLineIndex - twoDotsIndex);
                }

                return elementStr;
            }
        }

        static bool TryParseVector2Format1(string data, string field, int startingIndex, out Vector2 parsedVector2)
        {
            // For this format, example => m_Position (1016 220) (Vector2f)

            parsedVector2 = Vector2.zero;

            var positionIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (positionIndex == -1)
                return false;

            var openingParenthesisIndex = data.IndexOf("(", positionIndex, StringComparison.Ordinal);
            if (openingParenthesisIndex == -1)
                return false;

            var spaceIndex = data.IndexOf(" ", openingParenthesisIndex, StringComparison.Ordinal);
            if (spaceIndex == -1)
                return false;

            var closingParenthesisIndex = data.IndexOf(")", spaceIndex, StringComparison.Ordinal);
            if (closingParenthesisIndex == -1)
                return false;

            var posX = float.Parse(data.Substring(openingParenthesisIndex + 1, spaceIndex - openingParenthesisIndex), CultureInfo.CurrentCulture.NumberFormat);
            var posY = float.Parse(data.Substring(spaceIndex + 1, closingParenthesisIndex - spaceIndex - 1), CultureInfo.CurrentCulture.NumberFormat);

            parsedVector2 = new Vector2(posX, posY);

            return true;
        }

        static bool TryParseVector2Format2(string data, string field, int startingIndex, out Vector2 parsedVector2)
        {
            // For this format, example => m_Position: {x: 522, y: 106}

            parsedVector2 = Vector2.zero;

            var fieldIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (fieldIndex == -1)
                return false;

            if (TryParseLine(field, data, fieldIndex, out var valueStr))
            {
                const string xStr = "x: ";
                var startXValueIndex = valueStr.IndexOf(xStr, 0, StringComparison.Ordinal) + xStr.Length;
                if (startXValueIndex == -1)
                    return false;

                var endXValueIndex = valueStr.IndexOf(",", startXValueIndex, StringComparison.Ordinal);
                if (endXValueIndex == -1)
                    return false;

                const string yStr = "y: ";
                var startYValueIndex = valueStr.IndexOf(yStr, endXValueIndex, StringComparison.Ordinal) + yStr.Length;
                if (startYValueIndex == -1)
                    return false;

                var endYValueIndex = valueStr.IndexOf("}", startYValueIndex, StringComparison.Ordinal);
                if (endYValueIndex == -1)
                    return false;

                var xValue = float.Parse(valueStr.Substring(startXValueIndex, endXValueIndex - startXValueIndex), CultureInfo.CurrentCulture.NumberFormat);
                var yValue = float.Parse(valueStr.Substring(startYValueIndex, endYValueIndex - startYValueIndex), CultureInfo.CurrentCulture.NumberFormat);

                parsedVector2 = new Vector2(xValue, yValue);

                return true;
            }

            return false;
        }

        static bool TryParseSerializableGUIDFormat1(string data, string field, int startingIndex, out Hash128 parsedGuid)
        {
            // For this format, example =>
            // m_Guid  (SerializableGUID)
            //      m_Value0 2209188150080794339 (UInt64)
            //      m_Value1 10894006200981209430 (UInt64)

            parsedGuid = new Hash128();

            var guidIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (guidIndex == -1)
                return false;

            var firstValueIndex = data.IndexOf("m_Value0 ", guidIndex, StringComparison.Ordinal) + "m_Value0 ".Length;
            if (firstValueIndex == -1)
                return false;

            var firstSpaceIndex = data.IndexOf(" ", firstValueIndex, StringComparison.Ordinal);
            if (firstSpaceIndex == -1)
                return false;

            var secondValueIndex = data.IndexOf("m_Value1 ", firstValueIndex, StringComparison.Ordinal) + "m_Value1 ".Length;
            if (secondValueIndex == -1)
                return false;

            var secondSpaceIndex = data.IndexOf(" ", secondValueIndex, StringComparison.Ordinal);
            if (secondSpaceIndex == -1)
                return false;

            if (!ulong.TryParse(data.Substring(firstValueIndex, firstSpaceIndex - firstValueIndex), out var firstValue))
                return false;

            if (!ulong.TryParse(data.Substring(secondValueIndex, secondSpaceIndex - secondValueIndex), out var secondValue))
                return false;

            parsedGuid = new Hash128(firstValue, secondValue);

            return true;
        }

        static bool TryParseSerializableGUIDFormat2(string data, string field, int startingIndex, out Hash128 parsedGuid)
        {
            // For this format, example =>
            // m_Guid:
            // m_Value0: 6053792795968521525
            // m_Value1: 1379497253815963307

            parsedGuid = new Hash128();

            var guidIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (guidIndex == -1)
                return false;

            if (TryParseLine("m_Value0", data, guidIndex, out var firstValueStr) && TryParseLine("m_Value1", data, guidIndex, out var secondValueStr))
            {
                if (ulong.TryParse(firstValueStr, out var firstValue) && ulong.TryParse(secondValueStr, out var secondValue) && firstValue != 0 && secondValue != 0)
                {
                    parsedGuid = new Hash128(firstValue, secondValue);
                    return true;
                }
            }

            return false;
        }

        static bool TryParseHash128Format1(string data, string field, int startingIndex, out Hash128 parsedGuid)
        {
            // For this format, example =>
            // m_Guid  (SerializableGUID)
            //      m_Value0 2209188150080794339 (UInt64)
            //      m_Value1 10894006200981209430 (UInt64)

            parsedGuid = new Hash128();

            var guidIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (guidIndex == -1)
                return false;

            var firstValueIndex = data.IndexOf("u64_0 ", guidIndex, StringComparison.Ordinal) + "u64_0 ".Length;
            if (firstValueIndex == -1)
                return false;

            var firstSpaceIndex = data.IndexOf(" ", firstValueIndex, StringComparison.Ordinal);
            if (firstSpaceIndex == -1)
                return false;

            var secondValueIndex = data.IndexOf("u64_1 ", firstValueIndex, StringComparison.Ordinal) + "u64_1 ".Length;
            if (secondValueIndex == -1)
                return false;

            var secondSpaceIndex = data.IndexOf(" ", secondValueIndex, StringComparison.Ordinal);
            if (secondSpaceIndex == -1)
                return false;

            if (!ulong.TryParse(data.Substring(firstValueIndex, firstSpaceIndex - firstValueIndex), out var firstValue))
                return false;

            if (!ulong.TryParse(data.Substring(secondValueIndex, secondSpaceIndex - secondValueIndex), out var secondValue))
                return false;

            parsedGuid = new Hash128(firstValue, secondValue);

            return true;
        }

        static bool TryParseHash128Format2(string data, string field, int startingIndex, out Hash128 parsedGuid)
        {
            // For this format, example =>
            // m_Guid:
            // m_Value0: 6053792795968521525
            // m_Value1: 1379497253815963307

            parsedGuid = new Hash128();

            var guidIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (guidIndex == -1)
                return false;

            if (TryParseLine("u64_0", data, guidIndex, out var firstValueStr) && TryParseLine("u64_1", data, guidIndex, out var secondValueStr))
            {
                if (ulong.TryParse(firstValueStr, out var firstValue) && ulong.TryParse(secondValueStr, out var secondValue) && firstValue != 0 && secondValue != 0)
                {
                    parsedGuid = new Hash128(firstValue, secondValue);
                    return true;
                }
            }

            return false;
        }

        static bool TryParseStringFormat1(string data, string field, int startingIndex, out string parsedString)
        {
            // For this format, example => m_Name "Math Book" (string)
            parsedString = "";

            var titleIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);
            if (titleIndex == -1)
                return false;

            var firstQuotationIndex = data.IndexOf("\"", titleIndex, StringComparison.Ordinal) + 1;
            if (firstQuotationIndex == -1)
                return false;

            var secondQuotationIndex = data.IndexOf("\"", firstQuotationIndex, StringComparison.Ordinal);
            if (secondQuotationIndex == -1)
                return false;

            parsedString = data.Substring(firstQuotationIndex, secondQuotationIndex - firstQuotationIndex);

            return true;
        }

        static bool TryParseStringFormat2(string data, string field, int startingIndex, out string parsedString)
        {
            // For this format, example => m_Name: Math Book
            parsedString = "";

            var titleIndex = data.IndexOf(field, startingIndex, StringComparison.Ordinal);

            return titleIndex != -1 && TryParseLine(field, data, titleIndex, out parsedString);
        }

        static bool TryParseLine(string field, string data, int parentIndex, out string valueStr)
        {
            valueStr = null;

            var fieldStr = $"{field}: ";

            var fieldIndex = data.IndexOf(fieldStr, parentIndex, StringComparison.Ordinal);
            if (fieldIndex == -1)
            {
                fieldStr = field;
                fieldIndex = data.IndexOf(fieldStr, parentIndex, StringComparison.Ordinal);

                if (fieldIndex == -1)
                    return false;
            }

            fieldIndex += fieldStr.Length;
            var endOfLineIndex = data.IndexOf("\n", fieldIndex, StringComparison.Ordinal);
            if (endOfLineIndex == -1)
                return false;

            valueStr = data.Substring(fieldIndex, endOfLineIndex - fieldIndex);

            return true;
        }
    }
}
