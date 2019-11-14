// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.SceneManagement
{
    internal class PrefabOverridesUtility
    {
        static List<Component> s_ComponentList = new List<Component>();
        static List<Component> s_AssetComponentList = new List<Component>();

        static void ThrowExceptionIfNullOrNotPartOfPrefabInstance(GameObject prefabInstance)
        {
            if (prefabInstance == null)
                throw new ArgumentNullException(nameof(prefabInstance));

            if (!PrefabUtility.IsPartOfPrefabInstance(prefabInstance))
                throw new ArgumentException("Provided GameObject is not a Prefab instance");
        }

        public static List<ObjectOverride> GetObjectOverrides(GameObject prefabInstance, bool includeDefaultOverrides = false)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root of instance traverse all child go and detect any GameObjects or components
            // that are not part of that source prefab objects component list (these must be added)
            TransformVisitor transformVisitor = new TransformVisitor();
            var modifiedObjects = new List<ObjectOverride>();
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstance))
                return modifiedObjects;

            Func<Transform, object, bool> checkMethod;
            if (includeDefaultOverrides)
                checkMethod = CheckForModifiedObjectsIncludingDefaultOverrides;
            else
                checkMethod = CheckForModifiedObjectsExcludingDefaultOverrides;
            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, checkMethod, modifiedObjects);
            return modifiedObjects;
        }

        static bool CheckForModifiedObjectsIncludingDefaultOverrides(Transform transform, object userData)
        {
            var modifiedObjects = (List<ObjectOverride>)userData;
            return CheckForModifiedObjects(transform, modifiedObjects, true);
        }

        static bool CheckForModifiedObjectsExcludingDefaultOverrides(Transform transform, object userData)
        {
            var modifiedObjects = (List<ObjectOverride>)userData;
            return CheckForModifiedObjects(transform, modifiedObjects, false);
        }

        static bool CheckForModifiedObjects(Transform transform, List<ObjectOverride> modifiedObjects, bool includeDefaultOverrides)
        {
            GameObject gameObject = transform.gameObject;
            if (PrefabUtility.HasObjectOverride(gameObject, includeDefaultOverrides))
                modifiedObjects.Add(new ObjectOverride() { instanceObject = gameObject });

            s_ComponentList.Clear();
            gameObject.GetComponents(s_ComponentList);
            foreach (var component in s_ComponentList)
            {
                // This is possible if there's a component with a missing script.
                if (component == null)
                    continue;

                if (PrefabUtility.HasObjectOverride(component, includeDefaultOverrides))
                    modifiedObjects.Add(new ObjectOverride() { instanceObject = component });
            }

            return true;
        }

        public static List<AddedComponent> GetAddedComponents(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root of instance traverse all child go and detect any components that are not part of that source prefab objects component list (these must be added)
            TransformVisitor transformVisitor = new TransformVisitor();
            var addedComponents = new List<AddedComponent>();
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstance))
                return addedComponents;

            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForAddedComponents, addedComponents);
            return addedComponents;
        }

        // Return value indicates if caller should traverse children of the the input transform or not
        static bool CheckForAddedComponents(Transform transform, object userData)
        {
            s_ComponentList.Clear();
            transform.gameObject.GetComponents(s_ComponentList);
            var assetGameObject = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
            if (assetGameObject == null)
                return false; // If this is an added normal GameObject then we do not record added components

            foreach (var component in s_ComponentList)
            {
                // This is possible if there's a component with a missing script.
                if (component == null)
                    continue;

                // Don't list DontSave objects as they won't get applied or reverted.
                if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0)
                    continue;

                bool isAddedObject = PrefabUtility.GetCorrespondingObjectFromSource(component) == null;
                if (isAddedObject)
                {
                    var addedComponents = (List<AddedComponent>)userData;
                    addedComponents.Add(new AddedComponent() { instanceComponent = component });
                }
            }

            return true;
        }

        public static List<RemovedComponent> GetRemovedComponents(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root of asset traverse all children and detect any Components that are not present on the instance object (these must be deleted)
            var removedComponents = new List<RemovedComponent>();
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstance))
                return removedComponents;
            TransformVisitor transformVisitor = new TransformVisitor();
            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForRemovedComponents, removedComponents);
            return removedComponents;
        }

        public static List<RemovedComponent> GetRemovedComponentsForSingleGameObject(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var removedComponents = new List<RemovedComponent>();
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstance))
                return removedComponents;
            CheckForRemovedComponents(prefabInstance.transform, removedComponents);
            return removedComponents;
        }

        // Return value indicates if caller should traverse children of the the input transform or not
        static bool CheckForRemovedComponents(Transform transform, object userData)
        {
            GameObject instanceGameObject = transform.gameObject;
            GameObject assetGameObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceGameObject);
            if (assetGameObject == null)
                return false; // skip added GameObjects (non of its components will be in the asset)

            // Compare asset with instance component lists
            s_ComponentList.Clear();
            instanceGameObject.GetComponents(s_ComponentList);

            s_AssetComponentList.Clear();
            assetGameObject.GetComponents(s_AssetComponentList);

            // Find asset objects that no instance objects are referencing
            foreach (var assetComponent in s_AssetComponentList)
            {
                bool found = false;
                foreach (var instanceComponent in s_ComponentList)
                {
                    // This is possible if there's a component with a missing script.
                    if (instanceComponent == null)
                        continue;

                    if (PrefabUtility.GetCorrespondingObjectFromSource(instanceComponent) == assetComponent)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    List<RemovedComponent> removedComponents = (List<RemovedComponent>)userData;
                    removedComponents.Add(new RemovedComponent() { assetComponent = assetComponent, containingInstanceGameObject = instanceGameObject });
                }
            }
            return true;
        }

        internal class AddedGameObjectUserData
        {
            public List<AddedGameObject> addedGameObjects;
            public GameObject contextGameObject;
        }

        public static List<AddedGameObject> GetAddedGameObjects(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root instance traverse all children and detect any GameObjects that are not a prefab gameobject (these must be added)
            TransformVisitor transformVisitor = new TransformVisitor();
            var addedGameObjects = new List<AddedGameObject>();
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstance))
                return addedGameObjects;

            transformVisitor.VisitAndConditionallyEnterChildren(
                prefabInstanceRoot.transform,
                CheckForAddedGameObjectAndIfSoAddItAndReturnFalse,
                new AddedGameObjectUserData() { addedGameObjects = addedGameObjects, contextGameObject = prefabInstanceRoot });
            return addedGameObjects;
        }

        static bool CheckForAddedGameObjectAndIfSoAddItAndReturnFalse(Transform transform, object userData)
        {
            // Don't list DontSave objects or their children as they won't get applied or reverted.
            if ((transform.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
                return false;

            var addedGameObjectUserData = (AddedGameObjectUserData)userData;
            if (IsAddedGameObject(addedGameObjectUserData.contextGameObject, transform.gameObject))
            {
                var addedGameObjects = addedGameObjectUserData.addedGameObjects;
                addedGameObjects.Add(new AddedGameObject() { instanceGameObject = transform.gameObject, siblingIndex = transform.GetSiblingIndex() });
                return false;
            }
            return true;
        }

        internal static bool IsAddedGameObject(GameObject contextGameObject, GameObject gameObject)
        {
            // The context GameObject itself can never have status as being added to itself.
            if (gameObject == contextGameObject)
                return false;

            if (!PrefabUtility.IsAddedGameObjectOverride(gameObject))
                return false;

            // We now know that the GameObject is added to *some* Prefab,
            // but is it added to the Prefab in question?
            // It is if its parent belong to the same Prefab as the context GameObject.
            return (
                PrefabUtility.GetCorrespondingObjectFromSource(gameObject.transform.parent).root ==
                PrefabUtility.GetCorrespondingObjectFromSource(contextGameObject.transform).root);
        }

        internal static void CheckForInvalidComponent(Transform transform, object userData)
        {
            GameObject instanceGameObject = transform.gameObject;
            var GOList = (List<GameObject>)userData;
            if (!EditorUtility.IsPersistent(instanceGameObject))
            {
                s_ComponentList.Clear();
                instanceGameObject.GetComponents(s_ComponentList);

                foreach (var component in s_ComponentList)
                {
                    if (component == null)
                    {
                        GOList.Add(instanceGameObject);
                        return;
                    }
                }
                var assetGameObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceGameObject);
                if (assetGameObject == null)
                    return;

                s_AssetComponentList.Clear();
                assetGameObject.GetComponents(s_AssetComponentList);

                foreach (var assetComponent in s_AssetComponentList)
                {
                    if (assetComponent == null)
                    {
                        GOList.Add(instanceGameObject);
                        return;
                    }
                }
            }
            else
            {
                s_AssetComponentList.Clear();
                instanceGameObject.GetComponents(s_AssetComponentList);

                foreach (var assetComponent in s_AssetComponentList)
                {
                    if (assetComponent == null)
                    {
                        GOList.Add(instanceGameObject);
                        return;
                    }
                }
            }
        }
    }
}
