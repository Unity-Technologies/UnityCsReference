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
        public abstract void Apply(string prefabAssetPath);
        public abstract void Revert();

        public void Apply()
        {
            var asset = GetAssetObject();
            Apply(AssetDatabase.GetAssetPath(asset));
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

        internal void HandleApplyMenuItems(GenericMenu menu, GenericMenu.MenuFunction2 applyAction)
        {
            PrefabUtility.HandleApplyMenuItems(
                null,
                GetAssetObject(),
                (menuItemContent, sourceObject) =>
                {
                    string prefabAssetPath = AssetDatabase.GetAssetPath(sourceObject);
                    GameObject rootObject = PrefabUtility.GetRootGameObject(sourceObject);
                    if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject))
                        menu.AddDisabledItem(menuItemContent);
                    else
                        menu.AddItem(menuItemContent, false, applyAction, prefabAssetPath);
                });
        }
    }

    public class ObjectOverride : PrefabOverride
    {
        public UnityObject instanceObject { get; set; }

        public override void Apply(string prefabAssetPath)
        {
            PrefabUtility.ApplyObjectOverride(
                instanceObject,
                prefabAssetPath,
                InteractionMode.UserAction);
        }

        public override void Revert()
        {
            PrefabUtility.RevertObjectOverride(
                instanceObject,
                InteractionMode.UserAction);
        }

        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
        }
    }

    public class AddedComponent : PrefabOverride
    {
        public Component instanceComponent { get; set; }

        public override void Apply(string prefabAssetPath)
        {
            PrefabUtility.ApplyAddedComponent(
                instanceComponent,
                prefabAssetPath,
                InteractionMode.UserAction);
        }

        public override void Revert()
        {
            PrefabUtility.RevertAddedComponent(
                instanceComponent,
                InteractionMode.UserAction);
        }

        public override UnityObject GetAssetObject()
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instanceComponent.gameObject);
        }
    }

    public class RemovedComponent : PrefabOverride
    {
        public GameObject containingInstanceGameObject { get; set; }
        public Component assetComponent { get; set; }

        public override void Apply(string prefabAssetPath)
        {
            PrefabUtility.ApplyRemovedComponent(
                containingInstanceGameObject,
                (Component)FindApplyTargetAssetObject(prefabAssetPath),
                InteractionMode.UserAction);
        }

        public override void Revert()
        {
            PrefabUtility.RevertRemovedComponent(
                containingInstanceGameObject,
                assetComponent,
                InteractionMode.UserAction);
        }

        public override UnityObject GetAssetObject()
        {
            return assetComponent;
        }
    }

    public class AddedGameObject : PrefabOverride
    {
        public GameObject instanceGameObject { get; set; }
        public int siblingIndex { get; set; }

        public override void Apply(string prefabAssetPath)
        {
            PrefabUtility.ApplyAddedGameObject(
                instanceGameObject,
                prefabAssetPath,
                InteractionMode.UserAction);
        }

        public override void Revert()
        {
            PrefabUtility.RevertAddedGameObject(
                instanceGameObject,
                InteractionMode.UserAction);
        }

        public override UnityObject GetAssetObject()
        {
            GameObject parent = instanceGameObject.transform.parent.gameObject;
            return PrefabUtility.GetCorrespondingObjectFromSource(parent);
        }
    }
}
