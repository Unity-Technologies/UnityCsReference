// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation;

static class Hash128Extensions
{
    // <summary>
    /// Generates a unique Hash128.
    /// </summary>
    /// <returns>A new Hash128.</returns>
    public static Hash128 Generate()
    {
        return Hash128.Compute(Guid.NewGuid().ToByteArray());
    }

    // For tests
    public static (ulong, ulong) ToParts_Internal(this Hash128 hash)
    {
        return (hash.u64_0, hash.u64_1);
    }
}
