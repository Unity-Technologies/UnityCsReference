// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

using UnityEditor;

namespace Unity.GraphToolkit
{
    /// <summary>
    /// Provides helper methods for working with Hash128 and GUID types.
    /// </summary>
    /// <remarks>
    /// The `unsafe` keyword is required because the methods use pointers and `Buffer.MemoryCopy` for memory manipulation.
    /// </remarks>
    [UnityRestricted]
    internal static unsafe class Hash128Helpers
    {
        /// <summary>
        /// Generates a unique Hash128.
        /// </summary>
        /// <returns>A new Hash128.</returns>
        /// <remarks> This method uses a new GUID to generate a unique Hash128 value.
        /// </remarks>

        public static Hash128 GenerateUnique()
        {
            return Hash128.Compute(Guid.NewGuid().ToByteArray());
        }

        /// <summary>
        /// Converts a Hash128 to a GUID.
        /// </summary>
        /// <param name="hash128">The Hash128 to convert.</param>
        /// <returns>A GUID representation of the Hash128.</returns>
        /// <remarks> This method performs a memory copy to convert the Hash128 to a GUID.
        /// </remarks>
        public static GUID ToGUID(this Hash128 hash128)
        {
            var guid = new GUID();
            Buffer.MemoryCopy(&hash128, &guid, sizeof(GUID), sizeof(Hash128));
            return guid;
        }

        /// <summary>
        /// Converts a GUID to a Hash128.
        /// </summary>
        /// <param name="guid">The GUID to convert.</param>
        /// <returns>A Hash128 representation of the GUID.</returns>
        /// <remarks>
        /// This method performs a memory copy to convert the GUID to a Hash128.
        /// </remarks>
        public static Hash128 FromGUID(GUID guid)
        {
            Hash128 hash128 = default;
            Buffer.MemoryCopy(&guid, &hash128, sizeof(Hash128), sizeof(GUID));
            return hash128;
        }
    }
}
