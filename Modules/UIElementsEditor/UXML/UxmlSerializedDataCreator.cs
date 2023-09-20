// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Editor utility methods for dealing with UxmlSerializedData.
    /// </summary>
    public static class UxmlSerializedDataCreator
    {
        /// <summary>
        /// Creates a <see cref="UxmlSerializedData"/> instance for the given type with default values applied.
        /// </summary>
        /// <param name="fullTypeName">The full type name of the Type that contains the <see cref="UxmlSerializedData"/>.
        /// Should belong to a type that is decorated with either the <see cref="UxmlElementAttribute"/> or <see cref="UxmlObjectAttribute"/> attribute.</param>
        /// <returns></returns>
        public static UxmlSerializedData CreateUxmlSerializedData(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var desc = UxmlSerializedDataRegistry.GetDescription(type.FullName);
            if (desc == null)
            {
                Debug.LogError($"Could not find UxmlSerializedData for type {type}");
                return null;

            }
            return desc.CreateDefaultSerializedData();
        }
    }
}
