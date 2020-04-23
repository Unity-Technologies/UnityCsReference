// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.SubsystemsImplementation
{
    public static partial class SubsystemDescriptorStore
    {
#pragma warning disable CS0618
        internal static void RegisterDeprecatedDescriptor(SubsystemDescriptor descriptor) => RegisterDescriptor(descriptor, s_DeprecatedDescriptors);
#pragma warning restore CS0618
    }
}
