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
    [HelpURL("https://docs.unity3d.com/6000.1/Documentation/Manual/sprite/atlas/v2/sprite-atlas-v2.html")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasImporter.h")]
    public sealed partial class SpriteAtlasImporter : AssetImporter
    {
        extern internal static void MigrateAllSpriteAtlases();
        extern public float variantScale { get; set; }
        extern public bool includeInBuild { get; set; }
        extern public SpriteAtlasPackingSettings packingSettings { get; set; }
        extern public SpriteAtlasTextureSettings textureSettings { get; set; }

        [NativeName("SetPlatformSettings")]
        extern private void SetPlatformSettings_Internal(TextureImporterPlatformSettings src);
        public void SetPlatformSettings(TextureImporterPlatformSettings src)
        {
            src.name = TextureImporter.GetTexturePlatformSerializationName(src.name); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            SetPlatformSettings_Internal(src);
        }

        [NativeName("GetPlatformSettings")]
        extern private TextureImporterPlatformSettings GetPlatformSettings_Internal(string buildTarget);
        public TextureImporterPlatformSettings GetPlatformSettings(string buildTarget)
        {
            buildTarget = TextureImporter.GetTexturePlatformSerializationName(buildTarget); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            return GetPlatformSettings_Internal(buildTarget);
        }

        extern internal TextureFormat GetTextureFormat(BuildTarget target);

        [NativeName("GetSecondaryPlatformSettings")]
        extern private TextureImporterPlatformSettings GetSecondaryPlatformSettings_Internal(string buildTarget, string secondaryTextureName);
        internal TextureImporterPlatformSettings GetSecondaryPlatformSettings(string buildTarget, string secondaryTextureName)
        {
            buildTarget = TextureImporter.GetTexturePlatformSerializationName(buildTarget); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            return GetSecondaryPlatformSettings_Internal(buildTarget, secondaryTextureName);
        }

        [NativeName("SetSecondaryPlatformSettings")]
        extern private void SetSecondaryPlatformSettings_Internal(TextureImporterPlatformSettings src, string secondaryTextureName);
        internal void SetSecondaryPlatformSettings(TextureImporterPlatformSettings src, string secondaryTextureName)
        {
            src.name = TextureImporter.GetTexturePlatformSerializationName(src.name); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            SetSecondaryPlatformSettings_Internal(src, secondaryTextureName);
        }

        extern internal bool GetSecondaryColorSpace(string secondaryTextureName);
        extern internal void SetSecondaryColorSpace(string secondaryTextureName, bool srGB);
        extern internal void DeleteSecondaryPlatformSettings(string secondaryTextureName);
    }
};
