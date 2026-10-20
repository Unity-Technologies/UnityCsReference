// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace Unity.Entities.Serialization.Binary
{
    /// <summary>
    /// Base interface for binary adapters.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal interface IBinaryAdapter
    {

    }

    /// <summary>
    /// Implement this interface to override serialization and deserialization behaviour for a given type.
    /// </summary>
    /// <typeparam name="TValue">The type to override serialization for.</typeparam>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal interface IBinaryAdapter<TValue> : IBinaryAdapter
    {
        /// <summary>
        /// Invoked during serialization to handle writing out the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value to write.</param>
        void Serialize(in BinarySerializationContext<TValue> context, TValue value);

        /// <summary>
        /// Invoked during deserialization to handle reading the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The deserialized value.</returns>
        TValue Deserialize(in BinaryDeserializationContext<TValue> context);
    }

    /// <summary>
    /// Implement this interface to override serialization and deserialization behaviour for a given type.
    /// </summary>
    /// <typeparam name="TValue">The type to override serialization for.</typeparam>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal interface IContravariantBinaryAdapter<in TValue> : IBinaryAdapter
    {
        /// <summary>
        /// Invoked during serialization to handle writing out the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value to write.</param>
        void Serialize(IBinarySerializationContext context, TValue value);

        /// <summary>
        /// Invoked during deserialization to handle reading the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The deserialized value.</returns>
        object Deserialize(IBinaryDeserializationContext context);
    }
}
