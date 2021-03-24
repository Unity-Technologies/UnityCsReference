// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct VersionRanges<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        private readonly Dictionary<ExpressionTypeKey, ExpressionTypeValue<TVersion>> m_ExpressionTypes;
        private static readonly char[] k_LeftValidSymbols = new[] { '[', '(', };
        private static readonly char[] k_RightValidSymbols = new[] { ']', ')', };
        private static readonly TVersion m_versionTypeStaticFunctionalityProxy;

        static VersionRanges()
        {
            // String representations of Version Types must not allow any of these characters, because
            // they have other meaning inside a version range expression.
            m_versionTypeStaticFunctionalityProxy = new TVersion();
            IVersionTypeTraits versionTypeTraits = m_versionTypeStaticFunctionalityProxy.GetVersionTypeTraits();
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter('*'));
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter(','));
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter('['));
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter('('));
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter(']'));
            Assert.IsFalse(versionTypeTraits.IsAllowedCharacter(')'));
        }

        public VersionRanges(Dictionary<ExpressionTypeKey, ExpressionTypeValue<TVersion>> expressionTypes)
        {
            m_ExpressionTypes = expressionTypes;
        }

        public VersionDefineExpression<TVersion> GetExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            ExpressionParsedData parsedExpressionData = ParseExpression(expression);
            if (m_ExpressionTypes.ContainsKey(parsedExpressionData.GenerateExpressionTypeKey))
            {
                ExpressionTypeValue<TVersion> expressionTypeValue = m_ExpressionTypes[parsedExpressionData.GenerateExpressionTypeKey];

                return new VersionDefineExpression<TVersion>(expressionTypeValue.IsValid, parsedExpressionData.leftVersion, parsedExpressionData.rightVersion)
                {
                    AppliedRule = expressionTypeValue.AppliedRule,
                };
            }
            throw new ExpressionNotValidException($"'{expression}' is not a valid expression");
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

        private ExpressionParsedData ParseExpression(string expression)
        {
            ExpressionParsedData expressionParsedData = new ExpressionParsedData();

            if (expression.Length == 0)
            {
                return expressionParsedData;
            }

            IVersionTypeTraits versionTypeTraits = m_versionTypeStaticFunctionalityProxy.GetVersionTypeTraits();

            bool hasSeperator = Contains(expression, ',');
            char leftSymbol = default(char);
            char rightSymbol = default(char);

            int begin = 0;
            int end = expression.Length - 1;

            if (!versionTypeTraits.IsAllowedFirstCharacter(expression[0]))
            {
                leftSymbol = expression[0];
                if (!Contains(k_LeftValidSymbols, leftSymbol))
                {
                    throw new ExpressionNotValidException($"Invalid character '{leftSymbol}' in expression", expression);
                }

                begin++;
            }

            var lastChar = expression[end];
            if (!versionTypeTraits.IsAllowedLastCharacter(lastChar))
            {
                rightSymbol = lastChar;

                if (!Contains(k_RightValidSymbols, rightSymbol))
                {
                    throw new ExpressionNotValidException($"Invalid character '{rightSymbol}' in expression", expression);
                }

                end--;
            }

            if ((leftSymbol != default(char) && rightSymbol == default(char)) ||
                (leftSymbol == default(char) && rightSymbol != default(char)))
            {
                throw new ExpressionNotValidException("Incomplete expression, missing symbol in start or end", expression);
            }

            int nextVersion;
            string leftVersionString = PopVersionString(expression, begin, end, out nextVersion, versionTypeTraits);
            var hasLeftVersion = !string.IsNullOrEmpty(leftVersionString);
            if (hasLeftVersion)
            {
                expressionParsedData.leftVersion = (TVersion)m_versionTypeStaticFunctionalityProxy.Parse(leftVersionString);
            }

            int notNeeded;
            string rightVersionString = PopVersionString(expression, nextVersion, end, out notNeeded, versionTypeTraits);
            var hasRightVersion = !string.IsNullOrEmpty(rightVersionString);
            if (hasRightVersion)
            {
                expressionParsedData.rightVersion = (TVersion)m_versionTypeStaticFunctionalityProxy.Parse(rightVersionString);
            }
            expressionParsedData.GenerateExpressionTypeKey = new ExpressionTypeKey(leftSymbol: leftSymbol, rightSymbol: rightSymbol, hasSeparator: hasSeperator, hasLeftVersion: hasLeftVersion, hasRightVersion: hasRightVersion);
            return expressionParsedData;
        }

        private static string PopVersionString(string expression, int begin, int end, out int newBegin, IVersionTypeTraits versionTypeTraits)
        {
            newBegin = begin;
            if (begin > end)
            {
                return null;
            }

            int count = 0;
            while (newBegin <= end)
            {
                var value = expression[newBegin];

                if (value == ',')
                {
                    newBegin++;
                    break;
                }

                if (!versionTypeTraits.IsAllowedCharacter(value) && value != '*')
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
            public TVersion leftVersion;
            public TVersion rightVersion;
            public ExpressionTypeKey GenerateExpressionTypeKey;
        }
    }
}
