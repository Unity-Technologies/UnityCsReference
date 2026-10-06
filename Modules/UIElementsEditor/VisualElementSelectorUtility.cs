// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class VisualElementSelectorUtility
    {
        static readonly Regex s_UssIdentifier = new(@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static string GenerateTargetedSelector(VisualElement element, Func<VisualElement, bool> isDocumentRoot)
        {
            if (element == null)
                return null;

            string selector;

            if (IsUssIdentifier(element.name))
            {
                selector = $"#{element.name}";
            }
            else if (GetLastClassFromClassList(element, out var className))
            {
                selector = $".{className}";
            }
            else
            {
                selector = element.typeName;
            }

            if (element.parent != null && !isDocumentRoot(element.parent))
                selector = GenerateTargetedSelector(element.parent, isDocumentRoot) + " > " + selector;

            return selector;
        }

        static bool GetLastClassFromClassList(VisualElement element, out string className)
        {
            className = null;
            foreach (var c in element.GetClasses())
            {
                if (IsUssIdentifier(c))
                    className = c;
            }
            return className != null;
        }

        // USS has no escaping, so a name or class the parser cannot read as one identifier is skipped rather
        // than written into a rule that would target something else or be rejected on submission.
        static bool IsUssIdentifier(string value)
            => !string.IsNullOrEmpty(value) && s_UssIdentifier.IsMatch(value);
    }
}
