// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe.NotBurstCompatible;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities.Serialization.Binary
{
    /// <summary>
    /// This class is used to store state between multiple serialization calls.
    /// By passing this to <see cref="BinarySerializationParameters"/> will allow visitors and serialized references to be re-used.
    /// </summary>
    internal class BinarySerializationState
    {
        BinaryPropertyWriter m_BinaryPropertyWriter;
        BinaryPropertyReader m_BinaryPropertyReader;
        SerializedReferences m_SerializedReferences;

        /// <summary>
        /// Returns true if the given state is in use by either serialization or de-serialization.
        /// </summary>
        internal bool IsLocked => m_BinaryPropertyWriter != null && m_BinaryPropertyWriter.IsLocked || m_BinaryPropertyReader != null && m_BinaryPropertyReader.IsLocked;

        /// <summary>
        /// Gets the shared <see cref="BinaryPropertyWriter"/>.
        /// </summary>
        /// <returns>The <see cref="BinaryPropertyWriter"/>.</returns>
        internal BinaryPropertyWriter GetBinaryPropertyWriter()
        {
            if (null != m_BinaryPropertyWriter)
                return m_BinaryPropertyWriter.IsLocked ? new BinaryPropertyWriter() : m_BinaryPropertyWriter;

            m_BinaryPropertyWriter = new BinaryPropertyWriter();
            return m_BinaryPropertyWriter;
        }

        /// <summary>
        /// Gets the shared <see cref="BinaryPropertyReader"/>.
        /// </summary>
        /// <returns>The <see cref="BinaryPropertyReader"/>.</returns>
        internal BinaryPropertyReader GetBinaryPropertyReader()
        {
            if (null != m_BinaryPropertyReader)
                return m_BinaryPropertyReader.IsLocked ? new BinaryPropertyReader() : m_BinaryPropertyReader;

            m_BinaryPropertyReader = new BinaryPropertyReader();
            return m_BinaryPropertyReader;
        }

        /// <summary>
        /// Gets the shared <see cref="SerializedReferences"/>.
        /// </summary>
        /// <returns>The <see cref="SerializedReferences"/>.</returns>
        internal SerializedReferences GetSerializedReferences()
            => m_SerializedReferences ??= new SerializedReferences();

        /// <summary>
        /// Clears the serialized references state.
        /// </summary>
        internal void ClearSerializedReferences()
        {
            m_SerializedReferences?.Clear();
        }
    }

    /// <summary>
    /// Custom parameters to use for binary serialization or deserialization.
    /// </summary>
    internal struct BinarySerializationParameters
    {
        /// <summary>
        /// By default, a polymorphic root type will have it's assembly qualified type name written to the stream. Use this
        /// parameter to provide a known root type at both serialize and deserialize time to avoid writing this information.
        /// </summary>
        public Type SerializedType { get; set; }

        /// <summary>
        /// By default, adapters are evaluated for root objects. Use this to change the default behaviour.
        /// </summary>
        public bool DisableRootAdapters { get; set; }

        /// <summary>
        /// Provide a custom set of adapters for the serialization and deserialization.
        /// </summary>
        /// <remarks>
        /// These adapters will be evaluated first before any global or built in adapters.
        /// </remarks>
        public List<IBinaryAdapter> UserDefinedAdapters { get; set; }

        /// <summary>
        /// This parameter indicates if the serializer should be thread safe. The default value is false.
        /// </summary>
        /// <remarks>
        /// Setting this to true will cause managed allocations for the internal visitor.
        /// </remarks>
        public bool RequiresThreadSafety { get; set; }

        /// <summary>
        /// By default, references between objects are serialized. Use this to always write a copy of the object to the output.
        /// </summary>
        public bool DisableSerializedReferences { get; set; }

        /// <summary>
        /// Sets the state object for serialization. This can be used to share resources across multiple calls to serialize and deserialize.
        /// </summary>
        public BinarySerializationState State { get; set; }
    }

    /// <summary>
    /// High level API for serializing or deserializing json data from string, file or stream.
    /// </summary>
    internal static partial class BinarySerialization
    {
        [AutoStaticsCleanupOnCodeReload]
        static readonly List<IBinaryAdapter> k_Adapters = new List<IBinaryAdapter>();
        [NoAutoStaticsCleanup] // Each use clears the references it holds.
        static readonly BinarySerializationState k_SharedState = new BinarySerializationState();

        static BinarySerializationState GetSharedState()
        {
            // The current state is in use by the current stack. We must return a new instance to avoid trashing the serialized references.
            if (k_SharedState.IsLocked)
                return new BinarySerializationState();

            k_SharedState.ClearSerializedReferences();
            return k_SharedState;
        }

        /// <summary>
        /// Adds the specified <see cref="IBinaryAdapter"/> to the set of global adapters. This is be included by default in all BinarySerialization calls.
        /// </summary>
        /// <param name="adapter">The adapter to add.</param>
        /// <exception cref="ArgumentException">The given adapter is already registered.</exception>
        public static void AddGlobalAdapter(IBinaryAdapter adapter)
        {
            if (k_Adapters.Contains(adapter))
                throw new ArgumentException("IBinaryAdapter has already been registered.");

            k_Adapters.Add(adapter);
        }

        static List<IBinaryAdapter> GetGlobalAdapters() => k_Adapters;

        internal static unsafe void WritePrimitiveUnsafe<TValue>(UnsafeAppendBuffer* stream, ref TValue value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    stream->Add(UnsafeUtility.As<TValue, sbyte>(ref value));
                    return;
                case TypeCode.Int16:
                    stream->Add(UnsafeUtility.As<TValue, short>(ref value));
                    return;
                case TypeCode.Int32:
                    stream->Add(UnsafeUtility.As<TValue, int>(ref value));
                    return;
                case TypeCode.Int64:
                    stream->Add(UnsafeUtility.As<TValue, long>(ref value));
                    return;
                case TypeCode.Byte:
                    stream->Add(UnsafeUtility.As<TValue, byte>(ref value));
                    return;
                case TypeCode.UInt16:
                    stream->Add(UnsafeUtility.As<TValue, ushort>(ref value));
                    return;
                case TypeCode.UInt32:
                    stream->Add(UnsafeUtility.As<TValue, uint>(ref value));
                    return;
                case TypeCode.UInt64:
                    stream->Add(UnsafeUtility.As<TValue, ulong>(ref value));
                    return;
                case TypeCode.Single:
                    stream->Add(UnsafeUtility.As<TValue, float>(ref value));
                    return;
                case TypeCode.Double:
                    stream->Add(UnsafeUtility.As<TValue, double>(ref value));
                    return;
                case TypeCode.Boolean:
                    stream->Add(UnsafeUtility.As<TValue, bool>(ref value) ? (byte) 1 : (byte) 0);
                    return;
                case TypeCode.Char:
                    stream->Add(UnsafeUtility.As<TValue, char>(ref value));
                    return;
                case TypeCode.String:
                    stream->AddNBC(value as string);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static unsafe void WritePrimitiveBoxed(UnsafeAppendBuffer* stream, object value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    stream->Add((sbyte) value);
                    return;
                case TypeCode.Int16:
                    stream->Add((short) value);
                    return;
                case TypeCode.Int32:
                    stream->Add((int) value);
                    return;
                case TypeCode.Int64:
                    stream->Add((long) value);
                    return;
                case TypeCode.Byte:
                    stream->Add((byte) value);
                    return;
                case TypeCode.UInt16:
                    stream->Add((ushort) value);
                    return;
                case TypeCode.UInt32:
                    stream->Add((uint) value);
                    return;
                case TypeCode.UInt64:
                    stream->Add((ulong) value);
                    return;
                case TypeCode.Single:
                    stream->Add((float) value);
                    return;
                case TypeCode.Double:
                    stream->Add((double) value);
                    return;
                case TypeCode.Boolean:
                    stream->Add((bool) value ? (byte) 1 : (byte) 0);
                    return;
                case TypeCode.Char:
                    stream->Add((char) value);
                    return;
                case TypeCode.String:
                    stream->AddNBC(value as string);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static unsafe void ReadPrimitiveUnsafe<TValue>(UnsafeAppendBuffer.Reader* stream, ref TValue value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    stream->ReadNext<sbyte>(out var sbyteValue);
                    value = UnsafeUtility.As<sbyte, TValue>(ref sbyteValue);
                    return;
                case TypeCode.Int16:
                    stream->ReadNext<short>(out var shortValue);
                    value = UnsafeUtility.As<short, TValue>(ref shortValue);
                    return;
                case TypeCode.Int32:
                    stream->ReadNext<int>(out var intValue);
                    value = UnsafeUtility.As<int, TValue>(ref intValue);
                    return;
                case TypeCode.Int64:
                    stream->ReadNext<long>(out var longValue);
                    value = UnsafeUtility.As<long, TValue>(ref longValue);
                    return;
                case TypeCode.Byte:
                    stream->ReadNext<byte>(out var byteValue);
                    value = UnsafeUtility.As<byte, TValue>(ref byteValue);
                    return;
                case TypeCode.UInt16:
                    stream->ReadNext<ushort>(out var ushortValue);
                    value = UnsafeUtility.As<ushort, TValue>(ref ushortValue);
                    return;
                case TypeCode.UInt32:
                    stream->ReadNext<uint>(out var uintValue);
                    value = UnsafeUtility.As<uint, TValue>(ref uintValue);
                    return;
                case TypeCode.UInt64:
                    stream->ReadNext<ulong>(out var ulongValue);
                    value = UnsafeUtility.As<ulong, TValue>(ref ulongValue);
                    return;
                case TypeCode.Single:
                    stream->ReadNext<float>(out var floatValue);
                    value = UnsafeUtility.As<float, TValue>(ref floatValue);
                    return;
                case TypeCode.Double:
                    stream->ReadNext<double>(out var doubleValue);
                    value = UnsafeUtility.As<double, TValue>(ref doubleValue);
                    return;
                case TypeCode.Boolean:
                    stream->ReadNext<byte>(out var booleanValue);
                    var b = booleanValue == 1;
                    value = UnsafeUtility.As<bool, TValue>(ref b);
                    return;
                case TypeCode.Char:
                    stream->ReadNext<char>(out var charValue);
                    value = UnsafeUtility.As<char, TValue>(ref charValue);
                    return;
                case TypeCode.String:
                    stream->ReadNextNBC(out string stringValue);
                    value = (TValue) (object) stringValue;
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static unsafe void ReadPrimitiveBoxed<TValue>(UnsafeAppendBuffer.Reader* stream, ref TValue value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                {
                    var v = stream->ReadNext<sbyte>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Int16:
                {
                    var v = stream->ReadNext<short>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Int32:
                {
                    var v = stream->ReadNext<int>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Int64:
                {
                    var v = stream->ReadNext<long>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Byte:
                {
                    var v = stream->ReadNext<byte>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.UInt16:
                {
                    var v = stream->ReadNext<ushort>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.UInt32:
                {
                    var v = stream->ReadNext<uint>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.UInt64:
                {
                    var v = stream->ReadNext<ulong>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Single:
                {
                    var v = stream->ReadNext<float>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Double:
                {
                    var v = stream->ReadNext<double>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Boolean:
                {
                    var v = stream->ReadNext<byte>() == 1;
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.Char:
                {
                    var v = stream->ReadNext<char>();
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                case TypeCode.String:
                {
                    stream->ReadNextNBC(out var v);
                    TypeConversion.TryConvert(ref v, out value);
                }
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
