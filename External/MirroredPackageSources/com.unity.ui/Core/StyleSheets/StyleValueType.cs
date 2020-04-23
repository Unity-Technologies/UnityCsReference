using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
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
        FunctionSeparator,
        ScalableImage,
        MissingAssetReference,
    }
}
