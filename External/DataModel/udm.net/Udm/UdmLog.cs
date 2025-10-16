using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.DataModel;

internal enum UdmLogType
{
    Info,
    Warning,
    Error
}

internal static class UdmLog
{
    internal static void LogInfo(
        string message,
        UdmObjectId objectId = default,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        unsafe
        {
            UdmInterop.Instance.udm_logger_log(null, UdmLogType.Info, file, line, objectId, message);
        }
    }

    internal static void LogWarning(
        string message,
        UdmObjectId objectId = default,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        unsafe
        {
            UdmInterop.Instance.udm_logger_log(null, UdmLogType.Warning, file, line, objectId, message);
        }
    }

    internal static void LogError(
        string message,
        UdmObjectId objectId = default,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        unsafe
        {
            UdmInterop.Instance.udm_logger_log(null, UdmLogType.Error, file, line, objectId, message);
        }
    }
}
