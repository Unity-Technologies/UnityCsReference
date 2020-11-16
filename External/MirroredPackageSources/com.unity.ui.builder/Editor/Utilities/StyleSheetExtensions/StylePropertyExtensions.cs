using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class StylePropertyExtensions
    {
        public static bool IsVariable(this StyleProperty property)
        {
            return property.values.Length > 0 && property.values[0].IsVarFunction();
        }

        public static string ReadVariable(this StyleSheet styleSheet, StyleProperty property)
        {
            return styleSheet.ReadVariable(property.values[2]);
        }

        public static StyleValueHandle AddValueHandle(
            this StyleSheet styleSheet, StyleProperty property, int index, StyleValueType type)
        {
            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, type);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

        internal static StyleValueHandle AddValue(
            this StyleSheet styleSheet, StyleProperty property, StyleValueKeyword value, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = BuilderConstants.ChangeUIStyleValueUndoMessage;
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Keyword);

            return newValue;
        }

        internal static StyleValueHandle AddValue(
            this StyleSheet styleSheet, StyleProperty property, float value, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = BuilderConstants.ChangeUIStyleValueUndoMessage;
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Float);

            return newValue;
        }

        internal static StyleValueHandle AddValue(
            this StyleSheet styleSheet, StyleProperty property, Dimension value, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = BuilderConstants.ChangeUIStyleValueUndoMessage;
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Dimension);

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Color value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Color);

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.String);

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Object value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Determine real asset type.
            var resourcePath = BuilderAssetUtilities.GetResourcesPathForAsset(value);
            var styleValueType = string.IsNullOrEmpty(resourcePath) ? StyleValueType.AssetReference : StyleValueType.ResourcePath;

            // Add value data to data array.
            var index = string.IsNullOrEmpty(resourcePath) ? styleSheet.AddValueToArray(value) : styleSheet.AddValueToArray(resourcePath);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, styleValueType);

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Enum value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Enum);

            return newValue;
        }

        internal static StyleValueHandle AddValueAsEnum(this StyleSheet styleSheet, StyleProperty property, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Enum);

            return newValue;
        }

        internal static StyleValueHandle[] AddVariable(this StyleSheet styleSheet, StyleProperty property, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Add the function name
            var funcHandle = styleSheet.AddValueHandle(property, (int)StyleValueFunction.Var, StyleValueType.Function);

            // Add the argument count
            var index = styleSheet.AddValueToArray(1);
            var argHandle = styleSheet.AddValueHandle(property, index, StyleValueType.Float);

            // Add the variable name
            index = styleSheet.AddValueToArray(value);
            var newValue = styleSheet.AddValueHandle(property, index, StyleValueType.Variable);

            return new StyleValueHandle[] { funcHandle, argHandle, newValue };
        }

        internal static void RemoveValue(this StyleSheet styleSheet, StyleProperty property, StyleValueHandle valueHandle)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // We just leave the values in their data array. If we really wanted to remove them
            // we would have to the indicies of all values.

            var valuesList = property.values.ToList();
            valuesList.Remove(valueHandle);
            property.values = valuesList.ToArray();
        }

        internal static void RemoveVariable(this StyleSheet styleSheet, StyleProperty property)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            Array.Clear(property.values, 0, property.values.Length);
        }
    }
}
