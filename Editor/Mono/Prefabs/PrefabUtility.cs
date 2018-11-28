// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.VersionControl;
using UnityEditorInternal;

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

                var materials = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).Where(x => x.GetType() == typeof(Material));

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

        private static void GetObjectListFromHierarchy(HashSet<int> hierarchyInstanceIDs, GameObject gameObject)
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

        private static void CollectAddedObjects(GameObject gameObject, HashSet<int> hierarchyInstanceIDs, List<Object> danglingObjects)
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
                        Undo.RegisterCreatedObjectUndo(danglingObject, actionName);
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
                Undo.RegisterCreatedObjectUndo(component, actionName);
            }
        }

        static void CheckInstanceIsNotPersistent(Object prefabInstanceObject)
        {
            if (EditorUtility.IsPersistent(prefabInstanceObject))
                throw new ArgumentException("Calling apply or revert methods on an instance which is part of a Prefab asset is not supported.", nameof(prefabInstanceObject));
        }

        public static void RevertPrefabInstance(GameObject instanceRoot, InteractionMode action)
        {
            bool isDisconnected = PrefabUtility.IsDisconnectedFromPrefabAsset(instanceRoot);

            CheckInstanceIsNotPersistent(instanceRoot);

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
                if (isDisconnected)
                {
                    Undo.RegisterCreatedObjectUndo(GetPrefabInstanceHandle(prefabInstanceRoot), actionName);
                }

                RegisterNewObjects(prefabInstanceRoot, hierarchy, actionName);
            }

            EditorUtility.ForceRebuildInspectors();
        }

        public static void ApplyPrefabInstance(GameObject instanceRoot, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            CheckInstanceIsNotPersistent(instanceRoot);

            GameObject prefabInstanceRoot = GetOutermostPrefabInstanceRoot(instanceRoot);

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

        public static void ApplyPropertyOverride(SerializedProperty instanceProperty, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            Object prefabInstanceObject = instanceProperty.serializedObject.targetObject;
            CheckInstanceIsNotPersistent(prefabInstanceObject);

            ApplyPropertyOverrides(prefabInstanceObject, instanceProperty, assetPath, action);

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
        static void ApplyPropertyOverrides(Object prefabInstanceObject, SerializedProperty optionalSingleInstanceProperty, string assetPath, InteractionMode action)
        {
            Object prefabSourceObject = GetCorrespondingObjectFromSourceAtPath(prefabInstanceObject, assetPath);
            if (prefabSourceObject == null)
                return;

            SerializedObject prefabSourceSerializedObject = new SerializedObject(prefabSourceObject);

            // Cache SerializedObjects used.
            List<SerializedObject> serializedObjects = new List<SerializedObject>();

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

            if (!property.hasVisibleChildren)
            {
                if (property.prefabOverride)
                    ApplySingleProperty(property, prefabSourceSerializedObject, assetPath, isObjectOnRootInAsset, true, serializedObjects, action);
            }
            else
            {
                while (property.Next(property.hasVisibleChildren) && (endProperty == null || !SerializedProperty.EqualContents(property, endProperty)))
                {
                    // If we apply a property that has child properties that are object references if they
                    // reference non-asset objects, those references will get lost, since ApplySingleProperty
                    // only patches up references in the provided property; not its children.
                    // This could be fixed by letting ApplySingleProperty patch up all its child properties as well,
                    // but then calling ApplySingleProperty n times would result in time complexity n*log(n).
                    // Instead we only call ApplySingleProperty for leaf properties - the ones that actually contain
                    // the data. Here, that means properties with hasVisibleChildren=false.
                    // Note that an object reference property has hasVisibleChildren=false but has hasChildren=true.
                    // Applying all leaf properties applies exactly all data there is to apply only once and ensures
                    // that when an object reference is applied, it's via its own property and not a parent property.
                    if (property.prefabOverride && !property.hasVisibleChildren)
                        ApplySingleProperty(property, prefabSourceSerializedObject, assetPath, isObjectOnRootInAsset, false, serializedObjects, action);
                }
            }

            // Write modified value to prefab source object.
            for (int i = 0; i < serializedObjects.Count; i++)
            {
                serializedObjects[i].ApplyModifiedProperties();
                if (action == InteractionMode.UserAction)
                    Undo.FlushUndoRecordObjects(); // flush'es ensure that SavePrefab() on undo/redo on the source happens in the right order
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
        static void ApplySingleProperty(
            SerializedProperty instanceProperty,
            SerializedObject prefabSourceSerializedObject,
            string assetPath,
            bool isObjectOnRootInAsset,
            bool singlePropertyOnly,
            List<SerializedObject> serializedObjects,
            InteractionMode action)
        {
            if (isObjectOnRootInAsset && IsPropertyOverrideDefaultOverrideComparedToAnySource(instanceProperty))
            {
                if (singlePropertyOnly)
                {
                    // Neither of these will not happen from our own editor interface since we don't display
                    // any menus to apply for default-override properties in the first place.
                    if (action == InteractionMode.AutomatedAction)
                        Debug.LogWarning("Cannot apply default-override property, since it is protected from being applied or reverted.");
                    else
                        EditorUtility.DisplayDialog(
                            "Cannot apply default-override property",
                            "Default-override properties are protected from being applied or reverted.",
                            "OK");
                }
                return;
            }

            prefabSourceSerializedObject.CopyFromSerializedProperty(instanceProperty);

            // Abort if property has reference to object in scene.
            SerializedProperty sourceProperty = prefabSourceSerializedObject.FindProperty(instanceProperty.propertyPath);
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
                                "Cannot apply reference to object in scene",
                                "A reference to an object in the scene cannot be applied to the Prefab asset.",
                                "OK");
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

                SerializedProperty outerPrefabProp = outerPrefabSerializedObject.FindProperty(instanceProperty.propertyPath);
                if (outerPrefabProp.prefabOverride)
                {
                    outerPrefabProp.prefabOverride = false;
                }

                outerPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(outerPrefabObject);
                sourceIndex++;
            }
        }

        public static void RevertPropertyOverride(SerializedProperty instanceProperty, InteractionMode action)
        {
            instanceProperty.prefabOverride = false;
            // Because prefabOverride changed ApplyModifiedProperties will do a prefab merge causing the revert.
            if (action == InteractionMode.UserAction)
                instanceProperty.serializedObject.ApplyModifiedProperties();
            else
                instanceProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ApplyObjectOverride(Object instanceComponentOrGameObject, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            CheckInstanceIsNotPersistent(instanceComponentOrGameObject);

            ApplyPropertyOverrides(instanceComponentOrGameObject, null, assetPath, action);

            Analytics.SendApplyEvent(
                Analytics.ApplyScope.ObjectOverride,
                instanceComponentOrGameObject,
                assetPath,
                action,
                startTime,
                IsObjectOverrideAllDefaultOverridesComparedToAnySource(instanceComponentOrGameObject)
            );
        }

        public static void RevertObjectOverride(Object instanceComponentOrGameObject, InteractionMode action)
        {
            CheckInstanceIsNotPersistent(instanceComponentOrGameObject);

            if (action == InteractionMode.UserAction)
                Undo.RegisterCompleteObjectUndo(instanceComponentOrGameObject, "Revert component property overrides");
            PrefabUtility.RevertObjectOverride_Internal(instanceComponentOrGameObject);
        }

        public static void ApplyAddedComponent(Component component, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            CheckInstanceIsNotPersistent(component);

            try
            {
                GameObject prefabSourceGameObject = GetCorrespondingObjectFromSourceAtPath(component.gameObject, assetPath);
                if (prefabSourceGameObject == null)
                    return;

                var actionName = "Apply Added Component";
                if (action == InteractionMode.UserAction)
                {
                    Undo.RegisterFullObjectHierarchyUndo(prefabSourceGameObject, actionName);
                    Undo.RegisterFullObjectHierarchyUndo(component, actionName);
                }

                PrefabUtility.ApplyAddedComponent(component, prefabSourceGameObject);

                if (action == InteractionMode.UserAction)
                    Undo.RegisterCreatedObjectUndo(GetCorrespondingObjectFromOriginalSource(component), actionName);
            }
            catch (ArgumentException exception)
            {
                if (action == InteractionMode.UserAction)
                {
                    EditorUtility.DisplayDialog("Can't add component", exception.Message, "OK");
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
            CheckInstanceIsNotPersistent(component);

            if (component == null)
                throw new ArgumentNullException(nameof(component), "Can't revert added component. Component is null.");

            if (action == InteractionMode.UserAction)
            {
                string dependentComponents = string.Join(
                    ", ",
                    GetDependentComponents(component).Select(e => ObjectNames.GetInspectorTitle(e)).ToArray());
                if (!string.IsNullOrEmpty(dependentComponents))
                {
                    string error = String.Format(
                        L10n.Tr("Can't revert added component {0} because {1} depends on it"),
                        ObjectNames.GetInspectorTitle(component),
                        dependentComponents);
                    EditorUtility.DisplayDialog(L10n.Tr("Can't revert added component"), error, L10n.Tr("OK"));
                    return;
                }
                Undo.DestroyObjectImmediate(component);
            }
            else
                Object.DestroyImmediate(component);
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

            CheckInstanceIsNotPersistent(instanceGameObject);

            if (assetComponent == null)
                throw new ArgumentNullException(nameof(assetComponent), "Prefab source may not be null.");

            var actionName = "Apply Prefab removed component";

            if (action == InteractionMode.UserAction)
            {
                Undo.DestroyObjectUndoable(assetComponent, actionName);
                // Undo.DestroyObjectUndoable saves prefab asset internally.
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
            CheckInstanceIsNotPersistent(instanceGameObject);

            var actionName = "Revert Prefab removed component";
            var prefabInstanceObject = PrefabUtility.GetPrefabInstanceHandle(instanceGameObject);

            if (action == InteractionMode.UserAction)
                Undo.RegisterCompleteObjectUndo(instanceGameObject, actionName);

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
            }
        }

        public static void ApplyAddedGameObject(GameObject gameObject, string assetPath, InteractionMode action)
        {
            DateTime startTime = DateTime.UtcNow;

            CheckInstanceIsNotPersistent(gameObject);

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

            var actionName = "Apply Added Game Object";
            if (action == InteractionMode.UserAction)
            {
                Undo.RegisterFullObjectHierarchyUndo(sourceRoot, actionName);
                Undo.RegisterFullObjectHierarchyUndo(instanceRoot, actionName);
            }

            PrefabUtility.AddGameObjectsToPrefabAndConnect(
                new GameObject[] { gameObject },
                prefabSourceGameObjectParent);

            PrefabUtility.SavePrefabAsset(sourceRoot);

            if (action == InteractionMode.UserAction)
            {
                var createdAssetObject = GetCorrespondingObjectFromSourceInAsset(gameObject, prefabSourceGameObjectParent);
                if (createdAssetObject != null)
                {
                    Undo.RegisterCreatedObjectUndo(createdAssetObject, actionName);

                    EditorUtility.ForceRebuildInspectors();
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

        public static void RevertAddedGameObject(GameObject gameObject, InteractionMode action)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject), "Can't revert added GameObject. GameObject is null.");

            CheckInstanceIsNotPersistent(gameObject);

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
            bool defaultOverrideComparedToSomeSources = false)
        {
            HandleApplyMenuItems(thingThatChanged, instanceObject, addApplyMenuItemAction, defaultOverrideComparedToSomeSources);
            HandleRevertMenuItem(thingThatChanged, addRevertMenuItemAction);
        }

        internal static void HandleApplyMenuItems(
            string thingThatChanged,
            Object instanceOrAssetObject,
            Action<GUIContent, Object> addApplyMenuItemAction,
            bool defaultOverrideComparedToSomeSources = false)
        {
            // If thingThatChanged word is empty, apply menu items directly into menu.
            // Otherwise, insert as sub-menu named after thingThatChanged.
            if (thingThatChanged == null)
                thingThatChanged = String.Empty;
            if (thingThatChanged != String.Empty)
                thingThatChanged += "/";

            List<Object> applyTargets = GetApplyTargets(instanceOrAssetObject, defaultOverrideComparedToSomeSources);
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

#pragma warning disable CS0618 // Type or member is obsolete
            return SavePrefab(root, path, ReplacePrefabOptions.Default, PrefabCreationFlags.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void SaveAsPrefabAssetArgumentCheck(GameObject instanceRoot)
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
        }

        private static bool IsPrefabInstanceRoot(GameObject gameObject)
        {
            var instanceRoot = GetOutermostPrefabInstanceRoot(gameObject);
            return instanceRoot != null && instanceRoot == gameObject;
        }

        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath, out bool success)
        {
            SaveAsPrefabAssetArgumentCheck(instanceRoot);

            PrefabCreationFlags creationFlags = PrefabCreationFlags.None;
            if (IsPrefabInstanceRoot(instanceRoot))
                creationFlags = PrefabCreationFlags.CreateVariant;

#pragma warning disable CS0618 // Type or member is obsolete
            return SavePrefab(instanceRoot, assetPath, ReplacePrefabOptions.Default, creationFlags, out success);
#pragma warning restore CS0618 // Type or member is obsolete
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
            SaveAsPrefabAssetArgumentCheck(instanceRoot);

            var actionName = "Connect to Prefab";

            if (action == InteractionMode.UserAction)
            {
                Undo.RegisterFullObjectHierarchyUndo(instanceRoot, actionName);
            }

            PrefabCreationFlags creationFlags = PrefabCreationFlags.None;
            if (IsPrefabInstanceRoot(instanceRoot))
                creationFlags = PrefabCreationFlags.CreateVariant;

#pragma warning disable CS0618 // Type or member is obsolete
            var assetRoot = SavePrefab(instanceRoot, assetPath, ReplacePrefabOptions.ConnectToPrefab, creationFlags, out success);
#pragma warning restore CS0618 // Type or member is obsolete

            if (!success)
            {
                return null;
            }

            if (action == InteractionMode.UserAction)
            {
                Undo.RegisterCreatedObjectUndo(GetPrefabInstanceHandle(instanceRoot), actionName);
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

#pragma warning disable CS0618 // Type or member is obsolete
            SavePrefab(instance, path, ReplacePrefabOptions.ConnectToPrefab, PrefabCreationFlags.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // TOOO: Remove entirely once regular methods handle merging
        // based on both ids and names on a smarter and more granular level.
        internal static GameObject ReplacePrefabAssetNameBased(GameObject root, string targetPrefab, bool connectToInstance)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var options = ReplacePrefabOptions.ReplaceNameBased;
            if (connectToInstance)
                options |= ReplacePrefabOptions.ConnectToPrefab;

            var createOptions = PrefabCreationFlags.None;

            if (IsPartOfNonAssetPrefabInstance(root))
            {
                if (!IsOutermostPrefabInstanceRoot(root))
                    throw new ArgumentException("Can't replace with part of Prefab instance. Please specify instance root object or a non-instance object.");

                createOptions = PrefabCreationFlags.CreateVariant;
            }

            if (IsPartOfPrefabAsset(root) && connectToInstance)
                throw new ArgumentException("Argument connectToInstance is true but root object is an asset not an instance");

            return SavePrefab(root, targetPrefab, options, createOptions);
#pragma warning restore CS0618 // Type or member is obsolete
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
            if (options == ReplacePrefabOptions.ConnectToPrefab)
                return SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            else if ((options & ReplacePrefabOptions.ReplaceNameBased) != 0)
            {
                bool connectToPrefab = (options & ReplacePrefabOptions.ConnectToPrefab) != 0;
                return ReplacePrefabAssetNameBased(go, path, connectToPrefab);
            }
            else
                return SaveAsPrefabAsset(go, path);
        }

        // Instantiates the given prefab.
        public static Object InstantiatePrefab(Object assetComponentOrGameObject)
        {
            return InstantiatePrefab_internal(assetComponentOrGameObject, EditorSceneManager.GetTargetSceneForNewGameObjects());
        }

        // Instantiates the given prefab in a given scene
        public static Object InstantiatePrefab(Object assetComponentOrGameObject, Scene destinationScene)
        {
            return InstantiatePrefab_internal(assetComponentOrGameObject, destinationScene);
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
            return SavePrefab(go, assetPath, replaceOptions, PrefabCreationFlags.None);
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
        private static void Internal_CallPrefabInstanceUpdated(GameObject instance)
        {
            if (prefabInstanceUpdated != null)
                prefabInstanceUpdated(instance);
        }

        public static bool IsAddedGameObjectOverride(GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            if (parent == null)
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

        // Called before the prefab is saved to hdd (called after AssetModificationProcessor.OnWillSaveAssets and before OnPostprocessAllAssets)
        internal static event Action<GameObject, string> savingPrefab;
        [RequiredByNativeCode]
        static void Internal_SavingPrefab(GameObject gameObject, string path)
        {
            if (savingPrefab != null)
                savingPrefab(gameObject, path);
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
            string prefabNoun = assetPaths.Length > 1 ? "Prefabs" : "Prefab";
            bool result = Provider.PromptAndCheckoutIfNeeded(
                assetPaths,
                String.Format(
                    "The version control requires you to check out the {1} before {0} changes.",
                    saveVerb == SaveVerb.Save ? "saving" : "applying",
                    prefabNoun
                )
            );

            if (!result)
                EditorUtility.DisplayDialog(
                    String.Format(
                        "Could not {0} {1}",
                        saveVerb == SaveVerb.Save ? "save" : "apply to",
                        prefabNoun),
                    String.Format(
                        "It was not possible to check out the {1} so the {0} operation has been canceled.",
                        saveVerb == SaveVerb.Save ? "save" : "apply",
                        prefabNoun),
                    "OK");

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
        }

        internal static bool HasInvalidComponent(Object gameObjectOrComponent)
        {
            if (gameObjectOrComponent == null)
                return true;

            if (gameObjectOrComponent is Component)
            {
                Component comp = (Component)gameObjectOrComponent;
                gameObjectOrComponent = (GameObject)comp.gameObject;
            }

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
            var previewScene = EditorSceneManager.OpenPreviewScene(assetPath);
            var roots = previewScene.GetRootGameObjects();
            if (roots.Length != 1)
            {
                throw new ArgumentException(string.Format("Could not load Prefab contents at path {0}.", assetPath));
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

        // Same as IsPropertyOverrideDefaultOverrideComparedToAnySource, but checks if it's the case for all overrides
        // on the object.
        internal static bool IsObjectOverrideAllDefaultOverridesComparedToAnySource(Object componentOrGameObject)
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

        internal static List<Object> GetApplyTargets(Object instanceOrAssetObject, bool defaultOverrideComparedToSomeSources)
        {
            List<Object> applyTargets = new List<Object>();

            GameObject instanceGameObject = instanceOrAssetObject as GameObject;
            if (instanceGameObject == null)
                instanceGameObject = (instanceOrAssetObject as Component).gameObject;

            Object source = instanceOrAssetObject;
            if (!EditorUtility.IsPersistent(source))
                source = PrefabUtility.GetCorrespondingObjectFromSource(instanceOrAssetObject);
            if (source == null)
                return applyTargets;

            while (source != null)
            {
                if (defaultOverrideComparedToSomeSources)
                {
                    // If we're dealing with an override that's a default override compared to some sources,
                    // then we need to check if the source object is or is on the root GameObject in that Prefab.
                    // If it is, the overrides will be default overrides compared to that Prefab,
                    // and so it shouldn't be possible to apply to it.
                    // Note that for changed components that have some overrides that can be default overrides,
                    // and some not, we do allow applying, but only the properties
                    // that are not default overrides will be applied.
                    GameObject sourceGo = GetGameObject(source);
                    if (sourceGo.transform.root == sourceGo.transform)
                        break;
                }

                applyTargets.Add(source);

                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            }

            return applyTargets;
        }

        static List<Component> GetDependentComponents(Component component)
        {
            List<Component> dependencies = new List<Component>();
            var componentType = component.GetType();

            // Iterate all components on *this* GameObject.
            // We don't care about other components on the Prefab instance.
            var allComponents = component.gameObject.GetComponents<Component>();
            for (int i = 0; i < allComponents.Length; i++)
            {
                var comp = allComponents[i];

                // Ignore components that are not added.
                if (GetCorrespondingObjectFromSource(comp) != null)
                    continue;

                // Ignore component itself the user is reverting.
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

        static DateTime s_SaveStartTime = DateTime.MinValue;

        [RequiredByNativeCode]
        static void OnPrefabSavingStarted()
        {
            s_SaveStartTime = DateTime.UtcNow;
        }

        [RequiredByNativeCode]
        static void OnPrefabSavingEnded()
        {
            if (s_SaveStartTime == DateTime.MinValue)
            {
                Debug.LogError("Cannot measure duration of saving a Prefab. OnPrefabSavingStarted() has not been called first.");
                return;
            }
            var duration = DateTime.UtcNow.Subtract(s_SaveStartTime);
            UsabilityAnalytics.SendEvent("prefabSave", s_SaveStartTime, duration, true, null);
            s_SaveStartTime = DateTime.MinValue;
        }
    }
}
