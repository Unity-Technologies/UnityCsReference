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


public static partial class ImageConversion
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  byte[] EncodeToPNG (this Texture2D tex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  byte[] EncodeToJPG (this Texture2D tex, int quality) ;

    static public byte[] EncodeToJPG(this Texture2D tex)
        {
            return tex.EncodeToJPG(75);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  byte[] EncodeToEXR (this Texture2D tex, [uei.DefaultValue("Texture2D.EXRFlags.None")]  Texture2D.EXRFlags flags ) ;

    [uei.ExcludeFromDocs]
    public static byte[] EncodeToEXR (this Texture2D tex) {
        Texture2D.EXRFlags flags = Texture2D.EXRFlags.None;
        return EncodeToEXR ( tex, flags );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool LoadImage (this Texture2D tex, byte[] data, [uei.DefaultValue("false")]  bool markNonReadable ) ;

    [uei.ExcludeFromDocs]
    public static bool LoadImage (this Texture2D tex, byte[] data) {
        bool markNonReadable = false;
        return LoadImage ( tex, data, markNonReadable );
    }

}


}
