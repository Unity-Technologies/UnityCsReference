// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;

namespace UnityEditor.U2D
{


internal sealed partial class SpriteAtlasUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void PackAllAtlases (BuildTarget target) ;

}

[StructLayout(LayoutKind.Sequential)]
internal sealed partial class SpriteAtlasTextureSettings
{
    internal uint m_AnisoLevel;
    internal uint m_CompressionQuality;
    internal uint m_MaxTextureSize;
    internal TextureImporterCompression m_TextureCompression;
    internal ColorSpace m_ColorSpace;
    internal FilterMode m_FilterMode;
    internal TextureFormat m_CustomFormat;
    internal TextureFormat m_Format;
    internal int m_GenerateMipMaps;
    internal int m_Readable;
    
    
    public uint anisoLevel { get {return m_AnisoLevel; } set {m_AnisoLevel = value; } }
    public uint compressionQuality { get {return m_CompressionQuality; } set {m_CompressionQuality = value; } }
    public uint maxTextureSize { get {return m_MaxTextureSize; } set {m_MaxTextureSize = value; } }
    public TextureImporterCompression textureCompression { get {return m_TextureCompression; } set {m_TextureCompression = value; } }
    public ColorSpace colorSpace { get {return m_ColorSpace; } set {m_ColorSpace = value; } }
    public FilterMode filterMode { get {return m_FilterMode; } set {m_FilterMode = value; } }
    public TextureFormat customFormat { get {return m_CustomFormat; } set {m_CustomFormat = value; } }
    public bool generateMipMaps { get {return m_GenerateMipMaps != 0; } set {m_GenerateMipMaps = value ? 1 : 0; } }
    public bool readable { get {return m_Readable != 0; } set {m_Readable = value ? 1 : 0; } }
}

[StructLayout(LayoutKind.Sequential)]
internal sealed partial class SpriteAtlasPackingParameters
{
    internal uint m_BlockOffset;
    internal uint m_PaddingPower;
    internal int m_AllowAlphaSplitting;
    internal int m_EnableRotation;
    internal int m_EnableTightPacking;
    
            public uint blockOffset { get {return m_BlockOffset; } set {m_BlockOffset = value; } }
            public uint paddingPower { get {return m_PaddingPower; } set {m_PaddingPower = value; } }
            public bool allowAlphaSplitting { get {return m_AllowAlphaSplitting != 0; } set {m_AllowAlphaSplitting = value ? 1 : 0; } }
            public bool enableRotation { get {return m_EnableRotation != 0; } set {m_EnableRotation = value ? 1 : 0; } }
            public bool enableTightPacking { get {return m_EnableTightPacking != 0; } set {m_EnableTightPacking = value ? 1 : 0; } }
}

internal static partial class SpriteAtlasExtensions
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Add (this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Remove (this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RemoveAt (this SpriteAtlas spriteAtlas, int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  UnityEngine.Object[] GetPackables (this SpriteAtlas spriteAtlas) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopyTextureSettingsTo (this SpriteAtlas spriteAtlas, SpriteAtlasTextureSettings dest) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetTextureSettings (this SpriteAtlas spriteAtlas, SpriteAtlasTextureSettings src) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopyPackingParametersTo (this SpriteAtlas spriteAtlas, SpriteAtlasPackingParameters dest) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetPackingParameters (this SpriteAtlas spriteAtlas, SpriteAtlasPackingParameters src) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetIncludeInBuild (this SpriteAtlas spriteAtlas, bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetMasterSpriteAtlas (this SpriteAtlas spriteAtlas, SpriteAtlas value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetVariantMultiplier (this SpriteAtlas spriteAtlas, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetHashString (this SpriteAtlas spriteAtlas) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Pack (this SpriteAtlas spriteAtlas, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D[] GetPreviewTextures (this SpriteAtlas spriteAtlas) ;

}

}
