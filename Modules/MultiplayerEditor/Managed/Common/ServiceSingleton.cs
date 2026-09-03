// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Multiplayer.Common.Editor;

static partial class ServiceSingleton<TService, TDefault>
    where TService : class
    where TDefault : class, TService, new()
{
    [AutoStaticsCleanupOnCodeReload] // lazy singleton; old ALC instance would be stale after reload
    static TService s_Instance;

    internal static TService Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new TDefault();
            }
            return s_Instance;
        }
    }

    internal class OverrideScope : IDisposable
    {
        readonly TService m_PreviousInstance;

        public OverrideScope(TService newInstance)
        {
            m_PreviousInstance = s_Instance;
            s_Instance = newInstance;
        }

        public void Dispose()
        {
            s_Instance = m_PreviousInstance;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
