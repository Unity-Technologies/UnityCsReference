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
public partial class DownloadHandler : IDisposable
{
    [System.NonSerialized]
            internal IntPtr m_Ptr;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateBuffer () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateScript () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreateAssetBundle (string url, uint crc) ;

    internal void InternalCreateAssetBundle (string url, Hash128 hash, uint crc) {
        INTERNAL_CALL_InternalCreateAssetBundle ( this, url, ref hash, crc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalCreateAssetBundle (DownloadHandler self, string url, ref Hash128 hash, uint crc);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalDestroy () ;

    internal DownloadHandler()
        {}
    
    
    ~DownloadHandler()
        {
            InternalDestroy();
        }
    
    
    public void Dispose()
        {
            InternalDestroy();
            GC.SuppressFinalize(this);
        }
    
    
    public extern  bool isDone
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    
            public byte[] data
        {
            get { return GetData(); }
        }
    
    
    public string text
        {
            get { return GetText(); }
        }
    
    
    
    protected virtual byte[] GetData() { return null; }
    
    
    
    protected virtual string GetText()
        {
            byte[] bytes = GetData();
            if (bytes != null && bytes.Length > 0)
            {
                return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }
            else
            {
                return "";
            }
        }
    
    
    [UsedByNativeCode]
    protected virtual bool ReceiveData(byte[] data, int dataLength) { return true; }
    
    
    [UsedByNativeCode]
    protected virtual void ReceiveContentLength(int contentLength) {}
    
    
    [UsedByNativeCode]
    protected virtual void CompleteContent() {}
    
    
    [UsedByNativeCode]
    protected virtual float GetProgress() { return 0.0f; }
    
    
    protected static T GetCheckedDownloader<T>(UnityWebRequest www) where T : DownloadHandler
        {
            if (www == null)
                throw new System.NullReferenceException("Cannot get content from a null UnityWebRequest object");
            if (!www.isDone)
                throw new System.InvalidOperationException("Cannot get content from an unfinished UnityWebRequest object");
            if (www.isError)
                throw new System.InvalidOperationException(www.error);
            return (T)www.downloadHandler;
        }
    
    
}

[StructLayout(LayoutKind.Sequential)]
public sealed partial class DownloadHandlerBuffer : DownloadHandler
{
    public DownloadHandlerBuffer()
        {
            InternalCreateBuffer();
        }
    
    
    protected override byte[] GetData()
        {
            return InternalGetData();
        }
    
    
    protected override string GetText()
        {
            return InternalGetText();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private byte[] InternalGetData () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private string InternalGetText () ;

    public static string GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerBuffer>(www).text;
        }
    
    
}

[StructLayout(LayoutKind.Sequential)]
public partial class DownloadHandlerScript : DownloadHandler
{
    public DownloadHandlerScript()
        {
            InternalCreateScript();
        }
    
    
    public DownloadHandlerScript(byte[] preallocatedBuffer)
        {
            if (preallocatedBuffer == null || preallocatedBuffer.Length < 1)
            {
                throw new System.ArgumentException("Cannot create a preallocated-buffer DownloadHandlerScript backed by a null or zero-length array");
            }

            InternalCreateScript();
            InternalSetPreallocatedBuffer(preallocatedBuffer);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetPreallocatedBuffer (byte[] buffer) ;

}

[StructLayout(LayoutKind.Sequential)]
public sealed partial class DownloadHandlerAssetBundle : DownloadHandler
{
    public DownloadHandlerAssetBundle(string url, uint crc)
        {
            InternalCreateAssetBundle(url, crc);
        }
    
    
    public DownloadHandlerAssetBundle(string url, uint version, uint crc)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, version);
            InternalCreateAssetBundle(url, tempHash, crc);
        }
    
    
    public DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundle(url, hash, crc);
        }
    
    
    
    protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported for asset bundles");
        }
    
    
    
    protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for asset bundles");
        }
    
    
    public extern  AssetBundle assetBundle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static AssetBundle GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAssetBundle>(www).assetBundle;
        }
    
    
}


}
