// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Collections;
using System.Collections.Generic;

// NOTE:
// This file should only contain internal functions of the EditorGUI class
//

namespace UnityEditor
{
    internal class TargetChoiceHandler
    {
        internal delegate void TargetChoiceMenuFunction(SerializedProperty property, Object target);

        internal static void DuplicateArrayElement(object userData)
        {
            SerializedProperty property = (SerializedProperty)userData;
            property.DuplicateCommand();
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.ForceReloadInspectors();
        }

        internal static void DeleteArrayElement(object userData)
        {
            SerializedProperty property = (SerializedProperty)userData;
            property.DeleteCommand();
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.ForceReloadInspectors();
        }

        internal static void SetPrefabOverride(object userData)
        {
            SerializedProperty property = (SerializedProperty)userData;
            property.prefabOverride = false;
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.ForceReloadInspectors();
        }

        internal static void SetToValueOfTarget(SerializedProperty property, Object target)
        {
            property.SetToValueOfTarget(target);
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.ForceReloadInspectors();
        }

        private static void TargetChoiceForwardFunction(object userData)
        {
            var handler = (PropertyAndTargetHandler)userData;
            handler.function(handler.property, handler.target);
        }

        static internal void AddSetToValueOfTargetMenuItems(GenericMenu menu, SerializedProperty property, TargetChoiceMenuFunction func)
        {
            SerializedProperty propertyWithPath = property.serializedObject.FindProperty(property.propertyPath);
            Object[] targets = property.serializedObject.targetObjects;
            List<string> options = new List<string>();
            foreach (Object target in targets)
            {
                string option = string.Format("Set to Value of {0}", target.name);
                if (options.Contains(option))
                {
                    for (int i = 1;; i++)
                    {
                        option = string.Format("Set to Value of {0}({1})", target.name, i);
                        if (!options.Contains(option))
                            break;
                    }
                }
                options.Add(option);
                menu.AddItem(EditorGUIUtility.TextContent(option), false, TargetChoiceForwardFunction, new PropertyAndTargetHandler(propertyWithPath, target, func));
            }
        }
    }

    public sealed partial class EditorGUI
    {
        /// Popup to choose which target to pick the value from. Use only when hasMultipleDifferentValues is true.
        internal static void TargetChoiceField(Rect position, SerializedProperty property, GUIContent label)
        {
            TargetChoiceField(position, property, label, TargetChoiceHandler.SetToValueOfTarget);
        }

        internal static void TargetChoiceField(Rect position, SerializedProperty property, GUIContent label, TargetChoiceHandler.TargetChoiceMenuFunction func)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, 0, label);
            EditorGUI.BeginHandleMixedValueContentColor();
            if (GUI.Button(position, EditorGUI.mixedValueContent, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                TargetChoiceHandler.AddSetToValueOfTargetMenuItems(menu, property, func);
                menu.DropDown(position);
            }
            EditorGUI.EndHandleMixedValueContentColor();
            EditorGUI.EndProperty();
        }
    }

    public sealed partial class EditorGUILayout
    {
        /// Popup to choose which target to pick the value from. Use only when hasMultipleDifferentValues is true.
        internal static void TargetChoiceField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            TargetChoiceField(property, label, TargetChoiceHandler.SetToValueOfTarget, options);
        }

        internal static void TargetChoiceField(SerializedProperty property, GUIContent label, TargetChoiceHandler.TargetChoiceMenuFunction func, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            EditorGUI.TargetChoiceField(rect, property, label, func);
        }
    }

    internal class PropertyAndTargetHandler
    {
        public SerializedProperty property;
        public Object target;
        public TargetChoiceHandler.TargetChoiceMenuFunction function;
        public PropertyAndTargetHandler(SerializedProperty property, Object target, TargetChoiceHandler.TargetChoiceMenuFunction function)
        {
            this.property = property;
            this.target = target;
            this.function = function;
        }
    }
}
