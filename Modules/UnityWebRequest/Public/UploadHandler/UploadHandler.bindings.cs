// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Networking
{
    ///<summary>Helper object for <see cref="UnityWebRequest" />s. Manages the buffering and transmission of body data during HTTP requests.</summary>
    ///<remarks>When attached to a <see cref="UnityWebRequest" />, an UploadHandler object handles all information regarding the buffering and transmission of body data during an HTTP request. By placing data in an UploadHandler and attaching it to a <see cref="UnityWebRequest" />, the <see cref="UnityWebRequest" /> is implicitly instructed to transmit the UploadHandler's data to the remote server. The data will always be delivered as HTTP request body data.
    ///
    ///UploadHandler is a base class and cannot be directly instantiated. Currently, two types of UploadHandlers are available: <see cref="UploadHandlerRaw" /> and <see cref="UploadHandlerFile" />.</remarks>
    ///<seealso cref="UnityWebRequest" />
    ///<seealso cref="UploadHandlerRaw" />
    ///<seealso cref="UploadHandlerFile" />
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandler.h")]
    public class UploadHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void ReleaseFromScripting();

        internal UploadHandler() {}

#pragma warning disable UAC0045 // Releases the native handler; ReleaseFromScripting is thread-safe, so it is the safety net when Dispose is missed.
        ~UploadHandler()
        {
            Dispose();
        }
#pragma warning restore UAC0045

        ///<summary>Signals that this <see cref="UploadHandler" /> is no longer being used, and should clean up any resources it is using.</summary>
        ///<remarks>This method must be called once you have finished using an <see cref="UploadHandler" /> object.
        ///
        ///For convenience, <see cref="UnityWebRequest" /> exposes the <see cref="UnityWebRequest.disposeUploadHandlerOnDispose" /> property, which will automatically call this method when <see cref="UnityWebRequest.Dispose" /> is called.
        ///
        ///If you elect not to use <see cref="UnityWebRequest.disposeUploadHandlerOnDispose" /> (by setting it to false), then you must call Dispose on the [UploadHandler] yourself. This should only be in rare cases, such as when you wish to use the <see cref="UploadHandler" /> to transmit the same data multiple times.</remarks>
        public virtual void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ReleaseFromScripting();
                m_Ptr = IntPtr.Zero;
            }
        }

        ///<summary>The raw data which will be transmitted to the remote server as body data. (RO)</summary>
        public byte[] data
        {
            get
            {
                return GetData();
            }
        }

        ///<summary>Determines the default <c>Content-Type</c> header which will be transmitted with the outbound HTTP request.</summary>
        ///<remarks>If the parent <see cref="UnityWebRequest" /> does not have a custom <c>Content-Type</c> header set, then the value of this property will be used to determine the value of the <c>Content-Type</c> header for the HTTP request.
        ///
        ///Note: If the parent <see cref="UnityWebRequest" /> has a custom <c>Content-Type</c> header set, then the value of this property is ignored. If this property is <c>null</c> or an empty string, and the parent <see cref="UnityWebRequest" /> has no custom <c>Content-Type</c> header, then a default <c>Content-Type</c> will be assigned (usually <c>application/octet-stream</c>).
        ///
        ///Default value: <c>null</c>.</remarks>
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

        ///<summary>Returns the proportion of data uploaded to the remote server compared to the total amount of data to upload. (RO)</summary>
        ///<remarks>Behaves identically to <see cref="UnityWebRequest.uploadProgress" />.</remarks>
        public float progress
        {
            get
            {
                return GetProgress();
            }
        }

        internal virtual byte[] GetData() { return null; }
        internal virtual string GetContentType() { return InternalGetContentType(); }
        internal virtual void   SetContentType(string newContentType) { InternalSetContentType(newContentType); }
        internal virtual float  GetProgress() { return InternalGetProgress(); }

        [NativeMethod("GetContentType")]
        private extern string InternalGetContentType();

        [NativeMethod("SetContentType")]
        private extern void InternalSetContentType(string newContentType);

        [NativeMethod("GetProgress")]
        private extern float InternalGetProgress();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandler uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    ///<summary>A general-purpose <see cref="UploadHandler" /> subclass, using a native-code memory buffer.</summary>
    ///<remarks>This subclass copies input data into a native-code memory buffer at construction time, and transmits that data verbatim as HTTP request body data.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System;
    ///using System.Text;
    ///using System.Collections;
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///
    ///public class ApiClient : MonoBehaviour
    ///{
    ///    // Example endpoint and auth token – replace with your own
    ///    [SerializeField] private string baseUrl = "https://api.example.com";
    ///    [SerializeField] private string authToken = "Bearer YOUR_TOKEN";
    ///
    ///    // Example payload data structure
    ///    [Serializable]
    ///    public class PlayerStats
    ///    {
    ///        public string playerId;
    ///        public int score;
    ///        public int level;
    ///        public string region;
    ///    }
    ///
    ///    private void Start()
    ///    {
    ///        var stats = new PlayerStats
    ///        {
    ///            playerId = SystemInfo.deviceUniqueIdentifier,
    ///            score = 12345,
    ///            level = 7,
    ///            region = "NA"
    ///        };
    ///
    ///        // Send JSON payload
    ///        StartCoroutine(PostJson("/v1/stats", stats, onSuccess: response =>
    ///        {
    ///            Debug.Log($"Server response: {response}");
    ///        },
    ///        onError: error =>
    ///        {
    ///            Debug.LogError($"Upload failed: {error}");
    ///        }));
    ///
    ///        // Send raw binary payload (example)
    ///        var binaryData = GenerateMockBinary();
    ///        StartCoroutine(PostBinary("/v1/upload-binary", binaryData, "application/octet-stream"));
    ///    }
    ///
    ///    private IEnumerator PostJson(string path, object payload, Action<string> onSuccess, Action<string> onError)
    ///    {
    ///        string url = $"{baseUrl}{path}";
    ///
    ///        // Serialize to JSON
    ///        string json = JsonUtility.ToJson(payload);
    ///        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
    ///
    ///        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
    ///        {
    ///            // UploadHandlerRaw takes a byte[] buffer; it won’t copy data after construction,
    ///            // so do not modify bodyRaw until request completes.
    ///            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    ///            request.uploadHandler.contentType = "application/json"; // sets Content-Type
    ///
    ///            // Always set a DownloadHandler to read server response
    ///            request.downloadHandler = new DownloadHandlerBuffer();
    ///
    ///            // Optional headers
    ///            request.SetRequestHeader("Authorization", authToken);
    ///            request.SetRequestHeader("Accept", "application/json");
    ///            // If your server requires a custom header:
    ///            request.SetRequestHeader("X-Client-Version", Application.version);
    ///
    ///            // Reasonable timeout (seconds)
    ///            request.timeout = 15;
    ///
    ///            // You can allow chunked transfer if server supports it
    ///            request.chunkedTransfer = false; // default false; set true for streaming large bodies
    ///
    ///            // Send request
    ///            yield return request.SendWebRequest();
    ///
    ///            // Handle result
    ///            if (request.result == UnityWebRequest.Result.Success)
    ///            {
    ///                string responseText = request.downloadHandler.text;
    ///                onSuccess?.Invoke(responseText);
    ///            }
    ///            else
    ///            {
    ///                // Build a detailed error message
    ///                string errorMsg = $"HTTP {(int)request.responseCode} {request.error}";
    ///                string serverBody = request.downloadHandler != null ? request.downloadHandler.text : "";
    ///                if (!string.IsNullOrEmpty(serverBody))
    ///                {
    ///                    errorMsg += $" | Body: {serverBody}";
    ///                }
    ///                onError?.Invoke(errorMsg);
    ///            }
    ///        }
    ///    }
    ///
    ///    private IEnumerator PostBinary(string path, byte[] data, string contentType)
    ///    {
    ///        string url = $"{baseUrl}{path}";
    ///
    ///        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
    ///        {
    ///            request.uploadHandler = new UploadHandlerRaw(data);
    ///            request.uploadHandler.contentType = contentType; // e.g., "application/octet-stream"
    ///            request.downloadHandler = new DownloadHandlerBuffer();
    ///
    ///            request.SetRequestHeader("Authorization", authToken);
    ///            request.SetRequestHeader("Accept", "*/*");
    ///            request.timeout = 30;
    ///
    ///            yield return request.SendWebRequest();
    ///
    ///            if (request.result == UnityWebRequest.Result.Success)
    ///            {
    ///                Debug.Log($"Binary upload OK. Response: {request.downloadHandler.text}");
    ///            }
    ///            else
    ///            {
    ///                Debug.LogError($"Binary upload failed: {(int)request.responseCode} {request.error}");
    ///            }
    ///        }
    ///    }
    ///
    ///    private byte[] GenerateMockBinary()
    ///    {
    ///        // Example: create a simple byte array. In practice, this could be a file, image, or custom buffer.
    ///        var buffer = new byte[256];
    ///        for (int i = 0; i < buffer.Length; i++)
    ///            buffer[i] = (byte)(i % 256);
    ///        return buffer;
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerRaw.h")]
    public sealed class UploadHandlerRaw : UploadHandler
    {
        NativeArray<byte> m_Payload;

        private static extern unsafe IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerRaw self, byte* data, int dataLength);

        ///<summary>General constructor. Contents of the <c>input</c> argument are copied into a native buffer.</summary>
        ///<param name="data">Raw data to transmit to the remote server.</param>
        public UploadHandlerRaw(byte[] data)
            : this((data == null || data.Length == 0) ? new NativeArray<byte>() : new NativeArray<byte>(data, Allocator.Persistent), true)
        {
        }

        ///<summary>Creates an upload handler using NativeArray.</summary>
        ///<param name="data">The raw data to transmit to the remote server.</param>
        ///<param name="transferOwnership">When true, the upload handler takes ownership of the passed NativeArray. This means the upload handler will dispose of the NativeArray when the upload handler itself is disposed of. When false, NativeArray is owned by the caller and you must ensure it remains valid until the upload is complete.</param>
        public UploadHandlerRaw(NativeArray<byte> data, bool transferOwnership)
        {
            unsafe
            {
                if (!data.IsCreated || data.Length == 0)
                    m_Ptr = Create(this, null, 0);
                else
                {
                    if (transferOwnership)
                        m_Payload = data;
                    m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
                }
            }
        }

        ///<summary>Creates an upload handler using a read-only NativeArray. The passed array is owned by the caller and you must ensure it remains valid until the upload is complete.</summary>
        ///<param name="data">The raw data to transmit to the remote server.</param>
        public UploadHandlerRaw(NativeArray<byte>.ReadOnly data)
        {
            unsafe
            {
                if (!data.IsCreated || data.Length == 0)
                    m_Ptr = Create(this, null, 0);
                else
                {
                    if (data.Length == 0)
                        m_Ptr = Create(this, null, 0);
                    else
                        m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
                }
            }
        }

        internal override byte[] GetData()
        {
            if (m_Payload.IsCreated)
                return m_Payload.ToArray();
            return null;
        }

        ///<exclude />
        public override void Dispose()
        {
            if (m_Payload.IsCreated)
                m_Payload.Dispose();
            base.Dispose();
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerRaw uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    ///<summary>A specialized UploadHandler that reads data from a given file and sends raw bytes to the server as the request body.</summary>
    ///<remarks>You can use it to send a large amount of data to the server with a low memory footprint.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections;
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///
    ///public class UHFileSample : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        StartCoroutine(UploadFileData());
    ///    }
    ///
    ///    IEnumerator UploadFileData()
    ///    {
    ///        using (var uwr = new UnityWebRequest("https://yourwebsite.com/upload", UnityWebRequest.kHttpVerbPUT))
    ///        {
    ///            uwr.uploadHandler = new UploadHandlerFile("/path/to/file");
    ///            yield return uwr.SendWebRequest();
    ///            if (uwr.result != UnityWebRequest.Result.Success)
    ///                Debug.LogError(uwr.error);
    ///            else
    ///            {
    ///                // file data successfully sent
    ///            }
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerFile.h")]
    public sealed class UploadHandlerFile : UploadHandler
    {
        [NativeMethod(ThrowsException = true)]
        private static extern IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerFile self, string filePath);

        ///<summary>Create a new upload handler to send data from the given file to the server.</summary>
        ///<param name="filePath">A file containing data to send.</param>
        public UploadHandlerFile(string filePath)
        {
            m_Ptr = Create(this, filePath);
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerFile uploadHandler) => uploadHandler.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerStream.h")]
    internal sealed class UploadHandlerStream : UploadHandler
    {
        private static extern unsafe IntPtr Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] UploadHandlerStream self);

        [NativeMethod(IsThreadSafe = true)]
        public extern void Close();

        [NativeMethod(IsThreadSafe = true)]
        private extern void Reset();

        [NativeMethod(IsThreadSafe = true)]
        private extern void PushData(ReadOnlySpan<byte> data);

        public UploadHandlerStream()
        {
            unsafe
            {
                m_Ptr = Create(this);
            }
        }

        public void WriteData(ReadOnlySpan<byte> data) {
            PushData(data);
        }

        internal override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported");
        }

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UploadHandlerStream uploadHandler) => uploadHandler.m_Ptr;
        }
    }
}
