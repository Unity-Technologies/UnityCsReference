// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderApiReflection
{
    // Used to denote that a given internal element should be made public in the future.
    // This attribute definition itself should be deleted in the future.
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal class ShouldBePublicAttribute : Attribute
    {
    }
}
