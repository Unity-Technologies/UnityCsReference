// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IDiscoveryInvalidContextReporter
    {
        void ReportInvalidContext(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidContext);
    }

    class DiscoveryInvalidContextReporter : IDiscoveryInvalidContextReporter
    {
        public void ReportInvalidContext(IShortcutEntryDiscoveryInfo discoveryInfoWithInvalidContext)
        {
            var shortcutEntry = discoveryInfoWithInvalidContext.GetShortcutEntry();
            var context = shortcutEntry.context;

            if (context == typeof(ContextManager.GlobalContext))
                throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));

            var isEditorWindow = typeof(EditorWindow).IsAssignableFrom(context);
            var isIShortcutToolContext = typeof(IShortcutToolContext).IsAssignableFrom(context);

            string line2;
            if (isEditorWindow)
            {
                if (context == typeof(EditorWindow))
                    line2 = $"The context type cannot be {typeof(EditorWindow).FullName}.";
                else if (isIShortcutToolContext)
                    line2 = $"The context type cannot both derive from {typeof(EditorWindow).FullName} and implement {typeof(IShortcutToolContext).FullName}.";
                else
                    throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));
            }
            else if (isIShortcutToolContext)
            {
                if (context == typeof(IShortcutToolContext))
                    line2 = $"The context type cannot be {typeof(IShortcutToolContext).FullName}.";
                else
                    throw new ArgumentException("Context type is valid", nameof(discoveryInfoWithInvalidContext));
            }
            else
                line2 = $"The context type must either be null, derive from {typeof(EditorWindow).FullName}, or implement {typeof(IShortcutToolContext).FullName}.";

            var filePath = discoveryInfoWithInvalidContext.GetFilePath();
            var lineNumber = discoveryInfoWithInvalidContext.GetLineNumber();
            var fullMemberName = discoveryInfoWithInvalidContext.GetFullMemberName();

            var line1 = $"{filePath}({lineNumber}): Ignoring shortcut attribute with invalid context type {context.FullName} on {fullMemberName}.";

            Debug.LogWarning($"{line1}\n{line2}");
        }
    }
}
