// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper methods for <see cref="SerializableGUID"/>
    /// </summary>
    static class SerializableGUIDExtensions
    {
        /// <summary>
        /// Converts a <see cref="GUID"/> to a <see cref="SerializableGUID"/>.
        /// </summary>
        /// <param name="guid">The GUID to convert.</param>
        /// <returns>The resulting SerializableGUID.</returns>
        public static unsafe SerializableGUID ToSerializableGUID(this GUID guid)
        {
            return *(SerializableGUID*)&guid;
        }

        /// <summary>
        /// Converts a <see cref="SerializableGUID"/> to a <see cref="GUID"/>.
        /// </summary>
        /// <param name="guid">The SerializableGUID to convert.</param>
        /// <returns>The resulting GUID.</returns>
        public static unsafe GUID ToGUID(this SerializableGUID guid)
        {
            return *(GUID*)&guid;
        }
    }
}
