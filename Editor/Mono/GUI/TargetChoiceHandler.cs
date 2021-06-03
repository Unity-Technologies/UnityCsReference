// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Collections;
using System.Collections.Generic;
using System;

// NOTE:
// This file should only contain internal functions of the EditorGUI class
//

namespace UnityEditor
{
    internal class TargetChoiceHandler
    {
        internal struct PropertyAndSourcePathInfo
        {
            public SerializedProperty[] properties;
            public string assetPath;
        }

        internal struct ObjectInstanceAndSourcePathInfo
        {
            public Object instanceObject;
            public string assetPath;
        }

        internal struct ObjectInstanceAndSourceInfo
        {
            public Object instanceObject;
            public Object correspondingObjectInSource;
        }

        internal delegate void TargetChoiceMenuFunction(SerializedProperty property, Object target);

        internal static bool DuplicateArrayElement(object userData)
        {
            SerializedProperty property = (SerializedProperty)userData;
            bool result = property.DuplicateCommand();
            EditorUtility.ForceReloadInspectors();
            return result;
        }

        internal static bool DeleteArrayElement(object userData)
        {
            SerializedProperty property = (SerializedProperty)userData;
            bool result = property.DeleteCommand();
            EditorUtility.ForceReloadInspectors();
            return result;
        }

        internal static void ApplyPrefabPropertyOverride(object userData)
        {
            PropertyAndSourcePathInfo info = (PropertyAndSourcePathInfo)userData;
            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(info.assetPath, PrefabUtility.SaveVerb.Apply))
                return;
            for (int i = 0; i < info.properties.Length; i++)
                PrefabUtility.ApplyPropertyOverride(info.properties[i], info.assetPath, InteractionMode.UserAction);
            EditorUtility.ForceReloadInspectors();
        }

        internal static void RevertPrefabPropertyOverride(object userData)
        {
            SerializedProperty[] properties = (SerializedProperty[])userData;
            PrefabUtility.RevertPropertyOverrides(properties, InteractionMode.UserAction);
            EditorUtility.ForceReloadInspectors();
        }

        internal static void ApplyPrefabObjectOverride(object userData)
        {
            ObjectInstanceAndSourcePathInfo info = (ObjectInstanceAndSourcePathInfo)userData;
            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(info.assetPath, PrefabUtility.SaveVerb.Apply))
                return;
            PrefabUtility.ApplyObjectOverride(info.instanceObject, info.assetPath, InteractionMode.UserAction);
            EditorUtility.ForceReloadInspectors();
        }

        internal static void RevertPrefabObjectOverride(object userData)
        {
            Object obj = (Object)userData;
            PrefabUtility.RevertObjectOverride(obj, InteractionMode.UserAction);
            EditorUtility.ForceReloadInspectors();
        }

        internal static void ApplyPrefabAddedComponent(object userData)
        {
            ObjectInstanceAndSourcePathInfo info = (ObjectInstanceAndSourcePathInfo)userData;
            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(info.assetPath, PrefabUtility.SaveVerb.Apply))
                return;
            PrefabUtility.ApplyAddedComponent((Component)info.instanceObject, info.assetPath, InteractionMode.UserAction);
            EditorUtility.ForceRebuildInspectors();
        }

        internal static void RevertPrefabAddedComponent(object userData)
        {
            Component obj = (Component)userData;
            PrefabUtility.RevertAddedComponent(obj, InteractionMode.UserAction);
            EditorUtility.ForceRebuildInspectors();
        }

        internal static void ApplyPrefabRemovedComponent(object userData)
        {
            ObjectInstanceAndSourceInfo info = (ObjectInstanceAndSourceInfo)userData;
            string path = AssetDatabase.GetAssetPath(info.correspondingObjectInSource);
            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(path, PrefabUtility.SaveVerb.Apply))
                return;
            PrefabUtility.ApplyRemovedComponent((GameObject)info.instanceObject, (Component)info.correspondingObjectInSource, InteractionMode.UserAction);
            EditorUtility.ForceRebuildInspectors();
        }

        internal static void RevertPrefabRemovedComponent(object userData)
        {
            ObjectInstanceAndSourceInfo info = (ObjectInstanceAndSourceInfo)userData;
            PrefabUtility.RevertRemovedComponent((GameObject)info.instanceObject, (Component)info.correspondingObjectInSource, InteractionMode.UserAction);
            EditorUtility.ForceRebuildInspectors();
        }

        internal static void ApplyPrefabAddedGameObjects(object userData)
        {
            ObjectInstanceAndSourcePathInfo[] infos = (ObjectInstanceAndSourcePathInfo[])userData;

            GameObject[] gameObjects = new GameObject[infos.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                ObjectInstanceAndSourcePathInfo info = infos[i];
                gameObjects[i] = info.instanceObject as GameObject;
            }

            if (!PrefabUtility.HasSameParent(gameObjects))
                throw new ArgumentException(nameof(gameObjects), "ApplyPrefabAddedGameObjects requires that GameObjects share the same parent.");

            if (!HasSameAssetPath(infos))
                throw new ArgumentException(nameof(infos), "ApplyPrefabAddedGameObjects requires that GameObjects share the same parent asset path.");

            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(infos[0].assetPath, PrefabUtility.SaveVerb.Apply))
                return;

            PrefabUtility.ApplyAddedGameObjects(gameObjects, infos[0].assetPath, InteractionMode.UserAction);
            EditorUtility.ForceRebuildInspectors();
        }

        internal static bool HasSameAssetPath(ObjectInstanceAndSourcePathInfo[] infos)
        {
            if (infos == null || infos.Length == 0)
                throw new ArgumentException(nameof(infos), "Array is invalid.");

            string assetPath = infos[0].assetPath;
            for (int i = 1; i < infos.Length; i++)
            {
                if (assetPath != infos[i].assetPath)
                    return false;
            }

            return true;
        }

        internal static void RevertPrefabAddedGameObjects(object userData)
        {
            GameObject[] gameObjects = (GameObject[])userData;

            foreach (GameObject go in gameObjects)
                PrefabUtility.RevertAddedGameObject(go, InteractionMode.UserAction);

            EditorUtility.ForceRebuildInspectors();
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
                string option = string.Format("Set to Value of '{0}'", target.name);
                if (options.Contains(option))
                {
                    for (int i = 1;; i++)
                    {
                        option = string.Format("Set to Value of '{0}'({1})", target.name, i);
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
