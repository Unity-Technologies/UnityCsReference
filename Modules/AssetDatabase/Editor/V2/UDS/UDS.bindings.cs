// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetDatabase/Editor/V2/UDS/UDSInterface.h")]
    internal class UDS
    {
        [FreeFunction("UDSInterface::CommitSpan", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static Hash128 CommitSpanCopy(Span<byte> data);

        [FreeFunction("UDSInterface::GetContentBytes", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static byte[] RetrieveSpanCopy(Hash128 contentHash);

        [FreeFunction("UDSInterface::Acquire", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static IntPtr Acquire(Hash128 contentHash);

        [FreeFunction("UDSInterface::AcquireNoWait", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static IntPtr AcquireNoWait(Hash128 contentHash);

        [FreeFunction("UDSInterface::IsReadComplete", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static bool IsReadComplete(IntPtr readHandle);

        [FreeFunction("UDSInterface::IsReadSuccessful", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static bool IsReadSuccessful(IntPtr readHandle);

        [FreeFunction("UDSInterface::Release", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static void Release(IntPtr readHandle);

        [FreeFunction("UDSInterface::GetContent", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static ReadOnlySpan<byte> GetContent(IntPtr readHandle);
                
        [FreeFunction("UDSInterface::GetSystem", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern IntPtr GetSystem();

        [FreeFunction("uds_get_data_system_acquire_function", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern IntPtr GetDataSystemAcquireFunction();

        [FreeFunction("uds_get_data_system_commit_function", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern IntPtr GetDataSystemCommitFunction();

        [FreeFunction("uds_get_data_system_release_function", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern IntPtr GetDataSystemReleaseFunction();

        [FreeFunction("UDSInterface::UsingFullBackend", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        extern public static bool UsingFullBackend();
    }
}
