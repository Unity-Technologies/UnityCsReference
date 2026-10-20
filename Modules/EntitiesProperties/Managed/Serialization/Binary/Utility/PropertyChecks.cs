// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Helper class to validate properties during serialization and deserialization.
    /// </summary>
    static class PropertyChecks
    {
        internal static string GetReadOnlyValueTypeErrorMessage(Type type, string propertyName)
            => $"Failed to deserialize Property=[{propertyName}] for Type=[{type.FullName}]. The property is readonly value type and can not be assigned. The property needs to be writable or ignored by serialization using [{nameof(DontSerializeAttribute)}].";
        
        internal static string GetReadOnlyReferenceTypeErrorMessage(Type type, string propertyName)
            => $"Failed to deserialize Property=[{propertyName}] for Type=[{type.FullName}]. The property is readonly and the reference can not be assigned. The property needs to be writable, default constructed, or ignored by serialization using [{nameof(DontSerializeAttribute)}].";

        internal static string GetReadOnlyReferenceTypeWithInvalidTypeErrorMessage(Type type, string propertyName)
            => $"Failed to deserialize Property=[{propertyName}] for Type=[{type.FullName}]. The default constructed instance does not match the deserialized type. The property needs to be writable, default constructed with the correct type, or ignored by serialization using [{nameof(DontSerializeAttribute)}]";
        
        /// <summary>
        /// Validates a readonly property was correctly deserialized. This can be used this to catch any errors which could fail silently.
        /// </summary>
        /// <param name="property">The property being deserialized.</param>
        /// <param name="container">The container being deserialized.</param>
        /// <param name="value">The deserialized value from the stream.</param>
        /// <param name="error">When this method returns, contains the error message. If any.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns><see langword="true"/> if the value was deserialized correctly despite being readonly; otherwise, <see langword="false"/>.</returns>
        public static bool CheckReadOnlyPropertyForDeserialization<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value, out string error)
        {
            if (!property.IsReadOnly)
            {
                error = string.Empty;
                return false;
            }
            
            if (typeof(TValue).IsValueType)
            {
                // The deserialized value is a `ValueType`, since this is a copy and we can not assign back this is a failure.
                error = GetReadOnlyValueTypeErrorMessage(typeof(TContainer), property.Name);
                return true;
            }

            var existing = property.GetValue(ref container);

            if (!ReferenceEquals(existing, value))
            {
                // The deserialized value is a reference type but the instances do not match. Since we can not assign back this is a failure.
                // We also check the existing instance for null in order to provide a more meaningful error message. If the existing instance is NOT null
                // and the references do not match, this means a default constructor or field initializer was run AND a new instance was constructed by deserialization.
                // This is most commonly caused by deserializing a polymorphic type that does not match the default constructed.
                error = null == existing
                    ? GetReadOnlyReferenceTypeErrorMessage(typeof(TContainer), property.Name)
                    : GetReadOnlyReferenceTypeWithInvalidTypeErrorMessage(typeof(TContainer), property.Name);

                return true;
            }
            
            error = string.Empty;
            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the property should be skipped during serialization and deserialization.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns><see langword="true"/> if the property should be skipped; <see langword="false"/> otherwise.</returns>
        internal static bool IsPropertyExcludedFromSerialization(IProperty property)
        {
            if (property.HasAttribute<NonSerializedAttribute>())
                return true;

            if (property.HasAttribute<DontSerializeAttribute>())
                return true;

            return false;
        }
    }
}
