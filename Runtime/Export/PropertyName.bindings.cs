// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Bindings;

namespace UnityEngine
{
    [NativeType(Header = "Runtime/Utilities/PropertyName.h")]
    class PropertyNameUtils
    {
        [NativeMethod(IsFreeFunction = true)]
        public extern static PropertyName PropertyNameFromString([NativeParameter(Unmarshalled = true)] string name);
        [NativeMethod(IsFreeFunction = true)]
        public extern static string StringFromPropertyName(PropertyName propertyName);
        /// <summary>
        /// Returns the number of conflicts for the given id.
        /// Returns 0 if the id is unregistered or it has been mapped to only a single string.
        /// Otherwise returns the number of unique strings mapped to the given id.
        /// </summary>
        [NativeMethod(IsFreeFunction = true)]
        public extern static int ConflictCountForID(int id);
    }
}
