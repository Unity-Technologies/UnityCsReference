// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    [InitializeOnLoad]
    static internal class PhysicsCore2DMenuItems
    {
        internal const string k_CreatePhysicsCoreSettings2DMenuPath = "Assets/Create/2D/Physics (Core)/PhysicsCore Settings 2D";
        static internal Action<EntityId, UnityEditor.ProjectWindowCallback.AssetCreationEndAction, string, Texture2D, string> StartNewAssetNameEditingDelegate = ProjectWindowUtil.StartNameEditingIfProjectWindowExists;
        const int k_PhysicsCoreSettings2DAssetMenuPriority = 70;

        static PhysicsCore2DMenuItems()
        {
            EditorApplication.CallDelayed(UpdateMenuItem);
        }

        static void UpdateMenuItem()
        {
            if (ModuleMetadata.GetModuleIncludeSettingForModule("PhysicsCore2D") == ModuleIncludeSetting.ForceExclude)
            {
                Menu.RemoveMenuItem(k_CreatePhysicsCoreSettings2DMenuPath);
            }
        }

        [MenuItem(k_CreatePhysicsCoreSettings2DMenuPath, priority = k_PhysicsCoreSettings2DAssetMenuPriority)]
        static void AssetsCreatePhysicsCoreSettings2D(MenuCommand menuCommand) => CreateAsset<PhysicsCoreSettings2D>("New PhysicsCore Settings 2D.asset", CreateScriptableObject<PhysicsCoreSettings2D>);

        private delegate UnityEngine.Object CreateObject<T>() where T : UnityEngine.Object;
        static private UnityEngine.Object CreateScriptableObject<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

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

        private class CreateAssetEndNameEditAction : UnityEditor.ProjectWindowCallback.AssetCreationEndAction
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
