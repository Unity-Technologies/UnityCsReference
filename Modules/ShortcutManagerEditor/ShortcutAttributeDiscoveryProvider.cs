// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutAttributeDiscoveryProvider : IDiscoveryShortcutProvider
    {
        public IEnumerable<IShortcutEntryDiscoveryInfo> GetDefinedShortcuts()
        {
            const BindingFlags staticMethodsBindings = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = EditorAssemblies.GetAllMethodsWithAttribute<ShortcutBaseAttribute>(staticMethodsBindings);

            var results = new List<IShortcutEntryDiscoveryInfo>(methods.Count());
            foreach (var methodInfo in methods)
            {
                var attributes = (ShortcutBaseAttribute[])methodInfo.GetCustomAttributes(typeof(ShortcutBaseAttribute), true);
                foreach (var attribute in attributes)
                {
                    var discoveredAttributeEntry = new ShortcutAttributeEntryInfo(methodInfo, attribute);
                    results.Add(discoveredAttributeEntry);
                }
            }

            return results;
        }
    }

    class ShortcutMenuItemDiscoveryProvider : IDiscoveryShortcutProvider
    {
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
                KeyCombination keyCombination;
                if (KeyCombination.TryParseMenuItemBindingString(defaultShortcuts[index], out keyCombination))
                    keys.Add(keyCombination);
                entries.Add(new MenuItemEntryDiscoveryInfo(names[index], keys));
            }

            return entries;
        }
    }

    class MethodSourceFinderUtility
    {
        internal struct SourceInfo
        {
            public int lineNumber;
            public string filePath;
        }

        internal static SequencePoint FindSequencePoint(MethodDefinition methodWithBody)
        {
            foreach (var instruction in methodWithBody.Body.Instructions)
            {
                var seq = methodWithBody.DebugInformation.GetSequencePoint(instruction);
                if (seq != null)
                    return seq;
            }

            return null;
        }

        internal static MethodDefinition FromMethodInfo(MethodInfo methodInfo)
        {
            var assembly = methodInfo.DeclaringType.Assembly;
            var parms = new ReaderParameters { ReadSymbols = true };
            var module = ModuleDefinition.ReadModule(assembly.Location, parms);

            var convertedFullName = methodInfo.DeclaringType.FullName.Replace("+", "/");
            var typeDefinition = module.GetType(convertedFullName);
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
                    return methodDefinition;
            }

            return null;
        }

        internal static SourceInfo GetSourceInfo(MethodInfo methodInfo)
        {
            var methodWithBody = FromMethodInfo(methodInfo);
            var seq =  FindSequencePoint(methodWithBody);
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
            m_ShortcutEntry = new ShortcutEntry(new Identifier(m_MenuItemPath), m_KeyCombinations, menuAction, null, ShortcutType.Menu);
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
            var managedMenuItemMehtods = EditorAssemblies.GetAllMethodsWithAttribute<MenuItem>();
            foreach (var managedMenuItemMehtod in managedMenuItemMehtods)
            {
                var attrbiutes = managedMenuItemMehtod.GetCustomAttributes(typeof(MenuItem), false);
                foreach (var attribute in attrbiutes)
                {
                    var menuAttribute = (MenuItem)attribute;
                    if (menuAttribute.menuItem == m_MenuItemPath)
                    {
                        var sourceInfo = MethodSourceFinderUtility.GetSourceInfo(managedMenuItemMehtod);
                        m_FilePath = sourceInfo.filePath;
                        m_LineNumber = sourceInfo.lineNumber;
                        m_FullMemberName = managedMenuItemMehtod.DeclaringType.FullName + "." + managedMenuItemMehtod.Name;
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
