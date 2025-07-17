// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy.Editor
{
    [NativeHeader("Modules/HierarchyEditor/HierarchyWindowManager.h")]
    [NativeHeader("Modules/HierarchyCore/Public/Hierarchy.h")]
    static class HierarchyWindowManager
    {
        /// <summary>
        /// Register a node type handler for the hierarchy window.
        /// </summary>
        /// <remarks>
        /// A node type handler can only be registered once.
        /// </remarks>
        /// <typeparam name="T">The type of the node type handler.</typeparam>
        /// <returns><see langword="true"/> if the registred node type handler list was modified, <see langword="false"/> otherwise.</returns>
        public static bool RegisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandler => RegisterNodeTypeHandler(typeof(T));

        /// <summary>
        /// Unregister a node type handler for the hierarchy window.
        /// </summary>
        /// <typeparam name="T">The type of the node type handler.</typeparam>
        /// <returns><see langword="true"/> if the registred node type handler list was modified, <see langword="false"/> otherwise.</returns>
        public static bool UnregisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandler => UnregisterNodeTypeHandler(typeof(T));

        /// <summary>
        /// Instantiate registered node type handlers for the hierarchy window.
        /// </summary>
        /// <remarks>
        /// Each registered node type handler will be instantiated once and only once.
        /// </remarks>
        /// <param name="hierarchy">The hierarchy to instantiate registered node type handlers.</param>
        [StaticAccessor("HierarchyWindowManager::Get()"), NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public static extern void InstantiateNodeTypeHandlers(Hierarchy hierarchy);

        [StaticAccessor("HierarchyWindowManager::Get()"), NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal static extern bool RegisterNodeTypeHandler(Type type);

        [StaticAccessor("HierarchyWindowManager::Get()"), NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal static extern bool UnregisterNodeTypeHandler(Type type);
    }
}
