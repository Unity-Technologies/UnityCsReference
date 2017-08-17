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
public sealed partial class UnityWebRequest : IDisposable
{
    [System.NonSerialized]
            internal IntPtr m_Ptr;
    
    
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
        OK = 0,
        Unknown,
        SDKError,
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
    
    
    
            public bool disposeDownloadHandlerOnDispose
        {
            get; set;
        }
    
    
    
            public bool disposeUploadHandlerOnDispose
        {
            get; set;
        }
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalCreate () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalDestroy () ;

    private void InternalSetDefaults()
        {
            this.disposeDownloadHandlerOnDispose = true;
            this.disposeUploadHandlerOnDispose = true;
        }
    
    
    public UnityWebRequest()
        {
            InternalCreate();
            InternalSetDefaults();
        }
    
    
    public UnityWebRequest(string url)
        {
            InternalCreate();
            InternalSetDefaults();
            this.url = url;
        }
    
    
    public UnityWebRequest(string url, string method)
        {
            InternalCreate();
            InternalSetDefaults();
            this.url = url;
            this.method = method;
        }
    
    
    public UnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
        {
            InternalCreate();
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
            if (m_Ptr != IntPtr.Zero)
            {
                DisposeHandlers();
                InternalDestroy();
                GC.SuppressFinalize(this);
            }
        }
    
    
    private void DisposeHandlers()
        {
            if (disposeDownloadHandlerOnDispose)
            {
                DownloadHandler dh = this.GetDownloadHandler();
                if (dh != null)
                {
                    dh.Dispose();
                }
            }

            if (disposeUploadHandlerOnDispose)
            {
                UploadHandler uh = this.GetUploadHandler();
                if (uh != null)
                {
                    uh.Dispose();
                }
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal AsyncOperation InternalBegin () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalAbort () ;

    public AsyncOperation Send() { return InternalBegin(); }
    
    
    public void Abort() { InternalAbort(); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalSetMethod (UnityWebRequestMethod methodType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalSetCustomMethod (string customMethodName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int InternalGetMethod () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string InternalGetCustomMethod () ;

    public string method
        {
            get
            {
                UnityWebRequestMethod m = (UnityWebRequestMethod)InternalGetMethod();
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
                        return InternalGetCustomMethod();
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int InternalGetError () ;

    public extern  string error
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool useHttpContinue
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public string url
        {
            get
            {
                return InternalGetUrl();
            }

            set
            {

                string localUrl = "http://localhost/";

                InternalSetUrl(WebRequestUtils.MakeInitialUrl(value, localUrl));
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private string InternalGetUrl () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetUrl (string url) ;

    public extern  long responseCode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float uploadProgress
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isModifiable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isDone
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isNetworkError
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isHttpError
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float downloadProgress
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  ulong uploadedBytes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  ulong downloadedBytes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int redirectLimit
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool chunkedTransfer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string GetRequestHeader (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InternalSetRequestHeader (string name, string value) ;

    public void SetRequestHeader(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cannot set a Request Header with a null or empty name");
            }

            if (value == null)
            {
                throw new ArgumentException("Cannot set a Request header with a null");
            }

            InternalSetRequestHeader(name, value);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string GetResponseHeader (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string[] InternalGetResponseHeaderKeys () ;

    public Dictionary<string, string> GetResponseHeaders()
        {
            string[] headerKeys = InternalGetResponseHeaderKeys();
            if (headerKeys == null)
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
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private UploadHandler GetUploadHandler () ;

    public extern  UploadHandler uploadHandler
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private DownloadHandler GetDownloadHandler () ;

    public extern  DownloadHandler downloadHandler
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int timeout
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    
    private static string GetErrorDescription(UnityWebRequestError errorCode)
        {
            switch (errorCode)
            {
                case UnityWebRequestError.OK:
                    return "No Error";
                case UnityWebRequestError.SDKError:
                    return "Internal Error With Transport Layer";
                case UnityWebRequestError.UnsupportedProtocol:
                    return "Specified Transport Protocol is Unsupported";
                case UnityWebRequestError.MalformattedUrl:
                    return "URL is Malformatted";
                case UnityWebRequestError.CannotResolveProxy:
                    return "Unable to resolve specified proxy server";
                case UnityWebRequestError.CannotResolveHost:
                    return "Unable to resolve host specified in URL";
                case UnityWebRequestError.CannotConnectToHost:
                    return "Unable to connect to host specified in URL";
                case UnityWebRequestError.AccessDenied:
                    return "Remote server denied access to the specified URL";
                case UnityWebRequestError.GenericHttpError:
                    return "Unknown/Generic HTTP Error - Check HTTP Error code";
                case UnityWebRequestError.WriteError:
                    return "Error when transmitting request to remote server - transmission terminated prematurely";
                case UnityWebRequestError.ReadError:
                    return "Error when reading response from remote server - transmission terminated prematurely";
                case UnityWebRequestError.OutOfMemory:
                    return "Out of Memory";
                case UnityWebRequestError.Timeout:
                    return "Timeout occurred while waiting for response from remote server";
                case UnityWebRequestError.HTTPPostError:
                    return "Error while transmitting HTTP POST body data";
                case UnityWebRequestError.SSLCannotConnect:
                    return "Unable to connect to SSL server at remote host";
                case UnityWebRequestError.Aborted:
                    return "Request was manually aborted by local code";
                case UnityWebRequestError.TooManyRedirects:
                    return "Redirect limit exceeded";
                case UnityWebRequestError.ReceivedNoData:
                    return "Received an empty response from remote host";
                case UnityWebRequestError.SSLNotSupported:
                    return "SSL connections are not supported on the local machine";
                case UnityWebRequestError.FailedToSendData:
                    return "Failed to transmit body data";
                case UnityWebRequestError.FailedToReceiveData:
                    return "Failed to receive response body data";
                case UnityWebRequestError.SSLCertificateError:
                    return "Failure to authenticate SSL certificate of remote host";
                case UnityWebRequestError.SSLCipherNotAvailable:
                    return "SSL cipher received from remote host is not supported on the local machine";
                case UnityWebRequestError.SSLCACertError:
                    return "Failure to authenticate Certificate Authority of the SSL certificate received from the remote host";
                case UnityWebRequestError.UnrecognizedContentEncoding:
                    return "Remote host returned data with an unrecognized/unparseable content encoding";
                case UnityWebRequestError.LoginFailed:
                    return "HTTP authentication failed";
                case UnityWebRequestError.SSLShutdownFailed:
                    return "Failure while shutting down SSL connection";
                case UnityWebRequestError.Unknown:
                default:
                    return "Unknown error";
            }
        }
    
    
}


}
