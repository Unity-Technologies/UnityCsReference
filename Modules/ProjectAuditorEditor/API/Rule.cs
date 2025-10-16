// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Represents a rule which modifies the <see cref="Severity"/> of an Issue <see cref="ReportItem"/>
    /// or all of the ProjectIssues that share a <see cref="Descriptor"/>.
    /// </summary>
    [Serializable]
    public class Rule
    {
        /// <summary>
        /// The Severity level to apply to the issue(s) represented by this Rule
        /// </summary>
        [SerializeField]
        private SerializableEnum<Severity> severity;
        public Severity Severity { get { return severity; } set { severity = value; } }

        /// <summary>
        /// An optional location filter representing a ReportItem's location.
        /// If specified, this Rule applies to a single ReportItem. If the string is null or empty, this Rule applies to every ReportItem matching the Id.
        /// </summary>
        [SerializeField]
        private string filter;
        public string Filter { get { return filter; } set { filter = value; } }

        /// <summary>
        /// The Descriptor ID
        /// </summary>
        [SerializeField]
        private DescriptorId id;
        public DescriptorId Id { get => id; set => id = value; }

        /// <summary>Get the hashed integer representation of the Rule.</summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + Filter.GetHashCode();
                hash = hash * 23 + Severity.GetHashCode();
                return hash;
            }
        }
    }
}
