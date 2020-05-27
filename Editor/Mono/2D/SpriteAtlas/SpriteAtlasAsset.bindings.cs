// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Build;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.U2D;

namespace UnityEditor.U2D
{
    // SpriteAtlas Importer lets you modify [[SpriteAtlas]]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasAsset.h")]
    [NativeType(Header = "Editor/Src/2D/SpriteAtlas/SpriteAtlasAsset.h")]
    public class SpriteAtlasAsset : UnityEngine.Object
    {
        public SpriteAtlasAsset() { Internal_Create(this); }
        extern private static void Internal_Create([Writable] SpriteAtlasAsset self);

        extern public bool isVariant { [NativeMethod("GetIsVariant")] get; }
        extern public void SetIsVariant(bool value);
        extern public void SetMasterAtlas(SpriteAtlas atlas);
        extern public void SetIncludeInBuild(bool value);
        extern public void SetVariantScale(float value);
        extern public void SetPlatformSettings(TextureImporterPlatformSettings src);
        extern public TextureImporterPlatformSettings GetPlatformSettings(string buildTarget);
        extern public SpriteAtlasTextureSettings GetTextureSettings();
        extern public void SetTextureSettings(SpriteAtlasTextureSettings src);
        extern public SpriteAtlasPackingSettings GetPackingSettings();
        extern public void SetPackingSettings(SpriteAtlasPackingSettings src);

        extern public void Add(UnityEngine.Object[] objects);
        extern public void Remove(UnityEngine.Object[] objects);
        extern internal void RemoveAt(int index);

        extern internal TextureFormat GetTextureFormat(BuildTarget target);
        extern internal void CopyMasterAtlasSettings();
        extern internal TextureImporterPlatformSettings GetSecondaryPlatformSettings(string buildTarget, string secondaryTextureName);
        extern internal void SetSecondaryPlatformSettings(TextureImporterPlatformSettings src, string secondaryTextureName);
        extern internal bool GetSecondaryColorSpace(string secondaryTextureName);
        extern internal void SetSecondaryColorSpace(string secondaryTextureName, bool srGB);
        extern internal void DeleteSecondaryPlatformSettings(string secondaryTextureName);
    }
};
