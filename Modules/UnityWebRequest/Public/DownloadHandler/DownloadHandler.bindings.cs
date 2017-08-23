// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandler.h")]
    public class DownloadHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void Release();

        internal DownloadHandler()
        {}

        ~DownloadHandler()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

        public bool isDone { get { return IsDone(); } }

        private extern bool IsDone();

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
            // Check for charset type
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

            // Use default (utf8)
            return System.Text.Encoding.UTF8;
        }

        private extern string GetContentType();

        // Return true if you processed the data successfully, false otherwise.
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
            // Invalid cast exception will be thrown if T is not a correct DLH
            return (T)www.downloadHandler;
        }

        [NativeThrows]
        internal extern static byte[] InternalGetByteArray(DownloadHandler dh);
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerBuffer.h")]
    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
        private extern static IntPtr Create(DownloadHandlerBuffer obj);

        private void InternalCreateBuffer()
        {
            m_Ptr = Create(this);
        }

        public DownloadHandlerBuffer()
        {
            InternalCreateBuffer();
        }

        protected override byte[] GetData()
        {
            return InternalGetData();
        }

        private byte[] InternalGetData()
        {
            return InternalGetByteArray(this);
        }

        public static string GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerBuffer>(www).text;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerScript.h")]
    public class DownloadHandlerScript : DownloadHandler
    {
        private extern static IntPtr Create(DownloadHandlerScript obj);

        private void InternalCreateScript()
        {
            m_Ptr = Create(this);
        }

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
            SetPreallocatedBuffer(preallocatedBuffer);
        }

        private extern void SetPreallocatedBuffer(byte[] buffer);
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerAssetBundle.h")]
    public sealed class DownloadHandlerAssetBundle : DownloadHandler
    {
        private extern static IntPtr Create(DownloadHandlerAssetBundle obj, string url, uint crc);
        private extern static IntPtr CreateCached(DownloadHandlerAssetBundle obj, string url, string name, Hash128 hash, uint crc);

        private void InternalCreateAssetBundle(string url, uint crc)
        {
            m_Ptr = Create(this, url, crc);
        }

        private void InternalCreateAssetBundleCached(string url, string name, Hash128 hash, uint crc)
        {
            m_Ptr = CreateCached(this, url, name, hash, crc);
        }

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

        public extern AssetBundle assetBundle { get; }

        public static AssetBundle GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAssetBundle>(www).assetBundle;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerVFS.h")]
    public sealed class DownloadHandlerFile : DownloadHandler
    {
        [NativeThrows]
        private extern static IntPtr Create(DownloadHandlerFile obj, string path);

        private void InternalCreateVFS(string path)
        {
            m_Ptr = Create(this, path);
        }

        public DownloadHandlerFile(string path)
        {
            InternalCreateVFS(path);
        }

        protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported");
        }

        public extern bool removeFileOnAbort { get; set; }
    }
}
