//
// File autogenerated from Include/C/Baselib_DynamicLibrary.h
//

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using size_t = System.UIntPtr;

namespace Unity.Baselib.LowLevel
{
    [NativeHeader("baselib/CSharp/BindingsUnity/Baselib_DynamicLibrary.gen.binding.h")]
    internal static unsafe partial class Binding
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Baselib_DynamicLibrary_Handle
        {
            public IntPtr handle;
        }
        /// <summary>Open a dynamic library.</summary>
        /// <remarks>
        /// Dynamic libraries are reference counted, so if the same library is loaded again
        /// with Baselib_DynamicLibrary_OpenUtf8/Baselib_DynamicLibrary_OpenUtf16, the same file handle is returned.
        /// It is also possible to load two different libraries containing two different functions that have the same name.
        ///
        /// Please note that additional error information should be retrieved via error state explain and be presented to the end user.
        /// This is needed to improve ergonomics of debugging library loading issues.
        ///
        /// Possible error codes:
        /// - Baselib_ErrorCode_FailedToOpenDynamicLibrary: Unable to open requested dynamic library.
        /// - Baselib_ErrorCode_NotSupported: This feature is not supported on the current platform.
        /// </remarks>
        /// <param name="pathnameUtf8">
        /// Library file to be opened.
        /// If relative pathname is provided, platform library search rules are applied (if any).
        /// If nullptr is passed, Baselib_ErrorCode_InvalidArgument will be risen.
        /// </param>
        [FreeFunction(IsThreadSafe = true)]
        public static extern Baselib_DynamicLibrary_Handle Baselib_DynamicLibrary_OpenUtf8(byte* pathnameUtf8, Baselib_ErrorState* errorState);
        /// <summary>
        /// Open a dynamic library.
        /// Functionally identical to Baselib_DynamicLibrary_OpenUtf8, but accepts UTF-16 path instead.
        /// </summary>
        [FreeFunction(IsThreadSafe = true)]
        public static extern Baselib_DynamicLibrary_Handle Baselib_DynamicLibrary_OpenUtf16(char* pathnameUtf16, Baselib_ErrorState* errorState);
        /// <summary>
        /// Return a handle that can be used to query functions in the program's scope.
        /// Must be closed via Baselib_DynamicLibrary_Close.
        /// </summary>
        /// <remarks>
        /// Possible error codes:
        /// - Baselib_ErrorCode_NotSupported: This feature is not supported on the current platform.
        /// </remarks>
        [FreeFunction(IsThreadSafe = true)]
        public static extern Baselib_DynamicLibrary_Handle Baselib_DynamicLibrary_OpenProgramHandle(Baselib_ErrorState* errorState);
        /// <summary>Convert native handle into baselib handle without changing the dynamic library ref counter.</summary>
        /// <remarks>
        /// Provided handle should be closed either via Baselib_DynamicLibrary_Close or other means.
        /// The caller is responsible for closing the handle once done with it.
        /// Other corresponding resources should be closed by other means.
        /// </remarks>
        /// <param name="handle">Platform defined native handle.</param>
        /// <param name="type">
        /// Platform defined native handle type from Baselib_DynamicLibrary_NativeHandleType enum.
        /// If unsupported type is passed, will return Baselib_DynamicLibrary_Handle_Invalid.
        /// </param>
        /// <returns>Baselib_DynamicLibrary_Handle handle.</returns>
        [FreeFunction(IsThreadSafe = true)]
        public static extern Baselib_DynamicLibrary_Handle Baselib_DynamicLibrary_FromNativeHandle(UInt64 handle, UInt32 type, Baselib_ErrorState* errorState);
        /// <summary>Lookup a function in a dynamic library.</summary>
        /// <remarks>
        /// Possible error codes:
        /// - Baselib_ErrorCode_FunctionNotFound: Requested function was not found.
        /// </remarks>
        /// <param name="handle">
        /// Library handle.
        /// If Baselib_DynamicLibrary_Handle_Invalid is passed, Baselib_ErrorCode_InvalidArgument will be risen.
        /// </param>
        /// <param name="functionName">
        /// Function name to look for.
        /// If nullptr is passed, Baselib_ErrorCode_InvalidArgument will be risen.
        /// </param>
        /// <returns>pointer to the function (can be NULL for symbols mapped to NULL).</returns>
        [FreeFunction(IsThreadSafe = true)]
        public static extern IntPtr Baselib_DynamicLibrary_GetFunction(Baselib_DynamicLibrary_Handle handle, byte* functionName, Baselib_ErrorState* errorState);
        /// <summary>Close a dynamic library.</summary>
        /// <remarks>
        /// Decreases reference counter, if it becomes zero, closes the library.
        /// If system api will return an error during this operation, the process will be aborted.
        /// </remarks>
        /// <param name="handle">
        /// Library handle.
        /// If Baselib_DynamicLibrary_Handle_Invalid is passed, function is no-op.
        /// </param>
        [FreeFunction(IsThreadSafe = true)]
        public static extern void Baselib_DynamicLibrary_Close(Baselib_DynamicLibrary_Handle handle);
    }
}
