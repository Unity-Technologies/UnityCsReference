// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// A serializable version of <see cref="UnityEditor.GUID"/>.
    /// </summary>
    /// <remarks>
    /// This implementation is using a Hash128 backing. The binary representation is the same as the
    /// <see cref="UnityEditor.GUID"/> one, but the string version differs.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    [Obsolete("Use Hash128 instead")]
    struct SerializableGUID
    {
        [FieldOffset(0)]
        Hash128 m_Hash128;

        [SerializeField]
        [FieldOffset(0)]
        ulong m_Value0;
        [SerializeField]
        [FieldOffset(8)]
        ulong m_Value1;

        SerializableGUID(Hash128 hash)
        {
            // Values will be overriden by new Hash128 but are required for the struct constructor.
            m_Value0 = 0;
            m_Value1 = 0;
            m_Hash128 = hash;
        }

        /// <summary>
        /// Cast a SerializedGUID as a <see cref="Hash128"/>.
        /// </summary>
        /// <param name="sGuid">The SerializedGUID to cast.</param>
        /// <returns>The cast value.</returns>
        public static implicit operator Hash128(SerializableGUID sGuid) => sGuid.m_Hash128;

        /// <summary>
        /// Cast a <see cref="Hash128"/> as a SerializedGUID.
        /// </summary>
        /// <param name="hash">The <see cref="Hash128"/> to cast.</param>
        /// <returns>The cast value.</returns>
        public static implicit operator SerializableGUID(Hash128 hash) => new SerializableGUID(hash);
    }
}
