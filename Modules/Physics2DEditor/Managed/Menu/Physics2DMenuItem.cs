// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace UnityEditor.U2D.Physics2D
{
    [InitializeOnLoad]
    static internal class MenuItems
    {
        internal const string k_CreatePhysicsMaterial2DMenuPath = "Assets/Create/2D/Physics Material 2D";
        internal const string k_CreatePhysicsLowLevelSettings2DMenuPath = "Assets/Create/2D/Physics LowLevel Settings";
        static internal Action<EntityId, ProjectWindowCallback.AssetCreationEndAction, string, Texture2D, string> StartNewAssetNameEditingDelegate = ProjectWindowUtil.StartNameEditingIfProjectWindowExists;
        const int k_PhysicsMaterial2DAssetMenuPriority = 13;
        const int k_PhysicsLowLevelSettings2DAssetMenuPriority = k_PhysicsMaterial2DAssetMenuPriority + 1;

        static MenuItems()
        {
            EditorApplication.CallDelayed(UpdateMenuItem);
        }

        static void UpdateMenuItem()
        {
            if (ModuleMetadata.GetModuleIncludeSettingForModule("Physics2D") == ModuleIncludeSetting.ForceExclude)
            {
                Menu.RemoveMenuItem(k_CreatePhysicsMaterial2DMenuPath);
                Menu.RemoveMenuItem(k_CreatePhysicsLowLevelSettings2DMenuPath);
            }
        }

        [MenuItem(k_CreatePhysicsMaterial2DMenuPath, priority = k_PhysicsMaterial2DAssetMenuPriority)]
        static void AssetsCreatePhysicsMaterial2D(MenuCommand menuCommand) => CreateAsset<PhysicsMaterial2D>("New Physics Material 2D.physicsMaterial2D", CreateUnityObject<PhysicsMaterial2D>);

        [MenuItem(k_CreatePhysicsLowLevelSettings2DMenuPath, priority = k_PhysicsLowLevelSettings2DAssetMenuPriority)]
        static void AssetsCreatePhysicsLowLevelSettings2D(MenuCommand menuCommand) => CreateAsset<PhysicsLowLevelSettings2D>("New Physics LowLevel Settings 2D.asset", CreateScriptablObject<PhysicsLowLevelSettings2D>);

        private delegate UnityEngine.Object CreateObject<T>() where T : UnityEngine.Object;
        static private UnityEngine.Object CreateUnityObject<T>() where T: UnityEngine.Object => Activator.CreateInstance<T>();
        static private UnityEngine.Object CreateScriptablObject<T>() where T: ScriptableObject => ScriptableObject.CreateInstance<T>();

        static private T CreateAsset<T>(string name, CreateObject<T> createObject) where T : UnityEngine.Object
        {
            var assetSelectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var isFolder = false;
            if (!string.IsNullOrEmpty(assetSelectionPath))
                isFolder = File.GetAttributes(assetSelectionPath).HasFlag(FileAttributes.Directory);
            var path = ProjectWindowUtil.GetActiveFolderPath();
            if (isFolder)
            {
                path = assetSelectionPath;
            }
            var destName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name));
            var newObject = createObject();
            var icon = EditorGUIUtility.IconContent<T>().image as Texture2D;
            StartNewAssetNameEditing(null, destName, icon, newObject.GetEntityId());
            return Selection.activeObject as T;
        }

        static private void StartNewAssetNameEditing(string source, string dest, Texture2D icon, EntityId entityId)
        {
            var action = ScriptableObject.CreateInstance<CreateAssetEndNameEditAction>();
            StartNewAssetNameEditingDelegate(entityId, action, dest, icon, source);
        }

        private class CreateAssetEndNameEditAction : ProjectWindowCallback.AssetCreationEndAction
        {
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                var uniqueName = AssetDatabase.GenerateUniqueAssetPath(pathName);
                if (entityId == ProjectBrowser.kAssetCreationInstanceID_ForNonExistingAssets && !string.IsNullOrEmpty(resourceFile))
                {
                    AssetDatabase.CopyAsset(resourceFile, uniqueName);
                    entityId = AssetDatabase.LoadMainAssetAtPath(uniqueName).GetEntityId();
                }
                else
                {
                    var obj = EditorUtility.EntityIdToObject(entityId);
                    AssetDatabase.CreateAsset(obj, uniqueName);
                }
                ProjectWindowUtil.FrameObjectInProjectWindow(entityId);
            }
        }
    }
}
