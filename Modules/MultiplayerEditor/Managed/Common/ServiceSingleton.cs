// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.Common.Editor;

static class ServiceSingleton<TService, TDefault>
    where TService : class
    where TDefault : class, TService, new()
{
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
