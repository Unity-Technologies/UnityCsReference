// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe.NotBurstCompatible;

namespace Unity.Entities.Serialization.Binary
{
    unsafe partial class BinaryAdapter :
        IBinaryAdapter<Guid>,
        IBinaryAdapter<DateTime>,
        IBinaryAdapter<TimeSpan>,
        IBinaryAdapter<Version>
    {
        void IBinaryAdapter<Guid>.Serialize(in BinarySerializationContext<Guid> context, Guid value)
            => context.Writer->AddNBC(value.ToString("N", CultureInfo.InvariantCulture));

        Guid IBinaryAdapter<Guid>.Deserialize(in BinaryDeserializationContext<Guid> context)
        {
            context.Reader->ReadNextNBC(out var str);
            return Guid.TryParseExact(str, "N", out var value) ? value : default;
        }

        void IBinaryAdapter<DateTime>.Serialize(in BinarySerializationContext<DateTime> context, DateTime value)
            => context.Writer->AddNBC(value.ToString("o", CultureInfo.InvariantCulture));

        DateTime IBinaryAdapter<DateTime>.Deserialize(in BinaryDeserializationContext<DateTime> context)
        {
            context.Reader->ReadNextNBC(out var str);
            return DateTime.TryParseExact(str, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : default;
        }

        void IBinaryAdapter<TimeSpan>.Serialize(in BinarySerializationContext<TimeSpan> context, TimeSpan value)
            => context.Writer->AddNBC(value.ToString("c", CultureInfo.InvariantCulture));

        TimeSpan IBinaryAdapter<TimeSpan>.Deserialize(in BinaryDeserializationContext<TimeSpan> context)
        {
            context.Reader->ReadNextNBC(out var str);
            return TimeSpan.TryParseExact(str, "c", CultureInfo.InvariantCulture, out var value) ? value : default;
        }

        void IBinaryAdapter<Version>.Serialize(in BinarySerializationContext<Version> context, Version value)
            => context.Writer->AddNBC(value.ToString());

        Version IBinaryAdapter<Version>.Deserialize(in BinaryDeserializationContext<Version> context)
        {
            context.Reader->ReadNextNBC(out var str);
            return new Version(str);
        }
    }
}
