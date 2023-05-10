// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Used to persist the tool editor state to file system, in the k_StateCache folder.
    /// </summary>
    static class PersistedState
    {
        static readonly StateCache_Internal k_StateCache = new StateCache_Internal("Library/StateCache/ToolState/");

        /// <summary>
        /// Generates a unique key for a <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="graphModel">The graph for which to generate a key.</param>
        /// <returns>A unique key for the graph.</returns>
        public static string MakeGraphKey(GraphModel graphModel)
        {
            return graphModel?.Guid.ToString() ?? default(Hash128).ToString();
        }

        static Hash128 GetComponentStorageHash(string componentName, Hash128 viewGuid, string graphKey = "")
        {
            Hash128 hash = default;
            hash.Append(componentName);
            hash.Append(viewGuid.ToString());
            hash.Append(graphKey);
            return hash;
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGuid"/> and the graph.
        /// If none exists, creates a new one.
        /// </summary>
        /// <param name="componentName">The name of the component. If null, the component type name will be used.</param>
        /// <param name="viewGuid">The guid identifying the view.</param>
        /// <param name="graphKey">A unique key representing the graph backing this component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public static TComponent GetOrCreatePersistedStateComponent<TComponent>(string componentName, Hash128 viewGuid, string graphKey)
            where TComponent : class, IPersistedStateComponent, new()
        {
            TComponent CreateFunc() => new TComponent { ViewGuid = viewGuid, GraphKey = graphKey };
            return GetOrCreatePersistedStateComponent(componentName, viewGuid, graphKey, CreateFunc);
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGuid"/> and the graph.
        /// If none exists, creates a new one.
        /// </summary>
        /// <param name="componentName">The name of the component. If null, the component type name will be used.</param>
        /// <param name="viewGuid">The guid identifying the view.</param>
        /// <param name="graphKey">A unique key representing the graph backing this component.</param>
        /// <param name="createFunc">A function to create a new state component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public static TComponent GetOrCreatePersistedStateComponent<TComponent>(string componentName, Hash128 viewGuid, string graphKey, Func<TComponent> createFunc)
            where TComponent : class, IPersistedStateComponent
        {
            componentName ??= typeof(TComponent).FullName;
            var componentKey = GetComponentStorageHash(componentName, viewGuid, graphKey);

            Func<TComponent> wrappedCreationFunc = null;
            if (createFunc != null)
            {
                wrappedCreationFunc = () =>
                {
                    var c = createFunc();
                    c.ViewGuid = viewGuid;
                    c.GraphKey = graphKey;
                    return c;
                };
            }

            return k_StateCache.GetState(componentKey, wrappedCreationFunc);
        }

        /// <summary>
        /// Adds a state component to the state cache, using <paramref name="componentName"/>,
        /// <paramref name="viewGuid"/> and <paramref name="graphKey"/> to build a unique key for the state component.
        /// </summary>
        /// <param name="stateComponent">The state component to write.</param>
        /// <param name="componentName">The name of the state component.</param>
        /// <param name="viewGuid">The view GUID for the state component. Can be default
        /// if no view is associated with the state component.</param>
        /// <param name="graphKey">The graph key for the state component. Can be default
        /// if no graph is associated with the state component.</param>
        public static void StoreStateComponent(IStateComponent stateComponent, string componentName, Hash128 viewGuid, string graphKey)
        {
            var componentKey = GetComponentStorageHash(componentName, viewGuid, graphKey);
            k_StateCache.StoreState(componentKey, stateComponent);
        }

        /// <summary>
        /// Writes all state components to disk.
        /// </summary>
        public static void Flush()
        {
            k_StateCache.Flush();
        }
    }
}
