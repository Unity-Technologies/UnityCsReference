// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEditorInternal;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace UnityEditor
{


internal sealed partial class AsyncHTTPClient
{
    private delegate void RequestProgressCallback(AsyncHTTPClient.State status, int downloaded, int totalSize);
    private delegate void RequestDoneCallback(AsyncHTTPClient.State status, int httpStatus);
    
    
    private static IntPtr SubmitClientRequest (string tag, string url, string[] headers, string method, string data, RequestDoneCallback doneDelegate, [uei.DefaultValue("null")]  RequestProgressCallback progressDelegate ) {
        IntPtr result;
        INTERNAL_CALL_SubmitClientRequest ( tag, url, headers, method, data, doneDelegate, progressDelegate, out result );
        return result;
    }

    [uei.ExcludeFromDocs]
    private static IntPtr SubmitClientRequest (string tag, string url, string[] headers, string method, string data, RequestDoneCallback doneDelegate) {
        RequestProgressCallback progressDelegate = null;
        IntPtr result;
        INTERNAL_CALL_SubmitClientRequest ( tag, url, headers, method, data, doneDelegate, progressDelegate, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SubmitClientRequest (string tag, string url, string[] headers, string method, string data, RequestDoneCallback doneDelegate, RequestProgressCallback progressDelegate, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  byte[] GetBytesByHandle (IntPtr handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Texture2D GetTextureByHandle (IntPtr handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AbortByTag (string tag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void AbortByHandle (IntPtr handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CurlRequestCheck () ;

}


}
