// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("UnityEngine.DSPGraphModule")]

namespace Unity.Audio
{
    [VisibleToOtherModules]
    internal interface IHandle<HandleType> : IValidatable, IEquatable<HandleType>
        where HandleType : struct, IHandle<HandleType>
    {
    }
}
