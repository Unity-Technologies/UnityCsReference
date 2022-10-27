// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="TypeHandle"/> extension methods.
    /// </summary>
    static class TypeHandleExtensions
    {
        /// <summary>
        /// Retrieves the metadata associated with the given <see cref="TypeHandle"/> using the given
        /// <paramref name="stencil"/>'s <see cref="TypeMetadataResolver"/>.
        /// </summary>
        /// <param name="self">The TypeHandle to get the metadata from.</param>
        /// <param name="stencil">The stencil from which to get a metadata resolver.</param>
        /// <returns>The metadata associated with the given type.</returns>
        public static TypeMetadataBase GetMetadata(this TypeHandle self, StencilBase stencil)
        {
            return stencil.TypeMetadataResolver.Resolve(self);
        }

        /// <summary>
        /// Returns whether or not the type represented by <paramref name="self"/> is assignable from the type
        /// represented by <paramref name="other"/> in the context of the given <paramref name="stencil"/>.
        /// </summary>
        /// <param name="self">The type we want to query.</param>
        /// <param name="other">The type we want to know if <paramref name="self"/> is assignable to.</param>
        /// <param name="stencil">The context in which we want to check if this assignation is possible in.</param>
        /// <returns>True if <paramref name="self"/> is assignable from <paramref name="other"/>, false otherwise.</returns>
        public static bool IsAssignableFrom(this TypeHandle self, TypeHandle other, StencilBase stencil)
        {
            var selfMetadata = self.GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsAssignableFrom(otherMetadata);
        }
    }
}
