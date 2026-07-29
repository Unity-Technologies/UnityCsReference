using System;

namespace Unity.Baselib.LowLevel
{
    internal static partial class Binding
    {
        public static readonly Baselib_Memory_PageAllocation Baselib_Memory_PageAllocation_Invalid = new Baselib_Memory_PageAllocation();
        public static readonly Baselib_RegisteredNetwork_Socket_UDP Baselib_RegisteredNetwork_Socket_UDP_Invalid = new Baselib_RegisteredNetwork_Socket_UDP();
        public static readonly Baselib_Socket_Handle Baselib_Socket_Handle_Invalid = new Baselib_Socket_Handle { handle = (IntPtr)(-1) };
        public static readonly Baselib_DynamicLibrary_Handle Baselib_DynamicLibrary_Handle_Invalid = new Baselib_DynamicLibrary_Handle { handle = (IntPtr)(-1) };
        public static readonly Baselib_FileIO_EventQueue Baselib_FileIO_EventQueue_Invalid = new Baselib_FileIO_EventQueue { handle = (IntPtr)0 };
        public static readonly Baselib_FileIO_AsyncFile Baselib_FileIO_AsyncFile_Invalid = new Baselib_FileIO_AsyncFile { handle = (IntPtr)0 };
        public static readonly Baselib_FileIO_SyncFile Baselib_FileIO_SyncFile_Invalid = new Baselib_FileIO_SyncFile { handle = (IntPtr)(-1) };

        // Provided for backward compatibility for packages that were previously using the bool versions of these APIs.
        unsafe public static void Baselib_Socket_SetIPv4DontFragHeader(Baselib_Socket_Handle socket, bool set, Baselib_ErrorState* errorState)
        {
            Baselib_Socket_SetIPv4DontFragHeader(socket, set ? 1 : 0, errorState);
        }

        unsafe public static void Baselib_RegisteredNetwork_Socket_UDP_SetIPv4DontFragHeader(Baselib_RegisteredNetwork_Socket_UDP socket, bool set, Baselib_ErrorState* errorState)
        {
            Baselib_RegisteredNetwork_Socket_UDP_SetIPv4DontFragHeader(socket, set ? 1 : 0, errorState);
        }
    }
}
