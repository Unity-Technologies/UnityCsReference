// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum StyleValueType
    {
        Invalid,
        Keyword,
        Float,
        Dimension,
        Color,
        ResourcePath, // When using resource("...")
        AssetReference,
        Enum, // A literal value that is not quoted
        Variable, // A literal value starting with "--"
        String, // A quoted value or any other value that is not recognized as a primitive
        Function,
        CommaSeparator,
        ScalableImage,
        MissingAssetReference,
    }
}
