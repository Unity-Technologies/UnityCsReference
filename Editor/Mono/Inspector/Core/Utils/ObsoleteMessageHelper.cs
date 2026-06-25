// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    static partial class ObsoleteMessageHelper
    {
        [NoAutoStaticsCleanup] // compiled from a fixed literal pattern with no runtime-state dependency; safe to persist across reload
        static Regex s_VersionTagRegex;

        // Matches version tags in the format #tagName(MAJOR.MINOR) where tagName is any latin letters.
        // Examples: #from(2022.3), #breakingFrom(6000.5)
        static Regex VersionTagRegex => s_VersionTagRegex ??=
            new Regex(@"#[a-zA-Z]+\(\d+\.\d+\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static string StripVersionTags(string message)
        {
            var match = VersionTagRegex.Match(message);
            return !match.Success ? message.Trim() : message.Substring(0, match.Index).Trim();
        }

        internal readonly struct ObsoleteMessageContainer
        {
            public readonly string message;
            public readonly HelpBoxMessageType messageType;
            public readonly string buttonText;
            public readonly Type replacementType;

            public ObsoleteMessageContainer(string message, HelpBoxMessageType messageType, Type replacementType,
                string displayName)
            {
                this.message = message;
                this.messageType = messageType;

                if (replacementType == null)
                    return;

                this.replacementType = replacementType;
                var text = string.IsNullOrEmpty(displayName)
                    ? ObjectNames.NicifyVariableName(replacementType.Name)
                    : displayName;
                buttonText = $"Add {text}";
            }
        }

        [AutoStaticsCleanupOnCodeReload] // lazy cache of obsolete type messages, must reset on reload
        private static Dictionary<Type, ObsoleteMessageContainer> s_ObsoleteTypeMessages;

        private static Type ResolveReplacementType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // First try to get type by assembly qualified name
            var type = Type.GetType(typeName);

            // If that fails, search using TypeCache for a type matching the full name
            if (type == null)
            {
                var types = TypeCache.GetTypesDerivedFrom<Component>();
                foreach (var t in types)
                {
                    if (t.FullName == typeName)
                    {
                        type = t;
                        break;
                    }
                }
            }

            return type;
        }

        public static bool TryGetObsoleteMessage(Editor editor, out ObsoleteMessageContainer obsoleteMessageContainer)
        {
            if (!editor || !editor.target)
            {
                obsoleteMessageContainer = default;
                return false;
            }

            if (s_ObsoleteTypeMessages != null)
                return s_ObsoleteTypeMessages.TryGetValue(editor.target.GetType(), out obsoleteMessageContainer);

            var obsoleteTypes = TypeCache.GetTypesWithAttribute<ObsoleteAttribute>();
            s_ObsoleteTypeMessages = new Dictionary<Type, ObsoleteMessageContainer>(obsoleteTypes.Count);
            foreach (var type in obsoleteTypes)
            {
                var attr = type.GetCustomAttribute<ObsoleteAttribute>();
                var message = string.IsNullOrEmpty(attr.Message)
                    ? "This component has been marked as obsolete."
                    : attr.Message;
                message = StripVersionTags(message);
                var messageType = attr.IsError ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning;

                Type replacementType = null;
                string displayName = null;
                var replacementAttr = type.GetCustomAttribute<ReplacementComponentAttribute>();
                if (replacementAttr != null)
                {
                    replacementType = ResolveReplacementType(replacementAttr.TypeName);
                    displayName = replacementAttr.DisplayName;
                    message = replacementType != null && string.IsNullOrEmpty(replacementAttr.AlternativeMessage)
                        ? message : StripVersionTags(replacementAttr.AlternativeMessage);
                }

                s_ObsoleteTypeMessages[type] = new ObsoleteMessageContainer(message, messageType, replacementType, displayName);
            }

            return s_ObsoleteTypeMessages.TryGetValue(editor.target.GetType(), out obsoleteMessageContainer);
        }
    }
}
