// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutAttributeDiscoveryProvider : IDiscoveryShortcutProvider
    {
        internal const string k_FindShortcutAttributesMarkerName = "ShortcutManagement.FindShortcutAttributes";

        [NoAutoStaticsCleanup] // immutable profiler marker, safe to persist
        static readonly ProfilerMarker k_FindShortcutAttributesMarker = new(ProfilerCategory.Loading, k_FindShortcutAttributesMarkerName);

        // Assemblies only change on a code reload, which replaces this provider; UpdateBaseProfile resets reused entries
        List<IShortcutEntryDiscoveryInfo> m_DefinedShortcuts;

        public IEnumerable<IShortcutEntryDiscoveryInfo> GetDefinedShortcuts()
        {
            return m_DefinedShortcuts ??= FindDefinedShortcuts();
        }

        static List<IShortcutEntryDiscoveryInfo> FindDefinedShortcuts()
        {
            using var marker = k_FindShortcutAttributesMarker.Auto();
            const BindingFlags staticMethodsBindings = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = EditorAssemblies.GetAllMethodsWithAttribute<ShortcutBaseAttribute>(staticMethodsBindings);

            var results = new List<IShortcutEntryDiscoveryInfo>();
            foreach (var method in methods)
            {
                foreach (ShortcutBaseAttribute attribute in method.GetCustomAttributes(typeof(ShortcutBaseAttribute), true))
                    results.Add(new ShortcutAttributeEntryInfo(method, attribute));
            }

            return results;
        }
    }

    class ShortcutMenuItemDiscoveryProvider : IDiscoveryShortcutProvider
    {
        [NoAutoStaticsCleanup] // a binding string always parses to the same result, safe to persist
        static readonly Dictionary<string, KeyCombination?> s_ParsedBindingStrings = new();

        public IEnumerable<IShortcutEntryDiscoveryInfo> GetDefinedShortcuts()
        {
            var entries = new List<IShortcutEntryDiscoveryInfo>();
            var names = new List<string>();
            var defaultShortcuts = new List<string>();
            Menu.GetMenuItemDefaultShortcuts(names, defaultShortcuts);
            entries.Capacity += names.Count;

            for (var index = 0; index < names.Count; ++index)
            {
                var keys = new List<KeyCombination>();
                if (TryParseBindingString(defaultShortcuts[index], out var keyCombination))
                    keys.Add(keyCombination);
                entries.Add(new MenuItemEntryDiscoveryInfo(names[index], keys));
            }

            return entries;
        }

        internal static bool TryParseBindingString(string bindingString, out KeyCombination keyCombination)
        {
            keyCombination = default;
            if (string.IsNullOrEmpty(bindingString))
                return false;

            if (!s_ParsedBindingStrings.TryGetValue(bindingString, out var parsed))
            {
                if (KeyCombination.TryParseMenuItemBindingString(bindingString, out var parsedKeyCombination))
                    parsed = parsedKeyCombination;
                s_ParsedBindingStrings.Add(bindingString, parsed);
            }

            keyCombination = parsed.GetValueOrDefault();
            return parsed.HasValue;
        }
    }

    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    class MethodSourceFinderUtility
    {
        [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
        internal struct SourceInfo
        {
            public int lineNumber;
            public string filePath;
        }

        private static SequencePoint FindSequencePoint(MethodDefinition methodWithBody)
        {
            foreach (var instruction in methodWithBody.Body.Instructions)
            {
                var seq = methodWithBody.DebugInformation.GetSequencePoint(instruction);
                if (seq != null)
                    return seq;
            }

            return null;
        }

        private static SequencePoint FromMethodInfo(MethodInfo methodInfo)
        {
            var assembly = methodInfo.DeclaringType.Assembly;
            var parms = new ReaderParameters { ReadSymbols = true };
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly.GetLoadedAssemblyPath(), parms))
            {

                var convertedFullName = methodInfo.DeclaringType.FullName.Replace("+", "/");
                var typeDefinition = assemblyDefinition.MainModule.GetType(convertedFullName);
                var expectedParams = methodInfo.GetParameters();

                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    if (methodDefinition.Name != methodInfo.Name)
                        continue;

                    var paramz = methodDefinition.Parameters;
                    if (paramz.Count != expectedParams.Length)
                        continue;

                    var sameParameters = true;
                    for (int i = 0; i < expectedParams.Length; ++i)
                    {
                        var typeEquals = paramz[i].ParameterType.FullName == expectedParams[i].ParameterType.FullName;
                        sameParameters = sameParameters && typeEquals;
                        if (!sameParameters)
                            break;
                    }

                    if (sameParameters)
                        return FindSequencePoint(methodDefinition);
                }
            }

            return null;
        }

        [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
        internal static SourceInfo GetSourceInfo(MethodInfo methodInfo)
        {
            var seq = FromMethodInfo(methodInfo);
            if(seq == null)
            {
                throw new InvalidOperationException("Can't find symbol information for " + methodInfo);
            }
            SourceInfo sourceInfo = new SourceInfo()
            {
                lineNumber = seq.StartLine,
                filePath = seq.Document.Url
            };

            return sourceInfo;
        }
    }

    class MenuItemEntryDiscoveryInfo : IShortcutEntryDiscoveryInfo
    {
        string m_MenuItemPath;
        List<KeyCombination> m_KeyCombinations;
        ShortcutEntry m_ShortcutEntry;
        bool m_DebugInfoFetched;
        string m_FilePath;
        int m_LineNumber = -1;
        string m_FullMemberName;

        public MenuItemEntryDiscoveryInfo(string menuItemPath, List<KeyCombination> keys)
        {
            m_KeyCombinations = keys;
            m_MenuItemPath = menuItemPath;

            Action<ShortcutArguments> menuAction = (args) => { EditorApplication.ExecuteMenuItem(m_MenuItemPath); };
            m_ShortcutEntry = new ShortcutEntry(new Identifier(Discovery.k_MainMenuShortcutPrefix + m_MenuItemPath), m_KeyCombinations, menuAction, null, ShortcutType.Menu, null, int.MaxValue);
        }

        public ShortcutEntry GetShortcutEntry()
        {
            return m_ShortcutEntry;
        }

        public string GetFullMemberName()
        {
            GetMenuItemExtraInfoIfNeeded();
            return m_FullMemberName;
        }

        public int GetLineNumber()
        {
            GetMenuItemExtraInfoIfNeeded();
            return m_LineNumber;
        }

        public string GetFilePath()
        {
            GetMenuItemExtraInfoIfNeeded();
            return m_FilePath;
        }

        void GetMenuItemExtraInfoIfNeeded()
        {
            if (m_DebugInfoFetched)
                return;

            m_DebugInfoFetched = true;
            var managedMenuItemMethods = EditorAssemblies.GetAllMethodsWithAttribute<MenuItem>();
            foreach (var managedMenuItemMethod in managedMenuItemMethods)
            {
                var attributes = managedMenuItemMethod.GetCustomAttributes(typeof(MenuItem), false);
                foreach (var attribute in attributes)
                {
                    var menuAttribute = (MenuItem)attribute;
                    if (menuAttribute.menuItem == m_MenuItemPath)
                    {
                        var sourceInfo = MethodSourceFinderUtility.GetSourceInfo(managedMenuItemMethod);
                        m_FilePath = sourceInfo.filePath;
                        m_LineNumber = sourceInfo.lineNumber;
                        m_FullMemberName = managedMenuItemMethod.DeclaringType.FullName + "." + managedMenuItemMethod.Name;
                        return;
                    }
                }
            }
        }
    }

    class ShortcutAttributeEntryInfo : IShortcutEntryDiscoveryInfo
    {
        ShortcutEntry m_ShortcutEntry;
        MethodInfo m_MethodInfo;
        string m_FilePath;
        int m_LineNumber = -1;
        bool m_DebugInfoFetched;

        public ShortcutAttributeEntryInfo(MethodInfo methodInfo, ShortcutBaseAttribute attribute)
        {
            m_MethodInfo = methodInfo;
            m_ShortcutEntry = attribute.CreateShortcutEntry(m_MethodInfo);
        }

        public ShortcutEntry GetShortcutEntry()
        {
            return m_ShortcutEntry;
        }

        public string GetFullMemberName()
        {
            return m_MethodInfo.DeclaringType.FullName + "." + m_MethodInfo.Name;
        }

        public int GetLineNumber()
        {
            GetMethodDefinitionInfoIfNeeded();

            return m_LineNumber;
        }

        public string GetFilePath()
        {
            GetMethodDefinitionInfoIfNeeded();

            return m_FilePath;
        }

        void GetMethodDefinitionInfoIfNeeded()
        {
            if (m_DebugInfoFetched)
                return;
            m_DebugInfoFetched = true;
            var sourceInfo = MethodSourceFinderUtility.GetSourceInfo(m_MethodInfo);
            m_FilePath = sourceInfo.filePath;
            m_LineNumber = sourceInfo.lineNumber;
        }
    }
}
