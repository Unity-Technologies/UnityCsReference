// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StaticAccessor("BumpMapSettings::Get()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/BumpMapSettings.h")]
    internal class BumpMapSettings
    {
        public static extern void PerformBumpMapCheck([NotNull] Material material);
    }

    public static class MaterialEditorExtensions
    {
        [Obsolete("PerformBumpMapCheck is obsolete.", false)]
        public static void PerformBumpMapCheck(this Material material)
        {
            BumpMapSettings.PerformBumpMapCheck(material);
        }
    }
}
