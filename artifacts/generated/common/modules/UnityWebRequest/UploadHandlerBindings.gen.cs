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
public partial class UploadHandler : IDisposable
{
    [System.NonSerialized]
            internal IntPtr m_Ptr;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateRaw (byte[] data) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalDestroy () ;

    internal UploadHandler() {}
    
    
    ~UploadHandler()
        {
            InternalDestroy();
        }
    
    
    public void Dispose()
        {
            InternalDestroy();
            GC.SuppressFinalize(this);
        }
    
    
    public byte[] data
        {
            get
            {
                return GetData();
            }
        }
    
    
    public string contentType
        {
            get
            {
                return GetContentType();
            }
            set
            {
                SetContentType(value);
            }
        }
    
    
    public float progress
        {
            get
            {
                return GetProgress();
            }
        }
    
    
    internal virtual byte[] GetData() { return null; }
    internal virtual string GetContentType() { return "text/plain"; }
    internal virtual void   SetContentType(string newContentType) {}
    internal virtual float  GetProgress() { return 0.5f; }
}

[StructLayout(LayoutKind.Sequential)]
public sealed partial class UploadHandlerRaw : UploadHandler
{
    public UploadHandlerRaw(byte[] data)
        {
            InternalCreateRaw(data);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private string InternalGetContentType () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetContentType (string newContentType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private byte[] InternalGetData () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private float InternalGetProgress () ;

    internal override string GetContentType() { return InternalGetContentType(); }
    
    
    internal override void   SetContentType(string newContentType)
        {
            InternalSetContentType(newContentType);
        }
    
    
    internal override byte[] GetData()
        {
            return InternalGetData();
        }
    
    
    internal override float GetProgress()
        {
            return InternalGetProgress();
        }
    
    
}

}
