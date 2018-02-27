// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEditor.RestService
{
    [NativeHeader("Modules/RestService/Public/Request.h")]
    internal class Request
    {
        #pragma warning disable 169
        IntPtr m_nativeRequestPtr;

        public extern string Payload { get; }
        public extern string Url { get; }
        public extern int MessageType { get; }
        public extern int Depth { get; }
        public extern bool Info
        {
            [NativeMethod("IsInfo")]
            get;
        }
        public extern string GetParam(string paramName);
    }

    [NativeHeader("Modules/RestService/Public/Response.h")]
    [NativeHeader("Modules/RestService/ScriptBindings/RestService.bindings.h")]
    internal class Response
    {
        #pragma warning disable 169
        IntPtr m_nativeRequestPtr;

        public const ulong kCalcContentLength = ulong.MaxValue;
        public const ulong kChunkedContent = ulong.MaxValue - 1;

        [NativeMethod(Name = "RestServiceBindings::SimpleResponse", IsThreadSafe = true, HasExplicitThis = true)]
        extern public void SimpleResponse(HttpStatusCode status, string contentType, string payload);

        [NativeMethod(Name = "RestServiceBindings::SetStatusCode", IsThreadSafe = true, HasExplicitThis = true)]
        extern public void SetStatusCode(int statusCode);

        [NativeMethod(Name = "RestServiceBindings::SetContentType", IsThreadSafe = true, HasExplicitThis = true)]
        extern public void SetContentType(string contentType);

        [NativeMethod(IsThreadSafe = true)]
        extern public void SetContentLength(ulong contentLength);

        [NativeMethod(Name = "RestServiceBindings::AppendHeaders", IsThreadSafe = true, HasExplicitThis = true)]
        extern public void AppendHeaders(string headerFields);

        [NativeMethod(Name = "RestServiceBindings::EnqueueBodyData", IsThreadSafe = true, HasExplicitThis = true)]
        extern public void EnqueueBodyData(byte[] data, uint size);

        [NativeMethod(IsThreadSafe = true)]
        extern public void BeginTransport();

        [NativeMethod(IsThreadSafe = true)]
        extern public void Submit();
    }

    [NativeHeader("Modules/RestService/Public/Transport/HttpTransport.h")]
    internal class RestService
    {
        [NativeMethod(Name = "RestService::GetGeneratedCertificatePublicKey", IsThreadSafe = true, IsFreeFunction = true)]
        extern public static string GetGeneratedCertificatePublicKey();

        [NativeMethod(Name = "RestService::GetApiKey", IsThreadSafe = true, IsFreeFunction = true)]
        extern public static string GetApiKey();
    }

    [NativeHeader("Modules/RestService/ScriptBindings/RestService.bindings.h")]
    [NativeHeader("Modules/RestService/Public/Router.h")]
    internal class Router
    {
        [NativeMethod(Name = "RestServiceBindings::RegisterHandler", IsFreeFunction = true)]
        extern public static bool RegisterHandler(string route, Handler handler);

        [NativeMethod(Name = "RestService::UnregisterManagedHandler", IsFreeFunction = true)]
        extern static public void UnregisterHandler(string route);
    }
}
