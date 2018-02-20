// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/Enlighten/LightingDataAsset.h")]
    [ExcludeFromPreset]
    public sealed partial class LightingDataAsset : Object
    {
        private LightingDataAsset() {}

        internal extern bool isValid {[NativeName("IsValid")] get; }

        internal extern string validityErrorMessage { get; }
    }
}
