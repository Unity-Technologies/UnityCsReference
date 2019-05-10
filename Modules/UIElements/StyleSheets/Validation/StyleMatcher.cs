// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements.StyleSheets
{
    internal enum MatchResultErrorCode
    {
        None,
        Syntax,
        EmptyValue,
        ExpectedEndOfValue
    }

    internal struct MatchResult
    {
        public MatchResultErrorCode errorCode;
        public string errorValue;
        public bool success { get { return errorCode == MatchResultErrorCode.None; } }
    }

    internal class StyleMatcher
    {
        private MatchContext m_Context = new MatchContext();

        public MatchResult Match(Expression exp, string propertyValue)
        {
            var result = new MatchResult() {errorCode = MatchResultErrorCode.None};
            if (string.IsNullOrEmpty(propertyValue))
            {
                result.errorCode = MatchResultErrorCode.EmptyValue;
                return result;
            }

            bool match = false;
            m_Context.Initialize(propertyValue);

            // Handle global keyword and env()
            var firstValue = m_Context.current;
            if (firstValue == "initial" || firstValue.StartsWith("env("))
            {
                m_Context.MoveNext();
                match = true;
            }
            else
            {
                match = Match(exp, m_Context);
            }

            if (!match)
            {
                result.errorCode = MatchResultErrorCode.Syntax;
                result.errorValue = m_Context.current;
            }
            else if (m_Context.hasCurrent)
            {
                result.errorCode = MatchResultErrorCode.ExpectedEndOfValue;
                result.errorValue = m_Context.current;
            }

            return result;
        }

        private static bool Match(Expression exp, MatchContext context)
        {
            bool result = true;
            if (exp.multiplier.type == ExpressionMultiplierType.None)
            {
                result = MatchExpression(exp, context);
            }
            else
            {
                Debug.Assert(exp.multiplier.type != ExpressionMultiplierType.OneOrMoreComma, "'#' multiplier in syntax expression is not supported");
                Debug.Assert(exp.multiplier.type != ExpressionMultiplierType.GroupAtLeastOne, "'!' multiplier in syntax expression is not supported");

                int min = exp.multiplier.min;
                int max = exp.multiplier.max;

                int matchCount = 0;
                for (int i = 0; result && context.hasCurrent && i < max; i++)
                {
                    result = MatchExpression(exp, context);
                    if (result)
                        ++matchCount;
                }

                result = matchCount >= min && matchCount <= max;
            }

            return result;
        }

        private static bool MatchExpression(Expression exp, MatchContext context)
        {
            bool result = false;

            if (exp.type == ExpressionType.Combinator)
            {
                result = MatchCombinator(exp, context);
            }
            else if (exp.type == ExpressionType.Data)
            {
                result = MatchDataType(exp, context);
            }
            else if (exp.type == ExpressionType.Keyword)
            {
                result = MatchKeyword(exp, context);
            }

            return result;
        }

        private static bool MatchGroup(Expression exp, MatchContext context)
        {
            // Group always have just one subExpression
            Debug.Assert(exp.subExpressions.Length == 1, "Group has invalid number of sub expressions");
            var subExp = exp.subExpressions[0];

            return Match(subExp, context);
        }

        private static bool MatchCombinator(Expression exp, MatchContext context)
        {
            context.SaveMark();

            bool result = false;
            switch (exp.combinator)
            {
                case ExpressionCombinator.Or:
                    result = MatchOr(exp, context);
                    break;
                case ExpressionCombinator.OrOr:
                    result = MatchOrOr(exp, context);
                    break;
                case ExpressionCombinator.AndAnd:
                    result = MatchAndAnd(exp, context);
                    break;
                case ExpressionCombinator.Juxtaposition:
                    result = MatchJuxtaposition(exp, context);
                    break;
                case ExpressionCombinator.Group:
                    result = MatchGroup(exp, context);
                    break;
                case ExpressionCombinator.None:
                    break;
            }

            if (result)
                context.DropMark();
            else
                context.RestoreMark();

            return result;
        }

        private static bool MatchOr(Expression exp, MatchContext context)
        {
            bool result = false;
            for (int i = 0; !result && i < exp.subExpressions.Length; i++)
            {
                result = Match(exp.subExpressions[i], context);
            }
            return result;
        }

        private static bool MatchOrOr(Expression exp, MatchContext context)
        {
            // All sub expressions are options but at least one of them must match, and they may match in any order.
            // A sub expression must appear at most one single time
            int matchCount = MatchMany(exp, context);
            return matchCount > 0;
        }

        private static bool MatchAndAnd(Expression exp, MatchContext context)
        {
            // All sub expressions are mandatory but they may match in any order.
            int matchCount = MatchMany(exp, context);
            int subExpCount = exp.subExpressions.Length;
            return matchCount == subExpCount;
        }

        private unsafe static int MatchMany(Expression exp, MatchContext context)
        {
            int matchCount = 0;
            int subExpCount = exp.subExpressions.Length;
            int* matchedExp = stackalloc int[subExpCount];

            int i = 0;
            while (i < subExpCount && matchCount < subExpCount)
            {
                bool alreadyMatched = false;
                for (int j = 0; j < matchCount; j++)
                {
                    if (matchedExp[j] == i)
                    {
                        alreadyMatched = true;
                        break;
                    }
                }

                bool result = false;
                if (!alreadyMatched)
                {
                    result = Match(exp.subExpressions[i], context);
                }

                if (result)
                {
                    // Reset the loop to try the next value on all unmatched sub expressions
                    matchedExp[matchCount] = i;
                    ++matchCount;
                    i = 0;
                }
                else
                {
                    ++i;
                }
            }

            return matchCount;
        }

        private static bool MatchJuxtaposition(Expression exp, MatchContext context)
        {
            bool result = true;
            for (int i = 0; result && i < exp.subExpressions.Length; i++)
            {
                result = Match(exp.subExpressions[i], context);
            }
            return result;
        }

        private static bool MatchKeyword(Expression exp, MatchContext context)
        {
            if (context.current != null && exp.keyword == context.current.ToLower())
            {
                context.MoveNext();
                return true;
            }

            return false;
        }

        private static bool MatchDataType(Expression exp, MatchContext context)
        {
            bool result = false;

            if (context.hasCurrent)
            {
                switch (exp.dataType)
                {
                    case DataType.Number:
                        result = MatchNumber(exp, context.current);
                        break;
                    case DataType.Integer:
                        result = MatchInteger(exp, context.current);
                        break;
                    case DataType.Length:
                        result = MatchLength(exp, context.current);
                        break;
                    case DataType.Percentage:
                        result = MatchPercent(exp, context.current);
                        break;
                    case DataType.Color:
                        result = MatchColor(exp, context.current);
                        break;
                    case DataType.Resource:
                        result = MatchResource(exp, context.current);
                        break;
                    case DataType.Url:
                        result = MatchUrl(exp, context.current);
                        break;
                    default:
                        break;
                }

                if (result)
                    context.MoveNext();
            }

            return result;
        }

        static readonly Regex s_NumberRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?$", RegexOptions.Compiled);
        private static bool MatchNumber(Expression exp, string value)
        {
            Match match = s_NumberRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_IntegerRegex = new Regex(@"^[+-]?\d+$", RegexOptions.Compiled);
        private static bool MatchInteger(Expression exp, string value)
        {
            Match match = s_IntegerRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_ZeroRegex = new Regex(@"^0(?:\.0+)?$", RegexOptions.Compiled); // Zero is accepted without any unit
        static readonly Regex s_LengthRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?(?:px)$", RegexOptions.Compiled);
        private static bool MatchLength(Expression exp, string value)
        {
            Match match = s_LengthRegex.Match(value);
            if (match.Success)
                return true;

            match = s_ZeroRegex.Match(value);
            if (match.Success)
                return true;

            return false;
        }

        static readonly Regex s_PercentRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?(?:%)$", RegexOptions.Compiled);
        private static bool MatchPercent(Expression exp, string value)
        {
            Match match = s_PercentRegex.Match(value);
            if (match.Success)
                return true;

            match = s_ZeroRegex.Match(value);
            if (match.Success)
                return true;

            return false;
        }

        static readonly Regex s_HexColorRegex = new Regex(@"^#[a-fA-F0-9]{3}(?:[a-fA-F0-9]{3})?$", RegexOptions.Compiled);
        static readonly Regex s_RgbRegex = new Regex(@"^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$", RegexOptions.Compiled);
        static readonly Regex s_RgbaRegex = new Regex(@"rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([\d.]+)\s*\)$", RegexOptions.Compiled);
        private static bool MatchColor(Expression exp, string value)
        {
            Match match = s_HexColorRegex.Match(value);
            if (match.Success)
                return true;

            match = s_RgbRegex.Match(value);
            if (match.Success)
                return true;

            match = s_RgbaRegex.Match(value);
            if (match.Success)
                return true;

            Color c = Color.clear;
            if (StyleSheetColor.TryGetColor(value, out c))
                return true;

            return false;
        }

        static readonly Regex s_ResourceRegex = new Regex(@"^resource\(.+\)$", RegexOptions.Compiled);
        private static bool MatchResource(Expression exp, string value)
        {
            Match match = s_ResourceRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_UrlRegex = new Regex(@"^url\(.+\)$", RegexOptions.Compiled);
        private static bool MatchUrl(Expression exp, string value)
        {
            Match match = s_UrlRegex.Match(value);
            return match.Success;
        }

        class MatchContext
        {
            private StylePropertyValueParser m_Parser = new StylePropertyValueParser();
            private Stack<int> m_MarkStack = new Stack<int>();
            private string[] m_PropertyParts;
            private int m_CurrentIndex;

            public void Initialize(string propertyValue)
            {
                m_PropertyParts = m_Parser.Parse(propertyValue);
                m_CurrentIndex = 0;
                m_MarkStack.Clear();
            }

            public int count
            {
                get { return m_PropertyParts.Length; }
            }

            public string current
            {
                get { return hasCurrent ? m_PropertyParts[m_CurrentIndex] : null; }
            }

            public bool hasCurrent
            {
                get { return m_CurrentIndex < m_PropertyParts.Length; }
            }

            public void MoveNext()
            {
                if (m_CurrentIndex + 1 <= m_PropertyParts.Length)
                {
                    m_CurrentIndex++;
                }
            }

            public void SaveMark()
            {
                m_MarkStack.Push(m_CurrentIndex);
            }

            public void RestoreMark()
            {
                m_CurrentIndex = m_MarkStack.Pop();
            }

            public void DropMark()
            {
                m_MarkStack.Pop();
            }
        }
    }
}
