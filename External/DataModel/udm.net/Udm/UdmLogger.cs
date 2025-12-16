using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.DataModel;

// This is temporary, while .net 8 is supported by the rest of the build pipeline
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void UdmLogHandler(IntPtr context, UdmLogType type, string file, int line, UdmObjectId objectId, string message);

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UdmLogger
{
    internal static readonly UdmLogger Default = new UdmLogger
    {
        Handler = IntPtr.Zero,
        Context = IntPtr.Zero
    };

    // This is temporary, while .net 8 is supported by the rest of the build pipeline
    // Stub: original uses function pointer syntax unsupported by reference-source build
    internal UdmLogger(UdmLogHandler handler, IntPtr context = default)
    {
        Handler = (IntPtr)Marshal.GetFunctionPointerForDelegate(handler).ToPointer();
        Context = context;
    }

internal static UdmLogger GetStandardErrorLogger()
{
    return UdmInterop.Instance.udm_get_stderr_logger();
}

internal static UdmLogger GetDefaultLogger()
{
    return UdmInterop.Instance.udm_get_default_logger();
}

internal bool IsValid()
{
    unsafe
    {
        return Handler != IntPtr.Zero;
    }
}

internal void LogInfo(
    string message,
    UdmObjectId objectId = default,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
{
    ThrowIfInvalid();

    unsafe
    {
        fixed (UdmLogger* loggerPtr = &this)
        {
            UdmInterop.Instance.udm_logger_log(loggerPtr, UdmLogType.Info, file, line, objectId, message);
        }
    }
}

internal void LogWarning(
    string message,
    UdmObjectId objectId = default,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
{
    ThrowIfInvalid();

    unsafe
    {
        fixed (UdmLogger* loggerPtr = &this)
        {
            UdmInterop.Instance.udm_logger_log(loggerPtr, UdmLogType.Warning, file, line, objectId, message);
        }
    }
}

internal void LogError(
    string message,
    UdmObjectId objectId = default,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
{
    ThrowIfInvalid();

    unsafe
    {
        fixed (UdmLogger* loggerPtr = &this)
        {
            UdmInterop.Instance.udm_logger_log(loggerPtr, UdmLogType.Error, file, line, objectId, message);
        }
    }
}

internal void ThrowIfInvalid()
{
    if (!IsValid())
        throw new InvalidOperationException("Trying to use an invalid UdmLogger");
}

internal IntPtr Handler;
internal IntPtr Context;
}
