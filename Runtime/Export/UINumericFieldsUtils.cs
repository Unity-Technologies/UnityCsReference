// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    internal static class UINumericFieldsUtils
    {
        public static readonly string k_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()cosqrludxvRL=pP#";
        public static readonly string k_AllowedCharactersForInt = "0123456789-*/+%^()cosintaqrtelfundxvRL,=pPI#";
        public static readonly string k_DoubleFieldFormatString = "R";
        public static readonly string k_FloatFieldFormatString = "g7";
        public static readonly string k_IntFieldFormatString = "#######0";

        public static bool StringToDouble(string str, out double value)
        {
            return StringToDouble(str, out value, out _);
        }

        public static bool StringToDouble(string str, out double value, out ExpressionEvaluator.Expression expr)
        {
            expr = null;
            var lowered = str.ToLower();
            switch (lowered)
            {
                case "inf" or "infinity":
                    value = double.PositiveInfinity;
                    break;
                case "-inf" or "-infinity":
                    value = double.NegativeInfinity;
                    break;
                case "nan":
                    value = double.NaN;
                    break;
                default:
                    return ExpressionEvaluator.Evaluate(str, out value, out expr);
            }
            return true;
        }

        public static bool StringToDouble(string str, string initialValueAsString, out double value)
        {
            var success = StringToDouble(str, out value, out var expression);

            if (!success && expression != null && !string.IsNullOrEmpty(initialValueAsString))
            {
                if (StringToDouble(initialValueAsString, out var oldValue, out _))
                {
                    value = oldValue;
                    success = expression.Evaluate(ref value);
                }
            }
            return success;
        }

        public static bool StringToFloat(string str, string initialValueAsString, out float value)
        {
            var success = StringToDouble(str, initialValueAsString, out var v);
            value = Mathf.ClampToFloat(v);
            return success;
        }

        public static bool StringToLong(string str, out long value)
        {
            return ExpressionEvaluator.Evaluate(str, out value, out _);
        }

        public static bool StringToLong(string str, out long value, out ExpressionEvaluator.Expression expr)
        {
            return ExpressionEvaluator.Evaluate(str, out value, out expr);
        }

        public static bool StringToLong(string str, string initialValueAsString, out long value)
        {
            var success = StringToLong(str, out value, out var expression);

            if (!success && expression != null && !string.IsNullOrEmpty(initialValueAsString))
            {
                if (StringToLong(initialValueAsString, out var oldValue, out _))
                {
                    value = oldValue;
                    success = expression.Evaluate(ref value);
                }
            }
            return success;
        }

        public static bool StringToInt(string str, string initialValueAsString, out int value)
        {
            var success = StringToLong(str, initialValueAsString, out var v);
            value = Mathf.ClampToInt(v);
            return success;
        }
    }
}
