// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Proxy sub-asset linking a build profile to a standalone on-disk settings asset instead of an
    /// embedded settings object. Resolved by <see cref="BuildProfile.GetComponent{T}"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class ReferencedComponent : ScriptableObject
    {
        [SerializeField] ScriptableObject m_Reference;
        [SerializeField] MonoScript m_ReferenceType;

        public ScriptableObject reference
        {
            get => m_Reference;
            set => m_Reference = value;
        }

        public Type referenceType
        {
            get => m_ReferenceType != null ? m_ReferenceType.GetClass() : null;
            set => m_ReferenceType = value != null ? MonoScript.FromType(value) : null;
        }

        public bool Matches(Type componentType) =>
            componentType.IsInstanceOfType(reference) || componentType.IsAssignableFrom(referenceType);
    }
}
