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

    internal abstract class BaseStyleMatcher
    {
        private Stack<int> m_MarkStack = new Stack<int>();
        protected int m_CurrentIndex;

        protected abstract bool MatchKeyword(string keyword);
        protected abstract bool MatchNumber();
        protected abstract bool MatchInteger();
        protected abstract bool MatchLength();
        protected abstract bool MatchPercentage();
        protected abstract bool MatchColor();
        protected abstract bool MatchResource();
        protected abstract bool MatchUrl();

        public abstract int valueCount { get; }
        public abstract bool isVariable { get; }

        public bool hasCurrent => m_CurrentIndex < valueCount;
        public int matchedVariableCount { get; set; }

        protected void Initialize()
        {
            m_CurrentIndex = 0;
            m_MarkStack.Clear();
            matchedVariableCount = 0;
        }

        public void MoveNext()
        {
            if (m_CurrentIndex + 1 <= valueCount)
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

        protected bool Match(Expression exp)
        {
            bool result = true;
            if (exp.multiplier.type == ExpressionMultiplierType.None)
            {
                result = MatchExpression(exp);
            }
            else
            {
                Debug.Assert(exp.multiplier.type != ExpressionMultiplierType.OneOrMoreComma, "'#' multiplier in syntax expression is not supported");
                Debug.Assert(exp.multiplier.type != ExpressionMultiplierType.GroupAtLeastOne, "'!' multiplier in syntax expression is not supported");

                int min = exp.multiplier.min;
                int max = exp.multiplier.max;

                int matchCount = 0;
                for (int i = 0; result && hasCurrent && i < max; i++)
                {
                    result = MatchExpression(exp);
                    if (result)
                        ++matchCount;
                }

                result = matchCount >= min && matchCount <= max;
            }

            return result;
        }

        private bool MatchExpression(Expression exp)
        {
            bool result = false;

            if (exp.type == ExpressionType.Combinator)
            {
                result = MatchCombinator(exp);
            }
            else
            {
                // var function cannot be resolved here and is assumed to be valid
                if (isVariable)
                {
                    result = true;
                    matchedVariableCount++;
                }
                else if (exp.type == ExpressionType.Data)
                {
                    result = MatchDataType(exp);
                }
                else if (exp.type == ExpressionType.Keyword)
                {
                    result = MatchKeyword(exp.keyword);
                }

                if (result)
                    MoveNext();
            }

            // If more values were expected but a variable was matched
            // Assume that the variable also matched the expected values
            if (!result && !hasCurrent && matchedVariableCount > 0)
                result = true;

            return result;
        }

        private bool MatchGroup(Expression exp)
        {
            // Group always have just one subExpression
            Debug.Assert(exp.subExpressions.Length == 1, "Group has invalid number of sub expressions");
            var subExp = exp.subExpressions[0];

            return Match(subExp);
        }

        private bool MatchCombinator(Expression exp)
        {
            SaveMark();

            bool result = false;
            switch (exp.combinator)
            {
                case ExpressionCombinator.Or:
                    result = MatchOr(exp);
                    break;
                case ExpressionCombinator.OrOr:
                    result = MatchOrOr(exp);
                    break;
                case ExpressionCombinator.AndAnd:
                    result = MatchAndAnd(exp);
                    break;
                case ExpressionCombinator.Juxtaposition:
                    result = MatchJuxtaposition(exp);
                    break;
                case ExpressionCombinator.Group:
                    result = MatchGroup(exp);
                    break;
                case ExpressionCombinator.None:
                    break;
            }

            if (result)
                DropMark();
            else
                RestoreMark();

            return result;
        }

        private bool MatchOr(Expression exp)
        {
            bool result = false;
            for (int i = 0; !result && i < exp.subExpressions.Length; i++)
            {
                result = Match(exp.subExpressions[i]);
            }
            return result;
        }

        private bool MatchOrOr(Expression exp)
        {
            // All sub expressions are options but at least one of them must match, and they may match in any order.
            // A sub expression must appear at most one single time
            int matchCount = MatchMany(exp);
            return matchCount > 0;
        }

        private bool MatchAndAnd(Expression exp)
        {
            // All sub expressions are mandatory but they may match in any order.
            int matchCount = MatchMany(exp);
            int subExpCount = exp.subExpressions.Length;
            return matchCount == subExpCount;
        }

        private unsafe int MatchMany(Expression exp)
        {
            int matchCount = 0;
            int matchVariableCount = 0;
            int subExpCount = exp.subExpressions.Length;
            int* matchedExp = stackalloc int[subExpCount];

            int i = 0;
            while (i < subExpCount && matchCount + matchVariableCount < subExpCount)
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
                    result = Match(exp.subExpressions[i]);
                }

                if (result)
                {
                    if (matchVariableCount == matchedVariableCount)
                    {
                        matchedExp[matchCount] = i;
                        ++matchCount;
                    }
                    else
                    {
                        matchVariableCount = matchedVariableCount;
                    }

                    // Reset the loop to try the next value on all unmatched sub expressions
                    i = 0;
                }
                else
                {
                    ++i;
                }
            }

            return matchCount + matchVariableCount;
        }

        private bool MatchJuxtaposition(Expression exp)
        {
            bool result = true;
            for (int i = 0; result && i < exp.subExpressions.Length; i++)
            {
                result = Match(exp.subExpressions[i]);
            }
            return result;
        }

        private bool MatchDataType(Expression exp)
        {
            bool result = false;

            if (hasCurrent)
            {
                switch (exp.dataType)
                {
                    case DataType.Number:
                        result = MatchNumber();
                        break;
                    case DataType.Integer:
                        result = MatchInteger();
                        break;
                    case DataType.Length:
                        result = MatchLength();
                        break;
                    case DataType.Percentage:
                        result = MatchPercentage();
                        break;
                    case DataType.Color:
                        result = MatchColor();
                        break;
                    case DataType.Resource:
                        result = MatchResource();
                        break;
                    case DataType.Url:
                        result = MatchUrl();
                        break;
                    default:
                        break;
                }
            }

            return result;
        }
    }

    internal class StyleMatcher : BaseStyleMatcher
    {
        private StylePropertyValueParser m_Parser = new StylePropertyValueParser();
        private string[] m_PropertyParts;

        private string current => hasCurrent ? m_PropertyParts[m_CurrentIndex] : null;

        public override int valueCount => m_PropertyParts.Length;
        public override bool isVariable => hasCurrent && current.StartsWith("var(");

        private void Initialize(string propertyValue)
        {
            base.Initialize();

            m_PropertyParts = m_Parser.Parse(propertyValue);
        }

        public MatchResult Match(Expression exp, string propertyValue)
        {
            var result = new MatchResult() {errorCode = MatchResultErrorCode.None};
            if (string.IsNullOrEmpty(propertyValue))
            {
                result.errorCode = MatchResultErrorCode.EmptyValue;
                return result;
            }

            bool match = false;
            Initialize(propertyValue);

            // Handle global keyword and env()
            var firstValue = current;
            if (firstValue == "initial" || firstValue.StartsWith("env("))
            {
                MoveNext();
                match = true;
            }
            else
            {
                match = Match(exp);
            }

            if (!match)
            {
                result.errorCode = MatchResultErrorCode.Syntax;
                result.errorValue = current;
            }
            else if (hasCurrent)
            {
                result.errorCode = MatchResultErrorCode.ExpectedEndOfValue;
                result.errorValue = current;
            }

            return result;
        }

        static readonly Regex s_NumberRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?$", RegexOptions.Compiled);
        protected override bool MatchKeyword(string keyword)
        {
            return current != null && keyword == current.ToLower();
        }

        protected override bool MatchNumber()
        {
            var value = current;
            Match match = s_NumberRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_IntegerRegex = new Regex(@"^[+-]?\d+$", RegexOptions.Compiled);
        protected override bool MatchInteger()
        {
            var value = current;
            Match match = s_IntegerRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_ZeroRegex = new Regex(@"^0(?:\.0+)?$", RegexOptions.Compiled); // Zero is accepted without any unit
        static readonly Regex s_LengthRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?(?:px)$", RegexOptions.Compiled);
        protected override bool MatchLength()
        {
            var value = current;
            Match match = s_LengthRegex.Match(value);
            if (match.Success)
                return true;

            match = s_ZeroRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_PercentRegex = new Regex(@"^[+-]?\d+(?:\.\d+)?(?:%)$", RegexOptions.Compiled);
        protected override bool MatchPercentage()
        {
            var value = current;
            Match match = s_PercentRegex.Match(value);
            if (match.Success)
                return true;

            match = s_ZeroRegex.Match(value);
            return match.Success;
        }

        static readonly Regex s_HexColorRegex = new Regex(@"^#[a-fA-F0-9]{3}(?:[a-fA-F0-9]{3})?$", RegexOptions.Compiled);
        static readonly Regex s_RgbRegex = new Regex(@"^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$", RegexOptions.Compiled);
        static readonly Regex s_RgbaRegex = new Regex(@"rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([\d.]+)\s*\)$", RegexOptions.Compiled);
        protected override bool MatchColor()
        {
            var value = current;
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

        static readonly Regex s_VarFunctionRegex = new Regex(@"^var\(.+\)$", RegexOptions.Compiled);
        static readonly Regex s_ResourceRegex = new Regex(@"^resource\((.+)\)$", RegexOptions.Compiled);
        protected override bool MatchResource()
        {
            var value = current;
            Match match = s_ResourceRegex.Match(value);
            if (!match.Success)
                return false;

            var path = match.Groups[1].Value.Trim();
            match = s_VarFunctionRegex.Match(path);
            return !match.Success;
        }

        static readonly Regex s_UrlRegex = new Regex(@"^url\((.+)\)$", RegexOptions.Compiled);
        protected override bool MatchUrl()
        {
            var value = current;
            Match match = s_UrlRegex.Match(value);
            if (!match.Success)
                return false;

            var path = match.Groups[1].Value.Trim();
            match = s_VarFunctionRegex.Match(path);
            return !match.Success;
        }
    }

    internal class StylePropertyValueMatcher : BaseStyleMatcher
    {
        private List<StylePropertyValue> m_Values;

        private StylePropertyValue current => hasCurrent ? m_Values[m_CurrentIndex] : default(StylePropertyValue);

        public override int valueCount => m_Values.Count;
        // This matcher is only validating resolved value handles
        // A var function in this context is an error so this always return false
        public override bool isVariable => false;

        public MatchResult Match(Expression exp, List<StylePropertyValue> values)
        {
            var result = new MatchResult() {errorCode = MatchResultErrorCode.None};
            if (values == null || values.Count == 0)
            {
                result.errorCode = MatchResultErrorCode.EmptyValue;
                return result;
            }

            Initialize();
            m_Values = values;

            bool match = false;

            // Handle global initial keyword
            var firstHandle = m_Values[0].handle;
            if (firstHandle.valueType == StyleValueType.Keyword && (StyleValueKeyword)firstHandle.valueIndex == StyleValueKeyword.Initial)
            {
                MoveNext();
                match = true;
            }
            else
            {
                match = Match(exp);
            }

            if (!match)
            {
                var sheet = current.sheet;
                result.errorCode = MatchResultErrorCode.Syntax;
                result.errorValue = sheet.ReadAsString(current.handle);
            }
            else if (hasCurrent)
            {
                var sheet = current.sheet;
                result.errorCode = MatchResultErrorCode.ExpectedEndOfValue;
                result.errorValue = sheet.ReadAsString(current.handle);
            }

            return result;
        }

        protected override bool MatchKeyword(string keyword)
        {
            var value = current;
            if (value.handle.valueType == StyleValueType.Keyword)
            {
                var svk = (StyleValueKeyword)value.handle.valueIndex;
                return svk.ToUssString() == keyword.ToLower();
            }

            if (value.handle.valueType == StyleValueType.Enum)
            {
                var s = value.sheet.ReadEnum(value.handle);
                return s == keyword.ToLower();
            }

            return false;
        }

        protected override bool MatchNumber()
        {
            return current.handle.valueType == StyleValueType.Float;
        }

        protected override bool MatchInteger()
        {
            return current.handle.valueType == StyleValueType.Float;
        }

        protected override bool MatchLength()
        {
            var value = current;
            if (value.handle.valueType == StyleValueType.Dimension)
            {
                var dimension = value.sheet.ReadDimension(value.handle);
                return dimension.unit == Dimension.Unit.Pixel;
            }

            if (value.handle.valueType == StyleValueType.Float)
            {
                var f = value.sheet.ReadFloat(value.handle);
                return Mathf.Approximately(0f, f);
            }

            return false;
        }

        protected override bool MatchPercentage()
        {
            var value = current;
            if (value.handle.valueType == StyleValueType.Dimension)
            {
                var dimension = value.sheet.ReadDimension(value.handle);
                return dimension.unit == Dimension.Unit.Percent;
            }

            if (value.handle.valueType == StyleValueType.Float)
            {
                var f = value.sheet.ReadFloat(value.handle);
                return Mathf.Approximately(0f, f);
            }

            return false;
        }

        protected override bool MatchColor()
        {
            var value = current;
            if (value.handle.valueType == StyleValueType.Color)
                return true;

            if (value.handle.valueType == StyleValueType.Enum)
            {
                Color c = Color.clear;
                var colorName = value.sheet.ReadAsString(value.handle);
                if (StyleSheetColor.TryGetColor(colorName.ToLower(), out c))
                    return true;
            }

            return false;
        }

        protected override bool MatchResource()
        {
            return current.handle.valueType == StyleValueType.ResourcePath;
        }

        protected override bool MatchUrl()
        {
            return current.handle.valueType == StyleValueType.AssetReference;
        }
    }
}
