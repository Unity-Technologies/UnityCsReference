// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Properties.Internal
{
    class ReflectedPropertyBagAttribute : Attribute{}

    [ReflectedPropertyBag]
    class ReflectedPropertyBag<TContainer> : ContainerPropertyBag<TContainer>
    {
        internal new void AddProperty<TValue>(Property<TContainer, TValue> property)
        {
            var container = default(TContainer);

            if (TryGetProperty(ref container, property.Name, out var existing))
            {
                if (existing.DeclaredValueType() == typeof(TValue))
                {
                    // Property with the same name and value type, it's safe to ignore.
                    return;
                }
                Debug.LogWarning($"Detected multiple return types for PropertyBag=[{TypeUtility.GetTypeDisplayName(typeof(TContainer))}] Property=[{property.Name}]. The property will use the most derived Type=[{TypeUtility.GetTypeDisplayName(existing.DeclaredValueType())}] and IgnoreType=[{TypeUtility.GetTypeDisplayName(property.DeclaredValueType())}].");
                return;
            }

            base.AddProperty(property);
        }
    }
}
