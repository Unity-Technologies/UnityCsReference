// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;

namespace UnityEditor.U2D.Physics2D
{
    [InitializeOnLoad]
    static internal class MenuItems
    {
        internal const string k_CreatePhysicsMaterial2DMenuPath = "Assets/Create/2D/Physics Material 2D";
        internal delegate void StartNewAssetNameEditingDelegateType(int instanceID, ProjectWindowCallback.EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile);
        static internal StartNewAssetNameEditingDelegateType StartNewAssetNameEditingDelegate = ProjectWindowUtil.StartNameEditingIfProjectWindowExists;
        const int k_PhysicsMaterial2DAssetMenuPriority = 12;


        static MenuItems()
        {
            EditorApplication.CallDelayed(UpdateMenuItem);
        }

        static void UpdateMenuItem()
        {
            if (ModuleMetadata.GetModuleIncludeSettingForModule("Physics2D") == ModuleIncludeSetting.ForceExclude)
                Menu.RemoveMenuItem(k_CreatePhysicsMaterial2DMenuPath);
        }

        [MenuItem(k_CreatePhysicsMaterial2DMenuPath, priority = k_PhysicsMaterial2DAssetMenuPriority)]
        static void AssetsCreatePhysicsMaterial2D(MenuCommand menuCommand)
        {
            CreateAssetObject<PhysicsMaterial2D>("New Physics Material 2D.physicsMaterial2D");
        }

        static public T CreateAssetObject<T>(string name) where T : UnityEngine.Object
        {
            var assetSelectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var isFolder = false;
            if (!string.IsNullOrEmpty(assetSelectionPath))
                isFolder = (File.GetAttributes(assetSelectionPath) & FileAttributes.Directory) == FileAttributes.Directory;
            var path = ProjectWindowUtil.GetActiveFolderPath();
            if (isFolder)
            {
                path = assetSelectionPath;
            }
            var destName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name));
            var newObject = Activator.CreateInstance<T>();
            var icon = EditorGUIUtility.IconContent<T>().image as Texture2D;
            StartNewAssetNameEditing(null, destName, icon, newObject.GetInstanceID());
            return Selection.activeObject as T;
        }

        static private void StartNewAssetNameEditing(string source, string dest, Texture2D icon, int instanceId)
        {
            var action = ScriptableObject.CreateInstance<CreateAssetEndNameEditAction>();
            StartNewAssetNameEditingDelegate(instanceId, action, dest, icon, source);
        }

        internal class CreateAssetEndNameEditAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var uniqueName = AssetDatabase.GenerateUniqueAssetPath(pathName);
                if (instanceId == ProjectBrowser.kAssetCreationInstanceID_ForNonExistingAssets && !string.IsNullOrEmpty(resourceFile))
                {
                    AssetDatabase.CopyAsset(resourceFile, uniqueName);
                    instanceId = AssetDatabase.LoadMainAssetAtPath(uniqueName).GetInstanceID();
                }
                else
                {
                    var obj = EditorUtility.InstanceIDToObject(instanceId);
                    AssetDatabase.CreateAsset(obj, uniqueName);
                }
                ProjectWindowUtil.FrameObjectInProjectWindow(instanceId);
            }
        }
    }
}
