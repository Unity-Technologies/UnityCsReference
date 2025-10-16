// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.DataModel;

internal class UdmLogCapture
{
    internal readonly struct Entry
    {
        internal readonly UdmLogType Type;
        internal readonly UdmObjectId ObjectId;
        internal readonly string Message;

        internal Entry(UdmLogType type, UdmObjectId objectId, string message)
        {
            Type = type;
            ObjectId = objectId;
            Message = message;
        }
    }

    internal UdmLogCapture()
    {
        EntriesByType = new List<Entry>[LogTypesDescending.Length];
    }

    internal void AddEntry(UdmLogType type, UdmObjectId objectId, string message)
    {
        var entries = EntriesByType[(int)type] ??= new List<Entry>();
        entries.Add(new Entry(type, objectId, message));
    }

    internal IReadOnlyList<Entry> GetEntriesByType(UdmLogType type)
    {
        var entries = EntriesByType[(int)type];
        return (IReadOnlyList<Entry>)entries ?? Array.Empty<Entry>();
    }

    internal bool Any
    {
        get
        {
            foreach (var entries in EntriesByType)
            {
                if (entries != null)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal (UdmLogType, string) ProduceLogMessage(string prefixMessage)
    {
        var result = new StringBuilder(prefixMessage);
        result.AppendLine();

        UdmLogType maxLogType = default;
        foreach (var type in LogTypesDescending)
        {
            var entries = EntriesByType[(int)type];
            if (entries != null)
            {
                result.AppendLine($"{type}s:");

                foreach (var entry in entries)
                {
                    result.AppendLine(entry.Message);
                }

                if (maxLogType == default)
                {
                    maxLogType = type;
                }
            }
        }

        return (maxLogType, result.ToString());
    }

    private static UdmLogType[] GetLogTypesDescending()
    {
        var allLogTypes = (UdmLogType[])Enum.GetValues(typeof(UdmLogType));
        Array.Reverse(allLogTypes);
        return allLogTypes;
    }

    private readonly List<Entry>[] EntriesByType;
    internal static readonly UdmLogType[] LogTypesDescending = GetLogTypesDescending();
}

internal class UdmLogCaptureHandler : IDisposable
{
    internal UdmLogCaptureHandler(UdmLogCapture logCapture)
    {
        LogCapture = logCapture;
        LogCaptureHandle = GCHandle.Alloc(LogCapture);
        Logger = new UdmLogger(HandleLog, GCHandle.ToIntPtr(LogCaptureHandle));
    }

/*
#if !NETSTANDARD2_1
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#endif
*/
    internal static void HandleLog(IntPtr context, UdmLogType type, string file, int line, UdmObjectId objectId, string message)
    {
        object? target = GCHandle.FromIntPtr(context).Target;
        if (target is UdmLogCapture logCapture)
            logCapture.AddEntry(type, objectId, message);
    }

    public void Dispose()
    {
        LogCaptureHandle.Free();
    }

    private readonly UdmLogCapture LogCapture;
    private readonly GCHandle LogCaptureHandle;
    internal UdmLogger Logger { get; }
}
