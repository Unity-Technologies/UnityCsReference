// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Timeline.Foundation.CSO
{
    /// <summary>
    /// Helpers for serializing and deserializing <see cref="IStateComponent"/>.
    /// </summary>
    static class StateComponentHelper
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
        /// Gets the current version of the state component.
        /// </summary>
        /// <param name="component">The state component.</param>
        /// <returns>The current version of the state component.</returns>
        internal static StateComponentVersion GetStateComponentVersion(this IStateComponent component)
        {
            return new StateComponentVersion
            {
                HashCode = component.GetHashCode(),
                Version = component.CurrentVersion
            };
        }
    }
}
