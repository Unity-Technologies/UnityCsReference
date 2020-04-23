using System;

namespace UnityEngine.UIElements
{
    internal enum StyleValueKeyword
    {
        Inherit,
        Initial,
        Auto,
        Unset,
        True,
        False,
        None
    }

    internal static class StyleValueKeywordExtension
    {
        public static string ToUssString(this StyleValueKeyword svk)
        {
            switch (svk)
            {
                case StyleValueKeyword.Inherit:
                    return "inherit";
                case StyleValueKeyword.Initial:
                    return "initial";
                case StyleValueKeyword.Auto:
                    return "auto";
                case StyleValueKeyword.Unset:
                    return "unset";
                case StyleValueKeyword.True:
                    return "true";
                case StyleValueKeyword.False:
                    return "false";
                case StyleValueKeyword.None:
                    return "none";
                default:
                    throw new ArgumentOutOfRangeException(nameof(svk), svk, $"Unknown {nameof(StyleValueKeyword)}");
            }
        }
    }
}
