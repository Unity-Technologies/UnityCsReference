// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.U2D
{

    /// <summary>
    /// ObjectData holds information about a packed Object such as Sprite in the sprite atlas.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectData
    {
        /// <summary>
        /// Entity ID of the sprite.
        /// </summary>
        public EntityId     asset;

        /// <summary>
        /// Packing information for this object. X, Y coordinates of the packed position.
        /// </summary>
        public Vector4      packInfo;
    };

    /// <summary>
    /// TextureData holds information about a texture in the sprite atlas.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TextureData
    {
        /// <summary>
        /// Entity ID of the packed texture
        /// </summary>
        public EntityId     texture;

        /// <summary>
        /// Name of the material map associated with this texture.
        /// </summary>
        public string       mapName;
    };

    /// <summary>
    /// AtlasPage holds information about a page in the sprite atlas. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AtlasPage
    {
        /// <summary>
        /// Array of object data (packables) for this atlas page.
        /// </summary>
        public ObjectData[]     assets;

        /// <summary>
        /// Array of texture (packed textures) data for this atlas page.
        /// </summary>
        public TextureData[]    packedTextures;
    };

    /// <summary>
    /// Configuration settings for the sprite atlas at runtime.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]    
    public struct SpriteAtlasRuntimeConfig
    {
        /// <summary>
        /// Scale multiplier to be applied to the sprite.
        /// </summary>
        public float scaleMultiplier;
    }

    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [StaticAccessor("GetSpriteAtlasManager()", StaticAccessorType.Dot)]
    public class SpriteAtlasManager
    {
        public static event Action<string, Action<SpriteAtlas>> atlasRequested = null;

        [RequiredByNativeCode]
        private static bool RequestAtlas(string tag)
        {
            if (atlasRequested != null)
            {
                atlasRequested(tag, Register);
                return true;
            }
            return false;
        }

        public static event Action<SpriteAtlas> atlasRegistered = null;

        [RequiredByNativeCode]
        private static void PostRegisteredAtlas(SpriteAtlas spriteAtlas)
        {
            atlasRegistered?.Invoke(spriteAtlas);
        }

        extern internal static void Register(SpriteAtlas spriteAtlas);

        [FreeFunction("SpriteAtlasManager::CreateSpriteAtlas", ThrowsException = true)]
        extern public static SpriteAtlas CreateSpriteAtlas(string name, SpriteAtlasRuntimeConfig config, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] AtlasPage[] pages);
    }

    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    public class SpriteAtlas : UnityEngine.Object
    {
        public SpriteAtlas() { Internal_Create(this); }
        extern private static void Internal_Create([Writable] SpriteAtlas self);

        extern public bool isVariant {[NativeMethod("IsVariant")] get; }
        extern public string tag { get; }
        extern public int spriteCount { get; }

        extern public bool CanBindTo([NotNull] Sprite sprite);

        extern public Sprite GetSprite(string name);
        public int GetSprites(Sprite[] sprites) { return GetSpritesScripting(sprites); }
        public int GetSprites(Sprite[] sprites, string name) {  return GetSpritesWithNameScripting(sprites, name); }

        extern private int GetSpritesScripting([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Sprite[] sprites);
        extern private int GetSpritesWithNameScripting([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Sprite[] sprites, string name);
    }
}
