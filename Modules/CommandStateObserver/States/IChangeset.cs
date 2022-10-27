// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Interface for changesets of <see cref="IStateComponent"/>.
    /// </summary>
    interface IChangeset
    {
        /// <summary>
        /// Clears the changeset.
        /// </summary>
        void Clear();

        /// <summary>
        /// Makes this changeset a changeset that summarize <paramref name="changesets"/>.
        /// </summary>
        /// <param name="changesets">The changesets to summarize.</param>
        void AggregateFrom(IEnumerable<IChangeset> changesets);

        /// <summary>
        /// Reverse the direction of the changeset.
        /// </summary>
        /// <remarks>
        /// For example, if the changeset contains a list of created objects and a list of deleted objects,
        /// the two lists should be swapped.
        /// </remarks>
        bool Reverse();
    }
}
