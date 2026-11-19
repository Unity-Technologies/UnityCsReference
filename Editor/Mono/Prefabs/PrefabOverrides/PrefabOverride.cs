// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityObject = UnityEngine.Object;


namespace UnityEditor.SceneManagement
{
    public abstract class PrefabOverride
    {
        public abstract void Apply(string prefabAssetPath, InteractionMode mode);
        public abstract void Revert(InteractionMode mode);

        public void Apply()
        {
            var asset = GetAssetObject();
            Apply(AssetDatabase.GetAssetPath(asset), InteractionMode.UserAction);
        }

        public void Apply(string prefabAssetPath)
        {
            Apply(prefabAssetPath, InteractionMode.UserAction);
        }

        public void Apply(InteractionMode mode)
        {
            var asset = GetAssetObject();
            Apply(AssetDatabase.GetAssetPath(asset), mode);
        }

        public void Revert()
        {
            Revert(InteractionMode.UserAction);
        }

        protected UnityObject FindApplyTargetAssetObject(string prefabAssetPath)
        {
            var assetObject = GetAssetObject();
            while (assetObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(assetObject);
                if (assetPath == prefabAssetPath)
                    return assetObject;
                assetObject = PrefabUtility.GetCorrespondingObjectFromSource(assetObject);
            }
            return null;
        }

        public abstract UnityObject GetAssetObject();

        // Returns the object the override relates to.
        // For ObjectOverride, it's the object on the instance that has the overrides.
        // For AddedComponent, it's the added component on the instance.
        // For AddedGameObject, it's the added GameObject on the instance.
        // For RemovedComponent, it's the component on the Prefab Asset corresponding to the removed component on the instance.
        // For RemovedGameObject, it's the GameObject on the Prefab Asset corresponding to the removed GameObject on the instance.
        // For DataComponentOverride, AddedDataComponent and RemovedDataComponent, it's null, since a data component has no Object.
        internal abstract UnityObject GetObject();

