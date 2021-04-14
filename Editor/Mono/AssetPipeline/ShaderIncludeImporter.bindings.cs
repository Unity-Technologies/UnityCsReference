// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/ShaderIncludeImporter.h")]
    internal sealed partial class ShaderIncludeImporter : AssetImporter
    {
    }

    [NativeHeader("Editor/Src/Shaders/ShaderInclude.h")]
    public sealed partial class ShaderInclude : TextAsset
    {
    }
}
