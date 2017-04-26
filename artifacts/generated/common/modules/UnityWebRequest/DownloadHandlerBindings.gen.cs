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
using System.Text;
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

    internal void InternalCreateAssetBundleCached (string url, string name, Hash128 hash, uint crc) {
        INTERNAL_CALL_InternalCreateAssetBundleCached ( this, url, name, ref hash, crc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalCreateAssetBundleCached (DownloadHandler self, string url, string name, ref Hash128 hash, uint crc);
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
                return GetTextEncoder().GetString(bytes, 0, bytes.Length);
            }
            else
            {
                return "";
            }
        }
    
    
    private Encoding GetTextEncoder()
        {
            string contentType = GetContentType();
            if (!string.IsNullOrEmpty(contentType))
            {
                int charsetKeyIndex = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
                if (charsetKeyIndex > -1)
                {
                    int charsetValueIndex = contentType.IndexOf('=', charsetKeyIndex);
                    if (charsetValueIndex > -1)
                    {
                        string encoding = contentType.Substring(charsetValueIndex + 1).Trim().Trim(new[] {'\'', '"'}).Trim();
                        int semicolonIndex = encoding.IndexOf(';');
                        if (semicolonIndex > -1)
                            encoding = encoding.Substring(0, semicolonIndex);
                        try
                        {
                            return System.Text.Encoding.GetEncoding(encoding);
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogWarning(string.Format("Unsupported encoding '{0}': {1}", encoding, e.Message));
                        }
                    }
                }
            }

            return System.Text.Encoding.UTF8;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private string GetContentType () ;

    
    
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
            if (www.isNetworkError)
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private byte[] InternalGetData () ;

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
            InternalCreateAssetBundleCached(url, "", new Hash128(0, 0, 0, version), crc);
        }
    
    
    public DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, "", hash, crc);
        }
    
    
    public DownloadHandlerAssetBundle(string url, string name, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, name, hash, crc);
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
