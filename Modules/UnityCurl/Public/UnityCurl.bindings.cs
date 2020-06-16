// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Curl
{
    [Flags]
    internal enum CurlEasyHandleFlags : uint
    {
        kSendBody = 1,
        kReceiveHeaders = 1 << 1,
        kReceiveBody = 1 << 2,
        kFollowRedirects = 1 << 3,
    }

    internal enum BufferOwnership
    {
        Copy = 0,
        Transfer = 1,
        External = 2,
    }

    [NativeHeader("Modules/UnityCurl/Public/UnityCurl.h")]
    [StaticAccessor("UnityCurl", StaticAccessorType.DoubleColon)]
    internal static class UnityCurl
    {
        [NativeMethod(IsThreadSafe = true)]
        internal static extern IntPtr CreateMultiHandle();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void DestroyMultiHandle(IntPtr handle);

        [NativeMethod(IsThreadSafe = true)]
        internal static unsafe extern IntPtr CreateEasyHandle(byte* method, byte* url, out uint curlMethod);

        [NativeMethod(IsThreadSafe = true)]
        internal static unsafe extern void SetupEasyHandle(IntPtr handle, uint curlMethod, IntPtr headers, ulong contentLen, uint flags);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void DestroyEasyHandle(IntPtr handle);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void QueueRequest(IntPtr multiHandle, IntPtr easyHandle);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe IntPtr AppendHeader(IntPtr headerList, byte* header);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void FreeHeaderList(IntPtr headerList);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetRequestErrorCode(IntPtr request);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetRequestStatus(IntPtr request);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern uint GetRequestStatusCode(IntPtr request);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void GetDownloadSize(IntPtr request, out ulong downloaded, out ulong expected);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe byte* GetResponseHeader(IntPtr request, uint index, out uint length);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe byte* GetMoreBody(IntPtr handle, out int length);

        internal static unsafe void SendMoreBody(IntPtr handle, byte* chunk, uint length, BufferOwnership ownership)
        {
            SendMoreBody(handle, chunk, length, (int)ownership);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void SendMoreBody(IntPtr handle, byte* chunk, uint length, int ownership);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe void AbortRequest(IntPtr handle);
    }
}

