// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Sprites
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AtlasSettings
    {
        public TextureFormat format;
        public ColorSpace colorSpace;
        public int compressionQuality;
        public FilterMode filterMode;
        public int maxWidth;
        public int maxHeight;
        public uint paddingPower;
        public int anisoLevel;
        public bool generateMipMaps;
        public bool enableRotation;
        public bool allowsAlphaSplitting;
    }

    [NativeHeader("Editor/Src/SpritePacker/SpritePacker.h")]
    public sealed class PackerJob
    {
        internal PackerJob()
        {
        }

        [FreeFunction("SpritePacker::ActiveJob_AddAtlas")]
        private static extern void Internal_AddAtlas(string atlasName, AtlasSettings settings);
        [FreeFunction("SpritePacker::ActiveJob_AssignToAtlas")]
        private static extern void Internal_AssignToAtlas(string atlasName, Sprite sprite, SpritePackingMode packingMode, SpritePackingRotation packingRotation);

        public void AddAtlas(string atlasName, AtlasSettings settings)
        {
            Internal_AddAtlas(atlasName, settings);
        }

        public void AssignToAtlas(string atlasName, Sprite sprite, SpritePackingMode packingMode, SpritePackingRotation packingRotation)
        {
            Internal_AssignToAtlas(atlasName, sprite, packingMode, packingRotation);
        }
    }

    [NativeHeader("Editor/Src/SpritePacker/SpritePacker.h")]
    public sealed partial class Packer
    {
        public extern static string[] atlasNames
        {
            [FreeFunction("SpritePacker::GetAvailableAtlases")]
            get;
        }

        [FreeFunction("SpritePacker::GetAtlasNameForSprite")]
        private static extern string Internal_GetAtlasNameForSprite(Sprite sprite);
        [FreeFunction("SpritePacker::GetAtlasTextureSprite")]
        private static extern Texture2D Internal_GetAtlasTextureSprite(Sprite sprite);

        [FreeFunction("SpritePacker::GetTexturesForAtlas")]
        public static extern Texture2D[] GetTexturesForAtlas(string atlasName);
        [FreeFunction("SpritePacker::GetAlphaTexturesForAtlas")]
        public static extern Texture2D[] GetAlphaTexturesForAtlas(string atlasName);
        [FreeFunction("SpritePacker::RebuildAtlasCacheIfNeededFromScript")]
        public static extern void RebuildAtlasCacheIfNeeded(BuildTarget target, bool displayProgressBar, Execution execution);

        public static void RebuildAtlasCacheIfNeeded(BuildTarget target, bool displayProgressBar)
        {
            RebuildAtlasCacheIfNeeded(target, displayProgressBar, Execution.Normal);
        }

        public static void RebuildAtlasCacheIfNeeded(BuildTarget target)
        {
            RebuildAtlasCacheIfNeeded(target, false, Execution.Normal);
        }

        public static void GetAtlasDataForSprite(Sprite sprite, out string atlasName, out Texture2D atlasTexture)
        {
            atlasName = Internal_GetAtlasNameForSprite(sprite);
            atlasTexture = Internal_GetAtlasTextureSprite(sprite);
        }
    }
}
