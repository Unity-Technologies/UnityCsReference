// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    sealed class ShortcutProviderProxy_Internal : IDiscoveryShortcutProviderProxy_Internal
    {
        static ShortcutProviderProxy_Internal s_ShortcutProviderProxy;

        public static ShortcutProviderProxy_Internal GetInstance()
        {
            if (s_ShortcutProviderProxy == null)
            {
                s_ShortcutProviderProxy = new ShortcutProviderProxy_Internal();
                ToolShortcutDiscoveryProvider_Internal.GetInstance().Proxy = s_ShortcutProviderProxy;
            }

            return s_ShortcutProviderProxy;
        }

        List<(string toolName, Type context, Func<string, bool> shortcutFilter)> m_Tools;

        ShortcutProviderProxy_Internal()
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
                    ToolShortcutDiscoveryProvider_Internal.RebuildShortcuts();
                }
            }
        }

        public IEnumerable<ShortcutDefinition_Internal> GetDefinedShortcuts()
        {
            var shortcutEventTypes = TypeCache.GetTypesWithAttribute<ToolShortcutEventAttribute>()
                .Where(t => typeof(IShortcutEvent).IsAssignableFrom(t) && AssemblyCache_Internal.CachedAssemblies_Internal.Contains(t.Assembly))
                .ToList();

            foreach (var type in shortcutEventTypes)
            {
                MethodInfo methodInfo = null;
                var attributes = (ToolShortcutEventAttribute[])type.GetCustomAttributes(typeof(ToolShortcutEventAttribute), false);
                foreach (var attribute in attributes)
                {
                    if (attribute.OnlyOnPlatforms_Internal != null && !attribute.OnlyOnPlatforms_Internal.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (attribute.ExcludedPlatforms_Internal != null && attribute.ExcludedPlatforms_Internal.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (methodInfo == null)
                        methodInfo = type.GetMethod("SendEvent",
                            BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);

                    Debug.Assert(methodInfo != null);

                    foreach (var(toolName, context, shortcutFilter) in m_Tools)
                    {
                        if (attribute.ToolName_Internal != null && toolName != attribute.ToolName_Internal)
                            continue;

                        if (!shortcutFilter?.Invoke(attribute.Identifier_Internal) ?? false)
                            continue;

                        yield return new ShortcutDefinition_Internal
                        {
                            ToolName = toolName,
                            ShortcutId = attribute.Identifier_Internal,
                            Context = context,
                            DefaultBinding = attribute.DefaultBinding_Internal,
                            DisplayName = attribute.DisplayName_Internal,
                            IsClutch = attribute.IsClutch_Internal,
                            MethodInfo = methodInfo
                        };
                    }
                }
            }
        }
    }
}
