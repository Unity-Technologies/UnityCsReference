// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/AsyncHTTPClient.bindings.h")]
    internal partial class AsyncHTTPClient
    {
        private delegate void RequestProgressCallback(AsyncHTTPClient.State status, int downloaded, int totalSize);
        private delegate void RequestDoneCallback(AsyncHTTPClient.State status, int httpStatus);

        private static extern IntPtr SubmitClientRequest(string tag, string url, string[] headers, string method, string data, RequestDoneCallback doneDelegate, RequestProgressCallback progressDelegate = null);

        private static extern byte[] GetBytesByHandle(IntPtr handle);

        private static extern Texture2D GetTextureByHandle(IntPtr handle);

        public static extern void AbortByTag(string tag);

        private static extern void AbortByHandle(IntPtr handle);

        [FreeFunction]
        public static extern void CurlRequestCheck();
    }
}
