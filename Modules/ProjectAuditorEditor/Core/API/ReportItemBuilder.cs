// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Provides methods to construct a <see cref="ReportItem"/> object for a Report.
    /// </summary>
    public class ReportItemBuilder
    {
        ReportItem m_Issue;

        /// <summary>Implicit conversion of ReportItemBuilder to ReportItem.</summary>
        /// <param name="builder">A ReportItemBuilder to convert</param>
        /// <returns>A ReportItem</returns>
        public static implicit operator ReportItem(ReportItemBuilder builder) => builder.m_Issue;

        /// <summary>
        /// Constructor for an object to build ReportItems representing Issues.
        /// </summary>
        /// <param name="category">The IssueCategory of the reported Issue</param>
        /// <param name="id">Identifies the Descriptor object containing information about the Issue</param>
        /// <param name="args">Arguments to be used in the message formatting</param>
        public ReportItemBuilder(IssueCategory category, DescriptorId id, params object[] args)
        {
            m_Issue = new ReportItem(category, id, args);
        }

        /// <summary>
        /// Constructor for an object to build ReportItems representing Insights.
        /// </summary>
        /// <param name="category">The IssueCategory of the reported Insight</param>
        /// <param name="description">The primary descriptive string for this Insight</param>
        public ReportItemBuilder(IssueCategory category, string description)
        {
            m_Issue = new ReportItem(category, description);
        }

        /// <summary>
        /// Initialize all custom properties to the same value.
        /// </summary>
        /// <param name="numProperties"> total number of custom properties </param>
        /// <param name="property"> value the properties will be set to </param>
        /// <returns>The ReportItemBuilder object with the custom properties set.</returns>
        public ReportItemBuilder WithCustomProperties(int numProperties, object property)
        {
            m_Issue.CustomProperties = new string[numProperties];
            for (var i = 0; i < numProperties; i++)
                m_Issue.CustomProperties[i] = property.ToString();
            return this;
        }

        /// <summary>
        /// Initialize custom properties.
        /// </summary>
        /// <param name="properties"> Issue-specific properties </param>
        /// <returns>The ReportItemBuilder object with the custom properties initialized.</returns>
        public ReportItemBuilder WithCustomProperties(object[] properties)
        {
            if (properties != null)
                m_Issue.CustomProperties = properties.Select(p => p != null ? p.ToString() : string.Empty).ToArray();
            else
                m_Issue.CustomProperties = null;

            return this;
        }

        /// <summary>
        /// Adds a description string to the ReportItem being built.
        /// </summary>
        /// <param name="description">Description string to add</param>
        /// <returns>The ReportItemBuilder object with the description string added</returns>
        public ReportItemBuilder WithDescription(string description)
        {
            m_Issue.Description = description;
            return this;
        }

        /// <summary>
        /// Adds a DependencyNode to the ReportItem being built.
        /// </summary>
        /// <param name="dependencies">The root DependencyNode of a dependency chain to add</param>
        /// <returns>The ReportItemBuilder object with the DependencyNode added</returns>
        internal ReportItemBuilder WithDependencies(DependencyNode dependencies)
        {
            m_Issue.Dependencies = dependencies;
            return this;
        }

        /// <summary>
        /// Adds a Location to the ReportItem being built.
        /// </summary>
        /// <param name="location">Location object describing where the specific item was found within the project</param>
        /// <returns>The ReportItemBuilder object with the Location added</returns>
        public ReportItemBuilder WithLocation(Location location)
        {
            m_Issue.Location = location;
            return this;
        }

        /// <summary>
        /// Constructs a Location object and adds it to the ReportItem being built.
        /// </summary>
        /// <param name="path">File path within the project describing where the specific item was found</param>
        /// <param name="line">A line number within the file</param>
        /// <returns>The ReportItemBuilder object with the Location added</returns>
        public ReportItemBuilder WithLocation(string path, int line = 0)
        {
            m_Issue.Location = new Location(path, line);
            return this;
        }

        /// <summary>
        /// Adds a LogLevel to the ReportItem being built.
        /// </summary>
        /// <param name="logLevel">Log Level of the item</param>
        /// <returns>The ReportItemBuilder object with the LogLevel added</returns>
        public ReportItemBuilder WithLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    m_Issue.Severity = Severity.Error;
                    break;
                case LogLevel.Warning:
                    m_Issue.Severity = Severity.Warning;
                    break;
                case LogLevel.Info:
                    m_Issue.Severity = Severity.Info;
                    break;
            }
            return this;
        }

        /// <summary>
        /// Adds a Severity to the ReportItem being built.
        /// </summary>
        /// <param name="severity">Severity of the item</param>
        /// <returns>The ReportItemBuilder object with the LogLevel added</returns>
        public ReportItemBuilder WithSeverity(Severity severity)
        {
            m_Issue.Severity = severity;
            return this;
        }
    }
}