        internal virtual void HandleApplyMenuItems(GenericMenu menu, GenericMenu.MenuFunction2 applyAction)
        {
            bool isObjectOverride = this is ObjectOverride;
            Object obj = isObjectOverride ? GetObject() : GetAssetObject();
            bool isObjectOverrideAllDefaultOverridesComparedToOriginalSource =
                isObjectOverride ? PrefabUtility.IsObjectOverrideAllDefaultOverridesComparedToOriginalSource(obj) : false;

            bool isRemovedGameObjectOverride = this is RemovedGameObject;
            bool isRemovedNestedPrefabRoot = isRemovedGameObjectOverride && PrefabUtility.IsAssetANestedPrefabRoot(obj);

            PrefabUtility.HandleApplyMenuItems(
                null,
                obj,
                (menuItemContent, sourceObject, instanceOrAssetObject) =>
                {
                    string prefabAssetPath = AssetDatabase.GetAssetPath(sourceObject);
                    GameObject rootObject = PrefabUtility.GetRootGameObject(sourceObject);
                    bool isPersistent = EditorUtility.IsPersistent(instanceOrAssetObject);

                    // An AddedGameObject that is itself an instance of the target prefab cannot be applied
                    // to that prefab (it would nest the prefab inside itself). Disable rather than throw.
                    bool wouldRecurse = this is AddedGameObject addedGameObject
                        && PrefabUtility.WouldApplyingAddedGameObjectRecurse(addedGameObject.instanceGameObject, sourceObject);

                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject) || wouldRecurse || (!isPersistent && !PrefabUtility.HasApplicableObjectOverridesForTarget(instanceOrAssetObject, sourceObject, false)))
                        menu.AddDisabledItem(menuItemContent);
                    else
                        menu.AddItem(menuItemContent, false, applyAction, prefabAssetPath);
                },
                isObjectOverrideAllDefaultOverridesComparedToOriginalSource,
                !isObjectOverride,
                !isRemovedNestedPrefabRoot);
        }
    }

    public class ObjectOverride : PrefabOverride
    {
        public UnityObject instanceObject { get; set; }
        public PrefabOverride coupledOverride { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyObjectOverride(
                instanceObject,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertObjectOverride(
                instanceObject,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
        }

        internal override UnityObject GetObject()
        {
            return instanceObject;
        }
    }

    // A data component has no Object, so it is held by EntityId.
    internal abstract class DataComponentPrefabOverride : PrefabOverride
    {
        public EntityId instanceDataComponent { get; set; }

        public GameObject instanceGameObject
            => EditorUtility.EntityIdToObject(DataComponentEntityId.GetOwner(instanceDataComponent)) as GameObject;

        // The owner's source GameObject, which is in the same Prefab asset as the data component's source.
        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instanceGameObject);
        }

        internal override UnityObject GetObject()
        {
            return null;
        }

        internal abstract bool CanApplyTo(GameObject owner, UnityObject sourceObject);

        // A data component is in the same Prefab assets as its owner GameObject, so the owner's apply targets are its apply targets.
        internal override void HandleApplyMenuItems(GenericMenu menu, GenericMenu.MenuFunction2 applyAction)
        {
            GameObject owner = instanceGameObject;
            if (owner == null)
                return;

            PrefabUtility.HandleApplyMenuItems(
                null,
                owner,
                (menuItemContent, sourceObject, instanceOrAssetObject) =>
                {
                    string prefabAssetPath = AssetDatabase.GetAssetPath(sourceObject);
                    GameObject rootObject = PrefabUtility.GetRootGameObject(sourceObject);
                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject) || !CanApplyTo(owner, sourceObject))
                        menu.AddDisabledItem(menuItemContent);
                    else
                        menu.AddItem(menuItemContent, false, applyAction, prefabAssetPath);
                });
        }
    }

    internal class DataComponentOverride : DataComponentPrefabOverride
    {
        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyDataComponentOverride(
                instanceDataComponent,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertDataComponentOverride(
                instanceDataComponent,
                mode);
        }

        internal override bool CanApplyTo(GameObject owner, UnityObject sourceObject)
        {
            return !EditorUtility.IsPersistent(owner) && PrefabUtility.HasApplicableDataComponentOverridesForTarget(instanceDataComponent, sourceObject, false);
        }
    }

    internal class AddedDataComponent : DataComponentPrefabOverride
    {
        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyAddedDataComponent(
                instanceDataComponent,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertAddedDataComponent(
                instanceDataComponent,
                mode);
        }

        // On a GameObject from another Prefab it becomes an added override in that asset.
        internal override bool CanApplyTo(GameObject owner, UnityObject sourceObject)
        {
            return !EditorUtility.IsPersistent(owner);
        }
    }

    // A removed data component has no instance, so it is held by its owner on the instance and the asset data component's EntityId.
    internal class RemovedDataComponent : PrefabOverride
    {
        public GameObject containingInstanceGameObject { get; set; }
        public EntityId assetDataComponent { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyRemovedDataComponent(
                containingInstanceGameObject,
                assetDataComponent,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertRemovedDataComponent(
                containingInstanceGameObject,
                assetDataComponent,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(containingInstanceGameObject);
        }

        internal override UnityObject GetObject()
        {
            return null;
        }

        // A Prefab further down the chain can lack the data component, when a nearer one has it as an added override.
        internal static bool CanApplyTo(GameObject containingInstanceGameObject, UnityObject sourceObject, EntityId assetDataComponent)
        {
            var sourceGameObject = sourceObject as GameObject;
            return sourceGameObject != null
                && !EditorUtility.IsPersistent(containingInstanceGameObject)
                && PrefabUtility.GetCorrespondingObjectFromSourceAtPath(containingInstanceGameObject, AssetDatabase.GetAssetPath(sourceGameObject)) == sourceGameObject
                && PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(sourceGameObject)
                && System.Array.IndexOf(PrefabUtility.GetDataComponents(sourceGameObject.GetEntityId()), DataComponentEntityId.Make(sourceGameObject.GetEntityId(), DataComponentEntityId.GetTypeId(assetDataComponent))) >= 0;
        }

        internal override void HandleApplyMenuItems(GenericMenu menu, GenericMenu.MenuFunction2 applyAction)
        {
            if (containingInstanceGameObject == null)
                return;

            PrefabUtility.HandleApplyMenuItems(
                null,
                containingInstanceGameObject,
                (menuItemContent, sourceObject, instanceOrAssetObject) =>
                {
                    if (CanApplyTo(containingInstanceGameObject, sourceObject, assetDataComponent))
                        menu.AddItem(menuItemContent, false, applyAction, AssetDatabase.GetAssetPath(sourceObject));
                    else
                        menu.AddDisabledItem(menuItemContent);
                });
        }
    }

    public class AddedComponent : PrefabOverride
    {
        public Component instanceComponent { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyAddedComponent(
                instanceComponent,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertAddedComponent(
                instanceComponent,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instanceComponent.gameObject);
        }

        internal override UnityObject GetObject()
        {
            return instanceComponent;
        }
    }

    public class RemovedComponent : PrefabOverride
    {
        public GameObject containingInstanceGameObject { get; set; }
        public Component assetComponent { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyRemovedComponent(
                containingInstanceGameObject,
                (Component)FindApplyTargetAssetObject(prefabAssetPath),
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertRemovedComponent(
                containingInstanceGameObject,
                assetComponent,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            return assetComponent;
        }

        internal override UnityObject GetObject()
        {
            return assetComponent;
        }
    }

    public class AddedGameObject : PrefabOverride
    {
        public GameObject instanceGameObject { get; set; }
        public int siblingIndex { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyAddedGameObject(
                instanceGameObject,
                prefabAssetPath,
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertAddedGameObject(
                instanceGameObject,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            GameObject parent = instanceGameObject.transform.parent.gameObject;
            return PrefabUtility.GetCorrespondingObjectFromSource(parent);
        }

        internal override UnityObject GetObject()
        {
            return instanceGameObject;
        }
    }

    public class RemovedGameObject : PrefabOverride
    {
        public GameObject parentOfRemovedGameObjectInInstance { get; set; }
        public GameObject assetGameObject { get; set; }

        public override void Apply(string prefabAssetPath, InteractionMode mode)
        {
            PrefabUtility.ApplyRemovedGameObject(
                parentOfRemovedGameObjectInInstance,
                (GameObject)FindApplyTargetAssetObject(prefabAssetPath),
                mode);
        }

        public override void Revert(InteractionMode mode)
        {
            PrefabUtility.RevertRemovedGameObject(
                parentOfRemovedGameObjectInInstance,
                assetGameObject,
                mode);
        }

        public override UnityObject GetAssetObject()
        {
            return assetGameObject;
        }

        internal override UnityObject GetObject()
        {
            return assetGameObject;
        }
    }
}
