// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AssetPopupBackend
    {
        public static void AssetPopup<T>(SerializedProperty serializedProperty, GUIContent label, string fileExtension, string defaultFieldName) where T : Object, new()
        {
            bool orignalShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = serializedProperty.hasMultipleDifferentValues;

            var typeName = serializedProperty.objectReferenceTypeString;
            GUIContent buttonContent;
            if (serializedProperty.hasMultipleDifferentValues)
                buttonContent = EditorGUI.mixedValueContent;
            else if (serializedProperty.objectReferenceValue != null)
                buttonContent = GUIContent.Temp(serializedProperty.objectReferenceStringValue);
            else
                buttonContent = GUIContent.Temp(defaultFieldName);

            Rect buttonRect;
            if (AudioMixerEffectGUI.PopupButton(label, buttonContent, EditorStyles.popup, out buttonRect)) // move & rename PopupButton..
                ShowAssetsPopupMenu<T>(buttonRect, typeName, serializedProperty, fileExtension, defaultFieldName);

            EditorGUI.showMixedValue = orignalShowMixedValue;
        }

        public static void AssetPopup<T>(SerializedProperty serializedProperty, GUIContent label, string fileExtension) where T : Object, new()
        {
            AssetPopup<T>(serializedProperty, label, fileExtension, "Default");
        }

        private class DoCreateNewAsset : ProjectWindowCallback.EndNameEditAction
        {
            private SerializedProperty m_Property;

            public void SetProperty(SerializedProperty property)
            {
                using (var so = new SerializedObject(property.serializedObject.targetObject))
                {
                    m_Property = so.FindProperty(property.propertyPath);
                }
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var obj = EditorUtility.InstanceIDToObject(instanceId);
                AssetDatabase.CreateAsset(obj, AssetDatabase.GenerateUniqueAssetPath(pathName));
                ProjectWindowUtil.FrameObjectInProjectWindow(instanceId);
                m_Property.objectReferenceValue = obj;
                m_Property.serializedObject.ApplyModifiedProperties();
                m_Property.serializedObject.Dispose();
                m_Property.Dispose();
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile)
            {
                Selection.activeObject = m_Property.serializedObject.targetObject;
                m_Property.serializedObject.Dispose();
                m_Property.Dispose();
            }
        }

        internal static void ShowAssetsPopupMenu<T>(Rect buttonRect, string typeName, SerializedProperty serializedProperty, string fileExtension, string defaultFieldName) where T : Object, new()
        {
            GenericMenu gm = new GenericMenu();

            int selectedInstanceID = serializedProperty.objectReferenceValue != null ? serializedProperty.objectReferenceValue.GetInstanceID() : 0;


            bool foundDefaultAsset = false;
            var type = UnityEditor.UnityType.FindTypeByName(typeName);
            int classID = type != null ? type.persistentTypeID : 0;
            BuiltinResource[] resourceList = null;

            // Check the assets for one that matches the default name.
            if (classID > 0)
            {
                resourceList = EditorGUIUtility.GetBuiltinResourceList(classID);
                foreach (var resource in resourceList)
                {
                    if (resource.m_Name == defaultFieldName)
                    {
                        gm.AddItem(new GUIContent(resource.m_Name), resource.m_InstanceID == selectedInstanceID, AssetPopupMenuCallback, new object[] { resource.m_InstanceID, serializedProperty });
                        resourceList = resourceList.Where(x => x != resource).ToArray();
                        foundDefaultAsset = true;
                        break;
                    }
                }
            }

            // If no defalut asset was found, add defualt null value.
            if (!foundDefaultAsset)
            {
                gm.AddItem(new GUIContent(defaultFieldName), selectedInstanceID == 0, AssetPopupMenuCallback, new object[] { 0, serializedProperty });
            }

            // Add items from asset database
            foreach (var property in AssetDatabase.FindAllAssets(new SearchFilter() { classNames = new[] { typeName } }))
            {
                gm.AddItem(new GUIContent(property.name), property.instanceID == selectedInstanceID, AssetPopupMenuCallback, new object[] { property.instanceID, serializedProperty });
            }

            // Add builtin items, except for the already added default item.
            if (classID > 0 && resourceList != null)
            {
                foreach (var resource in resourceList)
                {
                    gm.AddItem(new GUIContent(resource.m_Name), resource.m_InstanceID == selectedInstanceID, AssetPopupMenuCallback, new object[] { resource.m_InstanceID, serializedProperty });
                }
            }

            var target = serializedProperty.serializedObject.targetObject;
            bool isPreset = target is Component ? ((int)(target as Component).gameObject.hideFlags == 93) : !AssetDatabase.Contains(target);

            // the preset object is destroyed with the inspector, and nothing new can be created that needs this link. Fix for case 1208437
            if (!isPreset)
            {
                // Create item
                gm.AddSeparator("");
                gm.AddItem(EditorGUIUtility.TrTextContent("Create New..."), false, delegate
                {
                    var newAsset = Activator.CreateInstance<T>();
                    var doCreate = ScriptableObject.CreateInstance<DoCreateNewAsset>();
                    doCreate.SetProperty(serializedProperty);
                    ProjectWindowUtil.StartNameEditingIfProjectWindowExists(newAsset.GetInstanceID(), doCreate, "New " + typeName + "." + fileExtension, AssetPreview.GetMiniThumbnail(newAsset), null);
                });
            }

            gm.DropDown(buttonRect);
        }

        static void ShowAssetsPopupMenu<T>(Rect buttonRect, string typeName, SerializedProperty serializedProperty, string fileExtension) where T : Object, new()
        {
            ShowAssetsPopupMenu<T>(buttonRect, typeName, serializedProperty, fileExtension, "Default");
        }

        static void AssetPopupMenuCallback(object userData)
        {
            var data = userData as object[];
            var instanceID = (int)data[0];
            var serializedProperty = (SerializedProperty)data[1];

            serializedProperty.objectReferenceValue = EditorUtility.InstanceIDToObject(instanceID);
            serializedProperty.m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
