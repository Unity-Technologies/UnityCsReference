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
using UnityEngine;

namespace UnityEditor.Sprites
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AtlasSettings
{
            public TextureFormat    format;
            public ColorSpace       colorSpace;
            public int              compressionQuality;
            public FilterMode       filterMode;
            public int              maxWidth;
            public int              maxHeight;
            public uint             paddingPower;
            public int              anisoLevel;
            public bool             generateMipMaps;
            public bool             enableRotation;
            public bool             allowsAlphaSplitting;
}

public sealed partial class PackerJob
{
    
            internal PackerJob() {}
    
    
    public void AddAtlas(string atlasName, AtlasSettings settings)
        {
            AddAtlas_Internal(atlasName, ref settings);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void AddAtlas_Internal (string atlasName, ref AtlasSettings settings) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void AssignToAtlas (string atlasName, Sprite sprite, SpritePackingMode packingMode, SpritePackingRotation packingRotation) ;

}

public sealed partial class Packer
{
    public extern static string[] atlasNames
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D[] GetTexturesForAtlas (string atlasName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D[] GetAlphaTexturesForAtlas (string atlasName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RebuildAtlasCacheIfNeeded (BuildTarget target, [uei.DefaultValue("false")]  bool displayProgressBar , [uei.DefaultValue("Execution.Normal")]  Execution execution ) ;

    [uei.ExcludeFromDocs]
    public static void RebuildAtlasCacheIfNeeded (BuildTarget target, bool displayProgressBar ) {
        Execution execution = Execution.Normal;
        RebuildAtlasCacheIfNeeded ( target, displayProgressBar, execution );
    }

    [uei.ExcludeFromDocs]
    public static void RebuildAtlasCacheIfNeeded (BuildTarget target) {
        Execution execution = Execution.Normal;
        bool displayProgressBar = false;
        RebuildAtlasCacheIfNeeded ( target, displayProgressBar, execution );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void GetAtlasDataForSprite (Sprite sprite, out string atlasName, out Texture2D atlasTexture) ;

}


}
