// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.Utils;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Prefabs/Prefab.h")]
    [NativeHeader("Editor/Src/Prefabs/PrefabCreation.h")]
    [NativeHeader("Editor/Src/Prefabs/PrefabConnection.h")]
    [NativeHeader("Editor/Mono/Prefabs/PrefabUtility.bindings.h")]
    public sealed partial class PrefabUtility
    {
        // Returns the corresponding GameObject/Component from /source/, or null if it can't be found.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static Object GetCorrespondingObjectFromSource(Object source);

        // Retrieves the prefab object representation.
        [NativeMethod("GetPrefabFromAnyObjectInPrefab", IsFreeFunction = true)]
        extern public static Object GetPrefabObject(Object targetObject);

        // Extract all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static PropertyModification[] GetPropertyModifications(Object targetPrefab);

        // Assigns all modifications that are applied to the prefab instance compared to the parent prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static void SetPropertyModifications(Object targetPrefab, PropertyModification[] modifications);

        // Instantiate an asset that is referenced by a prefab and use it on the prefab instance.
        [FreeFunction]
        [NativeHeader("Editor/Src/Prefabs/AttachedPrefabAsset.h")]
        extern public static Object InstantiateAttachedAsset(Object targetObject);

        // Force record property modifications by comparing against the parent prefab.
        [FreeFunction]
        extern public static void RecordPrefabInstancePropertyModifications(Object targetObject);

        // Force re-merging all prefab instances of this prefab.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void MergeAllPrefabInstances(Object targetObject);

        // Disconnects the prefab instance from its parent prefab.
        [FreeFunction]
        extern public static void DisconnectPrefabInstance(Object targetObject);

        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern static private Object InstantiatePrefab_internal(Object target, Scene destinationScene);

        // Creates an empty prefab at given path.
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern static private Object CreateEmptyPrefab_internal(string path);

        // Creates a prefab from a game object hierarchy
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern static private GameObject CreatePrefab_internal(string path, GameObject go, ReplacePrefabOptions options);

        // Replaces the /targetPrefab/ with a copy of the game object hierarchy /go/.
        [FreeFunction]
        extern public static GameObject ReplacePrefab(GameObject go, Object targetPrefab, ReplacePrefabOptions options);

        // Connect the source prefab to the game object, which replaces the instance content with the content of the prefab
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static GameObject ConnectGameObjectToPrefab(GameObject go, GameObject sourcePrefab);

        // Returns the topmost game object that has the same prefab parent as /target/
        [FreeFunction]
        extern public static GameObject FindRootGameObjectWithSameParentPrefab(GameObject target);

        // Returns root game object of the prefab instance. Given an instance object the function finds the prefab
        // and uses the prefab root game object to find the matching instance root game object
        [NativeMethod("FindValidUploadPrefabRoot", IsFreeFunction = true)]
        extern public static GameObject FindValidUploadPrefabInstanceRoot(GameObject target);

        // Connects the game object to the prefab that it was last connected to.
        [FreeFunction]
        extern public static bool ReconnectToLastPrefab(GameObject go);

        // Resets the properties of the component or game object to the parent prefab state
        [StaticAccessor("PrefabUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static bool ResetToPrefabState(Object obj);

        [FreeFunction]
        extern public static bool IsComponentAddedToPrefabInstance(Object source);

        // Resets the properties of all objects in the prefab, including child game objects and components that were added to the prefab instance
        [FreeFunction]
        extern public static bool RevertPrefabInstance(GameObject go);

        // Given an object, returns its prefab type (None, if it's not a prefab)
        [FreeFunction]
        extern public static PrefabType GetPrefabType(Object target);

        // Helper function to find the prefab root of an object (used for picking niceness)
        [FreeFunction]
        extern public static GameObject FindPrefabRoot(GameObject source);
    }
}
