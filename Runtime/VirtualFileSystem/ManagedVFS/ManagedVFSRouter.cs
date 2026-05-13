// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RequiredByNativeCode]
    [VisibleToOtherModules("UnityEngine.ContentLoadModule")]
    internal static class ManagedVFSRouter
    {
        struct Binding
        {
            public IManagedVFSFileHandler handler;
            public int handle;
        }

        static readonly Dictionary<InternalManagedFileHandle, Binding> s_HandleToBinding = new Dictionary<InternalManagedFileHandle, Binding>();
        static int s_NextHandle = 1;
        static readonly ReaderWriterLockSlim s_Lock = new ReaderWriterLockSlim();

        internal static InternalManagedFileHandle AllocateHandle(IManagedVFSFileHandler handler, int handle)
        {
            using (new WriteLockScope(s_Lock))
            {
                var internalHandle = new InternalManagedFileHandle { handle = s_NextHandle++ };
                s_HandleToBinding[internalHandle] = new Binding { handler = handler, handle = handle };
                return internalHandle;
            }
        }

        internal static void ReleaseHandle(InternalManagedFileHandle handle)
        {
            using (new WriteLockScope(s_Lock))
                s_HandleToBinding.Remove(handle);
        }

        static Binding GetBinding(InternalManagedFileHandle handle)
        {
            using (new ReadLockScope(s_Lock))
            {
                if (s_HandleToBinding.TryGetValue(handle, out var binding))
                    return binding;
            }
            throw new Exception($"ManagedVFSRouter: invalid handle {handle.handle}");
        }

        [RequiredByNativeCode]
        internal static long GetSize(InternalManagedFileHandle handle)
        {
            try
            {
                Binding binding = GetBinding(handle);
                return binding.handler.GetSize(binding.handle);
            }
            catch (Exception e)
            {
                Debug.LogError($"ManagedVFSRouter.GetSize failed: {e.Message}");
                return -1;
            }
        }

        [RequiredByNativeCode]
        internal static void ReadBytesAsync(
            InternalManagedFileHandle handle, long offset, IntPtr buffer, int count,
            ManagedReadAsyncCommand command)
        {
            try
            {
                Binding binding = GetBinding(handle);
                binding.handler.ReadAsync(binding.handle, offset, buffer, count, command);
            }
            catch (Exception e)
            {
                Debug.LogError($"ManagedVFSRouter.ReadBytesAsync failed: {e.Message}");
                command.Complete(0, false);
            }
        }

        [RequiredByNativeCode]
        internal static void CloseFile(InternalManagedFileHandle handle)
        {
            Binding binding;
            using (new WriteLockScope(s_Lock))
            {
                if (!s_HandleToBinding.TryGetValue(handle, out binding))
                    return;
                s_HandleToBinding.Remove(handle);
            }

            try
            {
                binding.handler.Close(binding.handle);
            }
            catch (Exception e)
            {
                Debug.LogError($"ManagedVFSRouter.CloseFile failed: {e.Message}");
            }
        }
    }
}
