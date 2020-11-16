using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Unity.UI.Builder
{
    internal static class BuilderNameUtilities
    {
        static string ConvertDashToUpperNoSpace(string dash, bool firstCase, bool addSpace)
        {
            if (BuilderConstants.SpecialEnumNamesCases.TryGetValue(dash, out var replacement))
                dash = replacement;

            var sb = new StringBuilder();
            bool caseFlag = firstCase;
            for (int i = 0; i < dash.Length; ++i)
            {
                char c = dash[i];
                if (c == '-')
                {
                    if (addSpace)
                        sb.Append(' ');
                    caseFlag = true;
                }
                else if (caseFlag)
                {
                    sb.Append(char.ToUpper(c));
                    caseFlag = false;
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }
            return sb.ToString();
        }

        public static string ConvertDashToCamel(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, false, false);
        }

        public static string ConvertDashToHungarian(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, true, false);
        }

        public static string ConvertDashToHuman(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, true, true);
        }

        public static string ConvertCamelToDash(string camel)
        {
            var split = Regex.Replace(Regex.Replace(camel, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1-$2"), @"(\p{Ll})(\P{Ll})", "$1-$2");
            var lowerCase = split.ToLower();
            foreach (var pair in BuilderConstants.SpecialEnumNamesCases.Where(pair => pair.Value.Equals(lowerCase)))
                return pair.Key;

            return lowerCase;
        }

        public static string ConvertStyleCSharpNameToUssName(string csharpName)
        {
            var dash = ConvertCamelToDash(csharpName);
            if (dash.StartsWith("unity-"))
                dash = "-" + dash;

            return dash;
        }

        public static string CapStringLengthAndAddEllipsis(string str, int maxLength)
        {
            if (str.Length < maxLength)
                return str;

            var strShortened = str.Substring(0, maxLength);
            strShortened += BuilderConstants.EllipsisText;
            return strShortened;
        }

        public static Regex attributeRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_]+$");
        public static Regex styleSelectorRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_:#\*>. ]+$");
        public static Regex bindingPathAttributeRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_.]+$");
    }
}
