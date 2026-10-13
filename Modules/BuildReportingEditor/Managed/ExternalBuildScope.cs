// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build
{
    // Returned by BuildHistory.BeginExternalBuild. Keeps the build running until disposed.
    internal sealed class ExternalBuildScope : IDisposable
    {
        readonly GUID m_BuildSessionGuid;
        bool m_Disposed;

        internal ExternalBuildScope(GUID buildSessionGuid)
        {
            m_BuildSessionGuid = buildSessionGuid;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            BuildHistory.EndActiveBuild(m_BuildSessionGuid);
        }
    }
}
