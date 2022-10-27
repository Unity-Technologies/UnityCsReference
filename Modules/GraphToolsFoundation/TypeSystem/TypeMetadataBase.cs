// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Base class for type metadata. which holds information about a <see cref="TypeHandle"/>.
    /// </summary>
    abstract class TypeMetadataBase
    {
        /// <summary>
        /// The <see cref="TypeHandle"/> referenced by this metadata.
        /// </summary>
        public virtual TypeHandle TypeHandle { get; }

        /// <summary>
        /// A human readable name of the type.
        /// </summary>
        public abstract string FriendlyName { get; }

        /// <summary>
        /// The namespace of the type.
        /// </summary>
        public abstract string Namespace { get; }

        /// <summary>
        /// Whether this type is an enum type.
        /// </summary>
        public abstract bool IsEnum { get; }

        /// <summary>
        /// Whether this type is a class.
        /// </summary>
        public abstract bool IsClass { get; }

        /// <summary>
        /// Whether this type is a value type.
        /// </summary>
        public abstract bool IsValueType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMetadata" /> class.
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/> instance represented by the metadata.</param>
        protected TypeMetadataBase(TypeHandle typeHandle)
        {
            TypeHandle = typeHandle;
        }

        /// <summary>
        /// Determines whether an instance of this type is assignable from an instance of <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The other type.</param>
        /// <returns>True if an instance of this type is assignable from an instance of <paramref name="metadata"/>.</returns>
        public abstract bool IsAssignableFrom(TypeMetadataBase metadata);

        /// <summary>
        /// Determines whether this an instance of this type is assignable from an instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if an instance of this type is assignable from an instance of <paramref name="type"/>.</returns>
        public abstract bool IsAssignableFrom(Type type);

        /// <summary>
        /// Determines whether an instance of this type is assignable to an instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if an instance of this type is assignable to an instance of <paramref name="type"/>.</returns>
        public abstract bool IsAssignableTo(Type type);

        /// <summary>
        /// Determines whether this type is a superclass of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if this type is a superclass of <paramref name="type"/>.</returns>
        public abstract bool IsSuperclassOf(Type type);

        /// <summary>
        /// Determines whether this type is a subclass of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if this type is a subclass of <paramref name="type"/>.</returns>
        public abstract bool IsSubclassOf(Type type);
    }
}
