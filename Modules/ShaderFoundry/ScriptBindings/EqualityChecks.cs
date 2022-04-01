// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.ShaderFoundry
{
    internal struct EqualityChecks
    {
        internal static bool ReferenceEquals(int index0, ShaderContainer container0, int index1, ShaderContainer container1)
        {
            // Even if the containers are different, invalid objects are always equal
            return (index0 == index1) && ((index0 == -1) || (container0 == container1));
        }

        internal static bool ReferenceEquals(FoundryHandle handle0, ShaderContainer container0, FoundryHandle handle1, ShaderContainer container1)
        {
            // Even if the containers are different, invalid objects are always equal
            return (handle0 == handle1) && ((!handle0.IsValid) || (container0 == container1));
        }
    }
}
