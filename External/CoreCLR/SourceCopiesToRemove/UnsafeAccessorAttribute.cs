// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides access to an inaccessible member of a specific type.
    /// </summary>
    /// <remarks>
    /// This is a copy of the definition in the framework at https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnsafeAccessorAttribute.cs
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [UnityEngine.Bindings.VisibleToOtherModules]
    internal sealed class UnsafeAccessorAttribute : Attribute
    {
        public UnsafeAccessorAttribute(UnsafeAccessorKind kind)
        {
            Kind = kind;
        }

        public UnsafeAccessorKind Kind { get; }

        public string Name { get; set; }
    }

    /// <summary>
    /// Specifies the kind of target to which an <see cref="UnsafeAccessorAttribute"/> is providing access.
    /// </summary>
    [UnityEngine.Bindings.VisibleToOtherModules]
    internal enum UnsafeAccessorKind
    {
        Constructor,
        Method,
        StaticMethod,
        Field,
        StaticField,
    }
}
