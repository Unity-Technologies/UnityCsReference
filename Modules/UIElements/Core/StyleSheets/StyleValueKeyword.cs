// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum StyleValueKeyword
    {
        Inherit,
        Initial,
        Auto,
        Unset,
        True,
        False,
        None,
        Cover,
        Contain
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
                case StyleValueKeyword.Cover:
                    return "cover";
                case StyleValueKeyword.Contain:
                    return "contain";
                default:
                    throw new ArgumentOutOfRangeException(nameof(svk), svk, $"Unknown {nameof(StyleValueKeyword)}");
            }
        }
    }
}
