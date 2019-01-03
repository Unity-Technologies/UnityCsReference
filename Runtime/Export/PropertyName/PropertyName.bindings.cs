// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Utilities/PropertyName.h")]
    class PropertyNameUtils
    {
        [FreeFunction]
        public extern static PropertyName PropertyNameFromString([Unmarshalled] string name);
        [FreeFunction]
        public extern static string StringFromPropertyName(PropertyName propertyName);
        /// <summary>
        /// Returns the number of conflicts for the given id.
        /// Returns 0 if the id is unregistered or it has been mapped to only a single string.
        /// Otherwise returns the number of unique strings mapped to the given id.
        /// </summary>
        [FreeFunction]
        public extern static int ConflictCountForID(int id);
    }
}
