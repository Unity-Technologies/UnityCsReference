// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.StyleSheets
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
