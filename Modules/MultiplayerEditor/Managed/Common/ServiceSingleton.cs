// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Multiplayer.Common.Editor;

static partial class ServiceSingleton<TService, TDefault>
    where TService : class
    where TDefault : class, TService, new()
{
    [AutoStaticsCleanupOnCodeReload] // lazy singleton; old ALC instance would be stale after reload
    // Lazy singleton: the Instance getter constructs the default service when this is null, so it comes
    // back on the next access.
    [IgnoreForUAL0015("Lazy singleton reconstructed by the Instance getter after cleanup nulls it")]
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
#pragma warning disable UAL0018 // OverrideScope is only ever used as a `using` scope: the saved instance is handed straight back on Dispose within the same call and never outlives it
            m_PreviousInstance = s_Instance;
#pragma warning restore UAL0018
            s_Instance = newInstance;
        }

        public void Dispose()
        {
            s_Instance = m_PreviousInstance;
        }
    }
}
