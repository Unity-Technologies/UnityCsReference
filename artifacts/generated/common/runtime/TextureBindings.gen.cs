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
    public extern static int masterTextureLimit
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static AnisotropicFiltering anisotropicFiltering
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetGlobalAnisotropicFilteringLimits (int forcedMin, int globalMax) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetWidth (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetHeight (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  UnityEngine.Rendering.TextureDimension Internal_GetDimension (Texture t) ;

    virtual public int width { get { return Internal_GetWidth(this); } set { throw new Exception("not implemented"); } }
    virtual public int height { get { return Internal_GetHeight(this); } set { throw new Exception("not implemented"); } }
    virtual public UnityEngine.Rendering.TextureDimension dimension { get { return Internal_GetDimension(this); } set { throw new Exception("not implemented"); } }
    
    
    public extern FilterMode filterMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int anisoLevel
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeU
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeV
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeW
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float mipMapBias
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Vector2 texelSize
    {
        get { Vector2 tmp; INTERNAL_get_texelSize(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_texelSize (out Vector2 value) ;


    public IntPtr GetNativeTexturePtr () {
        IntPtr result;
        INTERNAL_CALL_GetNativeTexturePtr ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeTexturePtr (Texture self, out IntPtr value);
    [System.Obsolete ("Use GetNativeTexturePtr instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetNativeTextureID () ;

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

}

public sealed partial class Texture2D : Texture
{
    public extern int mipmapCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Texture2D(int width, int height)
        {
            Internal_Create(this, width, height, TextureFormat.RGBA32, true, false, IntPtr.Zero);
        }
    
    
    public Texture2D(int width, int height, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, width, height, format, mipmap, false, IntPtr.Zero);
        }
    
    
    public Texture2D(int width, int height, TextureFormat format, bool mipmap, bool linear)
        {
            Internal_Create(this, width, height, format, mipmap, linear, IntPtr.Zero);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] Texture2D mono, int width, int height, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex) ;

    internal Texture2D(int width, int height, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex)
        {
            Internal_Create(this, width, height, format, mipmap, linear, nativeTex);
        }
    
    
    static public Texture2D CreateExternalTexture(int width, int height, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");

            return new Texture2D(width, height, format, mipmap, linear, nativeTex);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void UpdateExternalTexture (IntPtr nativeTex) ;

    public extern TextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static Texture2D whiteTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static Texture2D blackTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void SetPixel (int x, int y, Color color) {
        INTERNAL_CALL_SetPixel ( this, x, y, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPixel (Texture2D self, int x, int y, ref Color color);
    public Color GetPixel (int x, int y) {
        Color result;
        INTERNAL_CALL_GetPixel ( this, x, y, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPixel (Texture2D self, int x, int y, out Color value);
    public Color GetPixelBilinear (float u, float v) {
        Color result;
        INTERNAL_CALL_GetPixelBilinear ( this, u, v, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPixelBilinear (Texture2D self, float u, float v, out Color value);
    [uei.ExcludeFromDocs]
public void SetPixels (Color[] colors) {
    int miplevel = 0;
    SetPixels ( colors, miplevel );
}

public void SetPixels(Color[] colors, [uei.DefaultValue("0")]  int miplevel )
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            SetPixels(0, 0, w, h, colors, miplevel);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPixels (int x, int y, int blockWidth, int blockHeight, Color[] colors, [uei.DefaultValue("0")]  int miplevel ) ;

    [uei.ExcludeFromDocs]
    public void SetPixels (int x, int y, int blockWidth, int blockHeight, Color[] colors) {
        int miplevel = 0;
        SetPixels ( x, y, blockWidth, blockHeight, colors, miplevel );
    }

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
    extern private void LoadRawTextureData_ImplArray (byte[] data) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void LoadRawTextureData_ImplPointer (IntPtr data, int size) ;

    public void LoadRawTextureData(byte[] data)
        {
            LoadRawTextureData_ImplArray(data);
        }
    
    
    public void LoadRawTextureData(IntPtr data, int size)
        {
            LoadRawTextureData_ImplPointer(data, size);
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
    extern public void Apply ( [uei.DefaultValue("true")] bool updateMipmaps , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public void Apply (bool updateMipmaps ) {
        bool makeNoLongerReadable = false;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public void Apply () {
        bool makeNoLongerReadable = false;
        bool updateMipmaps = true;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Resize (int width, int height, TextureFormat format, bool hasMipMap) ;

    public bool Resize(int width, int height) { return Internal_ResizeWH(width, height); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool Internal_ResizeWH (int width, int height) ;

    public void Compress (bool highQuality) {
        INTERNAL_CALL_Compress ( this, highQuality );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Compress (Texture2D self, bool highQuality);
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

public sealed partial class Cubemap : Texture
{
    public void SetPixel (CubemapFace face, int x, int y, Color color) {
        INTERNAL_CALL_SetPixel ( this, face, x, y, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPixel (Cubemap self, CubemapFace face, int x, int y, ref Color color);
    public Color GetPixel (CubemapFace face, int x, int y) {
        Color result;
        INTERNAL_CALL_GetPixel ( this, face, x, y, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPixel (Cubemap self, CubemapFace face, int x, int y, out Color value);
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

    public extern int mipmapCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Apply ( [uei.DefaultValue("true")] bool updateMipmaps , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public void Apply (bool updateMipmaps ) {
        bool makeNoLongerReadable = false;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public void Apply () {
        bool makeNoLongerReadable = false;
        bool updateMipmaps = true;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    public extern TextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Cubemap(int size, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, size, format, mipmap, IntPtr.Zero);
        }
    
    
    internal Cubemap(int size, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            Internal_Create(this, size, format, mipmap, nativeTex);
        }
    
    
    static public Cubemap CreateExternalTexture(int size, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");

            return new Cubemap(size, format, mipmap, nativeTex);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] Cubemap mono, int size, TextureFormat format, bool mipmap, IntPtr nativeTex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SmoothEdges ( [uei.DefaultValue("1")] int smoothRegionWidthInPixels ) ;

    [uei.ExcludeFromDocs]
    public void SmoothEdges () {
        int smoothRegionWidthInPixels = 1;
        SmoothEdges ( smoothRegionWidthInPixels );
    }

}

public sealed partial class Texture3D : Texture
{
    public extern int depth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

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

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Apply ( [uei.DefaultValue("true")] bool updateMipmaps , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public void Apply (bool updateMipmaps ) {
        bool makeNoLongerReadable = false;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public void Apply () {
        bool makeNoLongerReadable = false;
        bool updateMipmaps = true;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    public extern TextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Texture3D(int width, int height, int depth, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, width, height, depth, format, mipmap);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] Texture3D mono, int width, int height, int depth, TextureFormat format, bool mipmap) ;

}

public sealed partial class Texture2DArray : Texture
{
    public extern int depth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern TextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Apply ( [uei.DefaultValue("true")] bool updateMipmaps , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public void Apply (bool updateMipmaps ) {
        bool makeNoLongerReadable = false;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public void Apply () {
        bool makeNoLongerReadable = false;
        bool updateMipmaps = true;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    public Texture2DArray(int width, int height, int depth, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, width, height, depth, format, mipmap, false);
        }
    
    
    public Texture2DArray(int width, int height, int depth, TextureFormat format, bool mipmap, bool linear)
        {
            Internal_Create(this, width, height, depth, format, mipmap, linear);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] Texture2DArray mono, int width, int height, int depth, TextureFormat format, bool mipmap, bool linear) ;

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
    public extern int cubemapCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern TextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Apply ( [uei.DefaultValue("true")] bool updateMipmaps , [uei.DefaultValue("false")]  bool makeNoLongerReadable ) ;

    [uei.ExcludeFromDocs]
    public void Apply (bool updateMipmaps ) {
        bool makeNoLongerReadable = false;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    [uei.ExcludeFromDocs]
    public void Apply () {
        bool makeNoLongerReadable = false;
        bool updateMipmaps = true;
        Apply ( updateMipmaps, makeNoLongerReadable );
    }

    public CubemapArray(int faceSize, int cubemapCount, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, faceSize, cubemapCount, format, mipmap, false);
        }
    
    
    public CubemapArray(int faceSize, int cubemapCount, TextureFormat format, bool mipmap, bool linear)
        {
            Internal_Create(this, faceSize, cubemapCount, format, mipmap, linear);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] CubemapArray mono, int faceSize, int cubemapCount, TextureFormat format, bool mipmap, bool linear) ;

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
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateRenderTexture ([Writable] RenderTexture rt) ;

    public RenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            Internal_CreateRenderTexture(this);
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.format = format;

            bool sRGB = readWrite == RenderTextureReadWrite.sRGB;
            if (readWrite == RenderTextureReadWrite.Default)
            {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
            }
            Internal_SetSRGBReadWrite(this, sRGB);
        }
    
    
    public RenderTexture(int width, int height, int depth, RenderTextureFormat format)
        {
            Internal_CreateRenderTexture(this);
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.format = format;
            Internal_SetSRGBReadWrite(this, QualitySettings.activeColorSpace == ColorSpace.Linear);
        }
    
    
    public RenderTexture(int width, int height, int depth)
        {
            Internal_CreateRenderTexture(this);
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.format = RenderTextureFormat.Default;
            Internal_SetSRGBReadWrite(this, QualitySettings.activeColorSpace == ColorSpace.Linear);
        }
    
    
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
    [uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing , RenderTextureMemoryless memorylessMode , VRTextureUsage vrUsage ) {
    bool useDynamicScale = false;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing , RenderTextureMemoryless memorylessMode ) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer , RenderTextureFormat format , RenderTextureReadWrite readWrite , int antiAliasing ) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer , RenderTextureFormat format , RenderTextureReadWrite readWrite ) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
    int antiAliasing = 1;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer , RenderTextureFormat format ) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
    int antiAliasing = 1;
    RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height, int depthBuffer ) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
    int antiAliasing = 1;
    RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
    RenderTextureFormat format = RenderTextureFormat.Default;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

[uei.ExcludeFromDocs]
public static RenderTexture GetTemporary (int width, int height) {
    bool useDynamicScale = false;
    VRTextureUsage vrUsage = VRTextureUsage.None;
    RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None;
    int antiAliasing = 1;
    RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
    RenderTextureFormat format = RenderTextureFormat.Default;
    int depthBuffer = 0;
    return GetTemporary ( width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale );
}

public static RenderTexture GetTemporary(int width, int height, [uei.DefaultValue("0")]  int depthBuffer , [uei.DefaultValue("RenderTextureFormat.Default")]  RenderTextureFormat format , [uei.DefaultValue("RenderTextureReadWrite.Default")]  RenderTextureReadWrite readWrite , [uei.DefaultValue("1")]  int antiAliasing , [uei.DefaultValue("RenderTextureMemoryless.None")]  RenderTextureMemoryless memorylessMode , [uei.DefaultValue("VRTextureUsage.None")]  VRTextureUsage vrUsage , [uei.DefaultValue("false")]  bool useDynamicScale )
        {
            var desc = new RenderTextureDescriptor(width, height);
            desc.depthBufferBits = depthBuffer;
            desc.vrUsage = vrUsage;
            desc.colorFormat = format;
            desc.sRGB = (readWrite != RenderTextureReadWrite.Linear);
            desc.msaaSamples = antiAliasing;
            desc.memoryless = memorylessMode;
            desc.useDynamicScale = useDynamicScale;
            return GetTemporary(desc);
        }

    
    
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

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetWidth (RenderTexture mono) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetWidth (RenderTexture mono, int width) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetHeight (RenderTexture mono) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetHeight (RenderTexture mono, int width) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  VRTextureUsage Internal_GetVRUsage (RenderTexture mono) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetVRUsage (RenderTexture mono, VRTextureUsage vrUsage) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetSRGBReadWrite (RenderTexture mono, bool sRGB) ;

    
            override public int width { get { return Internal_GetWidth(this); } set { Internal_SetWidth(this, value); } }
    
    
    
            override public int height { get { return Internal_GetHeight(this); } set { Internal_SetHeight(this, value); } }
    
    
    
            public VRTextureUsage vrUsage { get { return Internal_GetVRUsage(this); } set { Internal_SetVRUsage(this, value); }}
    
    
    public extern  int depth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool isPowerOfTwo
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool sRGB
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  RenderTextureFormat format
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useMipMap
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool autoGenerateMips
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  UnityEngine.Rendering.TextureDimension Internal_GetDimension (RenderTexture rt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetDimension (RenderTexture rt, UnityEngine.Rendering.TextureDimension dim) ;

    
            override public UnityEngine.Rendering.TextureDimension dimension { get { return Internal_GetDimension(this); } set { Internal_SetDimension(this, value); } }
    
    
    
    [System.Obsolete ("Use RenderTexture.dimension instead.")]
    public extern  bool isCubemap
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Use RenderTexture.dimension instead.")]
    public extern  bool isVolume
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int volumeDepth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  RenderTextureMemoryless memorylessMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int antiAliasing
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool bindTextureMS
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool enableRandomWrite
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useDynamicScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool Create () {
        return INTERNAL_CALL_Create ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Create (RenderTexture self);
    public void Release () {
        INTERNAL_CALL_Release ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Release (RenderTexture self);
    public bool IsCreated () {
        return INTERNAL_CALL_IsCreated ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsCreated (RenderTexture self);
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
    public void GenerateMips () {
        INTERNAL_CALL_GenerateMips ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GenerateMips (RenderTexture self);
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
