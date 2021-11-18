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
            string lowered = str.ToLower();
            if (lowered == "inf" || lowered == "infinity")
                value = double.PositiveInfinity;
            else if (lowered == "-inf" || lowered == "-infinity")
                value = double.NegativeInfinity;
            else if (lowered == "nan")
                value = double.NaN;
            else
                return ExpressionEvaluator.Evaluate(str, out value, out expr);
            return true;
        }

        public static bool StringToLong(string str, out long value)
        {
            return StringToLong(str, out value, out _);
        }

        public static bool StringToLong(string str, out long value, out ExpressionEvaluator.Expression expr)
        {
            return ExpressionEvaluator.Evaluate(str, out value, out expr);
        }
    }
}
