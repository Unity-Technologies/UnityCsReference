// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class PrefabUtility
    {
        private const string kMaterialExtension = ".mat";

        [RequiredByNativeCode]
        internal static void ExtractSelectedObjectsFromPrefab()
        {
            var assetsToReload = new HashSet<string>();
            string folder = null;
            foreach (var selectedObj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(selectedObj);

                // use the first selected element as the basis for the folder path where all the materials will be extracted
                if (folder == null)
                {
                    folder = EditorUtility.SaveFolderPanel("Select Materials Folder", FileUtil.DeleteLastPathNameComponent(path), "");
                    if (string.IsNullOrEmpty(folder))
                    {
                        // cancel the extraction if the user did not select a folder
                        return;
                    }

                    folder = FileUtil.GetProjectRelativePath(folder);
                }

                // TODO: [bogdanc 3/6/2017] if we want this function really generic, we need to know what extension the new asset files should have
                var extension = selectedObj is Material ? kMaterialExtension : string.Empty;
                var newAssetPath = FileUtil.CombinePaths(folder, selectedObj.name) + extension;
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                var error = AssetDatabase.ExtractAsset(selectedObj, newAssetPath);
                if (string.IsNullOrEmpty(error))
                {
                    assetsToReload.Add(path);
                }
            }

            foreach (var path in assetsToReload)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        internal static void ExtractMaterialsFromAsset(Object[] targets, string destinationPath)
        {
            var assetsToReload = new HashSet<string>();
            foreach (var t in targets)
            {
                var importer = t as ModelImporter;

                var materials = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).Where(x => x.GetType() == typeof(Material));

                foreach (var material in materials)
                {
                    var newAssetPath = FileUtil.CombinePaths(destinationPath, material.name) + kMaterialExtension;
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                    var error = AssetDatabase.ExtractAsset(material, newAssetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        assetsToReload.Add(importer.assetPath);
                    }
                }
            }

            foreach (var path in assetsToReload)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void GetObjectListFromHierarchy(List<Object> hierarchy, GameObject gameObject)
        {
            Transform transform = null;
            List<Component> components = new List<Component>();
            gameObject.GetComponents(components);
            foreach (var component in components)
            {
                if (component is Transform)
                    transform = component as Transform;

                hierarchy.Add(component);
            }
            if (transform == null)
                return;

            int childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
                GetObjectListFromHierarchy(hierarchy, transform.GetChild(i).gameObject);
        }

        private static void RegisterNewObjects(List<Object> newHierarchy, List<Object> hierarchy, string actionName)
        {
            var danglingObjects = new List<Object>();

            foreach (var i in newHierarchy)
            {
                if (i != null)
                {
                    var found = false;
                    foreach (var j in hierarchy)
                    {
                        if (j != null && j.GetInstanceID() == i.GetInstanceID())
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        danglingObjects.Add(i);
                    }
                }
            }

            // We need to ensure that dangling components are registered in an acceptable order regarding dependencies. For example, if we're adding RigidBody and ConfigurableJoint, the RigidBody will need to be added first (as the ConfigurableJoint depends upon it existing)
            var addedTypes = new HashSet<Type>()
            {
                typeof(Transform)
            };
            var emptyPass = false;
            while (danglingObjects.Count > 0 && !emptyPass)
            {
                emptyPass = true;
                for (var i = 0; i < danglingObjects.Count; i++)
                {
                    var danglingObject = danglingObjects[i];
                    var reqs = danglingObject.GetType().GetCustomAttributes(typeof(RequireComponent), inherit: true);
                    var requiredComponentsExist = true;
                    foreach (RequireComponent req in reqs)
                    {
                        if ((req.m_Type0 != null && !addedTypes.Contains(req.m_Type0)) || (req.m_Type1 != null && !addedTypes.Contains(req.m_Type1)) || (req.m_Type2 != null && !addedTypes.Contains(req.m_Type2)))
                        {
                            requiredComponentsExist = false;
                            break;
                        }
                    }
                    if (requiredComponentsExist)
                    {
                        if (danglingObject is Transform)
                            danglingObject = ((Transform)danglingObject).gameObject;

                        Undo.RegisterCreatedObjectUndo(danglingObject, actionName);
                        addedTypes.Add(danglingObject.GetType());
                        danglingObjects.RemoveAt(i);
                        i--;
                        emptyPass = false;
                    }
                }
            }

            Debug.Assert(danglingObjects.Count == 0, "Dangling components have unfulfilled dependencies");
            foreach (var component in danglingObjects)
            {
                Undo.RegisterCreatedObjectUndo(component, actionName);
            }
        }

        internal static void RevertPrefabInstanceWithUndo(GameObject target)
        {
            var actionName = "Revert Prefab Instance";

            PrefabType prefabType = GetPrefabType(target);
            bool isDisconnected = (prefabType == PrefabType.DisconnectedModelPrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance);

            GameObject root = null;
            if (isDisconnected)
                root = FindRootGameObjectWithSameParentPrefab(target);
            else
                root = FindValidUploadPrefabInstanceRoot(target);

            List<Object> hierarchy = new List<Object>();
            GetObjectListFromHierarchy(hierarchy, root);

            Undo.RegisterFullObjectHierarchyUndo(root, actionName);

            if (isDisconnected)
            {
                ReconnectToLastPrefab(root);
                Undo.RegisterCreatedObjectUndo(GetPrefabObject(root), actionName);
            }

            RevertPrefabInstance(root);
            List<Object> newHierarchy = new List<Object>();
            GetObjectListFromHierarchy(newHierarchy, FindPrefabRoot(root));

            RegisterNewObjects(newHierarchy, hierarchy, actionName);
        }

        internal static void ReplacePrefabWithUndo(GameObject target)
        {
            var actionName = "Apply instance to prefab";
            Object prefabParent = GetPrefabParent(target);
            GameObject rootUploadGameObject = FindValidUploadPrefabInstanceRoot(target);

            Undo.RegisterFullObjectHierarchyUndo(prefabParent, actionName);
            Undo.RegisterFullObjectHierarchyUndo(rootUploadGameObject, actionName);
            Undo.RegisterCreatedObjectUndo(rootUploadGameObject, actionName);

            List<Object> prefabHierarchy = new List<Object>();
            GetObjectListFromHierarchy(prefabHierarchy, prefabParent as GameObject);

            ReplacePrefab(rootUploadGameObject, prefabParent, ReplacePrefabOptions.ConnectToPrefab);

            List<Object> newPrefabHierarchy = new List<Object>();
            GetObjectListFromHierarchy(newPrefabHierarchy, prefabParent as GameObject);

            RegisterNewObjects(newPrefabHierarchy, prefabHierarchy, actionName);
        }
    }
}
