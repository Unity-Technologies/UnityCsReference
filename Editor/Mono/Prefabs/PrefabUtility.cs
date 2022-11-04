// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.Utils;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.VersionControl;

namespace UnityEditor
{
    public enum PrefabAssetType
    {
        NotAPrefab = 0,
        Regular = 1,
        Model = 2,
        Variant = 3,
        MissingAsset = 4
    }

    public enum PrefabInstanceStatus
    {
        NotAPrefab = 0,
        Connected = 1,
        Disconnected = 2,
        MissingAsset = 3
    }

    // Enum for the PrefabUtility.UnpackPrefabInstance function.
    public enum PrefabUnpackMode
    {
        OutermostRoot = 0,
        Completely = 1,
    }

    // The type of a prefab object as returned by PrefabUtility.GetPrefabType.
    [Obsolete("PrefabType no longer tells everything about Prefab instance.")]
    public enum PrefabType
    {
        // The object is not a prefab nor an instance of a prefab.
        None = 0,
        // The object is a user created prefab asset.
        Prefab = 1,
        // The object is an imported 3D model asset.
        ModelPrefab = 2,
        // The object is an instance of a user created prefab.
        PrefabInstance = 3,
        // The object is an instance of an imported 3D model.
        ModelPrefabInstance = 4,
        // The object was an instance of a prefab, but the original prefab could not be found.
        MissingPrefabInstance = 5,
        // The object is an instance of a user created prefab, but the connection is broken.
        DisconnectedPrefabInstance = 6,
        // The object is an instance of an imported 3D model, but the connection is broken.
        DisconnectedModelPrefabInstance = 7,
    }

    // Flags for the PrefabUtility.ReplacePrefab function.
    [Flags]
    [Obsolete("This has turned into the more explicit APIs, SavePrefabAsset, SaveAsPrefabAsset, SaveAsPrefabAssetAndConnect")]
    public enum ReplacePrefabOptions
    {
        // Replaces prefabs by matching pre-existing connections to the prefab.
        Default = 0,
        // Connects the passed objects to the prefab after uploading the prefab.
        ConnectToPrefab = 1,
        // Replaces the prefab using name based lookup in the transform hierarchy.
        ReplaceNameBased = 2,
    }

    public sealed partial class PrefabUtility
    {
        internal static class GameObjectStyles
        {
            public static Texture2D gameObjectIcon = EditorGUIUtility.LoadIconRequired("UnityEngine/GameObject Icon");
            public static Texture2D prefabIcon = EditorGUIUtility.LoadIconRequired("Prefab Icon");
        }

        private const string kMaterialExtension = ".mat";
        internal const string kDummyPrefabStageRootObjectName = "Prefab Mode in Context";

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
                    if (String.IsNullOrEmpty(folder))
                    {
                        // cancel the extraction if the user did not select a folder
                        return;
                    }

                    folder = FileUtil.GetProjectRelativePath(folder);
                }

                // TODO: [bogdanc 3/6/2017] if we want this function really generic, we need to know what extension the new asset files should have
                var extension = selectedObj is Material ? kMaterialExtension : String.Empty;
                var newAssetPath = FileUtil.CombinePaths(folder, selectedObj.name) + extension;
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                var error = AssetDatabase.ExtractAsset(selectedObj, newAssetPath);
                if (String.IsNullOrEmpty(error))
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
                var importer = t as AssetImporter;

