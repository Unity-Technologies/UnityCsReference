// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe.NotBurstCompatible;

namespace Unity.Entities.Serialization.Binary
{
    unsafe partial class BinaryAdapter :
        IBinaryAdapter<sbyte>,
        IBinaryAdapter<short>,
        IBinaryAdapter<int>,
        IBinaryAdapter<long>,
        IBinaryAdapter<byte>,
        IBinaryAdapter<ushort>,
        IBinaryAdapter<uint>,
        IBinaryAdapter<ulong>,
        IBinaryAdapter<float>,
        IBinaryAdapter<double>,
        IBinaryAdapter<bool>,
        IBinaryAdapter<char>,
        IBinaryAdapter<string>
    {
        void IBinaryAdapter<sbyte>.Serialize(in BinarySerializationContext<sbyte> context, sbyte value)
            => context.Writer->Add(value);

        void IBinaryAdapter<short>.Serialize(in BinarySerializationContext<short> context, short value)
            => context.Writer->Add(value);

        void IBinaryAdapter<int>.Serialize(in BinarySerializationContext<int> context, int value)
            => context.Writer->Add(value);

        void IBinaryAdapter<long>.Serialize(in BinarySerializationContext<long> context, long value)
            => context.Writer->Add(value);

        void IBinaryAdapter<byte>.Serialize(in BinarySerializationContext<byte> context, byte value)
            => context.Writer->Add(value);

        void IBinaryAdapter<ushort>.Serialize(in BinarySerializationContext<ushort> context, ushort value)
            => context.Writer->Add(value);

        void IBinaryAdapter<uint>.Serialize(in BinarySerializationContext<uint> context, uint value)
            => context.Writer->Add(value);

        void IBinaryAdapter<ulong>.Serialize(in BinarySerializationContext<ulong> context, ulong value)
            => context.Writer->Add(value);

        void IBinaryAdapter<float>.Serialize(in BinarySerializationContext<float> context, float value)
            => context.Writer->Add(value);

        void IBinaryAdapter<double>.Serialize(in BinarySerializationContext<double> context, double value)
            => context.Writer->Add(value);

        void IBinaryAdapter<bool>.Serialize(in BinarySerializationContext<bool> context, bool value)
            => context.Writer->Add((byte)(value ? 1 : 0));

        void IBinaryAdapter<char>.Serialize(in BinarySerializationContext<char> context, char value)
            => context.Writer->Add(value);

        void IBinaryAdapter<string>.Serialize(in BinarySerializationContext<string> context, string value)
            => context.Writer->AddNBC(value);

        sbyte IBinaryAdapter<sbyte>.Deserialize(in BinaryDeserializationContext<sbyte> context)
            => context.Reader->ReadNext<sbyte>();

        short IBinaryAdapter<short>.Deserialize(in BinaryDeserializationContext<short> context)
            => context.Reader->ReadNext<short>();

        int IBinaryAdapter<int>.Deserialize(in BinaryDeserializationContext<int> context)
            => context.Reader->ReadNext<int>();

        long IBinaryAdapter<long>.Deserialize(in BinaryDeserializationContext<long> context)
            => context.Reader->ReadNext<long>();

        byte IBinaryAdapter<byte>.Deserialize(in BinaryDeserializationContext<byte> context)
            => context.Reader->ReadNext<byte>();

        ushort IBinaryAdapter<ushort>.Deserialize(in BinaryDeserializationContext<ushort> context)
            => context.Reader->ReadNext<ushort>();

        uint IBinaryAdapter<uint>.Deserialize(in BinaryDeserializationContext<uint> context)
            => context.Reader->ReadNext<uint>();

        ulong IBinaryAdapter<ulong>.Deserialize(in BinaryDeserializationContext<ulong> context)
            => context.Reader->ReadNext<ulong>();

        float IBinaryAdapter<float>.Deserialize(in BinaryDeserializationContext<float> context)
            => context.Reader->ReadNext<float>();

        double IBinaryAdapter<double>.Deserialize(in BinaryDeserializationContext<double> context)
            => context.Reader->ReadNext<double>();

        bool IBinaryAdapter<bool>.Deserialize(in BinaryDeserializationContext<bool> context)
            => context.Reader->ReadNext<byte>() == 1;

        char IBinaryAdapter<char>.Deserialize(in BinaryDeserializationContext<char> context)
            => context.Reader->ReadNext<char>();

        string IBinaryAdapter<string>.Deserialize(in BinaryDeserializationContext<string> context)
        {
            context.Reader->ReadNextNBC(out var value);
            return value;
        }
    }
}
