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
        internal abstract UnityObject GetObject();

        internal void HandleApplyMenuItems(GenericMenu menu, GenericMenu.MenuFunction2 applyAction)
        {
            bool isObjectOverride = this is ObjectOverride;
            Object obj = isObjectOverride ? GetObject() : GetAssetObject();
            bool isObjectOverrideAllDefaultOverridesComparedToOriginalSource =
                isObjectOverride ? PrefabUtility.IsObjectOverrideAllDefaultOverridesComparedToOriginalSource(obj) : false;
            PrefabUtility.HandleApplyMenuItems(
                null,
                obj,
                (menuItemContent, sourceObject) =>
                {
                    string prefabAssetPath = AssetDatabase.GetAssetPath(sourceObject);
                    GameObject rootObject = PrefabUtility.GetRootGameObject(sourceObject);
                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject))
                        menu.AddDisabledItem(menuItemContent);
                    else
                        menu.AddItem(menuItemContent, false, applyAction, prefabAssetPath);
                },
                isObjectOverrideAllDefaultOverridesComparedToOriginalSource,
                !isObjectOverride);
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
}
