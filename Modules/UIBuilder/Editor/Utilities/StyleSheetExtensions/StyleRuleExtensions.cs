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
        public static StyleProperty FindLastProperty(this StyleSheet styleSheet, StyleRule rule, string propertyName)
        {
            if (rule == null)
                return null;

            for (var i = rule.properties.Length - 1; i >= 0; --i)
            {
                var property = rule.properties[i];
                if (property.name == propertyName)
                    return property;
            }

            return null;
        }

        public static StyleProperty AddProperty(
            this StyleSheet styleSheet, StyleRule rule, string name,
            string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Change UI Style Value";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            var newProperty = new StyleProperty
            {
                name = name
            };

            // Create empty values array.
            newProperty.values = new StyleValueHandle[0];

            // Add property to selector's rule's properties.
            var properties = rule.properties.ToList();
            properties.Add(newProperty);
            rule.properties = properties.ToArray();

            styleSheet.SetTemporaryContentHash();
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

            var properties = rule.properties.ToList();
            properties.Remove(property);
            rule.properties = properties.ToArray();
            styleSheet.SetTemporaryContentHash();
        }

        public static void RemoveProperty(this StyleSheet styleSheet, StyleRule rule,
            string name, string undoMessage = null)
        {
            var property = styleSheet.FindLastProperty(rule, name);
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
