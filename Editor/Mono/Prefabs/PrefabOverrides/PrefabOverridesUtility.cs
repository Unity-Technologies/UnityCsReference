// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.SceneManagement
{
    internal class PrefabOverridesUtility
    {
        [NoAutoStaticsCleanup] // Reusable scratch buffer, always Clear()ed before and after use; holds no live references across reload.
        static List<Component> s_ComponentList = new List<Component>();
        [NoAutoStaticsCleanup] // Reusable scratch buffer, always Clear()ed before and after use; holds no live references across reload.
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

            // Clear the list to avoid holding static references to components
            s_ComponentList.Clear();

            return true;
        }

        class DataComponentChanges
        {
            public List<DataComponentOverride> overrides;
            public List<AddedDataComponent> added;
            public List<RemovedDataComponent> removed;
        }

        // Data components are not returned by GetComponents, so they are collected separately, in one walk.
        // A null list is not collected.
        internal static void GetDataComponentChanges(GameObject prefabInstance, List<DataComponentOverride> overrides, List<AddedDataComponent> added, List<RemovedDataComponent> removed)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            var changes = new DataComponentChanges() { overrides = overrides, added = added, removed = removed };
            new TransformVisitor().VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForDataComponentChanges, changes);
        }

        internal static List<DataComponentOverride> GetDataComponentOverrides(GameObject prefabInstance)
        {
            var overrides = new List<DataComponentOverride>();
            GetDataComponentChanges(prefabInstance, overrides, null, null);
            return overrides;
        }

        internal static List<AddedDataComponent> GetAddedDataComponents(GameObject prefabInstance)
        {
            var added = new List<AddedDataComponent>();
            GetDataComponentChanges(prefabInstance, null, added, null);
            return added;
        }

        internal static List<RemovedDataComponent> GetRemovedDataComponents(GameObject prefabInstance)
        {
            var removed = new List<RemovedDataComponent>();
            GetDataComponentChanges(prefabInstance, null, null, removed);
            return removed;
        }

        static bool CheckForDataComponentChanges(Transform transform, object userData)
        {
            var changes = (DataComponentChanges)userData;
            var gameObject = transform.gameObject;

            if (changes.overrides != null || changes.added != null)
            {
                foreach (var dataComponent in PrefabUtility.GetDataComponents(gameObject.GetEntityId()))
                {
                    if (changes.added != null && PrefabUtility.IsAddedDataComponentOverride(dataComponent))
                        changes.added.Add(new AddedDataComponent() { instanceDataComponent = dataComponent });
                    if (changes.overrides != null && PrefabUtility.HasDataComponentOverride(dataComponent))
                        changes.overrides.Add(new DataComponentOverride() { instanceDataComponent = dataComponent });
                }
            }

            if (changes.removed != null)
            {
                foreach (var assetDataComponent in PrefabUtility.GetRemovedDataComponents(gameObject))
                    changes.removed.Add(new RemovedDataComponent() { containingInstanceGameObject = gameObject, assetDataComponent = assetDataComponent });
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
            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForAddedComponents, addedComponents);
            return addedComponents;
        }

        // Return value indicates if caller should traverse children of the the input transform or not
        static bool CheckForAddedComponents(Transform transform, object userData)
        {
            try
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
                        addedComponents.Add(new AddedComponent() {instanceComponent = component});
                    }
                }

                return true;
            }
            finally
            {
                s_ComponentList.Clear();
            }
        }

        public static List<RemovedComponent> GetRemovedComponents(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root of asset traverse all children and detect any Components that are not present on the instance object (these must be deleted)
            var removedComponents = new List<RemovedComponent>();
            TransformVisitor transformVisitor = new TransformVisitor();
            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForRemovedComponents, removedComponents);
            return removedComponents;
        }

        public static List<RemovedComponent> GetRemovedComponentsForSingleGameObject(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var removedComponents = new List<RemovedComponent>();
            CheckForRemovedComponents(prefabInstance.transform, removedComponents);
            return removedComponents;
        }

        // Return value indicates if caller should traverse children of the the input transform or not
        static bool CheckForRemovedComponents(Transform transform, object userData)
        {
            try
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
                    // This is possible if there's a component with a missing script.
                    if (assetComponent == null)
                        continue;

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
                        removedComponents.Add(new RemovedComponent() {assetComponent = assetComponent, containingInstanceGameObject = instanceGameObject});
                    }
                }
            }
            finally
            {
                // Clear the lists to avoid holding static references to components
                s_ComponentList.Clear();
                s_AssetComponentList.Clear();
            }

            return true;
        }

        public static List<RemovedGameObject> GetRemovedGameObjects(GameObject prefabInstance)
        {
            ThrowExceptionIfNullOrNotPartOfPrefabInstance(prefabInstance);

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabInstance);

            // From root of asset traverse all children and detect any GameObjects that are not present on the instance object (these must be deleted)
            var removedGameObjects = new List<RemovedGameObject>();
            TransformVisitor transformVisitor = new TransformVisitor();
            transformVisitor.VisitPrefabInstanceTransforms(prefabInstanceRoot.transform, CheckForRemovedGameObjects, removedGameObjects);
            return removedGameObjects;
        }

        // Return value indicates if caller should traverse children of the the input transform or not
        static bool CheckForRemovedGameObjects(Transform transform, object userData)
        {
            // Look for removed children GameObjects of instanceGameObject
            GameObject instanceGameObject = transform.gameObject;
            GameObject assetGameObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceGameObject);
            if (assetGameObject == null)
                return false; // skip added GameObjects

            Transform assetTransform = assetGameObject.GetComponent<Transform>();
            foreach (Transform assetChild in assetTransform)
            {
                bool found = false;
                foreach (Transform instanceChild in transform)
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(instanceChild.gameObject) == assetChild.gameObject)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    List<RemovedGameObject> removedGameObjects = (List<RemovedGameObject>)userData;
                    removedGameObjects.Add(new RemovedGameObject() { parentOfRemovedGameObjectInInstance = instanceGameObject, assetGameObject = assetChild.gameObject });
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
            try
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
            finally
            {
                // Clear the lists to avoid holding static references to components
                s_ComponentList.Clear();
                s_AssetComponentList.Clear();
            }
        }
    }
}
