// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        // private

        static void ShowAssetsPopupMenu<T>(Rect buttonRect, string typeName, SerializedProperty serializedProperty, string fileExtension, string defaultFieldName) where T : Object, new()
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
            var property = new HierarchyProperty(HierarchyType.Assets);
            var searchFilter = new SearchFilter() { classNames = new[] { typeName } };
            property.SetSearchFilter(searchFilter);
            property.Reset();
            while (property.Next(null))
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

            // Create item
            gm.AddSeparator("");
            gm.AddItem(new GUIContent("Create New..."), false, delegate
                {
                    var newAsset = Activator.CreateInstance<T>();
                    ProjectWindowUtil.CreateAsset(newAsset, "New " + typeName + "." + fileExtension);
                    serializedProperty.objectReferenceValue = newAsset;
                    serializedProperty.m_SerializedObject.ApplyModifiedProperties();
                });

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
