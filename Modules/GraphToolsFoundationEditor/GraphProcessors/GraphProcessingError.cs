// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Definition of a graph processing error.
    /// </summary>
    class GraphProcessingError : IEquatable<GraphProcessingError>
    {
        /// <summary>
        /// Description of the error.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Unique ID of the node that is the source of the error.
        /// </summary>
        public Hash128 SourceNodeGuid { get; set; }

        /// <summary>
        /// QuickFix to address the error.
        /// </summary>
        public QuickFix Fix { get; set; }

        /// <summary>
        /// Whether this is an error or a warning.
        /// </summary>
        public bool IsWarning { get; set; }

        /// <summary>
        /// Returns a string that represents the current error.
        /// </summary>
        /// <returns>A string that represents the current error.</returns>
        public override string ToString()
        {
            return $"Graph Processing Error: {Description}";
        }

        /// <inheritdoc />
        public bool Equals(GraphProcessingError other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Description == other.Description && SourceNodeGuid == other.SourceNodeGuid && IsWarning == other.IsWarning;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GraphProcessingError)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SourceNodeGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ IsWarning.GetHashCode();
                return hashCode;
            }
        }
    }
}
