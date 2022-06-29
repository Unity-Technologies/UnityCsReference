// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    /// <summary>
    /// Scope for using a given set of attributes.
    /// </summary>
    public readonly struct AttributesScope : IDisposable
    {
        readonly IAttributes m_Target;
        readonly List<Attribute> m_Previous;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributesScope"/> struct assigns the given attributes to the specified target.
        /// </summary>
        /// <param name="target">The target to set the attributes for.</param>
        /// <param name="source">The source to copy attributes from.</param>
        public AttributesScope(IProperty target, IProperty source)
        {
            m_Target = target as IAttributes;
            m_Previous = (target as IAttributes)?.Attributes;

            if (null != m_Target)
                m_Target.Attributes = (source as IAttributes)?.Attributes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributesScope"/> struct assigns the given attributes to the specified target.
        /// </summary>
        /// <param name="target">The target to set the attributes for.</param>
        /// <param name="attributes">The attributes to set.</param>
        internal AttributesScope(IAttributes target, List<Attribute> attributes)
        {
            m_Target = target;
            m_Previous = target.Attributes;
            target.Attributes = attributes;
        }

        /// <summary>
        /// Re-assigns the original attributes to the target.
        /// </summary>
        public void Dispose()
        {
            if (null != m_Target)
                m_Target.Attributes = m_Previous;
        }
    }
}
