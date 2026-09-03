// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Generic;
namespace Unity.AcceleratorClient.Contracts;

internal readonly record struct CacheEntry(UInt128 Key, ReadOnlyMemory<byte> Value, long Size);

internal readonly record struct GetValue(ReadOnlyMemory<byte> Bytes, long Size);

internal readonly record struct PutValue(long BytesUploaded, int ChunkCount);

internal sealed record BatchGetValue(
    IReadOnlyDictionary<UInt128, CacheEntry> Hits,
    IReadOnlyList<UInt128> Misses,
    IReadOnlyDictionary<UInt128, CacheError> Faults);

internal readonly record struct BatchPutValue(int Succeeded, int Failed, long BytesUploaded);
