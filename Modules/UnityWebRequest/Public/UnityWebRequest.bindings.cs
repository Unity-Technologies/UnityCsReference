// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Networking
{
    ///<summary>Asynchronous operation object returned from <see cref="UnityWebRequest.SendWebRequest" />().
    ///
    ///You can yield until it continues, register an event handler with <see cref="AsyncOperation.completed" />, or manually check whether it's done (<see cref="AsyncOperation.isDone" />) or progress (<see cref="AsyncOperation.progress" />).</summary>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Modules/UnityWebRequest/Public/UnityWebRequestAsyncOperation.h")]
    [NativeHeader("UnityWebRequestScriptingClasses.h")]
    public class UnityWebRequestAsyncOperation : AsyncOperation
    {
        ///<exclude />
        public UnityWebRequestAsyncOperation() { }

        private UnityWebRequestAsyncOperation(IntPtr ptr) : base(ptr) {}

        ///<summary>Returns the associated <see cref="UnityWebRequest" /> that created the operation.</summary>
        public UnityWebRequest webRequest { get; internal set; }

        new internal static class BindingsMarshaller
        {
            public static UnityWebRequestAsyncOperation ConvertToManaged(IntPtr ptr) => new UnityWebRequestAsyncOperation(ptr);
        }
    }

    ///<summary>The version of HTTP to force when using <see cref="UnityHttpMessageHandler" /> or <see cref="UnityWebRequest" />.</summary>
    ///<remarks>Refer to <see cref="UnityHttpMessageHandler.HttpForcedVersion" />.</remarks>
    public enum HttpForcedVersion
    {
        ///<summary>Do not force any HTTP version when using <see cref="UnityHttpMessageHandler" /> or <see cref="UnityWebRequest" />.</summary>
        NotForced = 0,
        ///<summary>Force HTTP/1.0 when using <see cref="UnityHttpMessageHandler" /> or <see cref="UnityWebRequest" />.</summary>
        HTTP1_0 = 1,
        ///<summary>Force HTTP/1.1 when using <see cref="UnityHttpMessageHandler" /> or <see cref="UnityWebRequest" />.</summary>
        HTTP1_1 = 2,
        ///<summary>Force HTTP/2 when using <see cref="UnityHttpMessageHandler" /> or <see cref="UnityWebRequest" />.</summary>
        HTTP2 = 3,
    }

    ///<summary>Provides methods to communicate with web servers.</summary>
    ///<remarks>
    ///  <c>UnityWebRequest</c> handles the flow of HTTP communication with web servers. To post-process downloaded data and pre-process uploaded data, use <see cref="Networking.DownloadHandler" /> and <see cref="Networking.UploadHandler" /> respectively.
    ///
    ///<c>UnityWebRequest</c> includes static utility functions that return <c>UnityWebRequest</c> instances configured for common use cases. For example:
    ///
    ///* <see cref="Networking.UnityWebRequest.Get" />
    ///* <see cref="Networking.UnityWebRequest.Post" />
    ///* <see cref="Networking.UnityWebRequest.Put" />
    ///
    ///To send a web request from a <c>UnityWebRequest</c> instance, call <see cref="Networking.UnityWebRequest.SendWebRequest" />. After the <c>UnityWebRequest</c> begins to communicate with a remote server, you can't change any of the properties in that <c>UnityWebRequest</c> instance.
    ///HTTPS is supported and the server certificate is validated against the root certificate store available on the system the app runs on. Validation can be disabled (for example, for development server using self-signed certificate) or changed to custom handling by assigning the <see cref="UnityWebRequest.certificateHandler" /> property.
    ///
    ///Depending on the platform your application runs on, <c>UnityWebRequest</c> either sets the &lt;a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent"&gt;User-Agent header&lt;/a&gt; itself or leaves it for the operating system to set. <c>UnityWebRequest</c> sets the <c>User-Agent</c> header for all platforms except iOS, Xbox platforms, and WebGL.
    ///
    ///**Note**: If the device that the application runs on uses proxy settings, <c>UnityWebRequest</c> applies the proxy settings after the application sends the request.
    ///**Note**: <c>UnityWebRequest</c> does not support WPAD PAC configuration with multiple proxy failover chain for the platforms it is supported. In case such configuration is used, <c>UnityWebRequest</c> will use the first proxy in the chain, and will not use other failover proxy in the chain in case of a request failure.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///using UnityEngine.Networking;
    ///
    ///public class MyBehaviour : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        StartCoroutine(GetText());
    ///    }
    ///
    ///    IEnumerator GetText()
    ///    {
    ///        UnityWebRequest www = UnityWebRequest.Get("https://www.my-server.com");
    ///        yield return www.SendWebRequest();
    ///
    ///        if (www.result != UnityWebRequest.Result.Success)
    ///        {
    ///            Debug.Log(www.error);
    ///        }
    ///        else
    ///        {
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
    [NativeHeader("Modules/UnityWebRequest/Public/UnityWebRequest.h")]
    public partial class UnityWebRequest : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [System.NonSerialized]
        internal DownloadHandler m_DownloadHandler;

        [System.NonSerialized]
        internal UploadHandler m_UploadHandler;

        [System.NonSerialized]
        internal CertificateHandler m_CertificateHandler;

        [System.NonSerialized]
        internal Uri m_Uri;

        internal enum UnityWebRequestMethod
        {
            Get = 0,
            Post = 1,
            Put = 2,
            Head = 3,
            Custom = 4
        }

        internal enum UnityWebRequestError
        {
            OK = 0,     // No Error
            OKCached = 1,
            Unknown = 2,
            SDKError = 3,     // SDK error, such as initialization failed
            UnsupportedProtocol = 4,
            MalformattedUrl = 5,
            CannotResolveProxy = 6,
            CannotResolveHost = 7,
            CannotConnectToHost = 8,
            AccessDenied = 9,
            GenericHttpError = 10,
            WriteError = 11,
            ReadError = 12,
            OutOfMemory = 13,
            Timeout = 14,
            HTTPPostError = 15,
            SSLCannotConnect = 16,
            Aborted = 17,
            TooManyRedirects = 18,
            ReceivedNoData = 19,
            SSLNotSupported = 20,
            FailedToSendData = 21,
            FailedToReceiveData = 22,
            SSLCertificateError = 23,
            SSLCipherNotAvailable = 24,
            SSLCACertError = 25,
            UnrecognizedContentEncoding = 26,
            LoginFailed = 27,
            SSLShutdownFailed = 28,
            RedirectLimitInvalid = 29,
            InvalidRedirect = 30,
            CannotModifyRequest = 31,
            HeaderNameContainsInvalidCharacters = 32,
            HeaderValueContainsInvalidCharacters = 33,
            CannotOverrideSystemHeaders = 34,
            AlreadySent = 35,
            InvalidMethod = 36,
            NotImplemented = 37,
            NoInternetConnection = 38,
            DataProcessingError = 39,
            InsecureConnectionNotAllowed = 40,
        }

        ///<summary>Defines codes describing the possible outcomes of a UnityWebRequest.</summary>
        public enum Result
        {
            ///<summary>The request hasn't finished yet.</summary>
            InProgress = 0,
            ///<summary>The request succeeded.</summary>
            Success = 1,
            ///<summary>Failed to communicate with the server. For example, the request couldn't connect or it could not establish a secure channel.</summary>
            ConnectionError = 2,
            ///<summary>The server returned an error response. The request succeeded in communicating with the server, but received an error as defined by the connection protocol.</summary>
            ///<remarks>For the HTTP protocol, response codes 4xx and 5xx mean errors.</remarks>
            ProtocolError = 3,
            ///<summary>Error processing data. The request succeeded in communicating with the server, but encountered an error when processing the received data. For example, the data was corrupted or not in the correct format.</summary>
            DataProcessingError = 4,
        }


        ///<summary>The string "GET", commonly used as the verb for an HTTP GET request.</summary>
        public const string kHttpVerbGET = "GET";
        ///<summary>The string "HEAD", commonly used as the verb for an HTTP HEAD request.</summary>
        public const string kHttpVerbHEAD = "HEAD";
        ///<summary>The string "POST", commonly used as the verb for an HTTP POST request.</summary>
        public const string kHttpVerbPOST = "POST";
        ///<summary>The string "PUT", commonly used as the verb for an HTTP PUT request.</summary>
        public const string kHttpVerbPUT = "PUT";
        ///<summary>The string "CREATE", commonly used as the verb for an HTTP CREATE request.</summary>
        public const string kHttpVerbCREATE = "CREATE";
        ///<summary>The string "DELETE", commonly used as the verb for an HTTP DELETE request.</summary>
        public const string kHttpVerbDELETE = "DELETE";


        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_UNITYWEBREQUEST")]
        private extern static string GetWebErrorString(UnityWebRequestError err);
        [VisibleToOtherModules]
        internal extern static string GetHTTPStatusString(long responseCode);

        ///<summary>If true, any <see cref="CertificateHandler" /> attached to this <see cref="UnityWebRequest" /> will have <see cref="CertificateHandler.Dispose" /> called automatically when <see cref="UnityWebRequest.Dispose" /> is called.</summary>
        ///<remarks>Default: true.
        ///
        ///If no <see cref="CertificateHandler" /> is attached to this <see cref="UnityWebRequest" />, this property has no effect.</remarks>
        public bool disposeCertificateHandlerOnDispose { get; set; }

        ///<summary>If true, any <see cref="DownloadHandler" /> attached to this <see cref="UnityWebRequest" /> will have <see cref="DownloadHandler.Dispose" /> called automatically when <see cref="UnityWebRequest.Dispose" /> is called.</summary>
        ///<remarks>Default: true.
        ///
        ///If no <see cref="DownloadHandler" /> is attached to this <see cref="UnityWebRequest" />, this property has no effect.</remarks>
        public bool disposeDownloadHandlerOnDispose { get; set; }

        ///<summary>If true, any <see cref="UploadHandler" /> attached to this <see cref="UnityWebRequest" /> will have <see cref="UploadHandler.Dispose" /> called automatically when <see cref="UnityWebRequest.Dispose" /> is called.</summary>
        ///<remarks>Default: true.
        ///
        ///If no <see cref="UploadHandler" /> is attached to this <see cref="UnityWebRequest" />, this property has no effect.</remarks>
        public bool disposeUploadHandlerOnDispose { get; set; }

        ///<summary>Clears stored cookies from the cache.</summary>
        ///<remarks>Unity holds the cookie cache in memory for the lifetime of the application process and discards it when the process ends, unless one of the following exceptions applies.
        ///
        ///This method allows you to remove cookies from the cache. If you don't specify an argument, the method removes all cookies in the cache. If you do specify a string argument, the method only removes the cookies that apply to the given URL.
        ///
        ///In the Unity Editor, the cache belongs to the Editor process rather than to Play mode. Cookies that a request stores during Play mode remain in the cache after you exit Play mode, and the next Play mode session reuses them. To remove them, call this method or restart the Editor.
        ///
        ///Exceptions:
        ///
        ///- iOS has a built-in cookie cache provided by the system, which persists across game sessions. This method removes cookies from that built-in cache.
        ///- On the Web platform, the browser manages cookies and this method can't remove them, so it does nothing.</remarks>
        public static void ClearCookieCache()
        {
            ClearCookieCache(null, null);
        }

        ///<summary>Clears stored cookies from the cache.</summary>
        ///<remarks>Unity holds the cookie cache in memory for the lifetime of the application process and discards it when the process ends, unless one of the following exceptions applies.
        ///
        ///This method allows you to remove cookies from the cache. If you don't specify an argument, the method removes all cookies in the cache. If you do specify a string argument, the method only removes the cookies that apply to the given URL.
        ///
        ///In the Unity Editor, the cache belongs to the Editor process rather than to Play mode. Cookies that a request stores during Play mode remain in the cache after you exit Play mode, and the next Play mode session reuses them. To remove them, call this method or restart the Editor.
        ///
        ///Exceptions:
        ///
        ///- iOS has a built-in cookie cache provided by the system, which persists across game sessions. This method removes cookies from that built-in cache.
        ///- On the Web platform, the browser manages cookies and this method can't remove them, so it does nothing.</remarks>
        ///<param name="uri">An optional URL to define which cookies are removed. Only cookies that apply to this URL are removed from the cache.</param>
        public static void ClearCookieCache(Uri uri)
        {
            if (uri == null)
                ClearCookieCache(null, null);
            else
            {
                string domain = uri.Host;
                string path = uri.AbsolutePath;
                if (path == "/")
                    path = null;
                ClearCookieCache(domain, path);
            }
        }

        private static extern void ClearCookieCache(string domain, string path);

        [NativeMethod(ThrowsException = true)]
        internal extern static IntPtr Create();

        [NativeMethod(IsThreadSafe = true)]
        private extern void Release();

        internal void InternalDestroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Abort();
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

        private void InternalSetDefaults()
        {
            this.disposeDownloadHandlerOnDispose = true;
            this.disposeUploadHandlerOnDispose = true;
            this.disposeCertificateHandlerOnDispose = true;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest()
        {
            m_Ptr = Create();
            InternalSetDefaults();
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="url">The target URL with which this UnityWebRequest will communicate. Also accessible via the <see cref="url" /> property.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(string url)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(Uri uri)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="url">The target URL with which this UnityWebRequest will communicate. Also accessible via the <see cref="url" /> property.</param>
        ///<param name="method">HTTP GET, POST, etc. methods.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(string url, string method)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="method">HTTP GET, POST, etc. methods.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(Uri uri, string method)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
            this.method = method;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="url">The target URL with which this UnityWebRequest will communicate. Also accessible via the <see cref="url" /> property.</param>
        ///<param name="method">HTTP GET, POST, etc. methods.</param>
        ///<param name="downloadHandler">Replies from the server.</param>
        ///<param name="uploadHandler">Upload data to the server.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
            this.downloadHandler = downloadHandler;
            this.uploadHandler = uploadHandler;
        }

        ///<summary>Creates a UnityWebRequest with the default options and no attached <see cref="DownloadHandler" /> or <see cref="UploadHandler" />. Default method is <c>GET</c>.</summary>
        ///<remarks>The raw constructor is useful for use cases which require detailed custom configuration of a <see cref="UnityWebRequest" />. Most use cases will require the attachment of a <see cref="DownloadHandler" />, an <see cref="UploadHandler" /> or both in order to function propertly.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="method">HTTP GET, POST, etc. methods.</param>
        ///<param name="downloadHandler">Replies from the server.</param>
        ///<param name="uploadHandler">Upload data to the server.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class SimpleGetRequest : MonoBehaviour
        ///{
        ///    private const string Url = "https://jsonplaceholder.typicode.com/todos/1";
        ///
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetExample());
        ///    }
        ///
        ///    private IEnumerator GetExample()
        ///    {
        ///        // Use the constructor: UnityWebRequest(string url, string method)
        ///        // Method can be UnityWebRequest.kHttpVerbGET, "GET", "POST", etc.
        ///        using (UnityWebRequest request = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbGET))
        ///        {
        ///            // Attach a download handler to receive body data
        ///            request.downloadHandler = new DownloadHandlerBuffer();
        ///
        ///            // Optionally set headers
        ///            request.SetRequestHeader("Accept", "application/json");
        ///
        ///            // Send the request and wait for completion
        ///            yield return request.SendWebRequest();
        ///
        ///            if (request.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError($"Request failed: {request.error}");
        ///            }
        ///            else
        ///            {
        ///                // Access the response body
        ///                string json = request.downloadHandler.text;
        ///                Debug.Log($"Response Code: {request.responseCode}");
        ///                Debug.Log($"Body: {json}");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Get" />
        ///<seealso cref="GetTexture" />
        ///<seealso cref="GetAudioClip" />
        ///<seealso cref="GetAssetBundle" />
        ///<seealso cref="Head" />
        ///<seealso cref="Post" />
        ///<seealso cref="Put" />
        ///<seealso cref="Delete" />
        public UnityWebRequest(Uri uri, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.uri = uri;
            this.method = method;
            this.downloadHandler = downloadHandler;
            this.uploadHandler = uploadHandler;
        }


#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~UnityWebRequest()
        {
            DisposeHandlers();
            InternalDestroy();
        }
#pragma warning restore UA5000

        ///<summary>Signals that this <see cref="UnityWebRequest" /> is no longer being used, and should clean up any resources it is using.</summary>
        ///<remarks>You must call Dispose once you have finished using a <see cref="UnityWebRequest" /> object, regardless of whether the request succeeded or failed.
        ///
        ///For safety, it is usually a best practice to employ the &lt;a href="https://msdn.microsoft.com/en-us/library/yh598w02.aspx"&gt;using statement&lt;/a&gt; to ensure that a [UnityWebRequest] is properly cleaned up in case of uncaught exceptions.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyExampleBehaviour : MonoBehaviour
        ///{
        ///    public IEnumerator Start()
        ///    {
        ///        using (UnityWebRequest request = UnityWebRequest.Get("https://my-website.com"))
        ///        {
        ///            yield return request.SendWebRequest();
        ///            Debug.Log("Server responded: " + request.downloadHandler.text);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Dispose()
        {
            DisposeHandlers();
            InternalDestroy();
            GC.SuppressFinalize(this);
        }

        private void DisposeHandlers()
        {
            if (disposeDownloadHandlerOnDispose)
            {
                DownloadHandler dh = this.downloadHandler;
                if (dh != null)
                {
                    dh.Dispose();
                }
            }

            if (disposeUploadHandlerOnDispose)
            {
                UploadHandler uh = this.uploadHandler;
                if (uh != null)
                {
                    uh.Dispose();
                }
            }

            if (disposeCertificateHandlerOnDispose)
            {
                CertificateHandler ch = this.certificateHandler;
                if (ch != null)
                {
                    ch.Dispose();
                }
            }
        }

        [NativeMethod(ThrowsException = true)]
        internal extern UnityWebRequestAsyncOperation BeginWebRequest();

        ///<summary>Begin communicating with the remote server.</summary>
        ///<remarks>After calling this method, the UnityWebRequest will perform DNS resolution (if necessary), transmit an HTTP request to the remote server at the target URL and process the server’s response.
        ///
        ///This method can only be called once on any given UnityWebRequest object. Once this method is called, you cannot change any of the UnityWebRequest’s properties.
        ///
        ///This method returns an <see cref="AsyncOperation" /> object. Yielding the AsyncOperation inside a coroutine will cause the coroutine to pause until the UnityWebRequest encounters a system error or finishes communicating.</remarks>
        ///<returns>An <see cref="AsyncOperation" /> indicating the progress/completion state of the UnityWebRequest. Yield this object to wait until the UnityWebRequest is done.</returns>
        [Obsolete("Use SendWebRequest.  It returns a UnityWebRequestAsyncOperation which contains a reference to the WebRequest object.", false)]
        public AsyncOperation Send() {return SendWebRequest(); }

        ///<summary>Begin communicating with the remote server.</summary>
        ///<remarks>After calling this method, the <see cref="UnityWebRequest" /> will perform DNS resolution (if necessary), transmit an HTTP request to the remote server at the target URL and process the server’s response.
        ///
        ///This method can only be called once on any given <see cref="UnityWebRequest" /> object.
        ///
        ///Once this method is called, you cannot change any of the UnityWebRequest’s properties. You cannot change <see cref="UnityWebRequest" /> properties after <see cref="SendWebRequest" /> is called.
        ///
        ///This method returns a <see cref="UnityWebRequestAsyncOperation" /> object. Yielding the <see cref="UnityWebRequestAsyncOperation" /> inside a coroutine will cause the coroutine to pause until the <see cref="UnityWebRequest" /> encounters a system error or finishes communicating.</remarks>
        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            UnityWebRequestAsyncOperation webOp = BeginWebRequest();
            if (webOp != null)
                webOp.webRequest = this;
            return webOp;
        }

        ///<summary>If in progress, halts the UnityWebRequest as soon as possible.</summary>
        ///<remarks>This method may be called at any time. If the UnityWebRequest has not already completed, the UnityWebRequest will halt uploading or downloading data as soon as possible. Aborted UnityWebRequests are considered to have encountered a system error. Depending upon the type of error, the <see cref="result" /> property will return one of the error values: <see cref="UnityWebRequest.Result.ConnectionError">ConnectionError</see>, <see cref="UnityWebRequest.Result.ProtocolError">ProtocolError</see>, or <see cref="UnityWebRequest.Result.DataProcessingError">DataProcessingError</see>. The <see cref="error" /> property will be <c>Request Aborted</c>.
        ///
        ///If this method is called prior to calling <see cref="Send" />, then the UnityWebRequest will abort immediately after the call to <see cref="Send" />.
        ///
        ///Calls to this method have no effect after this UnityWebRequest has encountered a different error, or has successfully finished communicating with the remote server.</remarks>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Abort();

        private extern UnityWebRequestError SetMethod(UnityWebRequestMethod methodType);

        internal void InternalSetMethod(UnityWebRequestMethod methodType)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request method can no longer be altered");

            UnityWebRequestError ret = SetMethod(methodType);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        private extern UnityWebRequestError SetCustomMethod(string customMethodName);

        internal void InternalSetCustomMethod(string customMethodName)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request method can no longer be altered");

            UnityWebRequestError ret = SetCustomMethod(customMethodName);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        internal extern UnityWebRequestMethod GetMethod();
        internal extern string GetCustomMethod();

        ///<summary>Defines the HTTP verb used by this <see cref="UnityWebRequest" />, such as <c>GET</c> or <c>POST</c>.</summary>
        ///<remarks>This property may be set to any non-zero-length alphabetic string, and will be used verbatim. Therefore, this property can be employed to set the UnityWebRequest to transmit any custom HTTP verb required by an application.
        ///
        ///This property cannot be changed after calling <see cref="Send" />.
        ///
        ///**Note:** This method will always return strings in UPPERCASE. When setting the verb, the input value will automatically be converted to UPPERCASE.
        ///
        ///Default value: <c>GET</c>.</remarks>
        public string method
        {
            get
            {
                UnityWebRequestMethod m = GetMethod();
                switch (m)
                {
                    case UnityWebRequestMethod.Get:
                        return kHttpVerbGET;
                    case UnityWebRequestMethod.Post:
                        return kHttpVerbPOST;
                    case UnityWebRequestMethod.Put:
                        return kHttpVerbPUT;
                    case UnityWebRequestMethod.Head:
                        return kHttpVerbHEAD;
                    default:
                        return GetCustomMethod();
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Cannot set a UnityWebRequest's method to an empty or null string");
                }

                switch (value.ToUpper())
                {
                    case kHttpVerbGET:
                        InternalSetMethod(UnityWebRequestMethod.Get);
                        break;
                    case kHttpVerbPOST:
                        InternalSetMethod(UnityWebRequestMethod.Post);
                        break;
                    case kHttpVerbPUT:
                        InternalSetMethod(UnityWebRequestMethod.Put);
                        break;
                    case kHttpVerbHEAD:
                        InternalSetMethod(UnityWebRequestMethod.Head);
                        break;
                    default:
                        InternalSetCustomMethod(value.ToUpper());
                        break;
                }
            }
        }

        private extern UnityWebRequestError GetError();

        ///<summary>A human-readable string describing any system errors encountered by this <see cref="UnityWebRequest" /> object while handling HTTP requests or responses. The default value is <c>null</c>. (RO)</summary>
        ///<remarks>If the <see cref="UnityWebRequest" /> has not encountered a system error, this property will return <c>null</c>. Examples of system errors include socket errors, errors resolving DNS entries, or the redirect limit being exceeded.
        ///
        ///**Note:** If the <see cref="UnityWebRequest" /> is complete and the response isn't successful, the error property will be non-empty.</remarks>
        ///<seealso cref="responseCode" />
        public string error
        {
            get
            {
                switch (result)
                {
                    case Result.InProgress:
                    case Result.Success:
                        return null;
                    case Result.ProtocolError:
                        return string.Format("HTTP/1.1 {0} {1}", responseCode, GetHTTPStatusString(responseCode));
                    default:
                        return GetWebErrorString(GetError());
                }
            }
        }

        // Lets UnityHttpMessageHandler attribute its requests to the .NET HttpClient API in the
        // profiler. UnityHttpMessageHandler builds a UnityWebRequest internally, so the two are
        // otherwise indistinguishable to the native capture. Values are WebRequestProfilerSource in
        // Modules/UnityWebRequest/Profiler/WebRequestProfilerCapture.h.
        internal extern byte profilerSource { get; set; }

        private extern bool use100Continue { get; set; }

        ///<summary>Determines whether this UnityWebRequest will include <c>Expect: 100-Continue</c> in its outgoing request headers. (Default: <c>true</c>).</summary>
        ///<remarks>If this property is set to <c>true</c>, then this UnityWebRequest will include an <c>Expect: 100-Continue</c> header in the initial outbound request. If set to <c>false</c>, an empty <c>Expect</c> header will be sent, which will suppress usage of the <c>100 Continue</c> response code.
        ///
        ///As detailed in &lt;a href="https://www.w3.org/Protocols/rfc2616/rfc2616-sec8.html"&gt;RFC 2616, Section 8&lt;/a&gt;, the <c>100 Continue</c> response code is intended to allow a remote server to decide whether or not it will accept a request based on a request's headers, prior to the client transmitting the full request body.
        ///
        ///This is useful in cases where the client need not transmit its full request to every server in a request/response chain, such as in a load-balanced application. For example, a client would present its request, with a <c>Expect: 100-Continue</c> header, to a load-balancing server. The load-balancing server would then respond with a redirect to a processing server. Next, the client would connect to the processing server and transmit the same request, again with a <c>Expect: 100-Continue</c> server. The processing server would then respond with a <c>100 Continue</c> HTTP status code, and the client would finally respond with the full body of its request.
        ///
        ///By using the <c>100 Continue</c> status code, the client only had to transmit the full body of its request to one server. If not using the <c>100 Continue</c> status code, the client must transmit the full body of its request to every server it communicates with, needlessly consuming bandwidth and processing time on both the client and any servers issuing redirects.
        ///
        ///In general, one should leave <c>100 Continue</c> enabled. Exceptions include requests which have a very small or no request body, or applications where the client knows the server will not issue a redirect.
        ///
        ///This property defaults to <c>true</c>.
        ///
        ///**Note:** On WebGL build targets, header negotiation is performed by the host browser. Therefore, this setting's value has no effect on WebGL builds.</remarks>
        public bool useHttpContinue
        {
            get { return use100Continue; }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent and its 100-Continue setting cannot be altered");
                use100Continue = value;
            }
        }

        ///<summary>Defines the target URL for the <see cref="UnityWebRequest" /> to communicate with.</summary>
        ///<remarks>This property cannot be set after calling <see cref="SendWebRequest" />.
        ///
        ///If the <see cref="UnityWebRequest" /> encounters and follows redirects, this property will be updated with the URL to which the <see cref="UnityWebRequest" /> was redirected.
        ///
        ///When inputting URLs, absolute URLs are preferred. However, if you input a partial URL, the system will follow these rules:
        ///
        ///**If the input URL starts with two slashes (//)**, then the input is assumed to be a domain and path intended for use over HTTPS.
        ///
        ///On non-WebGL platforms, the system will prepend https:. On WebGL, the system will inherit the scheme of the path by which the Unity WebGL application is being accessed.
        ///
        ///Examples: If the WebGL app is being accessed via https, the system will prepend https:. If the WebGL app is being accessed via http, the system will prepend http:.
        ///
        ///**If the input URL starts with a single slash (/)**, then the system assumes the inout is a path relative to the current domain on which the Unity application is running. On non-WebGL platforms, the system will prepend https://localhost to the URL.
        ///
        ///On WebGL, the system will prepend the scheme and host of the path by which the Unity WebGL application is being accessed. For example, if the Unity WebGL app is being accessed via https://unity3d.com/myapp, then the system will prepend https://unity3d.com to relative paths.
        ///
        ///**If neither of the above rules apply**, the system validates your input string via the built-in &lt;a href="https://msdn.microsoft.com/en-us/library/system.uri"&gt;System.Uri&lt;/a&gt; class. If this class throws a &lt;a href="https://msdn.microsoft.com/en-us/library/system.uriformatexception"&gt;URIFormatException&lt;/a&gt;, the system attempts to append the input string to the absolute URL by which the Unity app is being accessed. (see above)
        ///
        ///Any further exceptions will be re-thrown.</remarks>
        public string url
        {
            get
            {
                return GetUrl();
            }

            set
            {
                // We need to sanitize the incoming URL so it's a proper absolute URL
                // This permits us to allow relative URLs and correct minor user mistakes.

                string localUrl = "https://localhost/";

                InternalSetUrl(WebRequestUtils.MakeInitialUrl(value, localUrl));
            }
        }

        ///<summary>Defines the target URI for the <see cref="UnityWebRequest" /> to communicate with.</summary>
        ///<remarks>The passed URI must be a full and absolute URI.
        ///
        ///This property can't be set after calling <see cref="SendWebRequest" />.
        ///
        ///If the <see cref="UnityWebRequest" /> encounters and follows redirects, this property updates with the URL to which the <see cref="UnityWebRequest" /> was redirected.
        ///
        ///This property works like <see cref="url" /> but is faster to set because it requires less validation and pre-processing. However, each time you access its value, a new URI instance is created, which is resource-intensive. The recommended best practice is to use this property when you need a URI object and to use <see cref="url" /> when you need a resulting URL.</remarks>
        public Uri uri
        {
            get
            {
                // always return from native (it will change in case of redirect)
                return new Uri(GetUrl());
            }
            set
            {
                if (!value.IsAbsoluteUri)
                    throw new ArgumentException("URI must be absolute");
                InternalSetUrl(WebRequestUtils.MakeUriString(value, value.OriginalString, false));
                m_Uri = value;
            }
        }

        private extern string GetUrl();
        private extern UnityWebRequestError SetUrl(string url);

        private void InternalSetUrl(string url)
        {
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its URL cannot be altered");

            UnityWebRequestError ret = SetUrl(url);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        ///<summary>The numeric HTTP response code returned by the server, such as <c>200</c>, <c>404</c> or <c>500</c>. (RO)</summary>
        ///<remarks>If the UnityWebRequest has received multiple responses (due to redirects), then this property will return the response code of the newest (or final) HTTP response.
        ///
        ///If the UnityWebRequest has not yet processed a response, this property will return -1.</remarks>
        public extern long responseCode { get; }
        private extern float GetUploadProgress();
        private extern bool IsExecuting();

        ///<summary>Returns a floating-point value between 0.0 and 1.0, indicating the progress of uploading body data to the server.</summary>
        ///<remarks>If the <see cref="UnityWebRequest" /> is complete (either a success or a system error), this property will always return 1. If the <see cref="UnityWebRequest" /> is still communicating with the remote server, and <see cref="uploadHandler" /> is <c>null</c>, this property will return zero. If <see cref="Send" /> has not yet been called, this property will return -1.</remarks>
        public float uploadProgress
        {
            get
            {
                if (!(IsExecuting() || isDone))
                    return -1.0f;
                else
                    return GetUploadProgress();
            }
        }

        ///<summary>Returns <c>true</c> while a <see cref="UnityWebRequest" />’s configuration properties can be altered. (RO)</summary>
        ///<remarks>Examples of configuration properties include <see cref="downloadHandler" />, <see cref="method" /> and <see cref="url" />. This property will return <c>false</c> after a call to <see cref="Send" />.</remarks>
        public extern bool isModifiable {[NativeMethod("IsModifiable")] get; }
        ///<summary>Returns <c>true</c> after the <see cref="UnityWebRequest" /> has finished communicating with the remote server. (RO)</summary>
        ///<remarks>This property will return <c>true</c> both when the <see cref="UnityWebRequest" /> finishes successfully, or when it encounters a system error. All post-download processing by the <see cref="DownloadHandler" /> (if any) will be completed before this property returns <c>true</c>.</remarks>
        ///<seealso cref="isNetworkError" />
        ///<seealso cref="isHttpError" />
        public bool isDone { get { return result != Result.InProgress; } }
        // These two are referenced by packages, deprecate after packages get updated
        ///<summary>Returns <c>true</c> after this <see cref="UnityWebRequest" /> encounters a system error. (RO)</summary>
        ///<remarks>Examples of system errors include failure to resolve a DNS entry, a socket error or a redirect limit being exceeded. When this property returns <c>true</c>, the <see cref="error" /> property will contain a human-readable string describing the error.
        ///
        ///**Note:** Error-type server return codes, such as 404/Not Found and 500/Internal Server Error, are reflected in the <see cref="isHttpError" /> property, not the <see cref="isNetworkError" /> property.</remarks>
        ///<seealso cref="responseCode" />
        ///<seealso cref="isHttpError" />
        [System.Obsolete("UnityWebRequest.isNetworkError is deprecated. Use (UnityWebRequest.result == UnityWebRequest.Result.ConnectionError) instead.", false)]
        public bool isNetworkError { get { return result == Result.ConnectionError; } }
        ///<summary>Returns <c>true</c> after this <see cref="UnityWebRequest" /> receives an HTTP response code indicating an error. (RO)</summary>
        ///<remarks>True on response codes greater than or equal to 400.</remarks>
        ///<seealso cref="responseCode" />
        ///<seealso cref="isNetworkError" />
        [System.Obsolete("UnityWebRequest.isHttpError is deprecated. Use (UnityWebRequest.result == UnityWebRequest.Result.ProtocolError) instead.", false)]
        public bool isHttpError { get { return result == Result.ProtocolError; } }
        ///<summary>The result of this UnityWebRequest.</summary>
        public extern Result result { [NativeMethod("GetResult")] get; }

        private extern float GetDownloadProgress();

        ///<summary>Returns a floating-point value between 0.0 and 1.0, indicating the progress of downloading body data from the server. (RO)</summary>
        ///<remarks>**Note:** This property only works if the server’s response contains a Content-Length header and the <see cref="UnityWebRequest" /> has a <see cref="DownloadHandler" /> attached to the <see cref="downloadHandler" /> property.
        ///
        ///If the <see cref="UnityWebRequest" /> is complete (either a success or a system error), this property will always return 1. If the <see cref="UnityWebRequest" /> is still communicating with the remote server, and <see cref="downloadHandler" /> is <c>null</c>, this property will return 0.5. If <see cref="Send" /> has not yet been called, this property will return -1.</remarks>
        public float downloadProgress
        {
            get
            {
                if (!(IsExecuting() || isDone))
                    return -1.0f;
                else
                    return GetDownloadProgress();
            }
        }

        ///<summary>Returns the number of bytes of body data the system has uploaded to the remote server. (RO)</summary>
        ///<remarks>If this UnityWebRequest has no upload handler, this property will always return zero.</remarks>
        public extern ulong uploadedBytes { get; }
        ///<summary>Returns the number of bytes of body data the system has downloaded from the remote server. (RO)</summary>
        ///<remarks>If the UnityWebRequest has no download handler, this method will always return zero.</remarks>
        public extern ulong downloadedBytes { get; }

        private extern int GetRedirectLimit();
        [NativeMethod(ThrowsException = true)]
        private extern void SetRedirectLimitFromScripting(int limit);

        ///<summary>Indicates the number of redirects that this <see cref="UnityWebRequest" /> follows before halting with a <c>Redirect Limit Exceeded</c> system error.</summary>
        ///<remarks>If you want to disable redirects altogether, set this property to zero - this <c>UnityWebRequest</c> will then refuse to follow redirects. If a redirect is encountered while redirects are disabled, the request will halt with a <c>Redirect Limit Exceeded</c> system error.
        ///
        ///If you don't want to limit the number of redirects, you can set this property to any negative number. **Note:** **This is not recommended**. If the redirect limit is disabled and the <c>UnityWebRequest</c> encounters a redirect loop, the <c>UnityWebRequest</c> will consume processor time until <see cref="Abort" /> is called.
        ///
        ///**Note:**  On WebGL platforms, the <c>UnityWebRequest</c> API behaves differently. It only supports a redirect limit of <c>0</c> where the request fails on a redirect, and for anything above <c>0</c>, it uses the browser-default redirect limit. This applies to Unity 2021.3 and later versions.
        ///
        ///
        ///
        ///
        ///Default value: <c>32</c>.</remarks>
        public int redirectLimit
        {
            get { return GetRedirectLimit(); }
            set { SetRedirectLimitFromScripting(value); }
        }

        private extern bool GetChunked();
        private extern UnityWebRequestError SetChunked(bool chunked);

        ///<summary>**Deprecated.**. HTTP/2 and many HTTP/1.1 servers don't support this; we recommend leaving it set to false (default).</summary>
        ///<remarks>This property indicates whether the <see cref="UnityWebRequest" /> should employ the HTTP/1.1 chunked-transfer encoding method, which allows the system to send partial data and be prompted by the server for more data with a 100/Continue HTTP response. This property cannot be changed after calling <see cref="Send" />.
        ///
        ///**Note:** On WebGL build targets, this setting is ignored. Instead, the web browser handles protocol negotiations.
        ///
        ///**Note:** If this setting is set to true then HTTP/1.1 is forced. Refer to <see cref="HttpForcedVersion" /> for more information.
        ///
        ///Default value: <c>false</c>.</remarks>
        [Obsolete("HTTP/2 and many HTTP/1.1 servers don't support this; we recommend leaving it set to false (default).", false)]
        public bool chunkedTransfer
        {
            get { return GetChunked(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent and its chunked transfer encoding setting cannot be altered");

                UnityWebRequestError ret = SetChunked(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        ///<summary>Retrieves the value of a custom request header.</summary>
        ///<remarks>This method retrieves the value of custom (i.e. user-set) request headers. These are the headers which will be transmitted to the remote server as part of the HTTP request.</remarks>
        ///<param name="name">Name of the custom request header. Case-insensitive.</param>
        ///<returns>The value of the custom request header. If no custom header with a matching name has been set, returns an empty string.</returns>
        ///<seealso cref="SetRequestHeader" />
        public extern string GetRequestHeader(string name);

        [NativeMethod("SetRequestHeader")]
        internal extern UnityWebRequestError InternalSetRequestHeader(string name, string value);

        ///<summary>Sets an HTTP request header to a custom value.</summary>
        ///<remarks>Header keys and values must be valid according to HTTP protocol specification. Neither string may contain certain illegal characters, such as control characters. Both strings must be non-null, and <c>name</c> must contain at least one character. An empty <c>value</c> is valid for headers that allow one, such as <c>Accept-Encoding</c>. For more information, refer to &lt;a href="https://www.w3.org/Protocols/"&gt;HTTP specifications&lt;/a&gt;.
        ///
        ///This method can't be called after <see cref="SendWebRequest" /> is called.
        ///
        ///It is not recommended to set these headers to these custom values: <c>Accept-Charset</c>, <c>Accept-Encoding</c>, <c>Access-Control-Request-Headers</c>, <c>Access-Control-Request-Method</c>, <c>Connection</c>, <c>Date</c>, <c>Dnt</c>, <c>Expect</c>, <c>Host</c>, <c>Keep-Alive</c>, <c>Origin</c>, <c>Referer</c>, <c>Te</c>, <c>Trailer</c>, <c>Transfer-Encoding</c>, <c>Upgrade</c>, <c>Via</c>. Due to different limitations across platforms, the custom value might be overridden, ignored, or unsupported, therefore the resulting behavior is unreliable.
        ///It is strongly recommended to leave these headers for automatic handling unless you want to risk viewing any unexpected results.
        ///
        ///The <c>Accept-Encoding</c> header is automatically set to supported encodings. Use of a different value is ignored or might cause request to fail. For more information, refer to the [Mozilla docs](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept-Encoding) on Accept-Encoding.
        ///
        ///The <c>Content-Length</c> header is automatically populated based on the contents of the attached <see cref="UploadHandler" /> if any, and can't be set to a custom value.
        ///
        ///The <c>X-Unity-Version</c> header is automatically set by Unity and might not be set to a custom value.
        ///
        ///The <c>User-Agent</c> header is automatically set by Unity and it's not recommended to set it to a custom value.
        ///
        ///The underlying cookie engine manages the <c>Cookie</c> and <c>Cookie2</c> headers. The exact behavior depends on the platform, but setting cookies through these headers typically appends them to the ones the engine sets.
        ///
        ///In addition, the following headers are filled by the Web browser on the **Web** platform, and therefore might not have any custom values set: <c>Cookie</c>, <c>Cookie2</c>, <c>User-Agent</c>.</remarks>
        ///<param name="name">The key of the header to be set. Case-sensitive.</param>
        ///<param name="value">The header's intended value.</param>
        ///<seealso cref="ClearCookieCache" />
        public void SetRequestHeader(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Cannot set a Request Header with a null or empty name");

            // Only check for null here, as in general header value can be empty, i.e. Accept-Encoding can have empty value according spec.
            if (value == null)
                throw new ArgumentException("Cannot set a Request header with a null");
            if (!isModifiable)
                throw new InvalidOperationException("UnityWebRequest has already been sent and its request headers cannot be altered");

            UnityWebRequestError ret = InternalSetRequestHeader(name, value);
            if (ret != UnityWebRequestError.OK)
                throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
        }

        ///<summary>Retrieves the value of a response header from the latest HTTP response received.</summary>
        ///<remarks>In the case that this UnityWebRequest has received multiple responses (such as during redirects), only headers from the newest (or final) response are checked.</remarks>
        ///<param name="name">The name of the HTTP header to retrieve. Case-insensitive.</param>
        ///<returns>The value of the HTTP header from the latest HTTP response. If no header with a matching name has been received, or no responses have been received, returns <c>null</c>.</returns>
        public extern string GetResponseHeader(string name);

        internal extern string[] GetResponseHeaderKeys();

        ///<summary>Retrieves a dictionary containing all the response headers received by this UnityWebRequest in the latest HTTP response.</summary>
        ///<remarks>In the case that the UnityWebRequest has received multiple responses (such as during redirects), only headers from the latest/final response will be included.
        ///
        ///**Note:** This method allocates a new Dictionary object each time it is called. You may wish to cache the return value from this call if you are retrieving it multiple times.</remarks>
        ///<returns>A dictionary containing all the response headers received in the latest HTTP response. If no responses have been received, returns <c>null</c>.</returns>
        public Dictionary<string, string> GetResponseHeaders()
        {
            string[] headerKeys = GetResponseHeaderKeys();
            if (headerKeys == null || headerKeys.Length == 0)
            {
                return null;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>(headerKeys.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerKeys.Length; i++)
            {
                string val = GetResponseHeader(headerKeys[i]);
                headers.Add(headerKeys[i], val);
            }

            return headers;
        }

        ///<summary>Retrieves the value of a response trailer from the latest HTTP response received.</summary>
        ///<remarks>In the case that this UnityWebRequest has received multiple responses (such as during redirects), only trailers from the newest (or final) response are checked.</remarks>
        ///<param name="name">The name of the HTTP trailer to retrieve. Case-insensitive.</param>
        ///<returns>The value of the HTTP trailer from the latest HTTP response. If no trailer with a matching name has been received, or no responses have been received, returns <c>null</c>.</returns>
        public extern string GetResponseTrailer(string name);

        internal extern string[] GetResponseTrailerKeys();

        ///<summary>Retrieves a dictionary containing all the response trailers received by this UnityWebRequest in the latest HTTP response.</summary>
        ///<remarks>In the case that the UnityWebRequest has received multiple responses (such as during redirects), only trailers from the latest/final response will be included.
        ///
        ///**Note:** This method allocates a new Dictionary object each time it is called. You may wish to cache the return value from this call if you are retrieving it multiple times.</remarks>
        ///<returns>A dictionary containing all the response trailers received in the latest HTTP response. If no responses have been received, returns <c>null</c>.</returns>
        public Dictionary<string, string> GetResponseTrailers()
        {
            string[] headerKeys = GetResponseTrailerKeys();
            if (headerKeys == null || headerKeys.Length == 0)
            {
                return null;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>(headerKeys.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerKeys.Length; i++)
            {
                string val = GetResponseTrailer(headerKeys[i]);
                headers.Add(headerKeys[i], val);
            }

            return headers;
        }

        private extern UnityWebRequestError SetUploadHandler(UploadHandler uh);

        ///<summary>Holds a reference to the <see cref="UploadHandler" /> object which manages body data to be uploaded to the remote server.</summary>
        ///<remarks>Setting this property to <c>null</c> indicates that this <see cref="UnityWebRequest" /> has no body data to upload. See the reference on <see cref="UploadHandler" /> objects for more information on creating and using UploadHandlers.
        ///
        ///
        ///This property cannot be set after calling <see cref="Send" />.</remarks>
        public UploadHandler uploadHandler
        {
            get
            {
                return m_UploadHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the upload handler");
                UnityWebRequestError ret = SetUploadHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_UploadHandler = value;
            }
        }

        private extern UnityWebRequestError SetDownloadHandler(DownloadHandler dh);

        ///<summary>Holds a reference to a <see cref="DownloadHandler" /> object, which manages body data received from the remote server by this <see cref="UnityWebRequest" />.</summary>
        ///<remarks>Setting this property to <c>null</c> indicates that this <see cref="UnityWebRequest" /> does not care about the response’s body data; all received body data will be ignored and discarded. See the reference on <see cref="DownloadHandler" /> objects for more information on creating and using DownloadHandlers.
        ///
        ///This property cannot be changed after calling <see cref="Send" />.
        ///
        ///Default value: <c>null</c>.</remarks>
        public DownloadHandler downloadHandler
        {
            get
            {
                return m_DownloadHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the download handler");
                UnityWebRequestError ret = SetDownloadHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_DownloadHandler = value;
            }
        }

        private extern UnityWebRequestError SetCertificateHandler(CertificateHandler ch);

        ///<summary>Holds a reference to a <see cref="CertificateHandler" /> object, which manages certificate validation for this <see cref="UnityWebRequest" />.</summary>
        ///<remarks>Setting this property to <c>null</c> makes the platform use the default certificate validation, which will validate certificates against a root certificate authority store (most commonly Operating System store).
        ///
        ///Not all platforms support certificate validation callbacks. See <see cref="CertificateHandler" /> for a list of supported platforms.
        ///
        ///This property cannot be changed after calling <see cref="SendWebRequest" />.
        ///
        ///Default value: <c>null</c>.</remarks>
        public CertificateHandler certificateHandler
        {
            get
            {
                return m_CertificateHandler;
            }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the certificate handler");
                UnityWebRequestError ret = SetCertificateHandler(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
                m_CertificateHandler = value;
            }
        }
        private extern int GetTimeoutMsec();
        private extern UnityWebRequestError SetTimeoutMsec(int timeout);

        ///<summary>The number of seconds after which UnityWebRequest attempts to abort the request if no response is received.</summary>
        ///<remarks>The default value is <c>0</c> which means no timeout is applied and UnityWebRequest will wait until the response is received.
        ///
        ///When the response takes longer than the value specified in <c>timeout</c>, <see cref="UnityWebRequest.error" /> returns <c>Request timeout</c> message.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.Networking;
        ///
        /// // Ask the website to deliver an image that is very large.
        /// // Set the download to take more than 60 seconds. This causes
        /// // the "request timeout" error message.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(GetText());
        ///    }
        ///
        ///    IEnumerator GetText()
        ///    {
        ///        using UnityWebRequest www = UnityWebRequest.Get("https://my-website.com/verylargeimage.jpg");
        ///
        ///        // Set the timeout to 60 seconds.
        ///        // Abort the request if the image doesn't download within the specified timeout.
        ///        www.timeout = 60;
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("image arrived");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int timeout
        {
            get { return GetTimeoutMsec() / 1000; }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the timeout");

                value = Math.Max(value, 0);
                UnityWebRequestError ret = SetTimeoutMsec(value * 1000);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern bool GetSuppressErrorsToConsole();
        private extern UnityWebRequestError SetSuppressErrorsToConsole(bool suppress);

        internal bool suppressErrorsToConsole
        {
            get { return GetSuppressErrorsToConsole(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the timeout");
                UnityWebRequestError ret = SetSuppressErrorsToConsole(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern HttpForcedVersion GetHttpForcedVersion();
        private extern UnityWebRequestError SetHttpForcedVersion(HttpForcedVersion forceHttp2);

        ///<summary>Force the version of HTTP used when making web requests with <see cref="UnityWebRequest" />.</summary>
        ///<remarks>
        ///  <para>Setting this property to <c>HttpForcedVersion.NotForced</c> causes <see cref="UnityWebRequest" /> to use standard negotiation with the server to determine which HTTP version to use.
        ///
        ///Using other values causes <see cref="UnityWebRequest" /> to force the web requests to a particular version of HTTP even if insecure HTTP is being used.
        ///
        ///Default value: <c>HttpForcedVersion.NotForced</c>.
        ///
        ///Refer to <see cref="HttpForcedVersion" /> for more information.</para>
        ///  <para>Demonstrating how to force web requests to HTTP/2 using <c>UnityWebRequest</c>.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Networking;
        ///
        ///public class HttpForcedVersionExample
        ///{
        ///    public static UnityEngine.Networking.UnityWebRequest MakeUnityWebRequestWithForcedHttp2()
        ///    {
        ///        var httpHandler = new UnityEngine.Networking.UnityWebRequest()
        ///        {
        ///            httpForcedVersion = HttpForcedVersion.HTTP2
        ///        };
        ///        return httpHandler;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public HttpForcedVersion httpForcedVersion
        {
            get { return GetHttpForcedVersion(); }
            set
            {
                if (!isModifiable)
                    throw new InvalidOperationException("UnityWebRequest has already been sent; cannot modify the protocol version");
                UnityWebRequestError ret = SetHttpForcedVersion(value);
                if (ret != UnityWebRequestError.OK)
                    throw new InvalidOperationException(UnityWebRequest.GetWebErrorString(ret));
            }
        }

        private extern string responseVersionString { get; }
        internal Version responseVersion
        {
            get
            {
                return new Version(responseVersionString);
            }
        }

        // accept certificate for addresses which starts from url, required for running tests
        internal extern static void AcceptCertificateForUrl(string url);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityWebRequest unityWebRequest) => unityWebRequest.m_Ptr;
        }
    }
}
