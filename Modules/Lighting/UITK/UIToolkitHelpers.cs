// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Lighting
{
    internal static class StringUtilsExtensions
    {
        internal static string WithUssElement(this string blockName, string elementName) => blockName + "__" + elementName;

        internal static string WithUssModifier(this string blockName, string modifier) => blockName + "--" + modifier;
    }
}


