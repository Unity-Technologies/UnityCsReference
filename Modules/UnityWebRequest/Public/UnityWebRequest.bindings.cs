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
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Modules/UnityWebRequest/Public/UnityWebRequestAsyncOperation.h")]
    [NativeHeader("UnityWebRequestScriptingClasses.h")]
    public class UnityWebRequestAsyncOperation : AsyncOperation
    {
        public UnityWebRequest webRequest { get; internal set; }
    }

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
            Unknown,
            SDKError,     // SDK error, such as initialization failed
            UnsupportedProtocol,
            MalformattedUrl,
            CannotResolveProxy,
            CannotResolveHost,
            CannotConnectToHost,
            AccessDenied,
            GenericHttpError,
            WriteError,
            ReadError,
            OutOfMemory,
            Timeout,
            HTTPPostError,
            SSLCannotConnect,
            Aborted,
            TooManyRedirects,
            ReceivedNoData,
            SSLNotSupported,
            FailedToSendData,
            FailedToReceiveData,
            SSLCertificateError,
            SSLCipherNotAvailable,
            SSLCACertError,
            UnrecognizedContentEncoding,
            LoginFailed,
            SSLShutdownFailed,
            NoInternetConnection
        }

        public const string kHttpVerbGET = "GET";
        public const string kHttpVerbHEAD = "HEAD";
        public const string kHttpVerbPOST = "POST";
        public const string kHttpVerbPUT = "PUT";
        public const string kHttpVerbCREATE = "CREATE";
        public const string kHttpVerbDELETE = "DELETE";


        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_UNITYWEBREQUEST")]
        private extern static string GetWebErrorString(UnityWebRequestError err);

        public bool disposeDownloadHandlerOnDispose { get; set; }

        public bool disposeUploadHandlerOnDispose { get; set; }


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
        }

        public UnityWebRequest()
        {
            m_Ptr = Create();
            InternalSetDefaults();
        }

        public UnityWebRequest(string url)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
        }

        public UnityWebRequest(string url, string method)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
        }

        public UnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            m_Ptr = Create();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
            this.downloadHandler = downloadHandler;
            this.uploadHandler = uploadHandler;
        }


        ~UnityWebRequest()
        {
            DisposeHandlers();
            InternalDestroy();
        }

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
        }

        [NativeThrows]
        internal extern UnityWebRequestAsyncOperation BeginWebRequest();

        [Obsolete("Use SendWebRequest.  It returns a UnityWebRequestAsyncOperation which contains a reference to the WebRequest object.", false)]
        public AsyncOperation Send() {return SendWebRequest(); }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            UnityWebRequestAsyncOperation webOp = BeginWebRequest();
            if (webOp != null)
                webOp.webRequest = this;
            return webOp;
        }

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

        public string error
        {
            get
            {
                if (!(isNetworkError || isHttpError))
                    return null;

                return UnityWebRequest.GetWebErrorString(GetError());
            }
        }

        private extern bool use100Continue { get; set; }

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

                string localUrl = "http://localhost/";

                InternalSetUrl(WebRequestUtils.MakeInitialUrl(value, localUrl));
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

        public extern long responseCode { get; }
        private extern float GetUploadProgress();
        private extern bool IsExecuting();

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

        public extern bool isModifiable {[NativeMethod("IsModifiable")] get; }
        public extern bool isDone {[NativeMethod("IsDone")] get; }
        public extern bool isNetworkError {[NativeMethod("IsNetworkError")] get; }
        public extern bool isHttpError {[NativeMethod("IsHttpError")] get; }

        private extern float GetDownloadProgress();

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

        public extern ulong uploadedBytes { get; }
        public extern ulong downloadedBytes { get; }

        private extern int GetRedirectLimit();
        [NativeThrows]
        private extern void SetRedirectLimitFromScripting(int limit);

        public int redirectLimit
        {
            get { return GetRedirectLimit(); }
            set { SetRedirectLimitFromScripting(value); }
        }

        private extern bool GetChunked();
        private extern UnityWebRequestError SetChunked(bool chunked);

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

        public extern string GetRequestHeader(string name);

        [NativeMethod("SetRequestHeader")]
        internal extern UnityWebRequestError InternalSetRequestHeader(string name, string value);

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

        public extern string GetResponseHeader(string name);

        internal extern string[] GetResponseHeaderKeys();

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

        private extern UnityWebRequestError SetUploadHandler(UploadHandler uh);

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

        private extern int GetTimeoutMsec();
        private extern UnityWebRequestError SetTimeoutMsec(int timeout);

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
    }
}
