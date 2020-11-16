using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleRuleExtensions
    {
        public static StyleProperty FindProperty(this StyleSheet styleSheet, StyleRule rule, string propertyName)
        {
            foreach (var property in rule.properties)
            {
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

            StyleSheetCache.ClearCaches();

            return newProperty;
        }

        public static void RemoveProperty(
            this StyleSheet styleSheet, StyleRule rule, StyleProperty property,
            string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Change UI Style Value";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            var properties = rule.properties.ToList();
            properties.Remove(property);
            rule.properties = properties.ToArray();
        }

        public static void RemoveProperty(this StyleSheet styleSheet, StyleRule rule,
            string name, string undoMessage = null)
        {
            var property = styleSheet.FindProperty(rule, name);
            if (property == null)
                return;

            styleSheet.RemoveProperty(rule, property, undoMessage);
        }
    }
}
