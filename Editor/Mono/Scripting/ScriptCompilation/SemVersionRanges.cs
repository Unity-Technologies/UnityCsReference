// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct SemVersionRanges
    {
        private readonly Dictionary<ExpressionTypeKey, ExpressionTypeValue> m_ExpressionTypes;
        private static readonly char[] k_LeftValidSymbols = new[] { '[', '(', };
        private static readonly char[] k_RightValidSymbols = new[] { ']', ')', };

        public SemVersionRanges(Dictionary<ExpressionTypeKey, ExpressionTypeValue> expressionTypes)
        {
            m_ExpressionTypes = expressionTypes;
        }

        public VersionDefineExpression GetExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            ExpressionParsedData parsedExpressionData = ParseExpression(expression);
            if (m_ExpressionTypes.ContainsKey(parsedExpressionData.GenerateExpressionTypeKey))
            {
                ExpressionTypeValue expressionTypeValue = m_ExpressionTypes[parsedExpressionData.GenerateExpressionTypeKey];

                return new VersionDefineExpression(expressionTypeValue.IsValid, parsedExpressionData.leftSemVersion, parsedExpressionData.rightSemVersion)
                {
                    AppliedRule = expressionTypeValue.AppliedRule,
                };
            }
            throw new ArgumentException("Not Found");
        }

        private static bool Contains(string array, char doContain)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == doContain)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(char[] array, char doContain)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == doContain)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCharDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        private static bool IsCharLetter(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        private ExpressionParsedData ParseExpression(string expression)
        {
            ExpressionParsedData expressionParsedData = new ExpressionParsedData();

            if (expression.Length == 0)
            {
                return expressionParsedData;
            }

            bool hasSeperator = Contains(expression, ',');
            char leftSymbol = default(char);
            char rightSymbol = default(char);

            int begin = 0;
            int end = expression.Length - 1;

            if (!IsCharDigit(expression[0]))
            {
                leftSymbol = expression[0];
                if (!Contains(k_LeftValidSymbols, leftSymbol))
                {
                    throw new ExpressionNotValidException($"Invalid character {leftSymbol}", expression);
                }

                begin++;
            }

            var lastChar = expression[end];
            if (!IsCharDigit(lastChar) && !IsCharLetter(lastChar))
            {
                rightSymbol = lastChar;

                if (!Contains(k_RightValidSymbols, rightSymbol))
                {
                    throw new ExpressionNotValidException($"Invalid character {rightSymbol}", expression);
                }

                end--;
            }

            if (leftSymbol != default(char) && rightSymbol == default(char)
                || leftSymbol == default(char) && rightSymbol != default(char))
            {
                throw new ExpressionNotValidException("Incomplete expression, missing symbol in start or end", expression);
            }

            int nextSemVer;
            string leftSemVerString = PopSemanticVersion(expression, begin, end, out nextSemVer);
            var hasLeftSemVer = !string.IsNullOrEmpty(leftSemVerString);
            if (hasLeftSemVer)
            {
                expressionParsedData.leftSemVersion = SemVersionParser.Parse(leftSemVerString);
            }

            int notNeeded;
            string rightSemVerString = PopSemanticVersion(expression, nextSemVer, end, out notNeeded);
            var hasRightSemVer = !string.IsNullOrEmpty(rightSemVerString);
            if (hasRightSemVer)
            {
                expressionParsedData.rightSemVersion = SemVersionParser.Parse(rightSemVerString);
            }
            expressionParsedData.GenerateExpressionTypeKey = new ExpressionTypeKey(leftSymbol: leftSymbol, rightSymbol: rightSymbol, hasSeparator: hasSeperator, hasLeftSemVer: hasLeftSemVer, hasRightSemVer: hasRightSemVer);
            return expressionParsedData;
        }

        private static string PopSemanticVersion(string expression, int begin, int end, out int newBegin)
        {
            newBegin = begin;
            if (begin >= end)
            {
                return null;
            }

            int count = 0;
            while (newBegin <= end)
            {
                if (expression[newBegin] == ',')
                {
                    newBegin++;
                    break;
                }

                var value = expression[newBegin];

                if (!IsCharDigit(value) && value != '.'
                    && value != '-' && !IsCharLetter(value) && value != '*')
                {
                    throw new ExpressionNotValidException($"'{value}' is not valid in the expression");
                }
                count++;
                newBegin++;
            }

            return expression.Substring(begin, count);
        }

        private struct ExpressionParsedData
        {
            public SemVersion leftSemVersion;
            public SemVersion rightSemVersion;
            public ExpressionTypeKey GenerateExpressionTypeKey;
        }
    }
}
