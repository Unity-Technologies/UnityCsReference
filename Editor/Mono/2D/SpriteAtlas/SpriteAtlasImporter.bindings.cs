// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Build;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.U2D;

namespace UnityEditor.U2D
{
    // SpriteAtlas Importer lets you modify [[SpriteAtlas]]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasImporter.h")]
    public sealed partial class SpriteAtlasImporter : AssetImporter
    {
        extern internal static void MigrateAllSpriteAtlases();
        extern public float variantScale { get; set; }
        extern public bool includeInBuild { get; set; }
        extern public SpriteAtlasPackingSettings packingSettings { get; set; }
        extern public SpriteAtlasTextureSettings textureSettings { get; set; }
        extern public void SetPlatformSettings(TextureImporterPlatformSettings src);
        extern public TextureImporterPlatformSettings GetPlatformSettings(string buildTarget);
        extern internal TextureFormat GetTextureFormat(BuildTarget target);
        extern internal TextureImporterPlatformSettings GetSecondaryPlatformSettings(string buildTarget, string secondaryTextureName);
        extern internal void SetSecondaryPlatformSettings(TextureImporterPlatformSettings src, string secondaryTextureName);
        extern internal bool GetSecondaryColorSpace(string secondaryTextureName);
        extern internal void SetSecondaryColorSpace(string secondaryTextureName, bool srGB);
        extern internal void DeleteSecondaryPlatformSettings(string secondaryTextureName);
    }
};
