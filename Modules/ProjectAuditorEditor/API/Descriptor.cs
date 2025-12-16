// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Descriptor defines a potential problem and a recommended course of action.
    /// </summary>
    [Serializable]
    public sealed class Descriptor : IEquatable<Descriptor>
    {
        /// <summary>
        /// An unique identifier for the issue. IDs must have exactly 3 upper case characters, followed by 4 digits.
        /// </summary>
        public string Id;

        /// <summary>
        /// Issue title
        /// </summary>
        public string Title;

        /// <summary>
        /// Message used to describe a specific instance of the issue.
        /// </summary>
        [NonSerialized]
        public string MessageFormat;

        /// <summary>
        /// Default Severity of the issue.
        /// </summary>
        [NonSerialized]
        public Severity DefaultSeverity;

        /// <summary>
        /// Returns true if the issue is enabled by default.
        /// </summary>
        [NonSerialized]
        public bool IsEnabledByDefault = true;

        /// <summary>
        /// Affected areas
        /// </summary>
        public SerializableEnum<Areas> Areas;

        /// <summary>
        /// Affected platforms. If null, the issue applies to all platforms.
        /// </summary>
        public SerializableEnum<BuildTarget>[] Platforms;

        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Description;

        /// <summary>
        /// Recommendation to fix the issue.
        /// </summary>
        public string Recommendation;

        /// <summary>
        /// URL to documentation.
        /// </summary>
        public string DocumentationUrl;

        /// <summary>
        /// Minimum Unity version this issue applies to. If not specified, the issue applies to all versions.
        /// </summary>
        [NonSerialized]
        public string MinimumVersion;

        /// <summary>
        /// Maximum Unity version this issue applies to. If not specified, the issue applies to all versions.
        /// </summary>
        [NonSerialized]
        public string MaximumVersion;

        /// <summary>
        /// Optional Auto-Fixer
        /// </summary>
        public Func<ReportItem, AnalysisParams, bool> Fixer;

        /// <summary>
        /// The Auto-Fixer can have a custom label
        /// </summary>
        public string FixerLabel;

        /// <summary>
        /// Name of the type (namespace and class/struct) of a known code API issue.
        /// </summary>
        [NonSerialized]
        public string Type;

        /// <summary>
        /// Name of the method of a known code API issue.
        /// </summary>
        [NonSerialized]
        public string Method;

        /// <summary>
        /// Return type of a known code API issue. See https://jira.unity3d.com/browse/PROFB-318 for more details on why this is useful.
        /// </summary>
        [NonSerialized]
        public string ReturnType;

        /// <summary>
        /// The evaluated value of a know code API issue.
        /// </summary>
        [NonSerialized]
        public string Value;

        internal Descriptor()
        {
            // only for json serialization purposes.
            MessageFormat = string.Empty;
            FixerLabel = string.Empty;
            Type = string.Empty;
            Method = string.Empty;
            DefaultSeverity = Severity.Moderate;
        }

        /// <summary>
        /// Initializes and returns an instance of Descriptor.
        /// </summary>
        /// <param name="id">The Issue ID string.</param>
        /// <param name="title">A short human-readable 'name' for the issue</param>
        /// <param name="areas">The area(s) affected by this issue.</param>
        /// <param name="description">A description of the issue.</param>
        /// <param name="recommendation">Advice on how to resolve the issue.</param>
        public Descriptor(string id, string title, Areas areas, string description, string recommendation)
        {
            Id = id;
            Title = title;
            Areas = areas;
            MessageFormat = string.Empty;
            Description = description;
            Recommendation = recommendation;

            Type = string.Empty;
            Method = string.Empty;
            DefaultSeverity = Severity.Moderate;
        }

        /// <summary>Returns true if the Descriptor is equal to a given Descriptor, false otherwise.</summary>
        /// <param name="other">The Descriptor to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public bool Equals(Descriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        /// <summary>Returns true if the Descriptor is equal to a given object, false otherwise.</summary>
        /// <param name="obj">The object to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Descriptor)obj);
        }

        internal void Fix(ReportItem issue, AnalysisParams analysisParams)
        {
            // Temp workaround for lost 'Fixer' after domain reload
            if (Fixer == null)
                return;

            issue.WasFixed = Fixer(issue, analysisParams);
        }

        /// <summary>Returns the hash code for the Descriptor's Issue ID.</summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