                var materials = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).Where(x => x.GetType() == typeof(Material)).ToArray();

                foreach (var material in materials)
                {
                    var newAssetPath = FileUtil.CombinePaths(destinationPath, material.name) + kMaterialExtension;
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                    var error = AssetDatabase.ExtractAsset(material, newAssetPath);
                    if (String.IsNullOrEmpty(error))
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

        internal static void GetObjectListFromHierarchy(HashSet<int> hierarchyInstanceIDs, GameObject gameObject)
        {
            Transform transform = null;
            List<Component> components = new List<Component>();
            gameObject.GetComponents(components);
            hierarchyInstanceIDs.Add(gameObject.GetInstanceID());
            foreach (var component in components)
            {
                if (component == null)
                {
                    throw new Exception(String.Format("Component on GameObject '{0}' is invalid", gameObject.name));
                }

                if (component is Transform)
                    transform = component as Transform;
                else
                    hierarchyInstanceIDs.Add(component.GetInstanceID());
            }

            if (transform == null)
                return;

            int childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
                GetObjectListFromHierarchy(hierarchyInstanceIDs, transform.GetChild(i).gameObject);
        }

        internal static void CollectAddedObjects(GameObject gameObject, HashSet<int> hierarchyInstanceIDs, List<Object> danglingObjects)
        {
            Transform transform = null;
            List<Component> components = new List<Component>();

            if (hierarchyInstanceIDs.Contains(gameObject.GetInstanceID()))
            {
                gameObject.GetComponents(components);
                foreach (var component in components)
                {
                    if (component is Transform)
                        transform = component as Transform;
                    else
                    {
                        if (component == null)
                            continue;
                        if (!hierarchyInstanceIDs.Contains(component.GetInstanceID()))
                        {
                            danglingObjects.Add(component);
                        }
                    }
                }

                if (transform == null)
                    return;

                int childCount = transform.childCount;
                for (var i = 0; i < childCount; i++)
                    CollectAddedObjects(transform.GetChild(i).gameObject, hierarchyInstanceIDs, danglingObjects);
            }
            else
            {
                danglingObjects.Add(gameObject);
            }
        }

        private static void RegisterNewObjects(GameObject newHierarchy, HashSet<int> hierarchyInstanceIDs, string actionName)
        {
            var danglingObjects = new List<Object>();

            CollectAddedObjects(newHierarchy, hierarchyInstanceIDs, danglingObjects);

            // We need to ensure that dangling components are registered in an acceptable order regarding dependencies. For example, if we're adding RigidBody and ConfigurableJoint, the RigidBody will need to be added first (as the ConfigurableJoint depends upon it existing)
            var addedTypes = new HashSet<Type>()
            {
                typeof(Transform)
            };

            var emptyPass = false;
            GameObject currentGO = null;

            while (danglingObjects.Count > 0 && !emptyPass)
            {
                emptyPass = true;
                for (var i = 0; i < danglingObjects.Count; i++)
                {
                    var danglingObject = danglingObjects[i];

                    if (danglingObject is Component)
                    {
                        var comp = (Component)danglingObject;
                        if (comp.gameObject != currentGO)
                        {
                            addedTypes = new HashSet<Type>() { typeof(Transform) };
                            currentGO = comp.gameObject;
                        }
                    }

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
                        // This is a special case where the stack order must be forced. Created objects must be registered to the undo system before any structure changes can be recorded to it but in the case of applying prefabs this isn't possible. They are
                        // modified but we can only find what has been added/created as an "afterthought" when scooping up the dangling objects. In this special case we must insert the creation of those dangling objects to the front of the current undo operation's stack
                        // before they are referenced by any other entries on the undo stack
                        Undo.RegisterCreatedObjectUndoToFrontOfUndoQueue(danglingObject, actionName);
                        if (danglingObject is Component)
                        {
                            addedTypes.Add(danglingObject.GetType());
                        }

                        danglingObjects.RemoveAt(i);
                        i--;
                        emptyPass = false;
                    }
                }
            }

            Debug.Assert(danglingObjects.Count == 0, "Dangling components have unfulfilled dependencies");
            foreach (var component in danglingObjects)
            {
                Undo.RegisterCreatedObjectUndoToFrontOfUndoQueue(component, actionName);
            }
        }

        static void ThrowExceptionIfNotValidPrefabInstanceObject(Object prefabInstanceObject, bool isApply)
        {
            if (!(prefabInstanceObject is GameObject || prefabInstanceObject is Component))
                throw new ArgumentException("Calling apply or revert methods on an object which is not a GameObject or Component is not supported.", nameof(prefabInstanceObject));
            if (prefabInstanceObject == null)
                throw new NullReferenceException("Cannot apply or revert object. Object is null.");
            if (!PrefabUtility.IsPartOfPrefabInstance(prefabInstanceObject))
                throw new ArgumentException("Calling apply or revert methods on an object which is not part of a Prefab instance is not supported.", nameof(prefabInstanceObject));

            // We support revert operations on Prefab Assets, but not apply operations.
            if (isApply)
                ThrowExceptionIfInstanceIsPersistent(prefabInstanceObject);
        }

        static void ThrowExceptionIfAllPrefabInstanceObjectsAreInvalid(Object[] prefabInstanceObjects, bool isApply)
        {
            foreach (var obj in prefabInstanceObjects)
            {
                if (obj != null && (obj is GameObject || obj is Component) && IsPartOfPrefabInstance(obj) && !(isApply && EditorUtility.IsPersistent(obj)))
                    return;
            }

            // Throw exception if all objects are invalid
            throw new ArgumentException("Cannot apply or revert on any of the objects. Attempt with individual objects for details.", nameof(prefabInstanceObjects));
        }

        static void ThrowExceptionIfInstanceIsPersistent(Object prefabInstanceObject)
        {
            if (EditorUtility.IsPersistent(prefabInstanceObject))
                throw new ArgumentException("Calling apply methods on an instance which is part of a Prefab Asset is not supported.", nameof(prefabInstanceObject));
        }

        public static GameObject[] FindAllInstancesOfPrefab(GameObject prefabRoot)
        {
            return FindAllInstancesOfPrefab_internal(prefabRoot, 0);
        }

        public static GameObject[] FindAllInstancesOfPrefab(GameObject prefabRoot, Scene scene)
        {
            if (!scene.IsValid())
            {
                throw new ArgumentException("Input scene is not valid: Could not be found.");
            }

            return FindAllInstancesOfPrefab_internal(prefabRoot, scene.handle);
        }

        public static void MergePrefabInstance(GameObject instanceRoot)
        {
            MergePrefabInstance_internal(instanceRoot);
        }

        public static void RevertPrefabInstance(GameObject instanceRoot, InteractionMode action)
        {
            ThrowExceptionIfNotValidPrefabInstanceObject(instanceRoot, false);

            bool isDisconnected = PrefabUtility.IsDisconnectedFromPrefabAsset(instanceRoot);

            GameObject prefabInstanceRoot = GetOutermostPrefabInstanceRoot(instanceRoot);

            var actionName = "Revert Prefab Instance";
            HashSet<int> hierarchy = null;

            if (action == InteractionMode.UserAction)
            {
                hierarchy = new HashSet<int>();
                GetObjectListFromHierarchy(hierarchy, prefabInstanceRoot);
                Undo.RegisterFullObjectHierarchyUndo(prefabInstanceRoot, actionName);
            }

            RevertPrefabInstance_Internal(prefabInstanceRoot);

            if (action == InteractionMode.UserAction)
            {
                RegisterNewObjects(prefabInstanceRoot, hierarchy, actionName);
                if (isDisconnected)
                {
                    Undo.RegisterCreatedObjectUndo(GetPrefabInstanceHandle(prefabInstanceRoot), actionName);
                }
            }
        }

        public static void ApplyPrefabInstance(GameObject instanceRoot, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            ThrowExceptionIfNotValidPrefabInstanceObject(instanceRoot, true);

            using (new AtomicUndoScope())
            {
                GameObject prefabInstanceRoot = GetOutermostPrefabInstanceRoot(instanceRoot);
                var isDisconnected = GetPrefabInstanceHandle(prefabInstanceRoot) == null;

                var actionName = "Apply instance to prefab";
                Object correspondingSourceObject = GetCorrespondingObjectFromSource(prefabInstanceRoot);

                HashSet<int> prefabHierarchy = null;
                if (action == InteractionMode.UserAction)
                {
                    Undo.RegisterFullObjectHierarchyUndo(correspondingSourceObject, actionName); // handles changes to existing objects and object what will be deleted but not objects that are created
                    Undo.RegisterFullObjectHierarchyUndo(prefabInstanceRoot, actionName);

                    prefabHierarchy = new HashSet<int>();
                    GetObjectListFromHierarchy(prefabHierarchy, correspondingSourceObject as GameObject);
                }

                PrefabUtility.ApplyPrefabInstance(prefabInstanceRoot);

                if (action == InteractionMode.UserAction)
                {
                    RegisterNewObjects(correspondingSourceObject as GameObject, prefabHierarchy, actionName); // handles created objects
                    if (isDisconnected)
                    {
                        var prefabInstanceHandle = GetPrefabInstanceHandle(prefabInstanceRoot);
                        Assert.IsNotNull(prefabInstanceHandle);
                        Undo.RegisterCreatedObjectUndo(prefabInstanceHandle, actionName);
                    }
                }
            }

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.EntirePrefab,
                instanceRoot,
                null,
                action,
                startTime,
                false
            );
        }

        private static void MapObjectReferencePropertyToSourceIfApplicable(SerializedProperty property, string assetPath)
        {
            var referencedObject = property.objectReferenceValue;
            if (referencedObject == null)
            {
                return;
            }
            referencedObject = GetCorrespondingObjectFromSourceAtPath(referencedObject, assetPath);
            if (referencedObject != null)
            {
                property.objectReferenceValue = referencedObject;
            }
        }

        static bool WarnIfInAnimationMode(OverrideOperation overrideOperation, InteractionMode action)
        {
            if (!AnimationMode.InAnimationMode())
                return false;

            if (action == InteractionMode.AutomatedAction)
            {
                switch (overrideOperation)
                {
                    case OverrideOperation.Apply:
                        throw new InvalidOperationException("Cannot apply overriden properties in Animation Mode.");
                    case OverrideOperation.Revert:
                        throw new InvalidOperationException("Cannot revert overriden properties in Animation Mode.");
                }
            }
            else if (action == InteractionMode.UserAction)
            {
                var message = L10n.Tr("Overriden properties cannot be applied or reverted when in Animation Mode.\n\nDisable Animation Mode and try again.");
                switch (overrideOperation)
                {
                    case OverrideOperation.Apply:
                        EditorUtility.DisplayDialog(
                            L10n.Tr("Cannot apply property override to Prefab"),
                            message,
                            L10n.Tr("OK"));
                        break;
                    case OverrideOperation.Revert:
                        EditorUtility.DisplayDialog(
                            L10n.Tr("Cannot revert property override"),
                            message,
                            L10n.Tr("OK"));
                        break;
                }
            }
            return true;
        }

        public static void ApplyPropertyOverride(SerializedProperty instanceProperty, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            Object prefabInstanceObject = instanceProperty.serializedObject.targetObject;
            ThrowExceptionIfNotValidPrefabInstanceObject(prefabInstanceObject, true);

            if (WarnIfInAnimationMode(OverrideOperation.Apply, action))
                return;

            ApplyPropertyOverrides(prefabInstanceObject, instanceProperty, assetPath, true, action);

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.PropertyOverride,
                prefabInstanceObject,
                assetPath,
                action,
                startTime,
                IsPropertyOverrideDefaultOverrideComparedToAnySource(instanceProperty)
            );
        }

        // This method is called both from ApplyPropertyOverride and ApplyObjectOverride.
        // In the former case, optionalSingleInstanceProperty is passed along as is the only property that should be processed.
        // In the latter, all properties in the prefabInstanceObject are iterated.
        // An alternative approach was considered where the method takes an array of SerializedProperties,
        // but since there can be thousands of those in a component, and they would each have to be copied from the iterator,
        // it's better to handle properties inline as the iterator iterates over them.
        // This does mean that we need to cache the SerializedObjects that end up being touched.
        // Those are that of the prefabInstanceObject itself, and the chain of corresponding object
        // all the way to the Prefab at the specified assetPath.
        // Since calling ApplyModifiedProperties on a SerializedObject can trigger a whole chain of imports,
        // we don't want to create and call ApplyModifiedProperties more than once for each SerializedObject, hence the caching.
        // We also can't swap the inner and outer loop, iterating the chain of corresponding objects in the outer loop
        // and the SerializedProperties in the inner loop, since we only process overridden properties, so it would
        // again require storing information about that for each property in some kind of list, which we want to avoid.
        static void ApplyPropertyOverrides(Object prefabInstanceObject, SerializedProperty optionalSingleInstanceProperty, string assetPath, bool allowApplyDefaultOverride, InteractionMode action)
        {
            if (WarnIfInAnimationMode(OverrideOperation.Apply, action))
                return;

            Object prefabSourceObject = GetCorrespondingObjectFromSourceAtPath(prefabInstanceObject, assetPath);
            if (prefabSourceObject == null)
                return;

            SerializedObject prefabSourceSerializedObject = new SerializedObject(prefabSourceObject);

            // Cache SerializedObjects used.
            var serializedObjects = new List<SerializedObject>();
            var changedObjects = new HashSet<SerializedObject>();

            HandleApplySingleProperties(prefabInstanceObject, optionalSingleInstanceProperty, assetPath, prefabSourceObject, prefabSourceSerializedObject, serializedObjects, changedObjects, allowApplyDefaultOverride, action);

            // Ensure importing of saved Prefab Assets only kicks in after all Prefab Asset have been saved
            AssetDatabase.StartAssetEditing();

            try
            {
                Action<SerializedObject> saveIfChanged = (SerializedObject serializedObject) =>
                {
                    if (changedObjects.Contains(serializedObject))
                    {
                        bool applySuccess = action == InteractionMode.UserAction ? serializedObject.ApplyModifiedProperties() : serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        if (applySuccess)
                        {
                            SaveChangesToPrefabFileIfPersistent(serializedObject);

                            if (action == InteractionMode.UserAction)
                            {
                                Undo.FlushUndoRecordObjects(); // flush'es ensure that SavePrefab() on undo/redo on the source happens in the right order
                            }
                        }
                    }
                };

                // Case 1292519
                // The Prefab to which the changes are applied is added first in serializedObjects by ApplySingleProperty - it must be saved first
                // The rest of the objects are collected in reverse dependency order in ApplySingleProperty - starting from the instance where the changes are made down to the original Prefab
                // Apply the changes and save them in dependency order (reversed array order) to make sure dependent values are saved after the values they depend upon
                if (serializedObjects.Count > 0)
                {
                    saveIfChanged(serializedObjects[0]);
                    for (int i = serializedObjects.Count - 1; i > 0; i--)
                    {
                        saveIfChanged(serializedObjects[i]);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        static void HandleApplySingleProperties(
            Object prefabInstanceObject,
            SerializedProperty optionalSingleInstanceProperty,
            string assetPath, Object prefabSourceObject,
            SerializedObject prefabSourceSerializedObject,
            List<SerializedObject> serializedObjects,
            HashSet<SerializedObject> changedObjects,
            bool allowApplyDefaultOverride,
            InteractionMode action
            )
        {
            bool isObjectOnRootInAsset = IsObjectOnRootInAsset(prefabInstanceObject, assetPath);

            SerializedProperty property = null;
            SerializedProperty endProperty = null;
            if (optionalSingleInstanceProperty != null)
            {
                bool cancel;
                property = GetArrayPropertyIfGivenPropertyIsPartOfArrayElementInInstanceWhichDoesNotExistInAsset(
                    optionalSingleInstanceProperty,
                    prefabSourceSerializedObject,
                    action,
                    out cancel);

                if (cancel)
                    return;

                if (property == null)
                {
                    // We didn't find any mismatching array so just use the property the user supplied.
                    property = optionalSingleInstanceProperty.Copy();
                }
                endProperty = property.GetEndProperty();
            }
            else
            {
                // Note: Mismatching array sizes are not a problem when applying entire component,
                // since array sizes will always be applied before array content.
                SerializedObject so = new SerializedObject(prefabInstanceObject);
                property = so.GetIterator();
            }

            bool allowWarnAboutApplyingPartsOfManagedReferences = property.isReferencingAManagedReferenceField;

            if (!property.hasVisibleChildren)
            {
                if (property.prefabOverride)
                    ApplySinglePropertyAndRemoveOverride(property, prefabSourceSerializedObject, assetPath, isObjectOnRootInAsset, true, allowWarnAboutApplyingPartsOfManagedReferences, allowApplyDefaultOverride, serializedObjects, changedObjects, action, out _);
            }
            else
            {
                if (property.prefabOverride && property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    allowWarnAboutApplyingPartsOfManagedReferences = false; // we should always be allowed to apply a managed reference root
                    ApplySinglePropertyAndRemoveOverride(property, prefabSourceSerializedObject, assetPath, isObjectOnRootInAsset, false, allowWarnAboutApplyingPartsOfManagedReferences, allowApplyDefaultOverride, serializedObjects, changedObjects, action, out _);
                }

                var visitedManagedReferenceProperties = new HashSet<long>();
                bool visitChildren = property.hasVisibleChildren;

                while (property.Next(visitChildren) && (endProperty == null || !SerializedProperty.EqualContents(property, endProperty)))
                {
                    // If we apply a property that has child properties that are object references, and if they
                    // reference non-asset objects, those references will get lost, since ApplySingleProperty
                    // only patches up references in the provided property; not its children.
                    // This could be fixed by letting ApplySingleProperty patch up all its child properties as well,
                    // but then calling ApplySingleProperty n times would result in time complexity n*log(n).
                    // Instead we only call ApplySingleProperty for visible leaf properties - the ones that actually
                    // contain the data.
                    // Technically, leaf properties contain the data, but we're using visible leafs here, which
                    // corresponds to leaf nodes as shown in the Inspector. Note, this is not related to foldout
                    // expanded state; it's related to the fact that some nodes can have flags that hide them.
                    // Furthermore, special property types - like object references, which is what we're particularly
                    // interested in - are hardcoded to have their child nodes hidden. We need to call
                    // ApplySingleProperty on the object reference property and not its hidden children, so we use
                    // hasHiddenChildren, not hasChildren, to determine which properties to call the method on.
                    // Applying all visible leaf properties applies all data only once and ensures that when an
                    // object reference is applied, it's via its own property and not a parent property.

                    // NOTE: all property modifications are leafs except in the context of managed references.
                    // Managed references can be overriden (and have visible children).
                    bool isManagedReferenceRoot = property.propertyType == SerializedPropertyType.ManagedReference;
                    bool skipRestOfProperties = false;
                    if (property.prefabOverride && (isManagedReferenceRoot || !property.hasVisibleChildren))
                        ApplySinglePropertyAndRemoveOverride(property, prefabSourceSerializedObject, assetPath, isObjectOnRootInAsset, false, allowWarnAboutApplyingPartsOfManagedReferences, allowApplyDefaultOverride, serializedObjects, changedObjects, action, out skipRestOfProperties);

                    if (skipRestOfProperties)
                        break;

                    // Avoid cyclic mangaged references
                    if (isManagedReferenceRoot)
                    {
                        if (visitedManagedReferenceProperties.Add(property.managedReferenceId))
                            visitChildren = property.hasVisibleChildren; // First time seeing managed reference, so allow entering children if needed
                        else
                            visitChildren = false;
                    }
                    else
                    {
                        visitChildren = property.hasVisibleChildren;
                    }
                }
            }
        }

        static void SaveChangesToPrefabFileIfPersistent(SerializedObject serializedObject)
        {
            if (!EditorUtility.IsPersistent(serializedObject.targetObject))
                return;

            GameObject go = serializedObject.targetObject as GameObject;
            if (go == null)
            {
                var cmp = serializedObject.targetObject as Component;
                if (cmp != null)
                    go = cmp.gameObject;
            }

            if (go != null)
            {
                SavePrefabAsset(go.transform.root.gameObject);
            }
        }

        // Returns null if property is not part of array where whole array needs to be appled.
        static SerializedProperty GetArrayPropertyIfGivenPropertyIsPartOfArrayElementInInstanceWhichDoesNotExistInAsset(
            SerializedProperty optionalSingleInstanceProperty,
            SerializedObject prefabSourceSerializedObject,
            InteractionMode action,
            out bool cancel)
        {
            cancel = false;

            // Check if the property is part of an array with mismatching size,
            // and in that case make the property be that entire array.
            string propertyPath = optionalSingleInstanceProperty.propertyPath;
            int fullPropertyPathLength = optionalSingleInstanceProperty.propertyPath.Length;
            int startSearchIndex = 0;
            const string arrayElementIndexPrefix = ".Array.data[";
            // We need to handle nested arrays, hence the while loop.
            while (startSearchIndex < fullPropertyPathLength)
            {
                int arrayPropertySplitIndex = propertyPath.IndexOf(arrayElementIndexPrefix, startSearchIndex);

                // Break if no array was found in property path.
                if (arrayPropertySplitIndex < 0)
                    return null;

                // Find property path for array.
                string arrayPropertyPath = propertyPath.Substring(0, arrayPropertySplitIndex);

                // Find array element index start and length up front since we need it either way.
                int arrayElementIndexStart = arrayPropertySplitIndex + arrayElementIndexPrefix.Length;
                int arrayElementIndexLength = propertyPath.IndexOf(']', arrayElementIndexStart) - arrayElementIndexStart;

                // Check if found array has different length on instance than on Asset.
                SerializedProperty arrayPropertyOnInstance = optionalSingleInstanceProperty.serializedObject.FindProperty(arrayPropertyPath);
                SerializedProperty arrayPropertyOnAsset = prefabSourceSerializedObject.FindProperty(arrayPropertyPath);

                // With the ManagedReferences feature we no longer guarantee that the Prefab Asset will have the same property so we need to check
                // if it exists (could be a base class without any properties)
                if (arrayPropertyOnAsset == null)
                {
                    WarnIfApplyingManagedReferenceFieldIsNotPossible(optionalSingleInstanceProperty, null, action);
                    cancel = true;
                    return null;
                }

                if (arrayPropertyOnInstance.arraySize > arrayPropertyOnAsset.arraySize)
                {
                    // Array size mismatches. Check if the property the user is attempting to apply
                    // resides in an element which exceeds the array in the Prefab Asset.
                    string arrayElementIndexString = propertyPath.Substring(arrayElementIndexStart, arrayElementIndexLength);
                    int indexInArray;
                    if (!int.TryParse(arrayElementIndexString, out indexInArray))
                    {
                        // This should not be able to happen, but fallback by using array.
                        Debug.LogError("Misformed arrayElementIndexString " + arrayElementIndexString);
                        return arrayPropertyOnInstance;
                    }

                    if (indexInArray >= arrayPropertyOnAsset.arraySize)
                    {
                        if (action == InteractionMode.UserAction && !EditorUtility.DisplayDialog(
                            "Mismatching array size",
                            string.Format("The property is part of an array element which does not exist in the source Prefab because the corresponding array there is shorter. Do you want to apply the entire array '{0}'?", arrayPropertyOnInstance.displayName),
                            "Apply Array",
                            "Cancel"))
                        {
                            cancel = true;
                            return null;
                        }

                        return arrayPropertyOnInstance;
                    }
                }

                // Array does not mismatch, so look if there are other arrays (deeper in property path) that do.
                startSearchIndex = arrayPropertySplitIndex + arrayElementIndexPrefix.Length + arrayElementIndexLength + 1;
            }
            return null;
        }

        // Since method is called for each overridden property in a component.
        // That may be thousands of times if a component has lots of array data.
        // We provide as much information as possible to the method as parameters
        // so we don't have to recalculate it for each call.
        static void ApplySinglePropertyAndRemoveOverride(
            SerializedProperty instanceProperty,
            SerializedObject prefabSourceSerializedObject,
            string assetPath,
            bool isObjectOnRootInAsset,
            bool singlePropertyOnly,
            bool allowWarnAboutApplyingPartsOfManagedReferences,
            bool allowApplyDefaultOverride,
            List<SerializedObject> serializedObjects,
            HashSet<SerializedObject> changedObjects,
            InteractionMode action,
            out bool skipRestOfProperties)
        {
            skipRestOfProperties = false;

            if (!allowApplyDefaultOverride && isObjectOnRootInAsset && IsPropertyOverrideDefaultOverrideComparedToAnySource(instanceProperty))
            {
                if (singlePropertyOnly)
                {
                    // Neither of these will not happen from our own editor interface since we don't display
                    // any menus to apply for default-override properties in the first place.
                    if (action == InteractionMode.AutomatedAction)
                        Debug.LogWarning("Cannot apply default-override property, since it is protected from being applied or reverted.");
                    else
                        EditorUtility.DisplayDialog(
                            L10n.Tr("Cannot apply default-override property"),
                            L10n.Tr("Default-override properties are protected from being applied or reverted."),
                            L10n.Tr("OK"));
                }
                return;
            }

            SerializedProperty sourceProperty = prefabSourceSerializedObject.FindProperty(instanceProperty.propertyPath);
            if (sourceProperty == null)
            {
                // Special handling for arrays
                bool cancel;
                var instanceArrayProperty = GetArrayPropertyIfGivenPropertyIsPartOfArrayElementInInstanceWhichDoesNotExistInAsset(instanceProperty, prefabSourceSerializedObject, InteractionMode.AutomatedAction, out cancel);
                if (instanceArrayProperty != null)
                {
                    prefabSourceSerializedObject.CopyFromSerializedProperty(instanceArrayProperty);
                    changedObjects.Add(prefabSourceSerializedObject);

                    sourceProperty = prefabSourceSerializedObject.FindProperty(instanceProperty.propertyPath);
                    if (sourceProperty == null)
                    {
                        Debug.LogError($"ApplySingleProperty full array copy error: SerializedProperty could not be found for {instanceProperty.propertyPath}. Please report a bug.");
                        return;
                    }
                }
            }

            if (allowWarnAboutApplyingPartsOfManagedReferences && WarnIfApplyingManagedReferenceFieldIsNotPossible(instanceProperty, sourceProperty, action))
            {
                skipRestOfProperties = true;
                return;
            }

            if (sourceProperty == null)
            {
                // If we reach here we need to investigate the situation in which it happens and fix it
                Debug.LogError($"ApplySinglePropertyAndRemoveOverride error: Unhandled situation for {instanceProperty.propertyPath}. Please report a bug.");
                skipRestOfProperties = true;
                return;
            }

            // Copy overridden property value to asset
            if (instanceProperty.propertyType == SerializedPropertyType.ManagedReference)
            {
                // For a managed reference root property we assign the entire object reference value so we can handle if the Asset value is null from start, since in
                // this case we cannot use CopyFromSerializedProperty as we do for normal properties.
                sourceProperty.managedReferenceValue = instanceProperty.managedReferenceValue;
            }
            else
            {
                prefabSourceSerializedObject.CopyFromSerializedProperty(instanceProperty);
            }

            changedObjects.Add(prefabSourceSerializedObject);

            // Abort if property has reference to object in scene.
            if (sourceProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                MapObjectReferencePropertyToSourceIfApplicable(sourceProperty, assetPath);
                if (sourceProperty.objectReferenceValue != null && !EditorUtility.IsPersistent(sourceProperty.objectReferenceValue))
                {
                    // The property is a reference to a non-persistent object (scene object which could not be mapped to the asset.
                    // It can not be applied.

                    // Give a warning if the user tried to specifically apply this property.
                    if (singlePropertyOnly)
                    {
                        if (action == InteractionMode.AutomatedAction)
                            Debug.LogWarning("Cannot apply reference to scene object that is not part of apply target prefab.");
                        else
                            EditorUtility.DisplayDialog(
                                L10n.Tr("Cannot apply reference to object in scene"),
                                L10n.Tr("A reference to an object in the scene cannot be applied to the Prefab asset."),
                                L10n.Tr("OK"));
                    }
                    return;
                }
            }

            // Apply target SerializedObject should get ApplyModifiedProperties called first.
            if (serializedObjects.Count == 0)
                serializedObjects.Add(prefabSourceSerializedObject);

            // Clear overrides for property in Prefab instance and outer Prefabs that are using(nesting) the Prefab source.
            // Otherwise applied modification would appear to jump back to the value it had before applying.
            Object prefabInstanceObject = instanceProperty.serializedObject.targetObject;
            Object prefabSourceObject = prefabSourceSerializedObject.targetObject;
            Object outerPrefabObject = prefabInstanceObject;
            int sourceIndex = 1;
            while (outerPrefabObject != prefabSourceObject)
            {
                SerializedObject outerPrefabSerializedObject;
                if (sourceIndex >= serializedObjects.Count)
                {
                    outerPrefabSerializedObject = new SerializedObject(outerPrefabObject);
                    serializedObjects.Add(outerPrefabSerializedObject);
                }
                else
                {
                    outerPrefabSerializedObject = serializedObjects[sourceIndex];
                }

                // Case 1172835: When applying a new array size to an inner Prefab, this change won't yet have propagated to the outer Prefabs.
                // This means properties inside the array may not yet exist here.
                // To handle this, we first copy the serialized value (which correctly handles array size changes)
                // before we clear the overrides below (by setting outerPrefabProp.prefabOverride = false).
                var propertyType = instanceProperty.propertyType;
                if (propertyType == SerializedPropertyType.ArraySize)
                {
                    // Do not add outerPrefabSerializedObject to changedObjects
                    // CopyFromSerializedProperty is necessary to make "index" propertyPaths out of original bounds valid to be able to clear dangling prefabOverrides
                    // as noted in the comment for Case 1172835, but it should not be applied/saved if there are no prefabOverrides
                    outerPrefabSerializedObject.CopyFromSerializedProperty(instanceProperty);
                }

                SerializedProperty outerPrefabProp = outerPrefabSerializedObject.FindProperty(instanceProperty.propertyPath);
                if (outerPrefabProp != null && outerPrefabProp.prefabOverride)
                {
                    outerPrefabProp.prefabOverride = false;
                    changedObjects.Add(outerPrefabSerializedObject);
                }
                if (outerPrefabProp == null)
                    Debug.LogError($"ApplySingleProperty clear overrides error: SerializedProperty could not be found for {instanceProperty.propertyPath}. Please report a bug.");

                outerPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(outerPrefabObject);
                sourceIndex++;
            }
        }

        internal static void RevertPropertyOverrides(SerializedProperty[] instanceProperties, InteractionMode action)
        {
            if (WarnIfInAnimationMode(OverrideOperation.Revert, action))
                return;

            foreach (var property in instanceProperties)
                if (WarnIfRevertingManagedReferenceIsNotPossible(property, action))
                    return;

            foreach (var property in instanceProperties)
                RevertPropertyOverride(property, action);
        }

        public static void RevertPropertyOverride(SerializedProperty instanceProperty, InteractionMode action)
        {
            if (WarnIfInAnimationMode(OverrideOperation.Revert, action))
                return;

            ThrowExceptionIfAllPrefabInstanceObjectsAreInvalid(instanceProperty.serializedObject.targetObjects, false);

            if (WarnIfRevertingManagedReferenceIsNotPossible(instanceProperty, action))
                return;

            instanceProperty.prefabOverride = false;

            // Because prefabOverride changed ApplyModifiedProperties will do a prefab merge causing the revert.
            if (action == InteractionMode.UserAction)
                instanceProperty.serializedObject.ApplyModifiedProperties();
            else
                instanceProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool WarnIfApplyingManagedReferenceFieldIsNotPossible(SerializedProperty instanceProperty, SerializedProperty sourceProperty, InteractionMode action)
        {
            bool isReferencingAManagedReferenceField = instanceProperty.isReferencingAManagedReferenceField;
            if (!isReferencingAManagedReferenceField)
                return false;

            if (sourceProperty != null)
            {
                // Even if we find the propertyPath on the source object we also need to validate if this is a managed reference to the correct source object, except when
                // applying an entire object because then entire state is transfered to the asset and can merged back to the instance so here we don't show the dialog.
                bool applyingNotPossible = instanceProperty.managedReferencePropertyPath != sourceProperty.managedReferencePropertyPath;
                if (applyingNotPossible)
                {
                    string title = L10n.Tr("Mismatching Objects");
                    string errorMsg = L10n.Tr("Cannot apply SerializeReference field since the Prefab instance is referencing a new object compared to the Prefab Asset.\n\nThis means that the changes from the Prefab Asset cannot be merged back to the Prefab instance.\n\nYou can apply the root of the field or the entire component");
                    if (action == InteractionMode.AutomatedAction)
                        throw new InvalidOperationException(title + ": " + errorMsg + $"(instance property { instanceProperty.managedReferencePropertyPath}, asset property { sourceProperty.managedReferencePropertyPath})");
                    else
                        EditorUtility.DisplayDialog(
                            title,
                            errorMsg,
                            L10n.Tr("OK"));
                    return true;
                }
            }
            else
            {
                // Special handling for managed references.
                // If we have a valid managedReferencePropertyPath then the types must be different since we could not find the
                // propertyPath on the Prefab Asset, tell the user that this is not supported.
                if (!string.IsNullOrEmpty(instanceProperty.managedReferencePropertyPath))
                {
                    string title = L10n.Tr("Mismatching Types");
                    string errorMsg = L10n.Tr("Cannot apply a SerializeReference sub field when the type from the Prefab instance is different from the Prefab Asset.\n\nYou can apply the root of the field or the entire component");
                    if (action == InteractionMode.AutomatedAction)
                        throw new InvalidOperationException(title + ": " + errorMsg);
                    else
                        EditorUtility.DisplayDialog(
                            title,
                            errorMsg,
                            L10n.Tr("OK"));

                    return true;
                }
            }

            return false;
        }

        static bool WarnIfRevertingManagedReferenceIsNotPossible(SerializedProperty instanceProperty, InteractionMode action)
        {
            if (!instanceProperty.isReferencingAManagedReferenceField)
                return false;

            var instanceObject = instanceProperty.serializedObject.targetObject;
            var prefabSourceObject = GetCorrespondingObjectFromSource(instanceObject);
            SerializedObject prefabSourceSerializedObject = new SerializedObject(prefabSourceObject);
            SerializedProperty sourceProperty = prefabSourceSerializedObject.FindProperty(instanceProperty.propertyPath);

            string errorMsg = "";
            string title = "";
            if (sourceProperty != null)
            {
                // Object mismatch:
                bool revertingNotPossible = instanceProperty.managedReferencePropertyPath != sourceProperty.managedReferencePropertyPath;
                if (revertingNotPossible)
                {
                    title = L10n.Tr("Mismatching Objects");
                    errorMsg = L10n.Tr("Cannot revert a single SerializeReference field since the Prefab instance is referencing a new object compared to the Prefab Asset.\n\nThis means that the entire object is considered an override, cannot revert parts of it.\n\nYou can revert the root of the serialized reference or the entire component.");
                }
            }
            else
            {
                // Type mismatch:
                title = L10n.Tr("Mismatching Types");
                errorMsg = L10n.Tr("Cannot revert a parts of a SerializeReference property since the Prefab instance is referencing a different type compared to the Prefab Asset.\n\nYou can revert the root of the serialized reference or the entire component.");
            }

            bool warnUser = !string.IsNullOrEmpty(errorMsg);
            if (warnUser)
            {
                if (action == InteractionMode.AutomatedAction)
                {
                    throw new InvalidOperationException(title + ": " + errorMsg);
                }
                else if (action == InteractionMode.UserAction)
                {
                    EditorUtility.DisplayDialog(title, errorMsg, L10n.Tr("OK"));
                }
            }

            return warnUser;
        }

        public static void ApplyObjectOverride(Object instanceComponentOrGameObject, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            ThrowExceptionIfNotValidPrefabInstanceObject(instanceComponentOrGameObject, true);

            if (WarnIfInAnimationMode(OverrideOperation.Apply, action))
                return;

            ApplyPropertyOverrides(instanceComponentOrGameObject, null, assetPath, false, action);

            if (action == InteractionMode.UserAction)
            {
                Component instanceComponent = instanceComponentOrGameObject as Component;
                if (instanceComponent != null)
                {
                    Component coupledComponent = instanceComponent.GetCoupledComponent();
                    if (coupledComponent != null)
                        ApplyPropertyOverrides(coupledComponent, null, assetPath, false, action);
                }
            }

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.ObjectOverride,
                instanceComponentOrGameObject,
                assetPath,
                action,
                startTime,
                IsObjectOverrideAllDefaultOverridesComparedToOriginalSource(instanceComponentOrGameObject)
            );
        }

        public static void RevertObjectOverride(Object instanceComponentOrGameObject, InteractionMode action)
        {
            ThrowExceptionIfNotValidPrefabInstanceObject(instanceComponentOrGameObject, false);

            if (WarnIfInAnimationMode(OverrideOperation.Revert, action))
                return;

            if (action == InteractionMode.UserAction)
                Undo.RegisterCompleteObjectUndo(instanceComponentOrGameObject, "Revert component property overrides");
            PrefabUtility.RevertObjectOverride_Internal(instanceComponentOrGameObject);

            if (action == InteractionMode.UserAction)
            {
                Component instanceComponent = instanceComponentOrGameObject as Component;
                if (instanceComponent != null)
                {
                    Component coupledComponent = instanceComponent.GetCoupledComponent();
                    if (coupledComponent != null)
                    {
                        Undo.RegisterCompleteObjectUndo(coupledComponent, "Revert component property overrides");
                        PrefabUtility.RevertObjectOverride_Internal(coupledComponent);
                    }
                }
            }
        }

        public static void ApplyAddedComponent(Component component, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            if (component == null)
                throw new ArgumentNullException(nameof(component), "Cannot apply added component. Component is null.");

            if (!PrefabUtility.IsAddedComponentOverride(component))
                throw new ArgumentException("Cannot apply added component. Component is not an added component override on a Prefab instance.", nameof(component));

            ThrowExceptionIfInstanceIsPersistent(component);

            try
            {
                GameObject prefabSourceGameObject = GetCorrespondingObjectFromSourceAtPath(component.gameObject, assetPath);
                if (prefabSourceGameObject == null)
                    return;

                var actionName = "Apply Added Component";
                if (action == InteractionMode.UserAction)
                {
                    string dependentComponents = string.Join(
                        ", ",
                        GetAddedComponentDependencies(component, OverrideOperation.Apply).Select(e => ObjectNames.GetInspectorTitle(e)).ToArray());
                    if (!string.IsNullOrEmpty(dependentComponents))
                    {
                        string error = String.Format(
                            L10n.Tr("Can't apply added component {0} because it depends on {1}."),
                            ObjectNames.GetInspectorTitle(component),
                            dependentComponents);
                        EditorUtility.DisplayDialog(L10n.Tr("Can't apply added component"), error, L10n.Tr("OK"));
                        return;
                    }

                    Undo.RegisterFullObjectHierarchyUndo(prefabSourceGameObject, actionName);
                    Undo.RegisterFullObjectHierarchyUndo(component, actionName);
                }

                PrefabUtility.ApplyAddedComponent(component, prefabSourceGameObject);

                if (action == InteractionMode.UserAction)
                {
                    Undo.RegisterCreatedObjectUndo(GetCorrespondingObjectFromOriginalSource(component), actionName);

                    var coupledComponent = component.GetCoupledComponent();
                    if (coupledComponent != null)
                    {
                        PrefabUtility.ApplyAddedComponent(coupledComponent, prefabSourceGameObject);
                        Undo.RegisterCreatedObjectUndo(GetCorrespondingObjectFromOriginalSource(coupledComponent), actionName);
                    }
                }
            }
            catch (ArgumentException exception)
            {
                if (action == InteractionMode.UserAction)
                {
                    EditorUtility.DisplayDialog(
                        L10n.Tr("Can't add component"),
                        exception.Message,
                        L10n.Tr("OK"));
                    Undo.RevertAllInCurrentGroup();
                }
                else
                {
                    throw exception;
                }
            }

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.AddedComponent,
                component,
                assetPath,
                action,
                startTime,
                false
            );
        }

        public static void RevertAddedComponent(Component component, InteractionMode action)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component), "Cannot revert added component. Component is null.");

            if (!PrefabUtility.IsAddedComponentOverride(component))
                throw new ArgumentException("Cannot revert added component. Component is not an added component override on a Prefab instance.", nameof(component));

            var prefabInstanceGameObject = component.gameObject;

            if (action == InteractionMode.UserAction)
            {
                string dependentComponents = string.Join(
                    ", ",
                    GetAddedComponentDependencies(component, OverrideOperation.Revert).Select(e => ObjectNames.GetInspectorTitle(e)).ToArray());
                if (!string.IsNullOrEmpty(dependentComponents))
                {
                    string error = String.Format(
                        L10n.Tr("Can't revert added component {0} because {1} depends on it."),
                        ObjectNames.GetInspectorTitle(component),
                        dependentComponents);
                    EditorUtility.DisplayDialog(L10n.Tr("Can't revert added component"), error, L10n.Tr("OK"));
                    return;
                }

                var coupledComponent = component.GetCoupledComponent();

                Undo.DestroyObjectImmediate(component);
                if (coupledComponent != null)
                    Undo.DestroyObjectImmediate(coupledComponent);
            }
            else
                Object.DestroyImmediate(component);

            // Remerge the prefab instance to make any suppressed components show up if no longer suppressed
            PrefabUtility.MergePrefabInstance_internal(prefabInstanceGameObject);
        }

        private static bool IsPrefabInstanceObjectOf(Object instance, Object source)
        {
            var o = instance;
            while (o != null)
            {
                if (o == source)
                {
                    return true;
                }

                if (GetPrefabAssetHandle(o) == source)
                {
                    return true;
                }

                o = GetCorrespondingObjectFromSource(o);
            }
            return false;
        }

        private static void RemoveRemovedComponentOverridesWhichAreNull(Object prefabInstanceObject)
        {
            var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstanceObject);
            var filteredRemovedComponents = (from c in removedComponents where c != null select c).ToArray();
            PrefabUtility.SetRemovedComponents(prefabInstanceObject, filteredRemovedComponents);
        }

        // We can't use the same pattern of identifyiong the prefab asset via assetPath only,
        // since when the component is removed in the instance, the only way to identify which component it is,
        // is via the corresponding component on the asset. Additionally supplying an assetPath would be redundant.
        public static void ApplyRemovedComponent(GameObject instanceGameObject, Component assetComponent, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            ThrowExceptionIfNotValidPrefabInstanceObject(instanceGameObject, true);

            if (assetComponent == null)
                throw new ArgumentNullException(nameof(assetComponent), "Prefab source may not be null.");

            var actionName = "Apply Prefab removed component";

            if (action == InteractionMode.UserAction)
            {
                string dependentComponents = string.Join(
                    ", ",
                    GetRemovedComponentDependencies(assetComponent, instanceGameObject, OverrideOperation.Apply).Select(e => ObjectNames.GetInspectorTitle(e)).ToArray());
                if (!string.IsNullOrEmpty(dependentComponents))
                {
                    string error = String.Format(
                        L10n.Tr("Can't apply removed component {0} because {1} component in the Prefab Asset depends on it."),
                        ObjectNames.GetInspectorTitle(assetComponent),
                        dependentComponents);
                    EditorUtility.DisplayDialog(L10n.Tr("Can't apply removed component"), error, L10n.Tr("OK"));
                    return;
                }

                var coupledAssetComponent = assetComponent.GetCoupledComponent();

                Undo.DestroyObjectUndoable(assetComponent, actionName);
                // Undo.DestroyObjectUndoable saves prefab asset internally.

                if (coupledAssetComponent != null)
                    Undo.DestroyObjectUndoable(coupledAssetComponent, actionName);
            }
            else
            {
                GameObject prefabAsset = assetComponent.transform.root.gameObject;
                Object.DestroyImmediate(assetComponent, true);
                SavePrefabAsset(prefabAsset);
            }

            var prefabInstanceObject = PrefabUtility.GetPrefabInstanceHandle(instanceGameObject);

            if (action == InteractionMode.UserAction)
                Undo.RegisterCompleteObjectUndo(prefabInstanceObject, actionName);

            RemoveRemovedComponentOverridesWhichAreNull(prefabInstanceObject);

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.RemovedComponent,
                instanceGameObject,
                AssetDatabase.GetAssetPath(assetComponent),
                action,
                startTime,
                false
            );
        }

        private static void RemoveRemovedComponentOverride(Object instanceObject, Component assetComponent)
        {
            var removedComponents = PrefabUtility.GetRemovedComponents(instanceObject);
            int index = -1;
            for (int i = 0; i < removedComponents.Length; i++)
            {
                if (IsPrefabInstanceObjectOf(removedComponents[i], assetComponent))
                {
                    index = i;
                    break;
                }
            }
            if (index != -1)
            {
                var filteredRemovedComponents = (from c in removedComponents where c != removedComponents[index] select c).ToArray();
                PrefabUtility.SetRemovedComponents(instanceObject, filteredRemovedComponents);
            }
        }

        public static void RevertRemovedComponent(GameObject instanceGameObject, Component assetComponent, InteractionMode action)
        {
            ThrowExceptionIfNotValidPrefabInstanceObject(instanceGameObject, false);

            var actionName = "Revert Prefab removed component";
            var prefabInstanceObject = PrefabUtility.GetPrefabInstanceHandle(instanceGameObject);

            if (action == InteractionMode.UserAction)
            {
                string dependentComponents = string.Join(
                    ", ",
                    GetRemovedComponentDependencies(assetComponent, instanceGameObject, OverrideOperation.Revert).Select(e => ObjectNames.GetInspectorTitle(e)).ToArray());
                if (!string.IsNullOrEmpty(dependentComponents))
                {
                    string error = String.Format(
                        L10n.Tr("Can't revert removed component {0} because it depends on {1}."),
                        ObjectNames.GetInspectorTitle(assetComponent),
                        dependentComponents);
                    EditorUtility.DisplayDialog(L10n.Tr("Can't revert removed component"), error, L10n.Tr("OK"));
                    return;
                }

                Undo.RegisterCompleteObjectUndo(instanceGameObject, actionName);
            }

            RemoveRemovedComponentOverride(prefabInstanceObject, assetComponent);

            if (action == InteractionMode.UserAction)
            {
                foreach (var component in instanceGameObject.GetComponents<Component>())
                {
                    if (IsPrefabInstanceObjectOf(component, assetComponent))
                    {
                        Undo.RegisterCreatedObjectUndo(component, actionName);
                        break;
                    }
                }

                var coupledAssetComponent = assetComponent.GetCoupledComponent();
                if (coupledAssetComponent != null)
                {
                    RemoveRemovedComponentOverride(prefabInstanceObject, coupledAssetComponent);
                    foreach (var component in instanceGameObject.GetComponents<Component>())
                    {
                        if (IsPrefabInstanceObjectOf(component, coupledAssetComponent))
                        {
                            Undo.RegisterCreatedObjectUndo(component, actionName);
                            break;
                        }
                    }
                }
            }
        }

        public static void ApplyAddedGameObject(GameObject gameObject, string assetPath, InteractionMode action)
        {
            ApplyAddedGameObjects(new GameObject[] { gameObject}, assetPath, action);
        }

        public static void ApplyAddedGameObjects(GameObject[] gameObjects, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            if (gameObjects == null)
                throw new ArgumentNullException(nameof(gameObjects), "Cannot apply added GameObjects. GameObjects array is null.");

            if (!gameObjects.Any())
                throw new ArgumentException(nameof(gameObjects), "No GameObjects in array.");

            foreach (GameObject go in gameObjects)
            {
                if (go == null)
                    throw new ArgumentException(nameof(go), "Input GameObject is null.");

                if (!IsAddedGameObjectOverride(go))
                    throw new ArgumentException(nameof(go), $"Cannot apply added GameObject. GameObject '{go.name}' is not an added GameObject override on a Prefab instance.");

                ThrowExceptionIfInstanceIsPersistent(go);
            }

            if (gameObjects.Length > 1 && !HasSameParent(gameObjects))
                throw new ArgumentException(nameof(gameObjects), "ApplyAddedGameObjects requires that GameObjects share the same parent.");

            GameObject gameObject = gameObjects[0];
            Transform instanceParent = gameObject.transform.parent;
            if (instanceParent == null)
                return;

            GameObject prefabSourceGameObjectParent = GetCorrespondingObjectFromSourceAtPath(instanceParent.gameObject, assetPath);
            if (prefabSourceGameObjectParent == null)
                return;

            var instanceRoot = GetOutermostPrefabInstanceRoot(instanceParent);
            if (instanceRoot == null)
                return;

            var sourceRoot = prefabSourceGameObjectParent.transform.root.gameObject;

            var actionName = "Apply Added GameObject";
            if (action == InteractionMode.UserAction)
            {
                Undo.RegisterFullObjectHierarchyUndo(sourceRoot, actionName);
                Undo.RegisterFullObjectHierarchyUndo(instanceRoot, actionName);
            }

            AddGameObjectsToPrefabAndConnect(gameObjects, prefabSourceGameObjectParent);

            SavePrefabAsset(sourceRoot);

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject go = gameObjects[i];

                if (action == InteractionMode.UserAction)
                {
                    var createdAssetObject = GetCorrespondingObjectFromSourceInAsset(go, prefabSourceGameObjectParent);
                    if (createdAssetObject != null)
                    {
                        Undo.RegisterCreatedObjectUndoToFrontOfUndoQueue(createdAssetObject, actionName);
                    }
                }

                Analytics.SendApplyEvent(
                    Analytics.ApplyScope.AddedGameObject,
                    instanceRoot,
                    assetPath,
                    action,
                    startTime,
                    false
                );
            }

            EditorUtility.ForceRebuildInspectors();
        }

        internal static bool HasSameParent(GameObject[] gameObjects)
        {
            if (gameObjects == null || !gameObjects.Any() || gameObjects[0] == null)
                throw new ArgumentException(nameof(gameObjects), "Array is invalid.");

            Transform goParent = gameObjects[0].transform.parent;

            if (goParent == null)
                throw new ArgumentException(nameof(goParent), "Object is a parentless root.");

            foreach (GameObject go in gameObjects)
            {
                if (go.transform.parent != goParent)
                    return false;
            }

            return true;
        }

        public static void RevertAddedGameObject(GameObject gameObject, InteractionMode action)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject), "Cannot revert added GameObject. GameObject is null.");

            if (!IsAddedGameObjectOverride(gameObject))
                throw new ArgumentException("Cannot apply added GameObject. GameObject is not an added GameObject override on a Prefab instance.", nameof(gameObject));

            ThrowExceptionIfInstanceIsPersistent(gameObject);

            if (action == InteractionMode.UserAction)
                Undo.DestroyObjectImmediate(gameObject);
            else
                Object.DestroyImmediate(gameObject);
        }

        public static List<ObjectOverride> GetObjectOverrides(GameObject prefabInstance, bool includeDefaultOverrides = false)
        {
            return PrefabOverridesUtility.GetObjectOverrides(prefabInstance, includeDefaultOverrides);
        }

        public static List<AddedComponent> GetAddedComponents(GameObject prefabInstance)
        {
            return PrefabOverridesUtility.GetAddedComponents(prefabInstance);
        }

        public static List<RemovedComponent> GetRemovedComponents(GameObject prefabInstance)
        {
            return PrefabOverridesUtility.GetRemovedComponents(prefabInstance);
        }

        public static List<AddedGameObject> GetAddedGameObjects(GameObject prefabInstance)
        {
            return PrefabOverridesUtility.GetAddedGameObjects(prefabInstance);
        }

        internal static void HandleApplyRevertMenuItems(
            string thingThatChanged,
            Object instanceObject,
            Action<GUIContent, Object> addApplyMenuItemAction,
            Action<GUIContent> addRevertMenuItemAction,
            bool isAllDefaultOverridesComparedToOriginalSource = false,
            int targetCount = 1)
        {
            if (targetCount == 1)
                HandleApplyMenuItems(thingThatChanged, instanceObject, addApplyMenuItemAction, isAllDefaultOverridesComparedToOriginalSource);

            HandleRevertMenuItem(thingThatChanged, addRevertMenuItemAction);
        }

        internal static void HandleApplyMenuItems(
            string thingThatChanged,
            Object instanceOrAssetObject,
            Action<GUIContent, Object> addApplyMenuItemAction,
            bool isAllDefaultOverridesComparedToOriginalSource = false,
            bool includeSelfAsTarget = false)
        {
            // If thingThatChanged word is empty, apply menu items directly into menu.
            // Otherwise, insert as sub-menu named after thingThatChanged.
            if (thingThatChanged == null)
                thingThatChanged = String.Empty;
            if (thingThatChanged != String.Empty)
                thingThatChanged += "/";

            List<Object> applyTargets = GetApplyTargets(instanceOrAssetObject, isAllDefaultOverridesComparedToOriginalSource, includeSelfAsTarget);
            if (applyTargets == null || applyTargets.Count == 0)
                return;

            for (int i = 0; i < applyTargets.Count; i++)
            {
                Object source = applyTargets[i];
                GameObject sourceRoot = GetRootGameObject(source);

                var translatedText = L10n.Tr("Apply as Override in Prefab '{0}'");
                if (i == applyTargets.Count - 1)
                    translatedText = L10n.Tr("Apply to Prefab '{0}'");
                GUIContent applyContent = new GUIContent(thingThatChanged + String.Format(translatedText, sourceRoot.name));
                addApplyMenuItemAction(applyContent, source);
            }
        }

        internal static void HandleRevertMenuItem(
            string thingThatChanged,
            Action<GUIContent> addRevertMenuItemAction)
        {
            // If thingThatChanged word is empty, apply menu items directly into menu.
            // Otherwise, insert as sub-menu named after thingThatChanged.
            if (thingThatChanged == null)
                thingThatChanged = String.Empty;
            if (thingThatChanged != String.Empty)
                thingThatChanged += "/";

            GUIContent revertContent = new GUIContent(thingThatChanged + L10n.Tr("Revert"));
            addRevertMenuItemAction(revertContent);
        }

        private static Object GetParentPrefabInstance(GameObject gameObject)
        {
            var parent = gameObject.transform.parent;
            if (parent == null)
            {
                return null;
            }
            return GetPrefabInstanceHandle(parent);
        }

        internal static GameObject GetGameObject(Object componentOrGameObject)
        {
            GameObject go = componentOrGameObject as GameObject;
            if (go != null)
                return go;

            Component comp = componentOrGameObject as Component;
            if (comp != null)
                return comp.gameObject;

            return null;
        }

        internal static GameObject GetRootGameObject(Object componentOrGameObject)
        {
            GameObject go = GetGameObject(componentOrGameObject);
            if (go == null)
                return null;

            return go.transform.root.gameObject;
        }

        public static bool IsAnyPrefabInstanceRoot(GameObject gameObject)
        {
            GameObject prefabInstanceRoot = GetNearestPrefabInstanceRoot(gameObject);
            return (gameObject == prefabInstanceRoot);
        }

        public static bool IsOutermostPrefabInstanceRoot(GameObject gameObject)
        {
            GameObject prefabInstanceRoot = GetOutermostPrefabInstanceRoot(gameObject);
            return (gameObject == prefabInstanceRoot);
        }

        public static string GetPrefabAssetPathOfNearestInstanceRoot(Object instanceComponentOrGameObject)
        {
            return AssetDatabase.GetAssetPath(GetOriginalSourceOrVariantRoot(instanceComponentOrGameObject));
        }

        public static Texture2D GetIconForGameObject(GameObject gameObject)
        {
            if (IsAnyPrefabInstanceRoot(gameObject))
            {
                if (IsPrefabAssetMissing(gameObject))
                    return GameObjectStyles.prefabIcon;

                string assetPath = GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                return (Texture2D)AssetDatabase.GetCachedIcon(assetPath);
            }
            else
            {
                return GameObjectStyles.gameObjectIcon;
            }
        }

        [Obsolete("Use GetCorrespondingObjectFromSource.")]
        public static Object GetPrefabParent(Object obj)
        {
            return GetCorrespondingObjectFromSource(obj);
        }

        // Creates an empty prefab at given path.
        [Obsolete("The concept of creating a completely empty Prefab has been discontinued. You can however use SaveAsPrefabAsset with an empty GameObject.")]
        public static Object CreateEmptyPrefab(string path)
        {
            // This is here to simulate previous behaviour
            if (Path.GetExtension(path) != ".prefab")
            {
                Debug.LogError("Create Prefab path must use .prefab extension");
                return null;
            }

            var empty = new GameObject("EmptyPrefab");
            bool success;
            var assetObject = SaveAsPrefabAsset(empty, path, out success);
            Object.DestroyImmediate(empty);
            return PrefabUtility.GetPrefabObject(assetObject);
        }

        public static GameObject SavePrefabAsset(GameObject asset)
        {
            bool savedSuccesfully;
            return SavePrefabAsset(asset, out savedSuccesfully);
        }

        public static GameObject SavePrefabAsset(GameObject asset, out bool savedSuccessfully)
        {
            if (asset == null)
                throw new ArgumentNullException("Parameter prefabAssetGameObject is null");

            // Include model check even though models are also immutable, since we can give a more clear exception message.
            if (IsPartOfModelPrefab(asset))
                throw new ArgumentException("Can't save a Model Prefab");

            if (IsPartOfImmutablePrefab(asset))
                throw new ArgumentException("Can't save an immutable Prefab");

            string path = AssetDatabase.GetAssetPath(asset);
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException("Can't save a Prefab instance");

            var root = asset.transform.root.gameObject;
            if (root != asset)
                throw new ArgumentException("GameObject to save Prefab from must be a Prefab root");

            return SavePrefabAsset_Internal(root, out savedSuccessfully);
        }

        internal static void ValidatePath(GameObject instanceRoot, string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path is null or empty");

            if (!Paths.IsValidAssetPath(path, ".prefab"))
                throw new ArgumentException("Given path is not valid: '" + path + "'");

            if (Directory.Exists(path))
                throw new ArgumentException("Overwriting a folder with an Asset is not allowed: '" + path + "'");

            string directory = Path.GetDirectoryName(path);

            // We allow relative paths outside the Assets folder so we do not throw if isValidAssetFolder is false
            bool isRootFolder = false;
            bool isImmutableFolder = false;
            bool isValidAssetFolder = AssetDatabase.GetAssetFolderInfo(directory, out isRootFolder, out isImmutableFolder);

            if (isValidAssetFolder && isImmutableFolder)
                throw new ArgumentException("Saving Prefab to immutable folder is not allowed: '" + path + "'");

            if (directory.Length > 0 && !Directory.Exists(directory))
                throw new ArgumentException("Given path does not exist: '" + path + "'");

            if (isValidAssetFolder)
            {
                string projectRelativePath = Path.IsPathRooted(path) ? FileUtil.GetProjectRelativePath(path) : path;
                string prefabGUID = AssetDatabase.AssetPathToGUID(projectRelativePath);
                if (!VerifyNestingFromScript(new GameObject[] { instanceRoot }, prefabGUID, PrefabUtility.GetPrefabInstanceHandle(instanceRoot)))
                    throw new ArgumentException("Cyclic nesting detected");
            }
        }

        private static void SaveAsPrefabAssetArgumentCheck(GameObject instanceRoot, string path)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException("Parameter root is null");

            if (EditorUtility.IsPersistent(instanceRoot))
                throw new ArgumentException("Can't save persistent object as a Prefab asset");

            if (IsPrefabAssetMissing(instanceRoot))
                throw new ArgumentException("Can't save Prefab instance with missing asset as a Prefab. You may unpack the instance and save the unpacked GameObjects as a Prefab.");

            var actualInstanceRoot = GetOutermostPrefabInstanceRoot(instanceRoot);
            if (actualInstanceRoot)
            {
                if (actualInstanceRoot != instanceRoot)
                    throw new ArgumentException("Can't save part of a Prefab instance as a Prefab");
            }

            ValidatePath(instanceRoot, path);
        }

        private static void ReplacePrefabArgumentCheck(GameObject root, string path)
        {
            if (root == null)
                throw new ArgumentNullException("Parameter root is null");

            ValidatePath(root, path);
        }

        [RequiredByNativeCode]
        private static GameObject ReplacePrefabAndRegisterUndo_Internal(GameObject instanceRoot, GameObject existingPrefabRoot, bool connectedAfterwards, string actionName)
        {
            if (existingPrefabRoot == null)
            {
                throw new ArgumentNullException("Parameter existingPrefabRoot is null");
            }

            if (!IsPartOfPrefabAsset(existingPrefabRoot))
            {
                throw new ArgumentException("Parameter existingPrefabRoot is not a Prefab asset");
            }

            string assetPath = AssetDatabase.GetAssetPath(existingPrefabRoot);
            ReplacePrefabArgumentCheck(instanceRoot, assetPath);

            HashSet<int> hierarchy = new HashSet<int>();
            GetObjectListFromHierarchy(hierarchy, existingPrefabRoot);
            Undo.RegisterFullObjectHierarchyUndo(existingPrefabRoot, actionName);

            bool success;
            var prefabInstanceRoot = SavePrefab_Internal(instanceRoot, assetPath, connectedAfterwards, out success);
            if (!success)
            {
                return null;
            }

            RegisterNewObjects(prefabInstanceRoot, hierarchy, actionName);
            Undo.RegisterCreatedObjectUndo(prefabInstanceRoot, actionName);

            return prefabInstanceRoot;
        }

        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath, out bool success)
        {
            SaveAsPrefabAssetArgumentCheck(instanceRoot, assetPath);

            return SaveAsPrefabAsset_Internal(instanceRoot, assetPath, out success);
        }

        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath)
        {
            bool success;
            return SaveAsPrefabAsset(instanceRoot, assetPath, out success);
        }

        public static GameObject SaveAsPrefabAssetAndConnect(GameObject instanceRoot, string assetPath, InteractionMode action)
        {
            bool success;
            return SaveAsPrefabAssetAndConnect(instanceRoot, assetPath, action, out success);
        }

        public static GameObject SaveAsPrefabAssetAndConnect(GameObject instanceRoot, string assetPath, InteractionMode action, out bool success)
        {
            SaveAsPrefabAssetArgumentCheck(instanceRoot, assetPath);

            var actionName = "Connect to Prefab";

            if (action == InteractionMode.UserAction)
            {
                Undo.RegisterFullObjectHierarchyUndo(instanceRoot, actionName);
            }

            var assetRoot = SaveAsPrefabAssetAndConnect_Internal(instanceRoot, assetPath, out success);

            if (!success)
            {
                return null;
            }

            if (action == InteractionMode.UserAction)
            {
                Undo.RecordCreatedObject(GetPrefabInstanceHandle(instanceRoot), actionName);
            }

            return assetRoot;
        }

        internal static void ApplyPrefabInstance(GameObject instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            // Include model check even though models are also immutable, since we can give a more clear exception message.
            if (IsPartOfModelPrefab(instance))
                throw new ArgumentException("Can't apply to a Model Prefab");

            if (IsPartOfImmutablePrefab(instance))
                throw new ArgumentException("Can't apply to an immutable Prefab");

            if (!IsPartOfNonAssetPrefabInstance(instance))
                throw new ArgumentException("Provided GameObject is not a Prefab instance");

            if (IsDisconnectedFromPrefabAsset(instance))
            {
                // The concept of disconnecting are being deprecated. For now use FindRootGameObjectWithSameParentPrefab
                // to re-connect existing disconnected prefabs.
                var validRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(instance);
                var ok = validRoot == instance;
                if (!ok && PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance) != PrefabUtility.GetCorrespondingObjectFromSource(instance))
                    throw new ArgumentException("Can't save Prefab from an object that originates from a nested Prefab");
            }
            else
            {
                var root = GetOutermostPrefabInstanceRoot(instance);
                if (root != instance)
                    throw new ArgumentException("GameObject to save Prefab from must be a Prefab root");
            }

            var assetObject = GetCorrespondingObjectFromSource(instance);
            string path = AssetDatabase.GetAssetPath(assetObject);

            SaveAsPrefabAssetArgumentCheck(instance, path);

            ApplyPrefabInstance_Internal(instance);
        }

        // Can't use UnityUpgradable since it doesn't currently support swapping parameter order.
        [Obsolete("Use SaveAsPrefabAsset instead.")]
        public static GameObject CreatePrefab(string path, GameObject go)
        {
            return SaveAsPrefabAsset(go, path);
        }

        [Obsolete("Use SaveAsPrefabAsset or SaveAsPrefabAssetAndConnect instead.")]
        public static GameObject CreatePrefab(string path, GameObject go, ReplacePrefabOptions options)
        {
            if ((options & ReplacePrefabOptions.ConnectToPrefab) != 0)
                return SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            else
                return SaveAsPrefabAsset(go, path);
        }

        // Instantiates the given prefab.
        public static Object InstantiatePrefab(Object assetComponentOrGameObject)
        {
            return InstantiatePrefab_internal(assetComponentOrGameObject, EditorSceneManager.GetTargetSceneForNewGameObjects(), null);
        }

        // Instantiates the given prefab in a given scene
        public static Object InstantiatePrefab(Object assetComponentOrGameObject, Scene destinationScene)
        {
            return InstantiatePrefab_internal(assetComponentOrGameObject, destinationScene, null);
        }

        public static Object InstantiatePrefab(Object assetComponentOrGameObject, Transform parent)
        {
            return InstantiatePrefab_internal(assetComponentOrGameObject, EditorSceneManager.GetTargetSceneForNewGameObjects(), parent);
        }

        [Obsolete("Use SaveAsPrefabAsset with a path instead.")]
        public static GameObject ReplacePrefab(GameObject go, Object targetPrefab)
        {
            return ReplacePrefab(go, targetPrefab, ReplacePrefabOptions.Default);
        }

        [Obsolete("Use SaveAsPrefabAsset or SaveAsPrefabAssetAndConnect with a path instead.")]
        public static GameObject ReplacePrefab(GameObject go, Object targetPrefab, ReplacePrefabOptions replaceOptions)
        {
            var targetPrefabObject = PrefabUtility.GetPrefabObject(targetPrefab);

            // Previously ReplacePrefab didn't throw any exceptions
            // This reimplements the previous error handling
            if (targetPrefabObject == null)
            {
                Debug.LogError("The object you are trying to replace does not exist or is not a Prefab.");
                return null;
            }

            if (!EditorUtility.IsPersistent(targetPrefabObject))
            {
                Debug.LogError("The Prefab you are trying to replace is not a Prefab Asset but a Prefab instance. Please use PrefabUtility.GetCorrespondingObject().", targetPrefab);
                return null;
            }

            if (HideFlags.DontSaveInEditor == (HideFlags.DontSaveInEditor & go.hideFlags))
            {
                Debug.LogError("The root GameObject of the Prefab source cannot be marked with DontSaveInEditor as it would create an empty Prefab.", go);
                return null;
            }

            // Make sure the source object is not the same as the target prefab
            Object sourcePrefab = PrefabUtility.GetPrefabObject(go);
            if (sourcePrefab != null)
            {
                if (sourcePrefab == targetPrefabObject)
                {
                    Debug.LogError("A prefab asset cannot replace itself", go);
                    return null;
                }
            }

            var assetPath = AssetDatabase.GetAssetPath(targetPrefabObject);

            ReplacePrefabArgumentCheck(go, assetPath);

            bool success = false;
            return SavePrefab_Internal(go, assetPath, (replaceOptions & ReplacePrefabOptions.ConnectToPrefab) != 0, out success);
        }

        // Returns the corresponding object from its immediate source from a connected Prefab,
        // or null if it can't be found, or the Prefab instance is disconnected.
        internal static TObject GetCorrespondingConnectedObjectFromSource<TObject>(TObject componentOrGameObject) where TObject : Object
        {
            if (IsDisconnectedFromPrefabAsset(GetGameObject(componentOrGameObject)))
                return null;
            return (TObject)GetCorrespondingObjectFromSource_internal(componentOrGameObject);
        }

        // Returns the corresponding object from its immediate source, or null if it can't be found.
        public static TObject GetCorrespondingObjectFromSource<TObject>(TObject componentOrGameObject) where TObject : Object
        {
            return (TObject)GetCorrespondingObjectFromSource_internal(componentOrGameObject);
        }

        // Returns the corresponding object from its original source, or null if it can't be found.
        public static TObject GetCorrespondingObjectFromOriginalSource<TObject>(TObject componentOrGameObject) where TObject : Object
        {
            return (TObject)GetCorrespondingObjectFromOriginalSource_Internal(componentOrGameObject);
        }

        internal static TObject GetCorrespondingObjectFromSourceInAsset<TObject>(TObject instanceOrAsset, Object prefabAssetHandle) where TObject : Object
        {
            return (TObject)GetCorrespondingObjectFromSourceInAsset_internal(instanceOrAsset, prefabAssetHandle);
        }

        public static TObject GetCorrespondingObjectFromSourceAtPath<TObject>(TObject componentOrGameObject, string assetPath) where TObject : Object
        {
            return (TObject)GetCorrespondingObjectFromSourceAtPath_internal(componentOrGameObject, assetPath);
        }

        // Call native functon instead once it exists.
        private static Object GetCorrespondingObjectFromOriginalSource_Internal(Object instanceOrAsset)
        {
            var sourceObjectInPrefabAsset = instanceOrAsset;

            // First one is mandatory for non-persistant asset,
            // since we want the result to be null if there's asset object at all.
            if (!EditorUtility.IsPersistent(sourceObjectInPrefabAsset))
            {
                sourceObjectInPrefabAsset = GetCorrespondingObjectFromSource(sourceObjectInPrefabAsset);
                if (sourceObjectInPrefabAsset == null)
                    return null;
            }

            while (true)
            {
                var inner = GetCorrespondingObjectFromSource(sourceObjectInPrefabAsset);
                if (inner == null)
                    return sourceObjectInPrefabAsset;
                sourceObjectInPrefabAsset = inner;
            }
        }

        // Given an object, returns its prefab type (None, if it's not a prefab)
        [Obsolete("Use GetPrefabAssetType and GetPrefabInstanceStatus to get the full picture about Prefab types.")]
        public static PrefabType GetPrefabType(Object target)
        {
            if (!IsPartOfAnyPrefab(target))
                return PrefabType.None;

            bool isModel = IsPartOfModelPrefab(target);

            if (IsPartOfPrefabAsset(target))
            {
                if (isModel)
                    return PrefabType.ModelPrefab;

                return PrefabType.Prefab;
            }

            if (IsDisconnectedFromPrefabAsset(target))
            {
                var corresponding = GetCorrespondingObjectFromSource(target);
                var prefabObject = GetPrefabObject(corresponding);
                // Object was at some point connected to a prefab, but now it is not attached to one anymore and the prefab no longer exists
                if (prefabObject == null)
                    return PrefabType.None;

                if (isModel)
                    return PrefabType.DisconnectedModelPrefabInstance;

                return PrefabType.DisconnectedPrefabInstance;
            }

            if (IsPrefabAssetMissing(target))
                return PrefabType.MissingPrefabInstance;

            if (isModel)
                return PrefabType.ModelPrefabInstance;

            return PrefabType.PrefabInstance;
        }

        // Called after prefab instances in the scene have been updated
        public delegate void PrefabInstanceUpdated(GameObject instance);
        public static PrefabInstanceUpdated prefabInstanceUpdated;
        private static DelegateWithPerformanceTracker<PrefabInstanceUpdated> m_PrefabInstanceUpdated = new DelegateWithPerformanceTracker<PrefabInstanceUpdated>($"{nameof(PrefabUtility)}.{nameof(prefabInstanceUpdated)}");

        private static void Internal_CallPrefabInstanceUpdated(GameObject instance)
        {
            foreach (var evt in m_PrefabInstanceUpdated.UpdateAndInvoke(prefabInstanceUpdated))
                evt(instance);
        }

        public static bool IsAddedGameObjectOverride(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject), "GameObject is null.");

            Transform parent = gameObject.transform.parent;
            if (parent == null)
                return false;

            if (IsDisconnectedFromPrefabAsset(parent))
                return false;

            // Can't be added to a prefab instance if the parent is not part of a prefab instance.
            GameObject parentAsset = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(parent.gameObject);
            if (parentAsset == null)
                return false;

            GameObject asset = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(gameObject);

            // If object is not part of a prefab (but the parent is) we know it's added.
            if (asset == null)
                return true;

            // We know now that the object is part of a prefab.
            // If the root of that prefab, then it can't be part of the parent prefab, and must be added.
            // This check works regardless of whether this prefab instance is an instance of the same
            // prefab asset as the parent is an instance of (e.g. instance of A is added under instance of A),
            // or not (instance of B is added under instance of A).
            return (asset.transform.parent == null);
        }

        internal static bool IsAllAddedGameObjectOverrides(GameObject[] gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                if (!PrefabUtility.IsAddedGameObjectOverride(go))
                    return false;
            }
            return true;
        }

        // Called before the prefab is saved to hdd (called after AssetModificationProcessor.OnWillSaveAssets and before OnPostprocessAllAssets)
        internal static event Action<GameObject, string> savingPrefab;
        [RequiredByNativeCode]
        static void Internal_SavingPrefab(GameObject gameObject, string path)
        {
            if (savingPrefab != null)
                savingPrefab(gameObject, path);
        }

        internal static event Action prefabInstanceModificationCacheCleared;
        [RequiredByNativeCode]
        static void Internal_PrefabInstanceModificationCacheCleared()
        {
            if (prefabInstanceModificationCacheCleared != null)
                prefabInstanceModificationCacheCleared();
        }

        internal enum SaveVerb
        {
            Save,
            Apply
        }

        internal static bool PromptAndCheckoutPrefabIfNeeded(string assetPath, SaveVerb saveVerb)
        {
            return PromptAndCheckoutPrefabIfNeeded(new string[] { assetPath }, saveVerb);
        }

        internal static bool PromptAndCheckoutPrefabIfNeeded(string[] assetPaths, SaveVerb saveVerb)
        {
            // Some strings in these dialogs are nearly identical.
            // They are included like this instead of being created programatically
            // in order for localization strings to have sufficient context.

            string prefabNoun = assetPaths.Length > 1 ? L10n.Tr("Prefabs") : L10n.Tr("Prefab");
            bool result = AssetDatabase.MakeEditable(
                assetPaths,
                string.Format(
                    saveVerb == SaveVerb.Save ?
                    L10n.Tr("The version control requires you to check out the {0} before saving changes.") :
                    L10n.Tr("The version control requires you to check out the {0} before applying changes."),
                    prefabNoun
                )
            );

            if (!result)
                EditorUtility.DisplayDialog(
                    String.Format(
                        saveVerb == SaveVerb.Save ?
                        L10n.Tr("Could not save {0}") :
                        L10n.Tr("Could not apply to {0}"),
                        prefabNoun),
                    String.Format(
                        saveVerb == SaveVerb.Save ?
                        L10n.Tr("It was not possible to check out the {0} so the save operation has been canceled.") :
                        L10n.Tr("It was not possible to check out the {0} so the apply operation has been canceled."),
                        prefabNoun),
                    L10n.Tr("OK"));

            return result;
        }

        public static void UnpackPrefabInstance(GameObject instanceRoot, PrefabUnpackMode unpackMode, InteractionMode action)
        {
            if (!IsPartOfNonAssetPrefabInstance(instanceRoot))
                throw new ArgumentException("UnpackPrefabInstance must be called with a Prefab instance.");

            if (!IsOutermostPrefabInstanceRoot(instanceRoot))
                throw new ArgumentException("UnpackPrefabInstance must be called with a root Prefab instance GameObject.");

            if (action == InteractionMode.UserAction)
            {
                var undoActionName = "Unpack Prefab instance";
                Undo.RegisterFullObjectHierarchyUndo(instanceRoot, undoActionName);
                var newInstanceRoots = UnpackPrefabInstanceAndReturnNewOutermostRoots(instanceRoot, unpackMode);
                foreach (var newInstanceRoot in newInstanceRoots)
                {
                    var prefabInstance = PrefabUtility.GetPrefabInstanceHandle(newInstanceRoot);
                    if (prefabInstance)
                    {
                        Undo.RegisterCreatedObjectUndo(prefabInstance, undoActionName);
                    }
                }
            }
            else
            {
                UnpackPrefabInstanceAndReturnNewOutermostRoots(instanceRoot, unpackMode);
            }

            prefabInstanceUnpacked?.Invoke(instanceRoot);
        }

        public static void UnpackAllInstancesOfPrefab(GameObject prefabRoot, PrefabUnpackMode unpackMode, InteractionMode action)
        {
            var prefabInstances = FindAllInstancesOfPrefab(prefabRoot);
            foreach (var prefabInstance in prefabInstances)
            {
                UnpackPrefabInstance(prefabInstance, unpackMode, action);
            }
        }

        static internal event Action<GameObject> prefabInstanceUnpacked;

        internal static bool HasInvalidComponent(Object gameObjectOrComponent)
        {
            if (gameObjectOrComponent == null)
                return true;

            if (gameObjectOrComponent is Component)
            {
                Component comp = (Component)gameObjectOrComponent;
                gameObjectOrComponent = (GameObject)comp.gameObject;
            }

            if (!(gameObjectOrComponent is GameObject))
                return false;

            GameObject go;
            go = (GameObject)gameObjectOrComponent;
            TransformVisitor transformVisitor = new TransformVisitor();
            var GOsWithInvalidComponent = new List<GameObject>();
            transformVisitor.VisitAll(go.transform, PrefabOverridesUtility.CheckForInvalidComponent, GOsWithInvalidComponent);
            return GOsWithInvalidComponent.Count > 0;
        }

        public static bool IsPartOfPrefabThatCanBeAppliedTo(Object gameObjectOrComponent)
        {
            if (gameObjectOrComponent == null)
                return false;

            if (IsPartOfImmutablePrefab(gameObjectOrComponent))
                return false;

            if (!EditorUtility.IsPersistent(gameObjectOrComponent))
                gameObjectOrComponent = GetCorrespondingObjectFromSource(gameObjectOrComponent);

            if (HasInvalidComponent(gameObjectOrComponent))
                return false;

            if (PrefabUtility.HasManagedReferencesWithMissingTypes(gameObjectOrComponent))
                return false;

            return true;
        }

        public static PrefabInstanceStatus GetPrefabInstanceStatus(Object componentOrGameObject)
        {
            if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(componentOrGameObject))
                return PrefabInstanceStatus.NotAPrefab;

            if (PrefabUtility.IsDisconnectedFromPrefabAsset(componentOrGameObject))
                return PrefabInstanceStatus.Disconnected;

            if (PrefabUtility.IsPrefabAssetMissing(componentOrGameObject))
                return PrefabInstanceStatus.MissingAsset;

            return PrefabInstanceStatus.Connected;
        }

        public static PrefabAssetType GetPrefabAssetType(Object componentOrGameObject)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(componentOrGameObject))
                return PrefabAssetType.NotAPrefab;

            if (PrefabUtility.IsPrefabAssetMissing(componentOrGameObject))
                return PrefabAssetType.MissingAsset;

            if (PrefabUtility.IsPartOfModelPrefab(componentOrGameObject))
                return PrefabAssetType.Model;

            if (PrefabUtility.IsPartOfVariantPrefab(componentOrGameObject))
                return PrefabAssetType.Variant;

            return PrefabAssetType.Regular;
        }

        public static GameObject LoadPrefabContents(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException("assetPath", "Prefab Asset path is null or empty");

            if (!File.Exists(assetPath))
                throw new ArgumentException(string.Format("Path: {0}, does not exist", assetPath));

            if (Path.GetExtension(assetPath) != ".prefab")
                throw new ArgumentException(string.Format("Path: {0}, is not a prefab file", assetPath));

            var previewScene = EditorSceneManager.OpenPreviewScene(assetPath, false);
            var roots = previewScene.GetRootGameObjects();
            if (roots.Length != 1)
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
                throw new ArgumentException(string.Format("Could not load Prefab contents at path {0}. Prefabs should have exactly 1 root GameObject, {1} was found.", assetPath, roots.Length));
            }

            return roots[0];
        }

        public static void UnloadPrefabContents(GameObject contentsRoot)
        {
            if (!EditorSceneManager.IsPreviewSceneObject(contentsRoot))
            {
                throw new ArgumentException("Specified object is not part of Prefab contents");
            }
            var scene = contentsRoot.scene;
            EditorSceneManager.ClosePreviewScene(scene);
        }

        // Since an override can be applied to multiple different apply targets, what is not a default override
        // compared to e.g. the outermost apply target may still be a default override compared to e.g. the innermost.
        // For example, if you have child Prefab B inside Prefab A in the scene and try want to apply the position of B,
        // the position is not a default override if applying to A, but is if applying to B.
        // This can be used to determine which apply targets are valid to apply to.
        internal static bool IsPropertyOverrideDefaultOverrideComparedToAnySource(SerializedProperty property)
        {
            if (property == null || !property.prefabOverride)
                return false;

            Object componentOrGameObject = property.serializedObject.targetObject;

            // Only Transform, RectTransform (derived from Transform) and GameObject can have default overrides.
            if (!(componentOrGameObject is Transform || componentOrGameObject is GameObject))
                return false;

            Object innermostInstance = componentOrGameObject;
            Object source = GetCorrespondingObjectFromSource(innermostInstance);
            if (source == null)
                return false;

            while (source != null)
            {
                Object newSource = GetCorrespondingObjectFromSource(source);
                if (newSource == null)
                    break;
                innermostInstance = source;
                source = newSource;
            }

            SerializedObject innermostInstanceSO = new SerializedObject(innermostInstance);
            return innermostInstanceSO.FindProperty(property.propertyPath).isDefaultOverride;
        }

        internal static bool HasPrefabInstanceNonDefaultOverrides_CachedForUI(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            return HasPrefabInstanceNonDefaultOverrides_CachedForUI_Internal(gameObject);
        }

        internal static void ClearPrefabInstanceNonDefaultOverridesCache(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            ClearPrefabInstanceNonDefaultOverridesCache_Internal(gameObject);
        }

        // Same as IsPropertyOverrideDefaultOverrideComparedToAnySource, but checks if it's the case for all overrides
        // on the object.
        internal static bool IsObjectOverrideAllDefaultOverridesComparedToOriginalSource(Object componentOrGameObject)
        {
            // Only Transform, RectTransform (derived from Transform) and GameObject can have default overrides.
            if (!(componentOrGameObject is Transform || componentOrGameObject is GameObject))
                return false;

            Object innermostInstance = componentOrGameObject;
            Object source = GetCorrespondingObjectFromSource(innermostInstance);
            if (source == null)
                return false;

            while (source != null)
            {
                Object newSource = GetCorrespondingObjectFromSource(source);
                if (newSource == null)
                    break;
                innermostInstance = source;
                source = newSource;
            }

            SerializedObject passedSO = new SerializedObject(componentOrGameObject);
            SerializedObject innermostInstanceSO = new SerializedObject(innermostInstance);
            SerializedProperty property = passedSO.GetIterator();
            bool anyOverrides = false;
            while (property.Next(true))
            {
                if (property.prefabOverride)
                {
                    if (!innermostInstanceSO.FindProperty(property.propertyPath).isDefaultOverride)
                        return false;

                    anyOverrides = true;
                }
            }

            if (!anyOverrides)
                return false;

            return true;
        }

        internal static bool IsObjectOnRootInAsset(Object componentOrGameObject, string assetPath)
        {
            GameObject go = GetGameObject(componentOrGameObject);
            GameObject goInAsset = GetCorrespondingObjectFromSourceAtPath(go, assetPath);
            if (goInAsset == null)
                return false;
            return goInAsset.transform.root == goInAsset.transform;
        }

        internal static List<Object> GetApplyTargets(Object instanceOrAssetObject, bool isAllDefaultOverridesComparedToOriginalSource, bool includeSelfAsTarget = false)
        {
            List<Object> applyTargets = new List<Object>();

            GameObject instanceGameObject = instanceOrAssetObject as GameObject;
            if (instanceGameObject == null)
                instanceGameObject = (instanceOrAssetObject as Component).gameObject;

            Object source = instanceOrAssetObject;
            if (!EditorUtility.IsPersistent(source) || !includeSelfAsTarget)
                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            if (source == null)
                return applyTargets;

            while (source != null)
            {
                if (isAllDefaultOverridesComparedToOriginalSource)
                {
                    // Default overrides properties are select properties on the root GameObject (like name)
                    // and root Transform (like position and rotation, but not scale).
                    // If we're dealing with an override that's a default override compared to the original
                    // source, it will also be a default override compared to any Prefab Variants of the
                    // original source that might be in the chain of corresponding objects. It will however
                    // not be a default override compared to Prefabs that use the original source as a nested
                    // Prefab, because those Prefabs have a different root GameObject.
                    // So for each corresponding object we need to check if it's the Prefab root GameObject
                    // or Transform. If it is, the overrides will be default overrides compared to that Prefab,
                    // and so it shouldn't be possible to apply to it, so we don't add it as an apply target.
                    // Note that for changed components that have some overrides that can be default overrides,
                    // and some not, we do allow applying, but only the properties that are not default
                    // overrides will be applied.
                    GameObject sourceGo = GetGameObject(source);
                    if (sourceGo.transform.root == sourceGo.transform)
                        break;
                }

                applyTargets.Add(source);

                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            }

            return applyTargets;
        }

        internal enum OverrideOperation
        {
            Apply,
            Revert
        }

        // Applying/reverting multiple overrides at once is surprisingly tricky because
        // we have to handle components with dependencies on other components in just the right order.
        internal static bool ProcessMultipleOverrides(GameObject prefabInstanceRoot, List<PrefabOverride> overrides, PrefabUtility.OverrideOperation operation, InteractionMode mode)
        {
            if (WarnIfInAnimationMode(operation, mode))
                return false;

            Dictionary<PrefabOverride, List<Component>> overrideDependencies =
                new Dictionary<PrefabOverride, List<Component>>();
            List<PrefabOverride> acceptedOverrides = new List<PrefabOverride>();
            List<PrefabOverride> acceptedRemovedComponentOverrides = new List<PrefabOverride>();
            List<PrefabOverride> remainingOverrides = new List<PrefabOverride>();
            HashSet<Object> acceptedOverrideObjects = new HashSet<Object>();

            // Iterate over overrides. Immediately accept any overrides with no dependencies.
            // Otherwise save dependencies in dictionary for quick lookup.
            for (int i = 0; i < overrides.Count; i++)
            {
                PrefabOverride singleOverride = overrides[i];
                if (singleOverride == null)
                    continue;
                bool hasDependencies = false;

                RemovedComponent removedComponent = singleOverride as RemovedComponent;
                if (removedComponent != null)
                {
                    var deps = PrefabUtility.GetRemovedComponentDependencies(
                        removedComponent.assetComponent,
                        removedComponent.containingInstanceGameObject,
                        operation);
                    if (deps.Count > 0)
                    {
                        overrideDependencies[singleOverride] = deps;
                        hasDependencies = true;
                    }
                }

                AddedComponent addedComponent = singleOverride as AddedComponent;
                if (addedComponent != null)
                {
                    var deps = PrefabUtility.GetAddedComponentDependencies(
                        addedComponent.instanceComponent,
                        operation);
                    if (deps.Count > 0)
                    {
                        overrideDependencies[singleOverride] = deps;
                        hasDependencies = true;
                    }
                }

                if (hasDependencies)
                {
                    remainingOverrides.Add(singleOverride);
                }
                else
                {
                    if (singleOverride is RemovedComponent)
                        acceptedRemovedComponentOverrides.Add(singleOverride);
                    else
                        acceptedOverrides.Add(singleOverride);
                    acceptedOverrideObjects.Add(singleOverride.GetObject());
                }
            }

            // Iteratively accept overrides whose dependencies are all in the accepted overrides.
            // Theoretically this algorithm is worst case n*n.
            // However, long dependency chains are uncommon, so in practise it's much closer to just n.
            while (true)
            {
                bool didAcceptNewOverrides = false;

                for (int i = remainingOverrides.Count - 1; i >= 0; i--)
                {
                    var o = remainingOverrides[i];
                    var dependencies = overrideDependencies[o];
                    bool allDependenciesSatisfied = true;
                    for (int j = 0; j < dependencies.Count; j++)
                    {
                        if (!acceptedOverrideObjects.Contains(dependencies[j]))
                        {
                            allDependenciesSatisfied = false;
                            break;
                        }
                    }
                    if (allDependenciesSatisfied)
                    {
                        if (o is RemovedComponent)
                            acceptedRemovedComponentOverrides.Add(o);
                        else
                            acceptedOverrides.Add(o);
                        acceptedOverrideObjects.Add(o.GetObject());
                        remainingOverrides.RemoveAt(i);
                        didAcceptNewOverrides = true;
                    }
                }

                if (!didAcceptNewOverrides)
                    break;
            }

            if (remainingOverrides.Count > 0)
            {
                string dependenciesString = "";
                foreach (var singleOverride in remainingOverrides)
                {
                    var dependencies = overrideDependencies[singleOverride];
                    foreach (var dep in dependencies)
                    {
                        // The dependency direction is different for apply versus revert AND for added versus removed components.
                        bool dependsOnOther = (singleOverride is AddedComponent) ^ (operation == PrefabUtility.OverrideOperation.Revert);
                        dependenciesString += "\n" + string.Format(
                            dependsOnOther ? L10n.Tr("{0} depends on {1}") : L10n.Tr("{0} is depended on by {1}"),
                            ObjectNames.GetInspectorTitle(singleOverride.GetObject()),
                            ObjectNames.GetInspectorTitle(dep));
                    }
                }

                string error = null;
                string dialogTitle = null;
                if (operation == PrefabUtility.OverrideOperation.Apply)
                {
                    dialogTitle = L10n.Tr("Can't apply selected overrides");
                    error = L10n.Tr("Can't apply selected overrides due to dependencies with non-selected overrides:") + dependenciesString;
                }
                else
                {
                    dialogTitle = L10n.Tr("Can't revert selected overrides");
                    error = L10n.Tr("Can't revert selected overrides due to dependencies with non-selected overrides.") + dependenciesString;
                }

                if (mode == InteractionMode.UserAction)
                {
                    EditorUtility.DisplayDialog(dialogTitle, error, L10n.Tr("OK"));
                }
                else
                {
                    throw new ArgumentException(error);
                }

                return false;
            }

            if (operation == PrefabUtility.OverrideOperation.Apply)
            {
                if (mode == InteractionMode.UserAction)
                {
                    // Make sure asset is checked out in version control.
                    string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstanceRoot);
                    if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(prefabAssetPath, PrefabUtility.SaveVerb.Apply))
                        return false;
                }

                // Apply overrides *in the order* they were added, but all removed components first, so removed
                // and added component of same type can't cause GameObject to temporarily have two at once.
                for (int i = 0; i < acceptedRemovedComponentOverrides.Count; i++)
                    acceptedRemovedComponentOverrides[i].Apply(mode);
                for (int i = 0; i < acceptedOverrides.Count; i++)
                    acceptedOverrides[i].Apply(mode);
            }
            else
            {
                // Revert overrides *in the order* they were added, but all removed components last, so removed
                // and added component of same type can't cause GameObject to temporarily have two at once.
                for (int i = 0; i < acceptedOverrides.Count; i++)
                    acceptedOverrides[i].Revert(mode);
                for (int i = 0; i < acceptedRemovedComponentOverrides.Count; i++)
                    acceptedRemovedComponentOverrides[i].Revert(mode);
            }

            return true;
        }

        internal static List<Component> GetAddedComponentDependencies(Component component, OverrideOperation op)
        {
            GameObject instanceGameObject = component.gameObject;
            List<Component> addedComponentsOnGO =
                GetAddedComponents(instanceGameObject)
                    .Select(e => e.instanceComponent)
                    .Where(e => e.gameObject == instanceGameObject)
                    .ToList();
            if (op == OverrideOperation.Apply)
                // We can't apply an added component if it depends on a different added component.
                // This would violate a dependency in the asset.
                return GetComponentsWhichThisDependsOn(component, addedComponentsOnGO);
            else
                // We can't revert an added component if another added component depends on it.
                // This would violate a dependency in the instance.
                return GetComponentsWhichDependOnThis(component, addedComponentsOnGO);
        }

        internal static List<Component> GetRemovedComponentDependencies(Component assetComponent, GameObject instanceGameObject, OverrideOperation op)
        {
            GameObject assetGameObject = assetComponent.gameObject;
            List<Component> removedComponentsOnAssetGO =
                GetRemovedComponents(instanceGameObject)
                    .Select(e => e.assetComponent)
                    .Where(e => e.gameObject == assetGameObject)
                    .ToList();
            if (op == OverrideOperation.Apply)
                // We can't apply a removed component if another removed component depends on it.
                // This would violate a dependency in the asset.
                return GetComponentsWhichDependOnThis(assetComponent, removedComponentsOnAssetGO);
            else
                // We can't revert a removed component if it depends on another removed component.
                // This would violate a dependency in the instance.
                return GetComponentsWhichThisDependsOn(assetComponent, removedComponentsOnAssetGO);
        }

        static List<Component> GetComponentsWhichDependOnThis(Component component, List<Component> componentsToConsider)
        {
            List<Component> dependencies = new List<Component>();
            var componentType = component.GetType();

            // Iterate all components.
            for (int i = 0; i < componentsToConsider.Count; i++)
            {
                var comp = componentsToConsider[i];

                // Ignore component itself.
                if (comp == component)
                    continue;

                var requiredComps = comp.GetType().GetCustomAttributes(typeof(RequireComponent), inherit: true);
                foreach (RequireComponent reqComp in requiredComps)
                {
                    if (reqComp.m_Type0 == componentType || reqComp.m_Type1 == componentType || reqComp.m_Type2 == componentType)
                    {
                        // We might get the same component type requirement from multiple sources.
                        // Make sure we don't add the same component more than once.
                        if (!dependencies.Contains(comp))
                            dependencies.Add(comp);
                    }
                }
            }
            return dependencies;
        }

        static List<Component> GetComponentsWhichThisDependsOn(Component component, List<Component> componentsToConsider)
        {
            var requiredComps = component.GetType().GetCustomAttributes(typeof(RequireComponent), inherit: true);
            List<Component> dependencies = new List<Component>();
            if (requiredComps.Count() == 0)
                return dependencies;

            // Iterate all components.
            for (int i = 0; i < componentsToConsider.Count; i++)
            {
                var comp = componentsToConsider[i];

                // Ignore component itself.
                if (comp == component)
                    continue;

                var componentType = comp.GetType();
                foreach (RequireComponent reqComp in requiredComps)
                {
                    if (reqComp.m_Type0 == componentType || reqComp.m_Type1 == componentType || reqComp.m_Type2 == componentType)
                    {
                        // We might get the same component type requirement from multiple sources.
                        // Make sure we don't add the same component more than once.
                        if (!dependencies.Contains(comp))
                            dependencies.Add(comp);
                    }
                }
            }
            return dependencies;
        }

        internal static class Analytics
        {
            public enum ApplyScope
            {
                PropertyOverride,
                ObjectOverride,
                AddedComponent,
                RemovedComponent,
                AddedGameObject,
                EntirePrefab
            }

            public enum ApplyTarget
            {
                OnlyTarget,
                Outermost,
                Innermost,
                Middle
            }

            [Serializable]
            class EventData
            {
                public ApplyScope applyScope;
                public bool userAction;
                public string activeGUIView;
                public ApplyTarget applyTarget;
                public int applyTargetCount;
            }

            public static void SendApplyEvent(
                ApplyScope applyScope,
                Object instance,
                string applyTargetPath,
                InteractionMode interactionMode,
                DateTime startTime,
                bool defaultOverrideComparedToSomeSources
            )
            {
                var duration = DateTime.UtcNow.Subtract(startTime);

                var eventData = new EventData();
                eventData.applyScope = applyScope;
                eventData.userAction = (interactionMode == InteractionMode.UserAction);
                eventData.activeGUIView = GUIView.GetTypeNameOfMostSpecificActiveView();

                // Calculate apply target and apply target count relation.
                var applyTargets = GetApplyTargets(instance, defaultOverrideComparedToSomeSources);
                if (applyTargets == null)
                {
                    eventData.applyTarget = ApplyTarget.OnlyTarget;
                    eventData.applyTargetCount = 0;
                }
                else if (applyTargets.Count <= 1)
                {
                    eventData.applyTarget = ApplyTarget.OnlyTarget;
                    eventData.applyTargetCount = applyTargets.Count;
                }
                else
                {
                    eventData.applyTargetCount = applyTargets.Count;

                    int index = -1;
                    for (int i = 0; i < applyTargets.Count; i++)
                    {
                        if (AssetDatabase.GetAssetPath(applyTargets[i]) == applyTargetPath)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == 0)
                        eventData.applyTarget = ApplyTarget.Outermost;
                    else if (index == applyTargets.Count - 1)
                        eventData.applyTarget = ApplyTarget.Innermost;
                    else
                        eventData.applyTarget = ApplyTarget.Middle;
                }

                UsabilityAnalytics.SendEvent("prefabApply", startTime, duration, true, eventData);
            }
        }

        [RequiredByNativeCode]
        static void OnPrefabSavingEnded(long ticks)
        {
            var duration = new TimeSpan(ticks);
            var saveStartTime = DateTime.UtcNow.Subtract(duration);

            UsabilityAnalytics.SendEvent("prefabSave", saveStartTime, duration, true, null);
        }

        public struct EditPrefabContentsScope : IDisposable
        {
            public readonly string assetPath;
            public readonly GameObject prefabContentsRoot;

            public EditPrefabContentsScope(string assetPath)
            {
                this.assetPath = assetPath;
                prefabContentsRoot = LoadPrefabContents(assetPath);
            }

            public void Dispose()
            {
                SaveAsPrefabAsset(prefabContentsRoot, assetPath);
                UnloadPrefabContents(prefabContentsRoot);
            }
        }
    }
}
