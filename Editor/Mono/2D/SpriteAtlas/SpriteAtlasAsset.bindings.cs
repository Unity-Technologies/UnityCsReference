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
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasAsset.h")]
    [NativeType(Header = "Editor/Src/2D/SpriteAtlas/SpriteAtlasAsset.h")]
    public class SpriteAtlasAsset : UnityEngine.Object
    {
        public SpriteAtlasAsset() { Internal_Create(this); }
        extern private static void Internal_Create([Writable] SpriteAtlasAsset self);

        extern public bool isVariant { [NativeMethod("GetIsVariant")] get; }
        extern public void SetIsVariant(bool value);
        extern public void SetMasterAtlas(SpriteAtlas atlas);
        extern public SpriteAtlas GetMasterAtlas();
        extern public void Add(UnityEngine.Object[] objects);
        extern public void Remove(UnityEngine.Object[] objects);
        extern internal void RemoveAt(int index);

        [Obsolete("SetVariantScale is no longer supported and will be removed. Use SpriteAtlasImporter.SetVariantScale instead.")]
        public void SetVariantScale(float value) { }
        [Obsolete("SetIncludeInBuild is no longer supported and will be removed. Use SpriteAtlasImporter.SetIncludeInBuild instead.")]
        public void SetIncludeInBuild(bool value) { }
        [Obsolete("IsIncludeInBuild is no longer supported and will be removed. Use SpriteAtlasImporter.IsIncludeInBuild instead.")]
        public bool IsIncludeInBuild() { return true;  }
        [Obsolete("SetPlatformSettings is no longer supported and will be removed. Use SpriteAtlasImporter.SetPlatformSettings instead.")]
        public void SetPlatformSettings(TextureImporterPlatformSettings src) { }
        [Obsolete("SetTextureSettings is no longer supported and will be removed. Use SpriteAtlasImporter.SetTextureSettings instead.")]
        public void SetTextureSettings(SpriteAtlasTextureSettings src) { }
        [Obsolete("SetPackingSettings is no longer supported and will be removed. Use SpriteAtlasImporter.SetPackingSettings instead.")]
        public void SetPackingSettings(SpriteAtlasPackingSettings src) { }
        [Obsolete("GetPackingSettings is no longer supported and will be removed. Use SpriteAtlasImporter.GetPackingSettings instead.")]
        public SpriteAtlasPackingSettings GetPackingSettings() { return new SpriteAtlasPackingSettings(); }
        [Obsolete("GetTextureSettings is no longer supported and will be removed. Use SpriteAtlasImporter.GetTextureSettings instead.")]
        public SpriteAtlasTextureSettings GetTextureSettings() { return new SpriteAtlasTextureSettings(); }
        [Obsolete("GetPlatformSettings is no longer supported and will be removed. Use SpriteAtlasImporter.GetPlatformSettingss instead.")]
        public TextureImporterPlatformSettings GetPlatformSettings(string buildTarget) { return new TextureImporterPlatformSettings(); }

        // Load SpriteAtlasAsset
        public static SpriteAtlasAsset Load(string assetPath)
        {
            var objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
            return (objs.Length > 0) ? objs[0] as SpriteAtlasAsset : null;
        }

        public static void Save(SpriteAtlasAsset asset, string assetPath)
        {
            if (asset == null)
                throw new ArgumentNullException("Parameter asset is null");
            var objs = new UnityEngine.Object[] { asset };
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(objs, assetPath, UnityEditor.EditorSettings.serializationMode != UnityEditor.SerializationMode.ForceBinary);
        }
    }
};
