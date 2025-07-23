// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleRuleExtensions
    {
        public static StyleProperty AddProperty(
            this StyleSheet styleSheet, StyleRule rule, string name,
            string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Change UI Style Value";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            var newProperty = rule.AddProperty(styleSheet, name);
            StyleSheetCache.ClearCaches();

            return newProperty;
        }

        public static void RemoveProperty(
            this StyleSheet styleSheet, StyleRule rule, StyleProperty property, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = BuilderConstants.ChangeUIStyleValueUndoMessage;
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            rule.RemoveProperty(styleSheet, property);
        }

        public static void RemoveProperty(this StyleSheet styleSheet, StyleRule rule,
            string name, string undoMessage = null)
        {
            var property = rule.FindLastProperty(name);
            if (property == null)
                return;

            styleSheet.RemoveProperty(rule, property, undoMessage);
        }

        public static IEnumerable<string> GetAllSetStyleProperties(this StyleRule styleRule)
        {
            foreach (var property in styleRule.properties)
            {
                if (StylePropertyUtil.propertyNameToStylePropertyId.ContainsKey(property.name))
                    yield return property.name;
            }
        }
    }
}
