// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper methods for state components that are persisted to the file system.
    /// </summary>
    static class PersistedStateComponentHelpers
    {
        /// <summary>
        /// Serializes a <see cref="IStateComponent"/> to JSON.
        /// </summary>
        /// <param name="obj">The state component to serialize.</param>
        /// <returns>A JSON string containing the serialized state component.</returns>
        public static string Serialize(IStateComponent obj)
        {
            if (obj == null)
                return "";

            return JsonUtility.ToJson(obj);
        }

        /// <summary>
        /// Deserializes a state component from a JSON string.
        /// </summary>
        /// <param name="jsonString">The string containing the JSON representation of the state component.</param>
        /// <typeparam name="T">The type of the state component.</typeparam>
        /// <returns>A state component of type <typeparamref name="T"/>.</returns>
        public static T Deserialize<T>(string jsonString) where T : IStateComponent
        {
            var obj = JsonUtility.FromJson<T>(jsonString);
            return obj;
        }

        /// <summary>
        /// Deserializes a state component from a JSON string.
        /// </summary>
        /// <param name="jsonString">The string containing the JSON representation of the state component.</param>
        /// <param name="stateComponentType">The type of the object to deserialize.</param>
        /// <returns>An object that represents the state component. The object can safely be cast to <paramref name="stateComponentType"/>.</returns>
        public static object Deserialize(string jsonString, Type stateComponentType)
        {
            var obj = JsonUtility.FromJson(jsonString, stateComponentType);

            return obj;
        }

        /// <summary>
        /// Saves the state component and move the state component associated with <paramref name="graphModel"/> in it.
        /// </summary>
        /// <param name="stateComponent">The state component to save and replace.</param>
        /// <param name="updater">The state component updater used to move the newly state component in <paramref name="stateComponent"/>.</param>
        /// <param name="graphModel">The graph model for which we want to load a state component.</param>
        /// <typeparam name="TComponent">The state component type.</typeparam>
        /// <typeparam name="TUpdater">The updater type.</typeparam>
        public static void SaveAndLoadPersistedStateForGraph<TComponent, TUpdater>(TComponent stateComponent, TUpdater updater, GraphModel graphModel)
            where TComponent : StateComponent<TUpdater>, IPersistedStateComponent, new()
            where TUpdater : class, IStateComponentUpdater, new()
        {
            var key = PersistedState.MakeGraphKey(graphModel);
            PersistedState.StoreStateComponent(stateComponent, stateComponent.ComponentName, stateComponent.ViewGuid, stateComponent.GraphKey);

            if (key != stateComponent.GraphKey)
            {
                var newState = PersistedState.GetOrCreatePersistedStateComponent<TComponent>(stateComponent.ComponentName, stateComponent.ViewGuid, key);
                updater.RestoreFromPersistedState(newState);
            }
        }
    }
}
