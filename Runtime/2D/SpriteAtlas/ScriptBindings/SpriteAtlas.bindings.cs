// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.U2D
{
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlasManager.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [StaticAccessor("GetSpriteAtlasManager()", StaticAccessorType.Dot)]
    public class SpriteAtlasManager
    {
        public delegate void RequestAtlasCallback(string tag, System.Action<SpriteAtlas> action);
        public static event RequestAtlasCallback atlasRequested = null;

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

        extern internal static void Register(SpriteAtlas spriteAtlas);
    }

    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeType(Header = "Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    public class SpriteAtlas : UnityEngine.Object
    {
        public SpriteAtlas() { Internal_Create(this); }
        extern private static void Internal_Create([Writable] SpriteAtlas self);

        extern public bool isVariant {[NativeMethod("IsVariant")] get; }
        extern public string tag { get; }
        extern public int spriteCount { get; }

        extern public Sprite GetSprite(string name);
        public int GetSprites(Sprite[] sprites) { return GetSpritesScripting(sprites); }
        public int GetSprites(Sprite[] sprites, string name) {  return GetSpritesWithNameScripting(sprites, name); }

        extern private int GetSpritesScripting(Sprite[] sprites);
        extern private int GetSpritesWithNameScripting(Sprite[] sprites, string name);
    }
}
