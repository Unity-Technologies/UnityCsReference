// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    public sealed partial class BuildProfile
    {
        /// <summary>
        /// Gets the active build profile.
        /// </summary>
        /// <returns>
        /// The active build profile. Returns null when a classic platform is active.
        /// </returns>
        public static BuildProfile GetActiveBuildProfile()
        {
            return BuildProfileContext.activeProfile;
        }

        /// <summary>
        /// Sets the active build profile.
        /// </summary>
        /// <param name="buildProfile">
        /// The build profile to be set as the active build profile.
        /// When the value is null, Unity sets the classic platform as active.
        /// </param>
        public static void SetActiveBuildProfile(BuildProfile buildProfile)
        {
            BuildProfileContext.activeProfile = buildProfile;

            if (buildProfile == null)
                return;

            BuildProfileModuleUtil.SwitchLegacyActiveFromBuildProfile(buildProfile);
        }

        /// <summary>
        /// Instantiates a new component of type T and adds it as a sub-asset to the build profile.
        /// </summary>
        /// <returns>Returns the newly created ScriptableObect.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when a component of the same type already exists in the build profile.
        /// </exception>
        [VisibleToOtherModules]
        public T CreateComponent<T>() where T : UnityEngine.Object
        {
            ThrowIfComponentExists(typeof(T));

            var type = typeof(T);
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                var objectToAdd = ScriptableObject.CreateInstance(type);
                objectToAdd.hideFlags = HideFlags.HideInHierarchy;
                objectToAdd.name = type.Name;
                AssetDatabase.AddObjectToAsset(objectToAdd, this);
                return objectToAdd as T;
            }

            throw new ArgumentException($"Type {type.Name} is not a ScriptableObject or a supported settings object type.");
        }

        /// <summary>
        /// Gets a component of type T associated with the build profile, its global fallback,
        /// or null if the component is not available.
        /// </summary>
        public T GetComponent<T>() where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(PlayerSettings))
            {
                if (m_PlayerSettings != null)
                    return m_PlayerSettings as T;
                return s_GlobalPlayerSettings as T;
            }

            var found = FindComponentObject(typeof(T));
            if (found is null)
                return null;

            // A reference component resolves to its referenced asset; an unassigned/orphaned proxy
            // resolves to null and is treated as if the component was not added.
            return found is ReferencedComponent proxy ? proxy.reference as T : found as T;
        }

        /// <summary>
        /// Adds a component to the build profile, either as a subasset or as a reference to an existing project asset.
        /// </summary>
        /// <remarks>
        /// How the component is stored depends on whether <paramref name="objectToAdd"/> already exists as a
        /// project asset. An object that isn't a project asset becomes a hidden subasset of the build profile,
        /// and the profile owns it. An object that's already a project asset stays where it is, and the profile
        /// stores a reference to it instead.
        ///
        /// A build profile holds at most one component of each type.
        /// </remarks>
        /// <typeparam name="T">The type of the component to add.</typeparam>
        /// <param name="objectToAdd">The object to add to the build profile.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="objectToAdd"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the build profile already contains a component of
        /// type <typeparamref name="T"/>.</exception>
        public void AddComponent<T>(T objectToAdd) where T : UnityEngine.Object
        {
            if (objectToAdd == null)
            {
                throw new ArgumentNullException(nameof(objectToAdd), "The object to add cannot be null.");
            }

            ThrowIfComponentExists(typeof(T));

            if (!AssetDatabase.Contains(objectToAdd))
            {
                objectToAdd.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(objectToAdd, this);
                return;
            }

            if (objectToAdd is not ScriptableObject referenceAsset)
                throw new InvalidOperationException($"Referenced build profile components must be ScriptableObjects; {objectToAdd} is not.");

            CreateReferenceProxy(typeof(T), referenceAsset);
        }

        /// <summary>
        /// Removes the first occurence of a component of type T from the build profile.
        /// </summary>
        public void RemoveComponent<T>() where T : UnityEngine.Object
        {
            var toRemove = FindComponentObject(typeof(T));
            if (toRemove == null)
            {
                Debug.LogWarning($"The component {typeof(T).Name} does not exist in the build profile {name}. ");
                return;
            }

            if (Array.IndexOf(requiredComponents, toRemove) > -1)
                throw new ArgumentException($"The component {typeof(T).Name} is required and cannot be removed from the build profile {name}.");

            AssetDatabase.RemoveObjectFromAsset(toRemove);
        }

        /// <summary>
        /// Removes a specific component from the build profile.
        /// </summary>
        /// <remarks>
        /// Passing an object that the profile holds by reference removes the proxy sub-asset linking to
        /// it; the referenced object itself stays on disk.
        /// </remarks>
        /// <param name="objectToRemove">Object to remove.</param>
        /// <exception cref="ArgumentNullException">objectToRemove is null.</exception>
        /// <exception cref="ArgumentException">objectToRemove is not part of the build profile.</exception>
        public void RemoveComponent<T>(T objectToRemove) where T : UnityEngine.Object
        {
            if (objectToRemove == null)
            {
                throw new ArgumentNullException(nameof(objectToRemove), "The object to remove cannot be null.");
            }

            // A profile holds at most one component per type, so a reference component of type T is the
            // proxy pointing at objectToRemove; the proxy is the sub-asset that belongs to the profile.
            var toRemove = FindComponentObject(typeof(T));
            if (toRemove is ReferencedComponent proxy)
            {
                if (proxy.reference != objectToRemove)
                {
                    throw new ArgumentException($"The object {objectToRemove} is not the reference for the build profile component of type {typeof(T).Name}.");
                }
            }
            else if (toRemove != objectToRemove)
            {
                var objectToRemovePath = AssetDatabase.GetAssetPath(objectToRemove);
                var path = AssetDatabase.GetAssetPath(this);
                throw new ArgumentException($"The object {objectToRemove} (path: {objectToRemovePath}) is not part of the build profile {path}.");
            }

            if (Array.IndexOf(requiredComponents, objectToRemove) > -1)
                throw new ArgumentException($"The component {typeof(T).Name} is required and cannot be removed from the build profile {name}.");

            AssetDatabase.RemoveObjectFromAsset(toRemove);
        }

        /// <summary>
        /// Adds a reference to a standalone on-disk settings asset, or an unassigned reference when
        /// <paramref name="reference"/> is null.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when a component of the same type already exists.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a non-null reference is not an on-disk asset.</exception>
        internal void AddReferenceComponent(Type componentType, ScriptableObject reference)
        {
            ThrowIfComponentExists(componentType);
            ThrowIfNotOnDisk(reference);

            CreateReferenceProxy(componentType, reference);
        }

        /// <summary>
        /// Reassigns an existing reference component's source in place, preserving the proxy sub-asset.
        /// A null reference leaves the component present but unassigned. The caller is responsible for
        /// ensuring the reference component exists; use <see cref="AddReferenceComponent"/> to add one.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a non-null reference is not an on-disk asset, or when no reference component of
        /// <paramref name="componentType"/> exists.
        /// </exception>
        internal void SetReferenceComponent(Type componentType, ScriptableObject reference)
        {
            ThrowIfNotOnDisk(reference);

            var proxy = FindReferenceProxy(componentType);
            if (proxy == null)
                throw new InvalidOperationException($"No reference component of type {componentType.Name} exists in the build profile {name}. Add it before setting its reference.");

            Undo.RecordObject(proxy, "Set Settings Source");
            proxy.reference = reference;
            EditorUtility.SetDirty(proxy);
        }

        /// <summary>
        /// Forcibly removes the first occurrence of a component of type T from the build profile.
        /// </summary>
        [VisibleToOtherModules]
        internal void ForceRemoveComponent<T>() where T : UnityEngine.Object
        {
            var toRemove = FindComponentObject(typeof(T));
            if (toRemove == null)
            {
                Debug.LogWarning($"The component {typeof(T).Name} does not exist in the build profile {name}. ");
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(toRemove);
        }

        /// <summary>
        /// Gets a component of type T associated with the currently active build profile,
        /// its global fallback, or null if the component is not available.
        /// </summary>
        public static T GetActiveComponent<T>() where T : UnityEngine.Object
        {
            var buildProfile = GetActiveBuildProfile();
            if (buildProfile == null)
            {
                if (typeof(T) == typeof(PlayerSettings))
                    return s_GlobalPlayerSettings as T;

                return null;
            }

            return buildProfile.GetComponent<T>();
        }

        UnityEngine.Object[] LoadAllComponentObjects()
        {
            var path = AssetDatabase.GetAssetPath(this);
            return AssetDatabase.LoadAllAssetsAtPath(path);
        }

        UnityEngine.Object FindComponentObject(Type componentType)
        {
            foreach (var obj in LoadAllComponentObjects())
            {
                if (IsComponentOfType(obj, componentType))
                    return obj;
            }

            return null;
        }

        void ThrowIfComponentExists(Type componentType)
        {
            if (HasComponentOfType(componentType))
                throw new ArgumentException($"The component {componentType.Name} already exists in the build profile {name}.");
        }

        static void ThrowIfNotOnDisk(ScriptableObject reference)
        {
            if (reference != null && !AssetDatabase.Contains(reference))
                throw new InvalidOperationException($"Expected on-disk asset for {reference}.");
        }

        ReferencedComponent CreateReferenceProxy(Type componentType, ScriptableObject reference)
        {
            var proxy = ScriptableObject.CreateInstance<ReferencedComponent>();
            proxy.hideFlags = HideFlags.HideInHierarchy;
            proxy.name = componentType.Name;
            proxy.reference = reference;
            proxy.referenceType = componentType;
            AssetDatabase.AddObjectToAsset(proxy, this);
            return proxy;
        }

        ReferencedComponent FindReferenceProxy(Type componentType)
        {
            foreach (var obj in LoadAllComponentObjects())
            {
                if (obj is ReferencedComponent proxy && proxy.Matches(componentType))
                    return proxy;
            }

            return null;
        }

        bool HasComponentOfType(Type componentType)
        {
            if (componentType == typeof(PlayerSettings))
                return m_PlayerSettings != null || s_GlobalPlayerSettings != null;

            return FindComponentObject(componentType) is not null;
        }

        /// <summary>
        /// Returns true when a component of type T is present, including an unassigned reference
        /// whose source has not been set.
        /// </summary>
        internal bool HasComponent<T>() where T : UnityEngine.Object => HasComponentOfType(typeof(T));

        static bool IsComponentOfType(UnityEngine.Object obj, Type componentType) =>
            componentType.IsInstanceOfType(obj) || (obj is ReferencedComponent proxy && proxy.Matches(componentType));
    }
}
