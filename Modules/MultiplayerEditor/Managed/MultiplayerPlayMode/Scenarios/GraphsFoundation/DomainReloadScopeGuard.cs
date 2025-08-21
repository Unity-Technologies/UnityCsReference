// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// This class is used to guard the execution of a node from domain reloads.
    /// If a domain reload is requested while the node is running, the guard will throw an exception
    /// unless the node is decorated with the CanRequestDomainReloadAttribute.
    /// </summary>
    /// <remarks>
    /// using (var new DomainReloadScopeGuard(node))
    /// {
    ///    await node.RunAsyncImplementation();
    /// }
    /// </remarks>
    internal class DomainReloadScopeGuard : IDisposable
    {
        private Action<bool> m_Callback;

        public DomainReloadScopeGuard(Action<bool> onDomainReloadCallback)
        {
            m_Callback = onDomainReloadCallback;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeDomainReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeDomainReload;
        }

        public void Dispose()
        {
            // This will be called only when the execution of the node is completed before the domain
            // reload happens.

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeDomainReload;

            // Keeping this for reference. This will be invoked if the domain reload is requested but not necessarily
            // if it actually happened.
            // if (InternalUtilities.IsDomainReloadRequested())
            //     m_Callback?.Invoke(true);
        }

        private void OnBeforeDomainReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeDomainReload;

            // This will be called when the domain reload is requested while scope hasn't been completed.
            // In such cases, the Dispose method won't be called.

            m_Callback?.Invoke(false);
        }
    }
}
