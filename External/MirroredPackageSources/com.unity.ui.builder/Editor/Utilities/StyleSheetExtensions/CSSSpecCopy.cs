using System;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class CSSSpecCopy
    {
        static readonly Regex rgx = new Regex(
            @"(?<id>#[-]?\w[\w-]*)|(?<class>\.[\w-]+)|(?<pseudoclass>:[\w-]+(\((?<param>.+)\))?)|(?<type>([^\-]\w+|\w+))|(?<wildcard>\*)|\s+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        const int typeSelectorWeight = 1;
        const int classSelectorWeight = 10;
        const int idSelectorWeight = 100;

        public static int GetSelectorSpecificity(string selector)
        {
            StyleSelectorPart[] parts;
            int score = 0;
            if (ParseSelector(selector, out parts))
            {
                score = GetSelectorSpecificity(parts);
            }
            return score;
        }

        // See https://www.w3.org/TR/selectors/#specificity
        // A proper CSS library should provide us with this
        public static int GetSelectorSpecificity(StyleSelectorPart[] parts)
        {
            // always add 1 otherwise we wouldn't be able to distinguish between default C# value for int (0)
            // and an actual specificity (0 for *)
            int score = 1;
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i].type)
                {
                    case StyleSelectorType.Type:
                        score += typeSelectorWeight;
                        break;
                    case StyleSelectorType.Class:
                    case StyleSelectorType.PseudoClass:
                        score += classSelectorWeight;
                        break;
                    case StyleSelectorType.RecursivePseudoClass:
                        throw new ArgumentException("Recursive pseudo classes are not supported");
                    case StyleSelectorType.ID:
                        score += idSelectorWeight;
                        break;
                    default:
                        break;
                }
            }
            return score;
        }

        public static bool ParseSelector(string selector, out StyleSelectorPart[] parts)
        {
            var matches = rgx.Matches(selector);
            int count = matches.Count;

            if (count < 1)
            {
                parts = null;
                return false;
            }

            parts = new StyleSelectorPart[count];

            for (int i = 0; i < count; i++)
            {
                Match match = matches[i];
                StyleSelectorType type = StyleSelectorType.Unknown;
                string value = string.Empty;
                if (!string.IsNullOrEmpty(match.Groups["wildcard"].Value))
                {
                    value = "*";
                    type = StyleSelectorType.Wildcard;
                }
                else if (!string.IsNullOrEmpty(match.Groups["id"].Value))
                {
                    value = match.Groups["id"].Value.Substring(1);
                    type = StyleSelectorType.ID;
                }
                else if (!string.IsNullOrEmpty(match.Groups["class"].Value))
                {
                    value = match.Groups["class"].Value.Substring(1);
                    type = StyleSelectorType.Class;
                }
                else if (!string.IsNullOrEmpty(match.Groups["pseudoclass"].Value))
                {
                    var pseudoClassParam = match.Groups["param"].Value;
                    if (!string.IsNullOrEmpty(pseudoClassParam))
                    {
                        value = pseudoClassParam;
                        type = StyleSelectorType.RecursivePseudoClass;
                    }
                    else
                    {
                        value = match.Groups["pseudoclass"].Value.Substring(1);
                        type = StyleSelectorType.PseudoClass;
                    }
                }
                else if (!string.IsNullOrEmpty(match.Groups["type"].Value))
                {
                    value = match.Groups["type"].Value;
                    type = StyleSelectorType.Type;
                }
                parts[i] = new StyleSelectorPart() { type = type, value = value };
            }

            return true;
        }
    }
}
