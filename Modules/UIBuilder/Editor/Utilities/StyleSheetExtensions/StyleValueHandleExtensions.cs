// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class StyleValueHandleExtensions
    {
        public static Object GetAsset(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            switch (valueHandle.valueType)
            {
                case StyleValueType.ResourcePath:
                    var resourcePath = styleSheet.strings[valueHandle.valueIndex];
                    var asset = Resources.Load<Object>(resourcePath);
                    return asset;
                case StyleValueType.Keyword:
                    return null;
                default:
                    return styleSheet.ReadAssetReference(valueHandle);
            }
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, float value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleSheet.WriteFloat(ref valueHandle, value);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Dimension value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleSheet.WriteDimension(ref valueHandle, value);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Color value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleSheet.WriteColor(ref valueHandle, value);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, string value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleSheet.WriteString(ref valueHandle, value);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Enum value)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleSheet.WriteEnum(ref valueHandle, value);
        }

        public static void SetValue(this StyleSheet styleSheet, StyleValueHandle valueHandle, Object value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (valueHandle.valueType == StyleValueType.ResourcePath)
            {
                var resourcesPath = BuilderAssetUtilities.GetResourcesPathForAsset(value);
                styleSheet.WriteResourcePath(ref valueHandle, resourcesPath);
            }
            else
            {
                styleSheet.WriteAssetReference(ref valueHandle, value);
            }
        }
    }
}
