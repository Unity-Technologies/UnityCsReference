// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor.ShortcutManagement;

namespace Unity.GraphToolsFoundation.Editor
{
    class ToolShortcutEntryInfo_Internal : IShortcutEntryDiscoveryInfo
    {
        ShortcutEntry m_ShortcutEntry;
        MethodInfo m_MethodInfo;
        string m_FilePath;
        int m_LineNumber = -1;
        bool m_DebugInfoFetched;

        public ToolShortcutEntryInfo_Internal(ShortcutDefinition_Internal shortcutDefinition)
        {
            var identifier = new Identifier(shortcutDefinition.ToolName + "/" + shortcutDefinition.ShortcutId);
            var displayName = shortcutDefinition.ToolName + "/" + shortcutDefinition.DisplayName;
            var defaultCombination = shortcutDefinition.DefaultBinding.keyCombinationSequence;
            var type = shortcutDefinition.IsClutch ? ShortcutType.Clutch : ShortcutType.Action;
            Action<ShortcutArguments> action = (Action<ShortcutArguments>)Delegate.CreateDelegate(typeof(Action<ShortcutArguments>), null, shortcutDefinition.MethodInfo);

            m_ShortcutEntry = new ShortcutEntry(identifier, defaultCombination, action, shortcutDefinition.Context, type, displayName);
            m_MethodInfo = shortcutDefinition.MethodInfo;
        }

        public ShortcutEntry GetShortcutEntry()
        {
            return m_ShortcutEntry;
        }

        public string GetFullMemberName()
        {
            return m_MethodInfo.DeclaringType?.FullName + "." + m_MethodInfo.Name;
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
