// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements;

internal struct CustomData : IStyleDataGroup<CustomData>, IEquatable<CustomData>
{
    public Dictionary<string, StylePropertyValue> customProperties;

    public CustomData GetDefault()
    {
        return default;
    }

    public CustomData Copy()
    {
        return this;
    }

    public void CopyFrom(ref CustomData other)
    {
        this = other;
    }

    public bool Equals(CustomData other)
    {
        return ReferenceEquals(customProperties, other.customProperties);
    }
}
