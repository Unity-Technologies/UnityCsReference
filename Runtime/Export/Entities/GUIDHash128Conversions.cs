// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    // Declared on GUID because Unity.Entities.Hash128 lives in the Entities module, which can't see GUID.
    public partial struct GUID
    {
        /// <summary>
        /// Implicitly convert a UnityEditor.GUID to a Hash128.
        /// </summary>
        /// <param name="guid">The UnityEditor.GUID to convert.</param>
        /// <returns>The corresponding Hash128.</returns>
        public static unsafe implicit operator Unity.Entities.Hash128(GUID guid) => *(Unity.Entities.Hash128*)&guid;

        /// <summary>
        /// Implicitly convert a Hash128 to a UnityEditor.GUID.
        /// </summary>
        /// <param name="guid">The Hash128 to convert.</param>
        /// <returns>The corresponding UnityEditor.GUID.</returns>
        public static unsafe implicit operator GUID(Unity.Entities.Hash128 guid) => *(GUID*)&guid;
    }
}
