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
    [NativeHeader("Editor/Src/Prefabs/ReplacePrefabInstance.h")]
    [NativeHeader("Editor/Mono/Prefabs/PrefabUtility.bindings.h")]
    public sealed partial class PrefabUtility
    {
        [StaticAccessor("PrefabInstance", StaticAccessorType.DoubleColon)]
        extern internal static int defaultOverridesCount { get; }

        [StaticAccessor("PrefabInstance", StaticAccessorType.DoubleColon)]
        extern internal static int defaultOverridesCountUsingRectTransform { get; }

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
        extern internal static string GetAssetPathOfSourcePrefab(Object targetObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static Object GetPrefabAssetHandle(Object assetComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static bool HasManagedReferencesWithMissingTypes(Object assetComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static GameObject GetPrefabInstanceRootGameObject(Object instanceComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static GameObject GetPrefabAssetRootGameObject(Object assetComponentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static bool HasObjectOverride(Object componentOrGameObject, bool includeDefaultOverrides = false);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static bool HasPrefabInstanceUnusedOverrides_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static int GetPrefabInstanceUnusedRemovedComponentCount_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static int GetPrefabInstanceUnusedRemovedGameObjectCount_Internal([NotNull] GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static string TryGetCurrentPropertyPathFromOldPropertyPath_Internal(GameObject gameObject, Object target, string propertyPath);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static bool HasPrefabInstanceNonDefaultOverrides_CachedForUI_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static bool HasPrefabInstanceUnusedOverrides_CachedForUI_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static void ClearPrefabInstanceNonDefaultOverridesCache_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static void ClearPrefabInstanceUnusedOverridesCache_Internal(GameObject gameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern internal static InstanceOverridesInfo GetPrefabInstanceOverridesInfo_Internal(GameObject gameObject);

        // Extract all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static PropertyModification[] GetPropertyModifications(Object targetPrefab);

        // Assigns all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static void SetPropertyModifications(Object targetPrefab, PropertyModification[] modifications);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern internal static bool HasApplicableObjectOverridesForTarget([NotNull] Object targetPrefab, [NotNull] Object applyTarget, bool includeDefaultOverrides);

        [NativeMethod("PrefabUtilityBindings::FindNearestInstanceOfAsset", IsFreeFunction = true)]
        extern internal static GameObject FindNearestInstanceOfAsset(Object componentOrGameObjectInstance, Object prefab);

        [FreeFunction]
        extern public static bool HasPrefabInstanceAnyOverrides(GameObject instanceRoot, bool includeDefaultOverrides);

        // Instantiate an asset that is referenced by a prefab and use it on the prefab instance.
        [FreeFunction]
        [NativeHeader("Editor/Src/Prefabs/AttachedPrefabAsset.h")]
        extern public static Object InstantiateAttachedAsset([NotNull] Object targetObject);

        // Force record property modifications by comparing against the parent prefab.
        [FreeFunction]
        extern public static void RecordPrefabInstancePropertyModifications([NotNull] Object targetObject);

        // Force re-merging all prefab instances of this prefab.
        [Obsolete("MergeAllPrefabInstances is deprecated. Prefabs are merged automatically. There is no need to call this method.")]
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void MergeAllPrefabInstances(Object targetObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static void MergePrefabInstance_internal([NotNull] Object gameObjectOrComponent);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static MergeStatus GetMergeStatus(GameObject componentOrGameObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject[] FindAllInstancesOfPrefab_internal([NotNull] GameObject prefabRoot, int sceneHandle);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static GameObject[] UnpackPrefabInstanceAndReturnNewOutermostRoots_internal(GameObject instanceRoot, PrefabUnpackMode unpackMode);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private Object InstantiatePrefab_internal(Object target, Scene destinationScene, Transform parent);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private bool ReplacePrefabAssetOfPrefabInstance_Internal([NotNull] GameObject instanceRoot, [NotNull] GameObject assetRootRoot, PrefabReplacingSettings settings);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private bool ConvertToPrefabInstance_Internal([NotNull] GameObject plainGameObject, [NotNull] GameObject assetRootRoot, ConvertToPrefabInstanceSettings settings);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private bool InstantiateDraggedPrefabUpon_Internal([NotNull] GameObject draggedUponGameObject, [NotNull] GameObject assetRootRoot);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void LoadPrefabContentsIntoPreviewScene(string prefabPath, Scene scene);

        [Obsolete("Use ConvertToPrefabInstance() or ReplacePrefabAssetOfPrefabInstance() which has settings for better control.")]
        public static GameObject ConnectGameObjectToPrefab(GameObject go, GameObject sourcePrefab)
        {
            if (GetPrefabInstanceStatus(go) == PrefabInstanceStatus.NotAPrefab)
            {
                var settings = new ConvertToPrefabInstanceSettings();
                ConvertToPrefabInstance(go, sourcePrefab, settings, InteractionMode.AutomatedAction);
            }
            else if (IsOutermostPrefabInstanceRoot(go))
            {
                var settings = new PrefabReplacingSettings();
                ReplacePrefabAssetOfPrefabInstance(go, sourcePrefab, settings, InteractionMode.AutomatedAction);
            }

            return go;
        }

        // Returns the topmost game object that has the same prefab parent as /target/
        [FreeFunction]
        [Obsolete("FindRootGameObjectWithSameParentPrefab is deprecated, please use GetOutermostPrefabInstanceRoot instead.")]
        extern public static GameObject FindRootGameObjectWithSameParentPrefab([NotNull] GameObject target);

        // Returns root game object of the prefab instance. Given an instance object the function finds the prefab
        // and uses the prefab root game object to find the matching instance root game object
        [NativeMethod("FindInstanceRootGameObject", IsFreeFunction = true)]
        [Obsolete("FindValidUploadPrefabInstanceRoot is deprecated, please use GetOutermostPrefabInstanceRoot instead.")]
        extern public static GameObject FindValidUploadPrefabInstanceRoot([NotNull] GameObject target);

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
        extern internal static Component[] GetRemovedComponents([NotNull] Object prefabInstance);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static void SetRemovedComponents([NotNull] Object prefabInstance, [NotNull] Component[] removedComponents);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern internal static GameObject[] GetRemovedGameObjects([NotNull] Object prefabInstance);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static void SetRemovedGameObjects([NotNull] Object prefabInstance, [NotNull] GameObject[] removedGameObjects);

        // Returns true if the object is part of a any type of prefab, asset or instance
        [FreeFunction]
        extern public static bool IsPartOfAnyPrefab([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern internal static void SetHasSubscribersToAllowRecordingPrefabPropertyOverrides(bool hasSubscribers);

        // Returns true if the object is an asset,
        // does not matter if the asset is a regular prefab, a variant or Model
        // Is false for all non-persistent objects
        [FreeFunction]
        extern public static bool IsPartOfPrefabAsset([NotNull] Object componentOrGameObject);

        // Returns true if the object is an instance of a prefab,
        // regardless of whether the instance is a regular prefab, a variant or model.
        // Also returns if the asset is missing.
        // Is also true for prefab instances inside persistent prefab assets -
        // use IsPartOfNonAssetPrefabInstance to exclude those.
        [FreeFunction]
        extern public static bool IsPartOfPrefabInstance([NotNull] Object componentOrGameObject);

        // Returns true if the object is an instance of a prefab,
        // regardless of whether the instance is a regular prefab, a variant or model.
        // Also returns true if the asset is missing.
        // Is false for prefab instances inside persistent prefab assets -
        // use IsPartOfPrefabInstance to include those.
        // Note that prefab instances in prefab mode are not assets/persistent since technically,
        // the object edited in Prefab Mode is not the persistent prefab asset itself.
        [FreeFunction]
        extern public static bool IsPartOfNonAssetPrefabInstance([NotNull] Object componentOrGameObject);

        // We need a version of IsPartOfNonAssetPrefabInstance that uses an instanceID in order to handle missing monobehaviors
        // which leads to managed null references (unity null) even though we have a native object. See handling for missing
        // scripts for Prefab instances in GenericInspector.cs
        [FreeFunction]
        extern internal static bool IsInstanceIDPartOfNonAssetPrefabInstance(int componentOrGameObjectInstanceID);

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
        extern public static bool IsPrefabAssetMissing([NotNull] Object instanceComponentOrGameObject);

        [FreeFunction]
        extern public static GameObject GetOutermostPrefabInstanceRoot([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern public static GameObject GetNearestPrefabInstanceRoot([NotNull] Object componentOrGameObject);

        [FreeFunction]
        extern internal static GameObject GetOriginalSourceOrVariantRoot([NotNull] Object instanceOrAsset);

        [FreeFunction]
        extern public static GameObject GetOriginalSourceRootWhereGameObjectIsAdded([NotNull] GameObject gameObject);

        [NativeMethod("PrefabUtilityBindings::ApplyPrefabAddedComponent", IsFreeFunction = true, ThrowsException = true)]
        extern private static void ApplyAddedComponent([NotNull] Component addedComponent, [NotNull] Object applyTargetPrefabObject);

        [NativeMethod("PrefabUtilityBindings::IsDefaultOverride", IsFreeFunction = true)]
        extern public static bool IsDefaultOverride(PropertyModification modification);

        [NativeMethod("PrefabUtilityBindings::IsDefaultOverridePropertyPath", IsFreeFunction = true)]
        extern internal static bool IsDefaultOverridePropertyPath(string propertyPath);

        [FreeFunction]
        extern internal static bool CheckIfAddingPrefabWouldResultInCyclicNesting(Object prefabAssetThatIsAddedTo, Object prefabAssetThatWillBeAdded);

        [FreeFunction]
        extern internal static bool WasCreatedAsPrefabInstancePlaceholderObject(Object componentOrGameObject);

        [FreeFunction]
        extern internal static void ShowCyclicNestingWarningDialog();

        [NativeMethod("PrefabUtilityBindings::GetVariantParentGUID_Internal", IsFreeFunction = true, ThrowsException = true)]
        extern internal static string GetVariantParentGUID(int prefabAssetInstanceID);

        internal static string GetVariantParentGUID(GameObject prefabAsset)
        {
            if (prefabAsset == null)
                throw new ArgumentNullException(nameof(prefabAsset));
            return GetVariantParentGUID(prefabAsset.GetInstanceID());
        }

        [FreeFunction]
        extern internal static bool IsPathInStreamingAssets(string path);
    }
}
