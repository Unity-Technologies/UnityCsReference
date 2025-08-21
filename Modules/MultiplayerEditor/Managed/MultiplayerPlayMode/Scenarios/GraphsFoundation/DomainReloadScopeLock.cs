// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class DomainReloadScopeLock : IDisposable
    {
        public DomainReloadScopeLock()
        {
            EditorApplication.LockReloadAssemblies();
        }

        public void Dispose()
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}
