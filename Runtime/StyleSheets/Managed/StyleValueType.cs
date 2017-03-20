// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.StyleSheets
{
    internal enum StyleValueType
    {
        Keyword,
        Float,
        Color,
        ResourcePath, // When using resource("...")
        Enum, // A literal value that is not quoted
        String // A quoted value or any other value that is not recognized as a primitive
    }
}
