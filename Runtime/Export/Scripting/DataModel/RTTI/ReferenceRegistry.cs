// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Unity.DataModel;

internal sealed class ReferenceRegistry
{
    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public bool Equals(T left, T right)
        {
            return Object.ReferenceEquals(left, right);
        }

        public int GetHashCode(T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }

    internal Dictionary<object, Reference> ObjectToUDMReference { get; private set; } = new(new ReferenceEqualityComparer<object>());

    internal bool TryGetReference(object obj, out Reference reference)
    {
        return ObjectToUDMReference.TryGetValue(obj, out reference);
    }

    internal void SetReference(object obj, Reference reference)
    {
        if (obj == null)
            throw new InvalidOperationException("Trying to call SetReference with null object");
        ObjectToUDMReference[obj] = reference;
    }
}
