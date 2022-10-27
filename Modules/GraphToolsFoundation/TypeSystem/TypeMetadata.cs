// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Information about a <see cref="TypeHandle"/>.
    /// </summary>
    class TypeMetadata : TypeMetadataBase
    {
        readonly Type m_Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMetadata" /> class.
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/> instance represented by the metadata.</param>
        /// <param name="type">The type represented by <paramref name="typeHandle"/>.</param>
        public TypeMetadata(TypeHandle typeHandle, Type type)
            : base(typeHandle)
        {
            m_Type = type;
        }

        /// <inheritdoc />
        public override string FriendlyName => TypeHandle.IsCustomTypeHandle_Internal ? TypeHandle.Identification : m_Type.FriendlyName();

        /// <inheritdoc />
        public override string Namespace => m_Type.Namespace ?? string.Empty;

        /// <inheritdoc />
        public override bool IsEnum => m_Type.IsEnum;

        /// <inheritdoc />
        public override bool IsClass => m_Type.IsClass;

        /// <inheritdoc />
        public override bool IsValueType => m_Type.IsValueType;

        /// <inheritdoc />
        public override bool IsAssignableFrom(TypeMetadataBase metadata) => metadata.IsAssignableTo(m_Type);

        /// <inheritdoc />
        public override bool IsAssignableFrom(Type type) => m_Type.IsAssignableFrom(type);

        /// <inheritdoc />
        public override bool IsAssignableTo(Type t) => t.IsAssignableFrom(m_Type);

        /// <inheritdoc />
        public override bool IsSubclassOf(Type t) => m_Type.IsSubclassOf(t);

        /// <inheritdoc />
        public override bool IsSuperclassOf(Type t) => t.IsSubclassOf(m_Type);
    }
}
