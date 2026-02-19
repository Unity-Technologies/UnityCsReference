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
        /// Thrown when a sub asset of the same type already exists in the build profile.
        /// </exception>
        [VisibleToOtherModules]
        public T CreateComponent<T>() where T : UnityEngine.Object
        {
            var found = GetComponent<T>();
            if (found != null)
            {
                throw new ArgumentException($"The component {typeof(T).Name} already exists in the build profile {name}.");
            }

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

            var path = AssetDatabase.GetAssetPath(this);
            var objectsFound = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in objectsFound)
            {
                if (obj is T component)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a given component to the build profile. If the component is not an asset in
        /// the project, it will be added as a sub-asset to the build profile.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="objectToAdd"></param>
        /// <exception cref="ArgumentNullException">Thrown when objectToAdd is null.</exception>
        /// <exception cref="ArgumentException">Thrown if adding a sub-asset of an existing component type.</exception>
        public void AddComponent<T>(T objectToAdd) where T : UnityEngine.Object
        {
            if (objectToAdd == null)
            {
                throw new ArgumentNullException(nameof(objectToAdd), "The object to add cannot be null.");
            }

            // Don't allow duplicates
            var found = GetComponent<T>();
            if (found != null)
            {
                throw new ArgumentException($"The component {typeof(T).Name} already exists in the build profile {name}.");
            }

            var assetPath = AssetDatabase.GetAssetPath(objectToAdd);
            if (string.IsNullOrEmpty(assetPath))
            {
                objectToAdd.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(objectToAdd, this);
                return;
            }

            // TODO: Future API will support build profile components as reference.
            throw new InvalidOperationException($"Asset {objectToAdd} has a non-null path. Build profile components should be sub-assets.");
        }

        /// <summary>
        /// Removes the first occurence of a component of type T from the build profile.
        /// </summary>
        public void RemoveComponent<T>() where T : UnityEngine.Object
        {
            var found = GetComponent<T>();
            if (found == null)
            {
                Debug.LogWarning($"The component {typeof(T).Name} does not exist in the build profile {name}. ");
                return;
            }

            if (Array.IndexOf(this.requiredComponents, found) > -1)
                throw new ArgumentException($"The component {typeof(T).Name} is required and cannot be removed from the build profile {name}.");

            AssetDatabase.RemoveObjectFromAsset(found);
        }

        /// <summary>
        /// Removes a specific component from the build profile.
        /// </summary>
        /// <param name="objectToRemove">Object to remove.</param>
        /// <exception cref="ArgumentNullException">objectToRemove is null.</exception>
        /// <exception cref="ArgumentException">objectToRemove is not part of the build profile.</exception>
        public void RemoveComponent<T>(T objectToRemove) where T : UnityEngine.Object
        {
            if (objectToRemove == null)
            {
                throw new ArgumentNullException(nameof(objectToRemove), "The object to remove cannot be null.");
            }

            var path = AssetDatabase.GetAssetPath(this);
            var objectToRemovePath = AssetDatabase.GetAssetPath(objectToRemove);

            if (objectToRemovePath != path)
            {
                throw new ArgumentException($"The object {objectToRemove} (path: {objectToRemovePath}) is not part of the build profile {path}.");
            }

            if (Array.IndexOf(this.requiredComponents, objectToRemove) > -1)
                throw new ArgumentException($"The component {typeof(T).Name} is required and cannot be removed from the build profile {name}.");

            AssetDatabase.RemoveObjectFromAsset(objectToRemove);
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
    }
}
