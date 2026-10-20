// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Serialization.Binary
{
    static unsafe partial class BinarySerialization
    {
        /// <summary>
        /// Serializes the given object to the given stream as binary.
        /// </summary>
        /// <param name="stream">The stream to write the object to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="parameters">Parameters to use when writing.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        public static void ToBinary<T>(UnsafeAppendBuffer* stream, T value, BinarySerializationParameters parameters = default)
        {
            PropertyWrapper<T>.EnsurePropertyBag();
            var container = new PropertyWrapper<T>(value);

            var state = parameters.State ?? (parameters.RequiresThreadSafety ? new BinarySerializationState() : GetSharedState());
            var visitor = state.GetBinaryPropertyWriter();
            
            visitor.SetStream(stream);
            visitor.SetSerializedType(parameters.SerializedType);
            visitor.SetDisableRootAdapters(parameters.DisableRootAdapters);
            visitor.SetGlobalAdapters(GetGlobalAdapters());
            visitor.SetUserDefinedAdapters(parameters.UserDefinedAdapters);
            visitor.SetSerializedReferences(parameters.DisableSerializedReferences ? default : state.GetSerializedReferences());
            
            using (visitor.Lock()) PropertyContainer.Accept(visitor, ref container);
            
            if (null == parameters.State && !parameters.DisableSerializedReferences) state.GetSerializedReferences().Clear();
        }
    }
}
