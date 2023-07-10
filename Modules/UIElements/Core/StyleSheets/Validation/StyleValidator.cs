// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements.StyleSheets
{
    internal enum StyleValidationStatus
    {
        Ok,
        Error,
        Warning
    }

    internal struct StyleValidationResult
    {
        public StyleValidationStatus status;
        public string message;
        public string errorValue;
        public string hint;
        public bool success { get { return status == StyleValidationStatus.Ok; } }
    }

    internal class StyleValidator
    {
        private StyleSyntaxParser m_SyntaxParser;
        private StyleMatcher m_StyleMatcher;

        public StyleValidator()
        {
            m_SyntaxParser = new StyleSyntaxParser();
            m_StyleMatcher = new StyleMatcher();
        }

        public StyleValidationResult ValidateProperty(string name, string value)
        {
            var result = new StyleValidationResult() {status = StyleValidationStatus.Ok};

            // Bypass custom styles
            if (name.StartsWith("--"))
                return result;

            string syntax;
            if (!StylePropertyCache.TryGetSyntax(name, out syntax))
            {
                string closestName = StylePropertyCache.FindClosestPropertyName(name);
                result.status = StyleValidationStatus.Error;
                result.message = $"Unknown property '{name}'";
                if (!string.IsNullOrEmpty(closestName))
                    result.message = $"{result.message} (did you mean '{closestName}'?)";

                return result;
            }

            var syntaxTree = m_SyntaxParser.Parse(syntax);
            if (syntaxTree == null)
            {
                result.status = StyleValidationStatus.Error;
                result.message = $"Invalid '{name}' property syntax '{syntax}'";
                return result;
            }

            var matchResult = m_StyleMatcher.Match(syntaxTree, value);
            if (!matchResult.success)
            {
                result.errorValue = matchResult.errorValue;
                switch (matchResult.errorCode)
                {
                    case MatchResultErrorCode.Syntax:
                        result.status = StyleValidationStatus.Error;
                        if (IsUnitMissing(syntax, value, out var unitHint))
                            result.hint = $"Property expects a unit. Did you forget to add {unitHint}?";
                        else if (IsUnsupportedColor(syntax))
                            result.hint = $"Unsupported color '{value}'.";
                        result.message = $"Expected ({syntax}) but found '{value}'"; //The error may be after matchResult.errorValue when group are involved: not pointing to a specific token but to the whole value is more accurate
                        break;
                    case MatchResultErrorCode.EmptyValue:
                        result.status = StyleValidationStatus.Error;
                        result.message = $"Expected ({syntax}) but found empty value";
                        break;
                    case MatchResultErrorCode.ExpectedEndOfValue:
                        result.status = StyleValidationStatus.Warning;
                        result.message = $"Expected end of value but found '{matchResult.errorValue}'";
                        break;
                    default:
                        Debug.LogAssertion($"Unexpected error code '{matchResult.errorCode}'");
                        break;
                }
            }

            return result;
        }

        // A simple check to give a better error message when a unit is missing
        private bool IsUnitMissing(string propertySyntax, string propertyValue, out string unitHint)
        {
            unitHint = null;
            if (!float.TryParse(propertyValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                return false;

            if (propertySyntax.Contains("<length>") || propertySyntax.Contains("<length-percentage>"))
                unitHint = "px or %";
            else if (propertySyntax.Contains("<time>"))
                unitHint = "s or ms";

            return !string.IsNullOrEmpty(unitHint);
        }

        private bool IsUnsupportedColor(string propertySyntax)
        {
            return propertySyntax.StartsWith("<color>");
        }
    }
}
