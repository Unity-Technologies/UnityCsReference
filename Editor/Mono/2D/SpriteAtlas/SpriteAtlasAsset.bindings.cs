// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.U2D;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace UnityEditor.U2D
{

    [UsedByNativeCode]
    public abstract class ScriptablePacker : ScriptableObject
    {

        public enum PackTransform
        {
            None = 0,
            FlipHorizontal = 1,
            FlipVertical = 2,
            Rotate180 = 3,
        }

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct SpritePack
        {
            public int x;
            public int y;
            public int page;
            public PackTransform rot;
        };

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct SpriteData
        {
            public int guid;
            public int texIndex;
            public int indexCount;
            public int vertexCount;
            public int indexOffset;
            public int vertexOffset;
            public RectInt rect;
            public SpritePack output;
        };

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct TextureData
        {
            public int width;
            public int height;
            public int bufferOffset;
        };

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        internal struct PackerDataInternal
        {
            public int colorCount;
            public IntPtr colorData;
            public int spriteCount;
            public IntPtr spriteData;
            public int textureCount;
            public IntPtr textureData;
            public int indexCount;
            public IntPtr indexData;
            public int vertexCount;
            public IntPtr vertexData;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct PackerData
        {
            public NativeArray<Color32> colorData;
            public NativeArray<SpriteData> spriteData;
            public NativeArray<TextureData> textureData;
            public NativeArray<int> indexData;
            public NativeArray<Vector2> vertexData;
        };

        // Public function to pack.
        public abstract bool Pack(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerData input);

        // Internal Glue function.
        [RequiredByNativeCode]
        internal bool PackInternal(SpriteAtlasPackingSettings config, SpriteAtlasTextureSettings setting, PackerDataInternal packerData)
        {
            var input = new PackerData();
            unsafe
            {
                input.colorData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Color32>((void*)packerData.colorData, packerData.colorCount, Allocator.None);
                input.spriteData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SpriteData>((void*)packerData.spriteData, packerData.spriteCount, Allocator.None);
                input.textureData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TextureData>((void*)packerData.textureData, packerData.textureCount, Allocator.None);
                input.indexData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)packerData.indexData, packerData.indexCount, Allocator.None);
                input.vertexData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector2>((void*)packerData.vertexData, packerData.vertexCount, Allocator.None);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input.colorData, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input.spriteData, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input.textureData, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input.indexData, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input.vertexData, AtomicSafetyHandle.GetTempMemoryHandle());
            }
            return Pack(config, setting, input);
        }

    };

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
        extern internal UnityEngine.Object GetPacker();
        extern internal void SetPacker(UnityEngine.Object obj);
        extern public void Add(UnityEngine.Object[] objects);
        extern public void Remove(UnityEngine.Object[] objects);

        public void SetScriptablePacker(ScriptablePacker obj)
        {
            SetPacker(obj);
        }
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
