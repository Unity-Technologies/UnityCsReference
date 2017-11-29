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
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine
{


[UsedByNativeCode]
public partial class Texture : Object
{
    public  Hash128 imageContentsHash
    {
        get { Hash128 tmp; INTERNAL_get_imageContentsHash(out tmp); return tmp;  }
        set { INTERNAL_set_imageContentsHash(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_imageContentsHash (out Hash128 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_imageContentsHash (ref Hash128 value) ;

    public extern  uint updateCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void IncrementUpdateCount () ;

}

[UsedByNativeCode]
public sealed partial class Texture2D : Texture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void UpdateExternalTexture (IntPtr nativeTex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetAllPixels32 (Color32[] colors, int miplevel) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetBlockOfPixels32 (int x, int y, int blockWidth, int blockHeight, Color32[] colors, int miplevel) ;

    [uei.ExcludeFromDocs]
public void SetPixels32 (Color32[] colors) {
    int miplevel = 0;
    SetPixels32 ( colors, miplevel );
}

public void SetPixels32(Color32[] colors, [uei.DefaultValue("0")]  int miplevel )
        {
            SetAllPixels32(colors, miplevel);
        }

    
    
    [uei.ExcludeFromDocs]
public void SetPixels32 (int x, int y, int blockWidth, int blockHeight, Color32[] colors) {
    int miplevel = 0;
    SetPixels32 ( x, y, blockWidth, blockHeight, colors, miplevel );
}

public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors, [uei.DefaultValue("0")]  int miplevel )
        {
            SetBlockOfPixels32(x, y, blockWidth, blockHeight, colors, miplevel);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public byte[] GetRawTextureData () ;

    [uei.ExcludeFromDocs]
public Color[] GetPixels () {
    int miplevel = 0;
    return GetPixels ( miplevel );
}

public Color[] GetPixels( [uei.DefaultValue("0")] int miplevel )
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            return GetPixels(0, 0, w, h, miplevel);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color[] GetPixels (int x, int y, int blockWidth, int blockHeight, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color[] GetPixels (int x, int y, int blockWidth, int blockHeight) {
        int miplevel = 0;
        return GetPixels ( x, y, blockWidth, blockHeight, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color32[] GetPixels32 ( [uei.DefaultValue("0")] int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color32[] GetPixels32 () {
        int miplevel = 0;
        return GetPixels32 ( miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Resize (int width, int height, TextureFormat format, bool hasMipMap) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Rect[] PackTextures (Texture2D[] textures, int padding, [uei.DefaultValue("2048")]  int maximumAtlasSize , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public Rect[] PackTextures (Texture2D[] textures, int padding, int maximumAtlasSize ) {
        bool makeNoLongerReadable = false;
        return PackTextures ( textures, padding, maximumAtlasSize, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public Rect[] PackTextures (Texture2D[] textures, int padding) {
        bool makeNoLongerReadable = false;
        int maximumAtlasSize = 2048;
        return PackTextures ( textures, padding, maximumAtlasSize, makeNoLongerReadable );
    }

    public static bool GenerateAtlas(Vector2[] sizes, int padding, int atlasSize, List<Rect> results)
        {
            if (sizes == null)
                throw new ArgumentException("sizes array can not be null");
            if (results == null)
                throw new ArgumentException("results list cannot be null");
            if (padding < 0)
                throw new ArgumentException("padding can not be negative");
            if (atlasSize <= 0)
                throw new ArgumentException("atlas size must be positive");

            results.Clear();
            if (sizes.Length == 0)
            {
                return true;
            }

            GenerateAtlasInternal(sizes, padding, atlasSize, results);
            return results.Count != 0;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GenerateAtlasInternal (Vector2[] sizes, int padding, int atlasSize, object resultList) ;

    public void ReadPixels (Rect source, int destX, int destY, [uei.DefaultValue("true")]  bool recalculateMipMaps ) {
        INTERNAL_CALL_ReadPixels ( this, ref source, destX, destY, recalculateMipMaps );
    }

    [uei.ExcludeFromDocs]
    public void ReadPixels (Rect source, int destX, int destY) {
        bool recalculateMipMaps = true;
        INTERNAL_CALL_ReadPixels ( this, ref source, destX, destY, recalculateMipMaps );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ReadPixels (Texture2D self, ref Rect source, int destX, int destY, bool recalculateMipMaps);
    [Flags]
    public enum EXRFlags    
    {
        None = 0,
        OutputAsFloat = 1 << 0,
        
        CompressZIP = 1 << 1,
        CompressRLE = 1 << 2,
        CompressPIZ = 1 << 3,
    }

    public extern bool alphaIsTransparency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[ExcludeFromPreset]
public sealed partial class Cubemap : Texture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color[] GetPixels (CubemapFace face, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color[] GetPixels (CubemapFace face) {
        int miplevel = 0;
        return GetPixels ( face, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels (Color[] colors, CubemapFace face, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels (Color[] colors, CubemapFace face) {
        int miplevel = 0;
        SetPixels ( colors, face, miplevel );
    }

}

[ExcludeFromPreset]
public sealed partial class Texture3D : Texture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color[] GetPixels ( [uei.DefaultValue("0")] int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color[] GetPixels () {
        int miplevel = 0;
        return GetPixels ( miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color32[] GetPixels32 ( [uei.DefaultValue("0")] int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color32[] GetPixels32 () {
        int miplevel = 0;
        return GetPixels32 ( miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels (Color[] colors, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels (Color[] colors) {
        int miplevel = 0;
        SetPixels ( colors, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels32 (Color32[] colors, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels32 (Color32[] colors) {
        int miplevel = 0;
        SetPixels32 ( colors, miplevel );
    }

}

public sealed partial class Texture2DArray : Texture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels (Color[] colors, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels (Color[] colors, int arrayElement) {
        int miplevel = 0;
        SetPixels ( colors, arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels32 (Color32[] colors, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels32 (Color32[] colors, int arrayElement) {
        int miplevel = 0;
        SetPixels32 ( colors, arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color[] GetPixels (int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color[] GetPixels (int arrayElement) {
        int miplevel = 0;
        return GetPixels ( arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color32[] GetPixels32 (int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color32[] GetPixels32 (int arrayElement) {
        int miplevel = 0;
        return GetPixels32 ( arrayElement, miplevel );
    }

}

public sealed partial class CubemapArray : Texture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels (Color[] colors, CubemapFace face, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels (Color[] colors, CubemapFace face, int arrayElement) {
        int miplevel = 0;
        SetPixels ( colors, face, arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels32 (Color32[] colors, CubemapFace face, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels32 (Color32[] colors, CubemapFace face, int arrayElement) {
        int miplevel = 0;
        SetPixels32 ( colors, face, arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color[] GetPixels (CubemapFace face, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color[] GetPixels (CubemapFace face, int arrayElement) {
        int miplevel = 0;
        return GetPixels ( face, arrayElement, miplevel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Color32[] GetPixels32 (CubemapFace face, int arrayElement, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public Color32[] GetPixels32 (CubemapFace face, int arrayElement) {
        int miplevel = 0;
        return GetPixels32 ( face, arrayElement, miplevel );
    }

}

public sealed partial class SparseTexture : Texture
{
    public extern int tileWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int tileHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern bool isCreated
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public SparseTexture(int width, int height, TextureFormat format, int mipCount)
        {
            Internal_Create(this, width, height, format, mipCount, false);
        }
    
    
    public SparseTexture(int width, int height, TextureFormat format, int mipCount, bool linear)
        {
            Internal_Create(this, width, height, format, mipCount, linear);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] SparseTexture mono, int width, int height, TextureFormat format, int mipCount, bool linear) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void UpdateTile (int tileX, int tileY, int miplevel, Color32[] data) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void UpdateTileRaw (int tileX, int tileY, int miplevel, byte[] data) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void UnloadTile (int tileX, int tileY, int miplevel) ;

}

[UsedByNativeCode]
public partial class RenderTexture : Texture
{
    internal protected RenderTexture()
        {
        }
    
    
    private void SetRenderTextureDescriptor (RenderTextureDescriptor desc) {
        INTERNAL_CALL_SetRenderTextureDescriptor ( this, ref desc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRenderTextureDescriptor (RenderTexture self, ref RenderTextureDescriptor desc);
    private RenderTextureDescriptor GetDescriptor () {
        RenderTextureDescriptor result;
        INTERNAL_CALL_GetDescriptor ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetDescriptor (RenderTexture self, out RenderTextureDescriptor value);
    private static RenderTexture GetTemporary_Internal (RenderTextureDescriptor desc) {
        return INTERNAL_CALL_GetTemporary_Internal ( ref desc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RenderTexture INTERNAL_CALL_GetTemporary_Internal (ref RenderTextureDescriptor desc);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ReleaseTemporary (RenderTexture temp) ;

    public void ResolveAntiAliasedSurface()
        {
            Internal_ResolveAntiAliasedSurface(null);
        }
    
    
    public void ResolveAntiAliasedSurface(RenderTexture target)
        {
            Internal_ResolveAntiAliasedSurface(target);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_ResolveAntiAliasedSurface (RenderTexture target) ;

    public extern  int depth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void DiscardContents () {
        INTERNAL_CALL_DiscardContents ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DiscardContents (RenderTexture self);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void DiscardContents (bool discardColor, bool discardDepth) ;

    public void MarkRestoreExpected () {
        INTERNAL_CALL_MarkRestoreExpected ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MarkRestoreExpected (RenderTexture self);
    public RenderBuffer colorBuffer { get { RenderBuffer res; GetColorBuffer(out res); return res; } }
    
    
    public RenderBuffer depthBuffer { get { RenderBuffer res; GetDepthBuffer(out res); return res; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetColorBuffer (out RenderBuffer res) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetDepthBuffer (out RenderBuffer res) ;

    public IntPtr GetNativeDepthBufferPtr () {
        IntPtr result;
        INTERNAL_CALL_GetNativeDepthBufferPtr ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeDepthBufferPtr (RenderTexture self, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetGlobalShaderProperty (string propertyName) ;

    public extern static RenderTexture active
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("RenderTexture.enabled is always now, no need to use it")]
    public extern static bool enabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("GetTexelOffset always returns zero now, no point in using it.")]
public Vector2 GetTexelOffset()
        {
            return Vector2.zero;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SupportsStencil (RenderTexture rt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  VRTextureUsage GetActiveVRUsage () ;

    [System.Obsolete ("SetBorderColor is no longer supported.", true)]
public void SetBorderColor(Color color) {}
}

[System.Serializable]
[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct CustomRenderTextureUpdateZone
{
    public Vector3 updateZoneCenter;
    public Vector3 updateZoneSize;
    public float rotation;
    public int passIndex;
    public bool needSwap;
}

[UsedByNativeCode]
public sealed partial class CustomRenderTexture : RenderTexture
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateCustomRenderTexture ([Writable] CustomRenderTexture rt, RenderTextureReadWrite readWrite) ;

    public CustomRenderTexture(int width, int height, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            Internal_CreateCustomRenderTexture(this, readWrite);

            this.width = width;
            this.height = height;
            this.format = format;
        }
    
    
    public CustomRenderTexture(int width, int height, RenderTextureFormat format)
        {
            Internal_CreateCustomRenderTexture(this, RenderTextureReadWrite.Default);
            this.width = width;
            this.height = height;
            this.format = format;
        }
    
    
    public CustomRenderTexture(int width, int height)
        {
            Internal_CreateCustomRenderTexture(this, RenderTextureReadWrite.Default);
            this.width = width;
            this.height = height;
            this.format = RenderTextureFormat.Default;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Update ( [uei.DefaultValue("1")] int count ) ;

    [uei.ExcludeFromDocs]
    public void Update () {
        int count = 1;
        Update ( count );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Initialize () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ClearUpdateZones () ;

    public extern  Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Material initializationMaterial
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Texture initializationTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    bool IsCubemapFaceEnabled(CubemapFace face)
        {
            return (cubemapFaceMask & (1 << (int)face)) != 0;
        }
    
    
    void EnableCubemapFace(CubemapFace face, bool value)
        {
            uint oldValue = cubemapFaceMask;
            uint bit = 1u << (int)face;
            if (value)
                oldValue |= bit;
            else
                oldValue &= ~bit;
            cubemapFaceMask = oldValue;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void GetUpdateZonesInternal (object updateZones) ;

    public void GetUpdateZones(List<CustomRenderTextureUpdateZone> updateZones)
        {
            GetUpdateZonesInternal(updateZones);
        }
    
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetUpdateZonesInternal (CustomRenderTextureUpdateZone[] updateZones) ;

    public void SetUpdateZones(CustomRenderTextureUpdateZone[] updateZones)
        {
            if (updateZones == null)
                throw new ArgumentNullException("updateZones");

            SetUpdateZonesInternal(updateZones);
        }
    
    
    public extern CustomRenderTextureInitializationSource initializationSource
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color initializationColor
    {
        get { Color tmp; INTERNAL_get_initializationColor(out tmp); return tmp;  }
        set { INTERNAL_set_initializationColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_initializationColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_initializationColor (ref Color value) ;

    public extern CustomRenderTextureUpdateMode updateMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern CustomRenderTextureUpdateMode initializationMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern CustomRenderTextureUpdateZoneSpace updateZoneSpace
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int shaderPass
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern uint cubemapFaceMask
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool doubleBuffered
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool wrapUpdateZones
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}


}
