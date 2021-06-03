// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    [Obsolete("IDataWatchHandle is no longer supported and will be removed soon", true)]
    internal interface IDataWatchHandle : IDisposable
    {
        Object watched { get; }
        bool disposed { get; }
    }

    [Obsolete("IDataWatchService is no longer supported and will be removed soon", true)]
    internal interface IDataWatchService
    {
        IDataWatchHandle AddWatch(Object watched, Action<Object> onDataChanged);
        void RemoveWatch(IDataWatchHandle handle);
        void ForceDirtyNextPoll(Object obj);
    }
}
