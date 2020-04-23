// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial class IntegratedSubsystem<TSubsystemDescriptor> : IntegratedSubsystem
        where TSubsystemDescriptor : ISubsystemDescriptor
    {
        [Obsolete("The property 'SubsystemDescriptor' is deprecated. Use `subsystemDescriptor` instead. UnityUpgradeable -> subsystemDescriptor", false)]
        public TSubsystemDescriptor SubsystemDescriptor => subsystemDescriptor;
    }
}
