// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A shortcut discovery provider for shortcuts defined by Graph Tools Foundation.
    /// We need this special provider because the shortcut defined by GTF must be
    /// associated to a tool, not to GTF itself. We delegate the shortcut discovery
    /// to a proxy that will be set in GTF code.
    /// </summary>
    sealed class ToolShortcutDiscoveryProvider_Internal : IDiscoveryShortcutProvider
    {
        static ToolShortcutDiscoveryProvider_Internal s_Instance;
        bool m_Initialized;

        /// <summary>
        /// The single instance of this class.
        /// </summary>
        public static ToolShortcutDiscoveryProvider_Internal GetInstance() => s_Instance ??= new ToolShortcutDiscoveryProvider_Internal();

        /// <summary>
        /// The proxy to which we delegate the shortcut discovery.
        /// </summary>
        public IDiscoveryShortcutProviderProxy_Internal Proxy { get; set; }

        ToolShortcutDiscoveryProvider_Internal()
        {
            m_Initialized = false;
            EditorApplication.update += Initialize;
        }

        void Initialize()
        {
            if (m_Initialized)
                return;

            EditorApplication.update -= Initialize;
            m_Initialized = true;

            var providers = GetShortcutProviders().ToList();
            if (!providers.Contains(this))
            {
                providers.Add(this);
                SetShortcutProviders(providers);
                ShortcutIntegration.instance.RebuildShortcuts();
            }
        }

        /// <summary>
        /// Forces rebuilding the shortcut.
        /// </summary>
        public static void RebuildShortcuts()
        {
            ShortcutIntegration.instance.RebuildShortcuts();
        }

        IEnumerable<IShortcutEntryDiscoveryInfo> IDiscoveryShortcutProvider.GetDefinedShortcuts()
        {
            return Proxy?.GetDefinedShortcuts().Select(si => new ToolShortcutEntryInfo_Internal(si)) ?? Enumerable.Empty<IShortcutEntryDiscoveryInfo>();
        }

        static IEnumerable<IDiscoveryShortcutProvider> GetShortcutProviders()
        {
            var controller = ShortcutIntegration.instance;

            // Get controller.m_Discovery and cast it to UnityEditor.ShortcutManagement.Discovery
            var discoveryField = controller.GetType().GetField("m_Discovery", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(discoveryField?.GetValue(controller) is Discovery discovery))
                return Enumerable.Empty<IDiscoveryShortcutProvider>();

            // Get discovery.m_ShortcutProviders and replace it by our own (current + ours)
            var shortcutProvidersField = discovery.GetType().GetField("m_ShortcutProviders", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(shortcutProvidersField?.GetValue(discovery) is IEnumerable<IDiscoveryShortcutProvider> shortcutProviders))
                return Enumerable.Empty<IDiscoveryShortcutProvider>();

            return shortcutProviders;
        }

        static void SetShortcutProviders(IEnumerable<IDiscoveryShortcutProvider> providers)
        {
            var controller = ShortcutIntegration.instance;

            // Get controller.m_Discovery and cast it to UnityEditor.ShortcutManagement.Discovery
            var discoveryField = controller.GetType().GetField("m_Discovery", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(discoveryField?.GetValue(controller) is Discovery discovery))
                return;

            // Get discovery.m_ShortcutProviders and replace it by our own (current + ours)
            var shortcutProvidersField = discovery.GetType().GetField("m_ShortcutProviders", BindingFlags.NonPublic | BindingFlags.Instance);

            shortcutProvidersField?.SetValue(discovery, providers);
        }
    }
}
