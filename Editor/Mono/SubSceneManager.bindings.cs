// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    /// <summary>
    /// The sub scene manager.
    /// </summary>
    [NativeHeader("Runtime/SceneManager/SubSceneManager.h")]
    [NativeHeader("Runtime/SceneManager/SubSceneManagerBindings.h")]
    internal static class SubSceneManager
    {
        /// <summary>
        /// Register a game object as a sub scene.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <param name="description">The sub scene description.</param>
        public static void Register(GameObject gameObject, SubSceneDescription description) => Register(gameObject.GetEntityId(), description);

        /// <summary>
        /// Unregister a game object as a sub scene.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void Unregister(GameObject gameObject) => Unregister(gameObject.GetEntityId());

        /// <summary>
        /// Determine if a game object is a sub scene.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <returns><see langword="true"/> if the game object is a sub scene, <see langword="false"/> otherwise.</returns>
        public static bool IsSubScene(GameObject gameObject) => IsSubSceneFromInstanceID(gameObject.GetEntityId());

        /// <summary>
        /// Determine if a scene is a sub scene.
        /// </summary>
        /// <param name="scene">The scene.</param>
        /// <returns><see langword="true"/> if the scene is a sub scene, <see langword="false"/> otherwise.</returns>
        public static bool IsSubScene(Scene scene) => IsSubSceneFromScene(scene);

        /// <summary>
        /// Try to get the sub scene description for a game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <param name="description">The returned sub scene description.</param>
        /// <returns><see langword="true"/> if the sub scene description was found, <see langword="false"/> otherwise.</returns>
        public static bool TryGetSubSceneDescription(GameObject gameObject, out SubSceneDescription description) => TryGetSubSceneDescriptionFromInstanceID(gameObject.GetEntityId(), out description);

        /// <summary>
        /// Try to get the sub scene description for a scene.
        /// </summary>
        /// <param name="scene">The scene.</param>
        /// <param name="description">The returned sub scene description.</param>
        /// <returns><see langword="true"/> if the sub scene description was found, <see langword="false"/> otherwise.</returns>
        public static bool TryGetSubSceneDescription(Scene scene, out SubSceneDescription description) => TryGetSubSceneDescriptionFromScene(scene, out description);

        /// <summary>
        /// Gets the scene for a game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <returns>The sub scene.</returns>
        public static Scene GetScene(GameObject gameObject) => GetSceneFromInstanceID(gameObject.GetEntityId());

        /// <summary>
        /// Gets the game object for a sub scene.
        /// </summary>
        /// <param name="scene">The sub scene.</param>
        /// <returns>The game object.</returns>
        public static GameObject GetGameObject(Scene scene) => GetGameObjectFromScene(scene);

        extern public static bool HasAnySubSceneRegistered { [FreeFunction("SubSceneManagerBindings::HasAnySubSceneRegistered", IsThreadSafe = true)] get; }

        [StaticAccessor("SubSceneManager::Get()")]
        static extern void Register(EntityId instanceID, SubSceneDescription description);

        [StaticAccessor("SubSceneManager::Get()")]
        static extern void Unregister(EntityId instanceID);

        [FreeFunction("SubSceneManagerBindings::IsSubSceneFromEntityId", IsThreadSafe = true)]
        static extern bool IsSubSceneFromInstanceID(EntityId instanceID);

        [FreeFunction("SubSceneManagerBindings::IsSubSceneFromScene", IsThreadSafe = true)]
        static extern bool IsSubSceneFromScene(Scene scene);

        [FreeFunction("SubSceneManagerBindings::TryGetSubSceneDescriptionFromEntityId", IsThreadSafe = true)]
        static extern bool TryGetSubSceneDescriptionFromInstanceID(EntityId instanceID, out SubSceneDescription description);

        [FreeFunction("SubSceneManagerBindings::TryGetSubSceneDescriptionFromScene", IsThreadSafe = true)]
        static extern bool TryGetSubSceneDescriptionFromScene(Scene scene, out SubSceneDescription description);

        [FreeFunction("SubSceneManagerBindings::GetSceneFromEntityId", IsThreadSafe = true)]
        static extern Scene GetSceneFromInstanceID(EntityId instanceID);

        [FreeFunction("SubSceneManagerBindings::GetGameObjectFromScene", IsThreadSafe = true)]
        static extern GameObject GetGameObjectFromScene(Scene scene);
    }
}
