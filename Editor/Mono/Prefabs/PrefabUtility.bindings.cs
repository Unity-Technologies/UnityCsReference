// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.IO;
using UnityEditor.Utils;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Prefabs/Prefab.h")]
    [NativeHeader("Editor/Src/Prefabs/PrefabCreation.h")]
    [NativeHeader("Editor/Src/Prefabs/PrefabConnection.h")]
    [NativeHeader("Editor/Src/Prefabs/PrefabInstance.h")]
    [NativeHeader("Editor/Mono/Prefabs/PrefabUtility.bindings.h")]
    public sealed partial class PrefabUtility
    {
        // Returns the corresponding GameObject/Component from /source/, or null if it can't be found.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static Object GetCorrespondingObjectFromSource_internal([NotNull] Object obj);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static Object GetCorrespondingObjectFromSourceInAsset_internal([NotNull] Object obj, [NotNull] Object prefabAssetHandle);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static Object GetCorrespondingObjectFromSourceAtPath_internal([NotNull] Object obj, string prefabAssetPath);

        // Retrieves the prefab object representation.
        [Obsolete("Use GetPrefabInstanceHandle for Prefab instances. Handles for Prefab Assets has been discontinued.")]
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static Object GetPrefabObject(Object targetObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static Object GetPrefabInstanceHandle(Object instanceComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static Object GetPrefabAssetHandle(Object assetComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static GameObject GetPrefabInstanceRootGameObject(Object instanceComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static GameObject GetPrefabAssetRootGameObject(Object assetComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static bool HasObjectOverride(Object componentOrGameObject, bool includeDefaultOverrides = false);

        // Extract all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static PropertyModification[] GetPropertyModifications(Object targetPrefab);

        // Assigns all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static void SetPropertyModifications(Object targetPrefab, PropertyModification[] modifications);

        [FreeFunction]
        extern public static bool HasPrefabInstanceAnyOverrides(GameObject instanceRoot, bool includeDefaultOverrides);

        // Instantiate an asset that is referenced by a prefab and use it on the prefab instance.
        [FreeFunction]
        [NativeHeader("Editor/Src/Prefabs/AttachedPrefabAsset.h")]
        extern public static Object InstantiateAttachedAsset(Object targetObject);

        // Force record property modifications by comparing against the parent prefab.
        [FreeFunction]
        extern public static void RecordPrefabInstancePropertyModifications(Object targetObject);

        // Force re-merging all prefab instances of this prefab.
        [Obsolete("MergeAllPrefabInstances is deprecated. Prefabs are merged automatically. There is no need to call this method.")]
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void MergeAllPrefabInstances(Object targetObject);

        // Disconnects the prefab instance from its parent prefab.
        [Obsolete("The concept of disconnecting Prefab instances has been deprecated.")]
        [FreeFunction]
        extern public static void DisconnectPrefabInstance(Object targetObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static GameObject[] UnpackPrefabInstanceAndReturnNewOutermostRoots(GameObject instanceRoot, PrefabUnpackMode unpackMode);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private Object InstantiatePrefab_internal(Object target, Scene destinationScene, Transform parent);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void LoadPrefabContentsIntoPreviewScene(string prefabPath, Scene scene);

        // Connect the source prefab to the game object, which replaces the instance content with the content of the prefab
        [Obsolete("Use RevertPrefabInstance. Prefabs instances can no longer be connected to Prefab Assets they are not an instance of to begin with.")]
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static GameObject ConnectGameObjectToPrefab([NotNull] GameObject go, [NotNull] GameObject sourcePrefab);

        // Returns the topmost game object that has the same prefab parent as /target/
        [FreeFunction]
        [Obsolete("FindRootGameObjectWithSameParentPrefab is deprecated, please use GetOutermostPrefabInstanceRoot instead.")]
        extern public static GameObject FindRootGameObjectWithSameParentPrefab(GameObject target);

        // Returns root game object of the prefab instance. Given an instance object the function finds the prefab
        // and uses the prefab root game object to find the matching instance root game object
        [NativeMethod("FindInstanceRootGameObject", IsFreeFunction = true)]
        [Obsolete("FindValidUploadPrefabInstanceRoot is deprecated, please use GetOutermostPrefabInstanceRoot instead.")]
        extern public static GameObject FindValidUploadPrefabInstanceRoot(GameObject target);

        // Connects the game object to the prefab that it was last connected to.
        [FreeFunction]
        [Obsolete("Use RevertPrefabInstance.")]
        extern public static bool ReconnectToLastPrefab(GameObject go);

        // Resets the properties of the component or game object to the parent prefab state
        [Obsolete("Use RevertObjectOverride.")]
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static bool ResetToPrefabState(Object obj);

        // Resets the properties of the component or game object to the parent prefab state
        [NativeMethod("PrefabUtilityBindings::ResetToPrefabState", IsFreeFunction = true)]
        extern private static bool RevertObjectOverride_Internal(Object obj);

        [FreeFunction]
        extern public static bool IsAddedComponentOverride([NotNull] Object component);

        // Resets the properties of all objects in the prefab, including child game objects and components that were added to the prefab instance
        [Obsolete("Use the overload that takes an InteractionMode parameter.")]
        [FreeFunction]
        extern public static bool RevertPrefabInstance([NotNull] GameObject go);

        // Resets the properties of all objects in the prefab, including child game objects and components that were added to the prefab instance
        [NativeMethod("RevertPrefabInstance", IsFreeFunction = true)]
        extern private static bool RevertPrefabInstance_Internal([NotNull] GameObject go);

        // Helper function to find the prefab root of an object
        [FreeFunction]
        [Obsolete("Use GetOutermostPrefabInstanceRoot if source is a Prefab instance or source.transform.root.gameObject if source is a Prefab Asset object.")]
        extern public static GameObject FindPrefabRoot([NotNull] GameObject source);

        internal static GameObject CreateVariant(GameObject assetRoot, string path)
        {
            if (assetRoot == null)
                throw new ArgumentNullException("The inputObject is null");

            if (!IsPartOfPrefabAsset(assetRoot))
                throw new ArgumentException("Given input object is not a prefab asset");

            if (assetRoot.transform.root.gameObject != assetRoot)
                throw new ArgumentException("Object to create variant from has to be a Prefab root");

            if (path == null)
                throw new ArgumentNullException("The path is null");

            var assetRootObjectPath = AssetDatabase.GetAssetPath(assetRoot);
            if (Paths.AreEqual(path, assetRootObjectPath, true))
                throw new ArgumentException("Creating a variant of an object into the source file of the input object is not allowed");

            if (!Paths.IsValidAssetPath(path, ".prefab"))
                throw new ArgumentException("Given path is not valid: '" + path + "'");

            return CreateVariant_Internal(assetRoot, path);
        }

        [NativeMethod("CreateVariant", IsFreeFunction = true)]
        extern private static GameObject CreateVariant_Internal([NotNull] GameObject original, string path);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject SavePrefab_Internal([NotNull] GameObject root, string path, bool connectToInstance, out bool success);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static GameObject ApplyPrefabInstance_Internal([NotNull] GameObject root);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject SaveAsPrefabAsset_Internal([NotNull] GameObject root, string path, out bool success);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject SaveAsPrefabAssetAndConnect_Internal([NotNull] GameObject root, string path, out bool success);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject SavePrefabAsset_Internal([NotNull] GameObject root, out bool success);

        internal static void AddGameObjectsToPrefabAndConnect(GameObject[] gameObjects, Object targetPrefab)
        {
            if (gameObjects == null)
                throw new ArgumentNullException("gameObjects");

            if (gameObjects.Length == 0)
                throw new ArgumentException("gameObjects array is empty");

            if (targetPrefab == null)
                throw new ArgumentNullException("targetPrefab");

            if (!PrefabUtility.IsPartOfPrefabAsset(targetPrefab))
                throw new ArgumentException("Target Prefab has to be a Prefab Asset");

            Object targetPrefabInstance = null;

            var targetPrefabObject = PrefabUtility.GetPrefabAssetHandle(targetPrefab);

            foreach (GameObject go in gameObjects)
            {
                if (go == null)
                    throw new ArgumentException("GameObject in input 'gameObjects' array is null");

                if (EditorUtility.IsPersistent(go))  // Prefab asset
                    throw new ArgumentException("Game object is part of a prefab");

                var parentPrefabInstance = GetParentPrefabInstance(go);
                if (parentPrefabInstance == null)
                    throw new ArgumentException("GameObject is not (directly) parented under a target Prefab instance.");

                if (targetPrefabInstance == null)
                {
                    targetPrefabInstance = parentPrefabInstance;
                    if (!IsPrefabInstanceObjectOf(go.transform.parent, targetPrefabObject))
                        throw new ArgumentException("GameObject is not parented under a target Prefab instance.");
                }
                else
                {
                    if (parentPrefabInstance != targetPrefabInstance)
                    {
                        throw new ArgumentException("GameObjects must be parented under the same Prefab instance.");
                    }
                }

                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
                {
                    var correspondingGO = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    var correspondingGOPrefabObject = PrefabUtility.GetPrefabAssetHandle(correspondingGO);
                    if (targetPrefabObject == correspondingGOPrefabObject)
                        throw new ArgumentException("GameObject is already part of target prefab");
                }
            }

            string prefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetPrefab));
            if (!VerifyNestingFromScript(gameObjects, prefabGUID, null))
                throw new ArgumentException("Cyclic nesting detected");

            AddGameObjectsToPrefabAndConnect_Internal(gameObjects, targetPrefab);
        }

        [NativeMethod("AddGameObjectsToPrefabAndConnect", IsFreeFunction = true)]
        extern private static void AddGameObjectsToPrefabAndConnect_Internal([NotNull] GameObject[] gameObjects, [NotNull] Object prefab);

        [NativeMethod("VerifyNestingFromScript", IsFreeFunction = true)]
        extern private static bool VerifyNestingFromScript([NotNull] GameObject[] gameObjects, [NotNull] string targetPrefabGUID, Object prefabInstance);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static Component[] GetRemovedComponents([NotNull] Object prefabInstance);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static void SetRemovedComponents([NotNull] Object prefabInstance, [NotNull] Component[] removedComponents);

        // Returns true if the object is part of a any type of prefab, asset or instance
        [FreeFunction]
        extern public static bool IsPartOfAnyPrefab([NotNull] Object componentOrGameObject);

        // Returns true if the object is an asset,
        // does not matter if the asset is a regular prefab, a variant or Model
        // Is false for all non-persistent objects
        [FreeFunction]
        extern public static bool IsPartOfPrefabAsset([NotNull] Object componentOrGameObject);

        // Returns true if the object is an instance of a prefab,
        // regardless of whether the instance is a regular prefab, a variant or model.
        // Also returns true if disconnected, and if the asset is missing.
        // Is also true for prefab instances inside persistent prefab assets -
        // use IsPartOfNonAssetPrefabInstance to exclude those.
        [FreeFunction]
        extern public static bool IsPartOfPrefabInstance([NotNull] Object componentOrGameObject);

        // Returns true if the object is an instance of a prefab,
        // regardless of whether the instance is a regular prefab, a variant or model.
        // Also returns true if disconnected, and if the asset is missing.
        // Is false for prefab instances inside persistent prefab assets -
        // use IsPartOfPrefabInstance to include those.
        // Note that prefab instances in prefab mode are not assets/persistent since technically,
        // the object edited in Prefab Mode is not the persistent prefab asset itself.
        [FreeFunction]
        extern public static bool IsPartOfNonAssetPrefabInstance([NotNull] Object componentOrGameObject);

        // Returns true if the object is from a regular prefab or instance of regular prefab
        [FreeFunction]
        extern public static bool IsPartOfRegularPrefab([NotNull] Object componentOrGameObject);

        // Returns true if the object is from a model prefab or instance of model prefab
        [FreeFunction]
        extern public static bool IsPartOfModelPrefab([NotNull] Object componentOrGameObject);

        // Return true if the object is part of a Variant no matter if the object is an instance or asset object
        [FreeFunction]
        extern public static bool IsPartOfVariantPrefab([NotNull] Object componentOrGameObject);

        // Returns true if the object is from a Prefab Asset which is not editable, or an instance of such a Prefab
        // Examples are Model Prefabs and Prefabs in read-only folders.
        [FreeFunction]
        extern public static bool IsPartOfImmutablePrefab([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern public static bool IsDisconnectedFromPrefabAsset([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern public static bool IsPrefabAssetMissing([NotNull] Object instanceComponentOrGameObject);

        [FreeFunction]
        extern public static GameObject GetOutermostPrefabInstanceRoot([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern public static GameObject GetNearestPrefabInstanceRoot([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern internal static GameObject GetOriginalSourceOrVariantRoot([NotNull] Object instanceOrAsset);

        [FreeFunction]
        extern internal static GameObject GetOriginalSourceRootWhereGameObjectIsAdded([NotNull] GameObject gameObject);

        [NativeMethod("PrefabUtilityBindings::ApplyPrefabAddedComponent", IsFreeFunction = true, ThrowsException = true)]
        extern private static void ApplyAddedComponent([NotNull] Component addedComponent, [NotNull] Object applyTargetPrefabObject);

        [NativeMethod("PrefabUtilityBindings::IsDefaultOverride", IsFreeFunction = true)]
        extern public static bool IsDefaultOverride(PropertyModification modification);

        [FreeFunction]
        extern internal static bool CheckIfAddingPrefabWouldResultInCyclicNesting(Object prefabAssetThatIsAddedTo, Object prefabAssetThatWillBeAdded);

        [FreeFunction]
        extern internal static void ShowCyclicNestingWarningDialog();
    }
}
