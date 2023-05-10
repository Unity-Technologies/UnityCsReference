// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandler.h")]
    public class DownloadHandler : IDisposable
    {
        [System.NonSerialized]
        [VisibleToOtherModules]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void Release();

        [VisibleToOtherModules]
        internal DownloadHandler()
        {}

        ~DownloadHandler()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

        public bool isDone { get { return IsDone(); } }

        private extern bool IsDone();

        public string error { get { return GetErrorMsg(); } }

        private extern string GetErrorMsg();

        public NativeArray<byte>.ReadOnly nativeData
        {
            get { return GetNativeData().AsReadOnly(); }
        }

        public byte[] data
        {
            get { return GetData(); }
        }

        public string text
        {
            get { return GetText(); }
        }

        protected virtual NativeArray<byte> GetNativeData() { return default; }

        protected virtual byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        protected virtual string GetText()
        {
            var nativeData = GetNativeData();
            if (nativeData.IsCreated && nativeData.Length > 0)
                unsafe
                {
                    return new string((sbyte*)nativeData.GetUnsafeReadOnlyPtr(), 0, nativeData.Length, GetTextEncoder());
                }
            return "";
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
                        catch (NotSupportedException e)
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

        [RequiredByNativeCode]
        protected virtual void ReceiveContentLengthHeader(ulong contentLength)
        {
            #pragma warning disable 618
            ReceiveContentLength((int)contentLength);
            #pragma warning restore 0618
        }

        [Obsolete("Use ReceiveContentLengthHeader")]
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
            if (www.result == UnityWebRequest.Result.ProtocolError)
                throw new System.InvalidOperationException(www.error);
            // Invalid cast exception will be thrown if T is not a correct DLH
            return (T)www.downloadHandler;
        }

        [NativeThrows]
        [VisibleToOtherModules]
        internal extern static unsafe byte* InternalGetByteArray(DownloadHandler dh, out int length);

        internal static byte[] InternalGetByteArray(DownloadHandler dh)
        {
            var nativeData = dh.GetNativeData();
            if (nativeData.IsCreated)
                return nativeData.ToArray();
            return null;
        }

        [VisibleToOtherModules("UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestTextureModule")]
        internal static NativeArray<byte> InternalGetNativeArray(DownloadHandler dh, ref NativeArray<byte> nativeArray)
        {
            unsafe
            {
                int length;
                byte* bytes = InternalGetByteArray(dh, out length);
                if (nativeArray.IsCreated)
                {
                    // allow partial data to be accessed, recreate array if changed
                    if (nativeArray.Length == length)
                        return nativeArray;
                    DisposeNativeArray(ref nativeArray);
                }
                CreateNativeArrayForNativeData(ref nativeArray, bytes, length);
                return nativeArray;
            }
        }

        [VisibleToOtherModules("UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestTextureModule")]
        internal static void DisposeNativeArray(ref NativeArray<byte> data)
        {
            if (!data.IsCreated)
                return;
            var safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(data);
            AtomicSafetyHandle.Release(safety);
            data = default;
        }

        internal static unsafe void CreateNativeArrayForNativeData(ref NativeArray<byte> data, byte* bytes, int length)
        {
            data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(bytes, length, Allocator.Persistent);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref data, safety);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandler handler) => handler.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerBuffer.h")]
    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
        private extern static IntPtr Create([Unmarshalled] DownloadHandlerBuffer obj);

        private NativeArray<byte> m_NativeData;

        private void InternalCreateBuffer()
        {
            m_Ptr = Create(this);
        }

        public DownloadHandlerBuffer()
        {
            InternalCreateBuffer();
        }

        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        public static string GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerBuffer>(www).text;
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerBuffer handler) => handler.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerScript.h")]
    public class DownloadHandlerScript : DownloadHandler
    {
        private extern static IntPtr Create([Unmarshalled] DownloadHandlerScript obj);
        private extern static IntPtr CreatePreallocated([Unmarshalled] DownloadHandlerScript obj, [Unmarshalled]byte[] preallocatedBuffer);

        private void InternalCreateScript()
        {
            m_Ptr = Create(this);
        }

        private void InternalCreateScript(byte[] preallocatedBuffer)
        {
            m_Ptr = CreatePreallocated(this, preallocatedBuffer);
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

            InternalCreateScript(preallocatedBuffer);
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerScript handler) => handler.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerVFS.h")]
    public sealed class DownloadHandlerFile : DownloadHandler
    {
        [NativeThrows]
        private extern static IntPtr Create([Unmarshalled] DownloadHandlerFile obj, string path, bool append);

        private void InternalCreateVFS(string path, bool append)
        {
            string dir = Path.GetDirectoryName(path);
            // On UWP CreateDirectory fails when passing something like Application.presistentDataPath (works if subdir of it)
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            m_Ptr = Create(this, path, append);
        }

        public DownloadHandlerFile(string path)
        {
            InternalCreateVFS(path, false);
        }

        public DownloadHandlerFile(string path, bool append)
        {
            InternalCreateVFS(path, append);
        }

        protected override NativeArray<byte> GetNativeData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
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

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerFile handler) => handler.m_Ptr;
        }

    }
}
