// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    sealed class ShortcutProviderProxy : IDiscoveryShortcutProviderProxy
    {
        static ShortcutProviderProxy s_ShortcutProviderProxy;

        public static ShortcutProviderProxy GetInstance()
        {
            if (s_ShortcutProviderProxy == null)
            {
                s_ShortcutProviderProxy = new ShortcutProviderProxy();
                ToolShortcutDiscoveryProvider.GetInstance().Proxy = s_ShortcutProviderProxy;
            }

            return s_ShortcutProviderProxy;
        }

        List<(string toolName, Type context, Func<string, bool> shortcutFilter)> m_Tools;

        ShortcutProviderProxy()
        {
            m_Tools = new List<(string toolName, Type editorWindowType, Func<string, bool> shortcutFilter)>();
        }

        public void AddTool(string toolName, Type editorWindowType, Func<string, bool> shortcutFilter, bool rebuildNow = false)
        {
            if (!m_Tools.Contains((toolName, editorWindowType, shortcutFilter)))
            {
                m_Tools.Add((toolName, editorWindowType, shortcutFilter));

                if (rebuildNow)
                {
                    ToolShortcutDiscoveryProvider.RebuildShortcuts();
                }
            }
        }

        internal void RemoveTool(string toolName, Type editorWindowType, Func<string, bool> shortcutFilter, bool rebuildNow = false)
        {
            m_Tools.Remove((toolName, editorWindowType, shortcutFilter));
            if (rebuildNow)
            {
                ToolShortcutDiscoveryProvider.RebuildShortcuts();
            }
        }

        public static void RegisterShortcutContext(IShortcutContext context)
        {
            ToolShortcutDiscoveryProvider.RegisterShortcutContext(context);
        }

        public IEnumerable<ShortcutDefinition> GetDefinedShortcuts()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var shortcutEventTypes = TypeCache.GetTypesWithAttribute<ToolShortcutEventAttribute>()
#pragma warning restore RS0030
                .Where(t => typeof(IShortcutEvent).IsAssignableFrom(t) && AssemblyCache.CachedAssemblies.Contains(t.Assembly))
                .ToList();

            foreach (var type in shortcutEventTypes)
            {
                MethodInfo methodInfo = null;
                var attributes = (ToolShortcutEventAttribute[])type.GetCustomAttributes(typeof(ToolShortcutEventAttribute), false);
                foreach (var attribute in attributes)
                {
                    if (attribute.OnlyOnPlatforms != null && !attribute.OnlyOnPlatforms.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (attribute.ExcludedPlatforms != null && attribute.ExcludedPlatforms.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (methodInfo == null)
                        methodInfo = type.GetMethod("SendEvent",
                            BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);

                    Debug.Assert(methodInfo != null);

                    foreach (var (toolName, context, shortcutFilter) in m_Tools)
                    {
                        if (attribute.ToolName != null && toolName != attribute.ToolName)
                            continue;

                        if (!shortcutFilter?.Invoke(attribute.Identifier) ?? false)
                            continue;

                        yield return new ShortcutDefinition
                        {
                            ToolName = toolName,
                            ShortcutId = attribute.Identifier,
                            Context = context,
                            DefaultBinding = attribute.DefaultBinding,
                            DisplayName = attribute.DisplayName,
                            IsClutch = attribute.IsClutch,
                            MethodInfo = methodInfo
                        };
                    }
                }
            }
        }
    }
}
