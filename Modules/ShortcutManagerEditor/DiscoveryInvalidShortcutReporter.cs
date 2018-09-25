// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IDiscoveryInvalidShortcutReporter
    {
        void ReportReservedIdentifierPrefixConflict(IShortcutEntryDiscoveryInfo discoveryInfoWithConflictingIdentifierPrefix, string reservedPrefix);
        void ReportIdentifierConflict(IShortcutEntryDiscoveryInfo discoveryInfoWithConflictingIdentifier);
        void ReportInvalidContext(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidContext);
        void ReportInvalidBinding(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidBinding, string invalidBindingMessage);
    }

    class DiscoveryInvalidShortcutReporter : IDiscoveryInvalidShortcutReporter
    {
        static void LogWarning(IShortcutEntryDiscoveryInfo discoveryInfo, string summary, string detail)
        {
            var filePath = discoveryInfo.GetFilePath();
            if (filePath == null)
                Debug.LogWarning($"{summary}\n{detail}");
            else
                Debug.LogWarning($"{filePath}({discoveryInfo.GetLineNumber()}): {summary}\n{detail}");
        }

        public void ReportReservedIdentifierPrefixConflict(IShortcutEntryDiscoveryInfo discoveryInfoWithConflictingIdentifierPrefix, string reservedPrefix)
        {
            var shortcutEntry = discoveryInfoWithConflictingIdentifierPrefix.GetShortcutEntry();

            var fullMemberName = discoveryInfoWithConflictingIdentifierPrefix.GetFullMemberName();
            var summary = $"Ignoring shortcut attribute with identifier using reserved prefix \"{shortcutEntry.identifier.path}\".";
            var detail = $"Shortcut attribute on {fullMemberName} is using identifier \"{shortcutEntry.identifier.path}\" with reserved prefix \"{reservedPrefix}\".";

            LogWarning(discoveryInfoWithConflictingIdentifierPrefix, summary, detail);
        }

        public void ReportIdentifierConflict(IShortcutEntryDiscoveryInfo discoveryInfoWithConflictingIdentifier)
        {
            var shortcutEntry = discoveryInfoWithConflictingIdentifier.GetShortcutEntry();

            var fullMemberName = discoveryInfoWithConflictingIdentifier.GetFullMemberName();
            var summary = $"Ignoring shortcut attribute with duplicate identifier \"{shortcutEntry.identifier.path}\".";
            var detail = $"Shortcut attribute on {fullMemberName} is using identifier \"{shortcutEntry.identifier.path}\" which is already in use by another shortcut attribute.";

            LogWarning(discoveryInfoWithConflictingIdentifier, summary, detail);
        }

        public void ReportInvalidContext(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidContext)
        {
            var shortcutEntry = discoveryInfoWithInvalidContext.GetShortcutEntry();
            var context = shortcutEntry.context;

            if (context == typeof(ContextManager.GlobalContext))
                throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));

            var isEditorWindow = typeof(EditorWindow).IsAssignableFrom(context);
            var isIShortcutToolContext = typeof(IShortcutToolContext).IsAssignableFrom(context);

            string detail;
            if (isEditorWindow)
            {
                if (context == typeof(EditorWindow))
                    detail = $"The context type cannot be {typeof(EditorWindow).FullName}.";
                else if (isIShortcutToolContext)
                    detail = $"The context type cannot both derive from {typeof(EditorWindow).FullName} and implement {typeof(IShortcutToolContext).FullName}.";
                else
                    throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));
            }
            else if (isIShortcutToolContext)
            {
                if (context == typeof(IShortcutToolContext))
                    detail = $"The context type cannot be {typeof(IShortcutToolContext).FullName}.";
                else
                    throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));
            }
            else
                detail = $"The context type must either be null, derive from {typeof(EditorWindow).FullName}, or implement {typeof(IShortcutToolContext).FullName}.";

            var fullMemberName = discoveryInfoWithInvalidContext.GetFullMemberName();
            var summary = $"Ignoring shortcut attribute with invalid context type {context.FullName} on {fullMemberName}.";

            LogWarning(discoveryInfoWithInvalidContext, summary, detail);
        }

        public void ReportInvalidBinding(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidBinding, string invalidBindingMessage)
        {
            var fullMemberName = discoveryInfoWithInvalidBinding.GetFullMemberName();
            var summary = $"Ignoring shortcut attribute with invalid binding on {fullMemberName}.";
            var detail = $"{invalidBindingMessage}.";

            LogWarning(discoveryInfoWithInvalidBinding, summary, detail);
        }
    }
}
