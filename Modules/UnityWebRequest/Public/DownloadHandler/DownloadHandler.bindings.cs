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
    ///<summary>Manage and process HTTP response body data received from a remote server.</summary>
    ///<remarks>DownloadHandler objects are helper objects. When attached to a <see cref="UnityWebRequest" />, they define how to handle HTTP response body data received from a remote server. Generally, they are used to buffer, stream and/or process response bodies.
    ///
    ///DownloadHandler is a base class. Depending on usage scenario, different specialized classes are available. <see cref="DownloadHandlerBuffer" /> provides basic buffering, while <see cref="T:UnityEngine.Networking.DownloadHandlerTexture" /> and <see cref="T:UnityEngine.Networking.DownloadHandlerAssetBundle" /> provide more efficient solutions for <see cref="Texture" /> and <see cref="T:UnityEngine.AssetBundle" /> downloads.
    ///
    ///For custom use cases, see <see cref="DownloadHandlerScript" />.</remarks>
    ///<seealso cref="UnityWebRequest" />
    ///<seealso cref="DownloadHandlerBuffer" />
    ///<seealso cref="T:UnityEngine.Networking.DownloadHandlerTexture" />
    ///<seealso cref="T:UnityEngine.Networking.DownloadHandlerAudioClip" />
    ///<seealso cref="T:UnityEngine.Networking.DownloadHandlerAssetBundle" />
    ///<seealso cref="DownloadHandlerScript" />
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandler.h")]
    public class DownloadHandler : IDisposable
    {
        [System.NonSerialized]
        [VisibleToOtherModules]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void ReleaseFromScripting();

        [VisibleToOtherModules]
        internal DownloadHandler()
        {}

#pragma warning disable UAC0045 // Releases the native handler; ReleaseFromScripting is thread-safe, so it is the safety net when Dispose is missed.
        ~DownloadHandler()
        {
            Dispose();
        }
#pragma warning restore UAC0045

        ///<summary>Signals that this <see cref="DownloadHandler" /> is no longer being used, and should clean up any resources it is using.</summary>
        ///<remarks>This method must be called once you have finished using a [DownloadHandler] object.
        ///
        ///For convenience, <see cref="UnityWebRequest" /> exposes the <see cref="UnityWebRequest.disposeDownloadHandlerOnDispose" /> property, which will automatically call this method when <see cref="UnityWebRequest.Dispose" /> is called.
        ///
        ///If you elect not to use <see cref="UnityWebRequest.disposeDownloadHandlerOnDispose" /> (by setting it to false), then you must call Dispose on the <see cref="DownloadHandler" /> yourself. This should only be in rare cases, such as when you wish to save the data in the <see cref="DownloadHandler" />'s internal buffer(s) for later processing.</remarks>
        public virtual void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ReleaseFromScripting();
                m_Ptr = IntPtr.Zero;
            }
        }

        ///<summary>Returns <c>true</c> if this DownloadHandler has been informed by its parent <see cref="UnityWebRequest" /> that all data has been received, and this DownloadHandler has completed any necessary post-download processing. (RO)</summary>
        public bool isDone { get { return IsDone(); } }

        private extern bool IsDone();

        ///<summary>Error message describing a failure that occurred inside the download handler.</summary>
        ///<remarks>When a UnityWebRequest ends with the result, <see cref="UnityWebRequest.Result.DataProcessingError" />, this property contains a message describing the error. For example, when an AssetBundle is successfully downloaded, but cannot be loaded, this property explains the reason the asset bundle could not be loaded.</remarks>
        public string error { get { return GetErrorMsg(); } }

        private extern string GetErrorMsg();

        ///<summary>Provides direct access to downloaded data.</summary>
        ///<remarks>Allocation-free access to downloaded data, stored inside download handler. See also <see cref="GetNativeData" />.</remarks>
        public NativeArray<byte>.ReadOnly nativeData
        {
            get { return GetNativeData().AsReadOnly(); }
        }

        ///<summary>Returns the raw bytes downloaded from the remote server, or <c>null</c>. (RO)</summary>
        ///<remarks>This property returns the raw bytes downloaded from the remote server. If no data has yet been received, this property returns <c>null</c>.
        ///
        ///Note: Note: The precise memory allocation behavior of this property changes depending on subclass. See subclass documentation of <see cref="GetData" /> for further information on exactly how the data property handles memory allocation.</remarks>
        public byte[] data
        {
            get { return GetData(); }
        }

        ///<summary>Convenience property. Returns the bytes from <see cref="data" /> interpreted as a UTF8 string. (RO)</summary>
        public string text
        {
            get { return GetText(); }
        }

        ///<summary>Provides allocation-free access to the downloaded data as a NativeArray.</summary>
        ///<remarks>The data is owned by the download handler and memory is released when the download handler is disposed. The returned NativeArray is read-only.</remarks>
        ///<returns>NativeArray providing access to downloaded data.</returns>
        protected virtual NativeArray<byte> GetNativeData() { return default; }

        ///<summary>Callback, invoked when the <see cref="data" /> property is accessed.</summary>
        ///<remarks>The return value of this method will be returned as the value of the <see cref="data" /> property.
        ///
        ///This method will be invoked on the main thread.
        ///
        ///If not overridden, the default behavior of this callback is to return <c>null</c>.</remarks>
        ///<returns>Byte array to return as the value of the <see cref="data" /> property.</returns>
        protected virtual byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        ///<summary>Callback, invoked when the <see cref="text" /> property is accessed.</summary>
        ///<remarks>The return value of this method will be returned as the value of the <see cref="data" /> property.
        ///
        ///This method will be invoked on the main thread.
        ///
        ///If not overridden, the default behavior of this callback is to call <see cref="GetData" />. If <see cref="GetData" /> returns <c>null</c> or an empty string, then this method will return <c>null</c> or an empty string (respectively). Otherwise, this method will decode the byte array returned from <see cref="GetData" /> as a UTF8 string and return the decoded string.</remarks>
        ///<returns>String to return as the return value of the <see cref="text" /> property.</returns>
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
        ///<summary>Callback, invoked as data is received from the remote server.</summary>
        ///<remarks>This callback is invoked on the main thread.
        ///
        ///<c>ReceiveData</c> is called after data has arrived from the remote server and can be called multiple times per frame. The <c>data</c> argument contains the raw bytes received from the remote server, and <c>dataLength</c> indicates the length of new data in the data array.
        ///
        ///<c>ReceiveData</c> requires a return value of either <c>true</c> or <c>false</c>. If you return <c>false</c>, the system immediately aborts the UnityWebRequest. If you return <c>true</c>, processing continues normally.
        ///
        ///Data arriving from the remote server for a <see cref="DownloadHandlerScript" /> is kept in a temporary buffer. When there is unprocessed data in the buffer, this method is called once per frame to pass chunks of the data to the script. If multiple datagrams arrive within one frame, they are combined before being passed to this callback. The data byte array contains the received data.
        ///
        ///When operating in non-preallocated mode, the system allocates a new byte array each time this callback is invoked. In this case, <c>data.Length</c> is equal to <c>dataLength</c>, and you can safely ignore the <c>dataLength</c> argument.
        ///
        ///When operating in preallocated mode, the data argument will be the byte array passed in at construction time, and the dataLength argument indicates which bytes in the byte array are new.
        ///
        ///**Important:** The system does not zero-out the array between calls.
        ///
        ///For more information on allocation modes, refer to the constructor description for <see cref="DownloadHandlerScript" />.</remarks>
        ///<param name="data">A buffer containing unprocessed data, received from the remote server.</param>
        ///<param name="dataLength">The number of bytes in <c>data</c> which are new.</param>
        ///<returns>True if the download should continue, false to abort.</returns>
        [RequiredByNativeCode]
        protected virtual bool ReceiveData(byte[] data, int dataLength) { return true; }

        ///<summary>Callback, invoked with a <c>Content-Length</c> header is received.</summary>
        ///<remarks>This callback is invoked on the main thread.
        ///
        ///This callback is only called if a <c>Content-Length</c> header is received. If the remote server doesn't transmit a <c>Content-Length</c> header, but includes body data, then it's possible for <see cref="DownloadHandler.ReceiveData" /> to be invoked without receiving a call to this method.
        ///
        ///This callback might be invoked more than once. For example, if a redirect is encountered that has a <c>Content-Length</c> header, followed by a standard response, which also has a <c>Content-Length</c> header, this method is invoked twice.</remarks>
        ///<param name="contentLength">The value of the received <c>Content-Length</c> header.</param>
        [RequiredByNativeCode]
        protected virtual void ReceiveContentLengthHeader(ulong contentLength)
        {
            #pragma warning disable 618
            ReceiveContentLength((int)contentLength);
            #pragma warning restore 0618
        }

        ///<summary>Callback, invoked with a <c>Content-Length</c> header is received.</summary>
        ///<remarks>Obsolete. Use <see cref="ReceiveContentLengthHeader" /> instead.</remarks>
        ///<param name="contentLength">The value of the received <c>Content-Length</c> header.</param>
        [Obsolete("Use ReceiveContentLengthHeader")]
        protected virtual void ReceiveContentLength(int contentLength) {}

        [RequiredByNativeCode]
        private static void CompleteHeadersStatic(DownloadHandler handler) { handler.CompleteHeaders(); }
        internal virtual void CompleteHeaders() { }

        ///<summary>Callback, invoked when all data has been received from the remote server.</summary>
        ///<remarks>This callback is guaranteed to be invoked on the main thread. If not overridden, this callback has no default behavior and will no-op.</remarks>
        [RequiredByNativeCode]
        protected virtual void CompleteContent() {}

        ///<summary>Callback, invoked when <see cref="UnityWebRequest.downloadProgress" /> is accessed.</summary>
        ///<remarks>This callback will be invoked when scripts access the <see cref="UnityWebRequest.downloadProgress" /> property on this DownloadHandler's parent <see cref="UnityWebRequest" />. The return value of this method will be returned as the value of the <see cref="UnityWebRequest.downloadProgress" /> property.
        ///
        ///This callback will be invoked on the main thread.
        ///
        ///If not overridden, the default behavior of this callback is to return <c>0.5</c>.</remarks>
        ///<returns>The return value for <see cref="UnityWebRequest.downloadProgress" />.</returns>
        [RequiredByNativeCode]
        protected virtual float GetProgress() { return 0.0f; }

        ///<exclude/>
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

        [NativeMethod(ThrowsException = true)]
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

    ///<summary>A general-purpose <see cref="DownloadHandler" /> implementation which stores received data in a native byte buffer.</summary>
    ///<remarks>This is a general-purpose <see cref="DownloadHandler" /> subclass. It stores received data in native memory. It preallocates a data buffer based on any received <c>Content-Length</c> header, but expands its buffer if the actual download size exceeds the value of the <c>Content-Length</c> header (or if a <c>Content-Length</c> header is not received).
    ///
    ///**Note**: When accessing <see cref="DownloadHandler.data" /> or <see cref="DownloadHandler.text" /> on this subclass, a new byte array or string is allocated each time the property is accessed.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///using System.Collections;
    ///
    ///
    ///public class MyBehaviour : MonoBehaviour {
    ///    void Start() {
    ///        StartCoroutine(GetText());
    ///    }
    ///
    ///    IEnumerator GetText() {
    ///        UnityWebRequest www = new UnityWebRequest("https://www.my-server.com");
    ///        www.downloadHandler = new DownloadHandlerBuffer();
    ///        yield return www.SendWebRequest();
    ///
    ///        if (www.result != UnityWebRequest.Result.Success) {
    ///            Debug.Log(www.error);
    ///        }
    ///        else {
    ///            // Show results as text
    ///            Debug.Log(www.downloadHandler.text);
    ///
    ///            // Or retrieve results as binary data
    ///            byte[] results = www.downloadHandler.data;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerBuffer.h")]
    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerBuffer obj);

        private NativeArray<byte> m_NativeData;

        private void InternalCreateBuffer()
        {
            m_Ptr = Create(this);
        }

        ///<summary>Default constructor.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        using (var uwr = new UnityWebRequest("https://unity3d.com/", UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            uwr.downloadHandler = new DownloadHandlerBuffer();
        ///            yield return uwr.SendWebRequest();
        ///            Debug.Log(uwr.downloadHandler.text);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public DownloadHandlerBuffer()
        {
            InternalCreateBuffer();
        }

        ///<exclude />
        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        ///<exclude />
        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        ///<summary>Returns a copy of the native-memory buffer interpreted as a UTF8 string.</summary>
        ///<remarks>A static function provided for convenience; equivalent to ((DownloadHandlerBuffer)www.downloadHandler).text.</remarks>
        ///<param name="www">A finished UnityWebRequest object with <see cref="DownloadHandlerBuffer" /> attached.</param>
        ///<returns>The same as <see cref="DownloadHandler.text" /></returns>
        public static string GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerBuffer>(www).text;
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerBuffer handler) => handler.m_Ptr;
        }
    }

    ///<summary>An abstract base class for custom, scriptable <see cref="DownloadHandler" /> implementations.</summary>
    ///<remarks>Derive from <c>DownloadHandlerScript</c> and override some or all of its callbacks to implement custom data handling for key events in the download process.
    ///
    ///For example, override <see cref="DownloadHandler.ReceiveData" /> to implement custom handling when data arrives from the remote server and override <see cref="DownloadHandler.ReceiveContentLengthHeader" /> to implement custom handling when a Content-Length header is received.
    ///
    ///The <c>UnityWebRequest</c> system permits the pre-allocation of a managed-code byte array, which is used to deliver downloaded data to the <see cref="DownloadHandler.ReceiveData" /> callback. Using this function eliminates managed-code memory allocation when using classes derived from <c>DownloadHandlerScript</c>-to capture downloaded data.
    ///
    ///To make a <c>DownloadHandlerScript</c> operate with a pre-allocated managed buffer, supply a byte array to the <see cref="DownloadHandlerScript(byte[])" />. The size of the byte array limits the amount of data that can be received in each <c>ReceiveData</c> call. If data arrives slowly, over many frames, the supplied byte array might be too small.
    ///
    ///**Note**: The actual downloads occur on a worker thread, but all <c>DownloadHandlerScript</c> callbacks operate on the main thread. Avoid performing computationally heavy operations from these callbacks.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///
    ///public class LoggingDownloadHandler : DownloadHandlerScript {
    ///
    ///    // Standard scripted download handler - allocates memory on each ReceiveData callback
    ///
    ///    public LoggingDownloadHandler(): base() {
    ///    }
    ///
    ///    // Pre-allocated scripted download handler
    ///    // reuses the supplied byte array to deliver data.
    ///    // Eliminates memory allocation.
    ///
    ///    public LoggingDownloadHandler(byte[] buffer): base(buffer) {
    ///    }
    ///
    ///    // Required by DownloadHandler base class. Called when you address the 'bytes' property.
    ///
    ///    protected override byte[] GetData() { return null; }
    ///
    ///    // Called once per frame when data has been received from the network.
    ///
    ///    protected override bool ReceiveData(byte[] data, int dataLength) {
    ///        if(data == null) {
    ///            Debug.Log("LoggingDownloadHandler :: ReceiveData - received a null/empty buffer");
    ///            return false;
    ///        }
    ///
    ///        Debug.Log(string.Format("LoggingDownloadHandler :: ReceiveData - received {0} bytes", dataLength));
    ///        return true;
    ///    }
    ///
    ///    // Called when all data has been received from the server and delivered via ReceiveData.
    ///
    ///    protected override void CompleteContent() {
    ///        Debug.Log("LoggingDownloadHandler :: CompleteContent - DOWNLOAD COMPLETE!");
    ///    }
    ///
    ///    // Called when a Content-Length header is received from the server.
    ///
    ///    protected override void ReceiveContentLengthHeader(ulong contentLength) {
    ///        Debug.Log(string.Format("LoggingDownloadHandler :: ReceiveContentLength - length {0}", contentLength));
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="DownloadHandler.ReceiveData" />
    ///<seealso cref="DownloadHandler.ReceiveContentLengthHeader" />
    ///<seealso cref="DownloadHandler.CompleteContent" />
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerScript.h")]
    public class DownloadHandlerScript : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerScript obj);
        private extern static IntPtr CreatePreallocated([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerScript obj, [UnityMarshalAs(NativeType.ScriptingObjectPtr)]byte[] preallocatedBuffer);

        private void InternalCreateScript()
        {
            m_Ptr = Create(this);
        }

        private void InternalCreateScript(byte[] preallocatedBuffer)
        {
            m_Ptr = CreatePreallocated(this, preallocatedBuffer);
        }

        ///<summary>Create a DownloadHandlerScript which allocates new buffers when passing data to callbacks.</summary>
        ///<remarks>This default constructor places this <see cref="DownloadHandlerScript" /> into non-preallocated mode. This affects the operation of the <see cref="DownloadHandler.ReceiveData" /> callback.
        ///
        ///When in non-preallocated mode, a new managed byte array will be allocated each time <see cref="DownloadHandler.ReceiveData" /> is called, and the length of the array passed to <see cref="DownloadHandler.ReceiveData" /> will always be equal to the number of new bytes available for consumption.
        ///
        ///This is convenient, but may cause undesirable garbage collection. If your use case requires an implementation which avoids unnecessary garbage collection, use preallocated mode instead.</remarks>
        public DownloadHandlerScript()
        {
            InternalCreateScript();
        }

        ///<summary>Create a DownloadHandlerScript which reuses a preallocated buffer to pass data to callbacks.</summary>
        ///<remarks>This constructor places this <see cref="DownloadHandlerScript" /> into preallocated mode. This affects the operation of the <see cref="DownloadHandler.ReceiveData" /> callback.
        ///
        ///When in preallocated mode, the <c>preallocatedBuffer</c> byte array will be repeatedly reused to pass data to the <see cref="DownloadHandler.ReceiveData" /> callback, instead of allocating new buffers each time. The system will not zero-out the array between uses, so the <c>dataLength</c> argument to <see cref="DownloadHandler.ReceiveData" /> must be used to discover which bytes are new.
        ///
        ///When in this mode, the <see cref="DownloadHandlerScript" /> will not allocate any memory during the download or processing of HTTP response data. If your use case is sensitive to garbage collection, usage of preallocated mode is recommended.</remarks>
        ///<param name="preallocatedBuffer">A byte buffer into which data will be copied, for use by <see cref="DownloadHandler.ReceiveData" />.</param>
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

    ///<summary>Download handler for saving the downloaded data to file.</summary>
    ///<remarks>This specialized download handler writes all downloaded bytes directly to file. This can help avoid high memory usage.
    ///
    ///You can't retrieve data from this download handler. Instead, you work with the resulting file when the download is complete.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections;
    ///using System.IO;
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///
    ///public class FileDownloader : MonoBehaviour {
    ///
    ///    void Start () {
    ///        StartCoroutine(DownloadFile());
    ///    }
    ///
    ///    IEnumerator DownloadFile() {
    ///        var uwr = new UnityWebRequest("https://unity3d.com/", UnityWebRequest.kHttpVerbGET);
    ///        string path = Path.Combine(Application.persistentDataPath, "unity3d.html");
    ///        uwr.downloadHandler = new DownloadHandlerFile(path);
    ///        yield return uwr.SendWebRequest();
    ///        if (uwr.result != UnityWebRequest.Result.Success)
    ///            Debug.LogError(uwr.error);
    ///        else
    ///            Debug.Log("File successfully downloaded and saved to " + path);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerVFS.h")]
    public sealed class DownloadHandlerFile : DownloadHandler
    {
        [NativeMethod(ThrowsException = true)]
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerFile obj, string path, bool append);

        private void InternalCreateVFS(string path, bool append)
        {
            string dir = Path.GetDirectoryName(path);
            // On UWP CreateDirectory fails when passing something like Application.presistentDataPath (works if subdir of it)
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            m_Ptr = Create(this, path, append);
        }

        ///<summary>Creates a new instance and a file on disk where downloaded data will be written to.</summary>
        ///<remarks>Throws an ArgumentException if a file cannot be created.
        ///If a path to a file in a non-existent directory is given, all required directories are created. If a file exists, it will be overwritten, unless in append mode. If a file doesn't exist, it is created regardless of append flag.</remarks>
        ///<param name="path">Path to file to be written.</param>
        public DownloadHandlerFile(string path)
        {
            InternalCreateVFS(path, false);
        }

        ///<summary>Creates a new instance and a file on disk where downloaded data will be written to.</summary>
        ///<remarks>Throws an ArgumentException if a file cannot be created.
        ///If a path to a file in a non-existent directory is given, all required directories are created. If a file exists, it will be overwritten, unless in append mode. If a file doesn't exist, it is created regardless of append flag.</remarks>
        ///<param name="path">Path to file to be written.</param>
        ///<param name="append">When true, appends data to the given file instead of overwriting.</param>
        public DownloadHandlerFile(string path, bool append)
        {
            InternalCreateVFS(path, append);
        }

        ///<exclude />
        protected override NativeArray<byte> GetNativeData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        ///<exclude />
        protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        ///<exclude />
        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported");
        }

        ///<summary>Should the created file be removed if download is aborted (manually or due to an error). Default: false.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.IO;
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class DownloadHandlerFileSample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Download());
        ///    }
        ///
        ///    IEnumerator Download()
        ///    {
        ///        using var uwr = new UnityWebRequest("https://unity3d.com/");
        ///        uwr.method = UnityWebRequest.kHttpVerbGET;
        ///        var resultFile = Path.Combine(Application.persistentDataPath, "result.txt");
        ///        var dh = new DownloadHandlerFile(resultFile);
        ///        dh.removeFileOnAbort = true;
        ///        uwr.downloadHandler = dh;
        ///        yield return uwr.SendWebRequest();
        ///        if (uwr.result != UnityWebRequest.Result.Success)
        ///            Debug.LogError(uwr.error);
        ///        else
        ///        {
        ///            Debug.Log("Download saved to: " + resultFile);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool removeFileOnAbort { get; set; }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerFile handler) => handler.m_Ptr;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerCallback.h")]
    internal sealed class DownloadHandlerCallback : DownloadHandler
    {
        // Invoked on the transport thread; data is only valid until the delegate returns
        internal delegate bool ReceiveDataCallback(ReadOnlySpan<byte> data);
        internal delegate void ReceiveContentLengthCallback(ulong contentLength);
        internal delegate void CompleteCallback();

        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerCallback obj);

        private readonly ReceiveDataCallback m_OnReceiveData;
        private readonly ReceiveContentLengthCallback m_OnReceiveContentLength;
        private readonly CompleteCallback m_OnComplete;

        internal DownloadHandlerCallback(ReceiveDataCallback onReceiveData, ReceiveContentLengthCallback onReceiveContentLength = null, CompleteCallback onComplete = null)
        {
            if (onReceiveData == null)
                throw new ArgumentNullException(nameof(onReceiveData));

            m_OnReceiveData = onReceiveData;
            m_OnReceiveContentLength = onReceiveContentLength;
            m_OnComplete = onComplete;
            m_Ptr = Create(this);
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

        [RequiredByNativeCode]
        private static bool InvokeReceiveData(DownloadHandlerCallback handler, ReadOnlySpan<byte> data)
        {
            try
            {
                return handler.m_OnReceiveData(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        [RequiredByNativeCode]
        private static bool InvokeReceiveContentLength(DownloadHandlerCallback handler, ulong contentLength)
        {
            if (handler.m_OnReceiveContentLength == null)
                return true;

            try
            {
                handler.m_OnReceiveContentLength(contentLength);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        [RequiredByNativeCode]
        private static void InvokeComplete(DownloadHandlerCallback handler)
        {
            if (handler.m_OnComplete == null)
                return;

            try
            {
                handler.m_OnComplete();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerCallback handler) => handler.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/DownloadHandler/DownloadHandlerStream.h")]
    internal sealed class DownloadHandlerStream : DownloadHandler
    {
        private extern static IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] DownloadHandlerStream obj);

        private bool m_headersComplete = false;
        private System.Action m_headersCompleteCallback;

        private void InternalCreateStream()
        {
            m_Ptr = Create(this);
        }

        public DownloadHandlerStream()
        {
            InternalCreateStream();
        }

        [NativeMethod(IsThreadSafe = true)]
        private extern int PopData(Span<byte> outData);

        [NativeMethod(IsThreadSafe = true)]
        public extern void Close();

        public int ReadData(Span<byte> outData)
        {
            return InternalReadData(outData);
        }

        internal int InternalReadData(Span<byte> outData)
        {
            if (m_Ptr == IntPtr.Zero) return 0;

            return PopData(outData);
        }

        internal override void CompleteHeaders()
        {
            if (m_headersCompleteCallback != null)
            {
                m_headersCompleteCallback();
                m_headersCompleteCallback = null;
            }
            m_headersComplete = true;
        }

        public event System.Action headersCompleted
        {
            add
            {
                if (m_headersComplete)
                {
                    value();
                }
                else
                {
                    m_headersCompleteCallback += value;
                }
            }
            remove
            {
                m_headersCompleteCallback -= value;
            }
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerStream handler) => handler.m_Ptr;
        }
    }
}
