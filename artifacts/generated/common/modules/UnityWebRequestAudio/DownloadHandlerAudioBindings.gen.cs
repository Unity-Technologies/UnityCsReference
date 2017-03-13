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
using UnityEngineInternal;

namespace UnityEngine.Networking
{



[StructLayout(LayoutKind.Sequential)]
public sealed partial class DownloadHandlerAudioClip : DownloadHandler
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateAudioClip (string url, AudioType audioType) ;

    [RequiredByNativeCode] 
    public DownloadHandlerAudioClip(string url, AudioType audioType)
        {
            InternalCreateAudioClip(url, audioType);
        }
    
    
    protected override byte[] GetData()
        {
            return InternalGetData();
        }
    
    
    
    protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for audio clips");
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private byte[] InternalGetData () ;

    public extern  AudioClip audioClip
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static AudioClip GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAudioClip>(www).audioClip;
        }
    
    
}

[StructLayout(LayoutKind.Sequential)]
public sealed partial class DownloadHandlerMovieTexture : DownloadHandler
{
    public DownloadHandlerMovieTexture()
        {
            InternalCreateDHMovieTexture();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateDHMovieTexture () ;

    protected override byte[] GetData()
        {
            return InternalGetData();
        }
    
    
    protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for movies");
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private byte[] InternalGetData () ;

    public extern  MovieTexture movieTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static MovieTexture GetContent(UnityWebRequest uwr)
        {
            return GetCheckedDownloader<DownloadHandlerMovieTexture>(uwr).movieTexture;
        }
    
    
}


}
