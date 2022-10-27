// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A UI creation context for inspector section content.
    /// </summary>
    class InspectorSectionContext : IViewContext
    {
        /// <summary>
        /// The inspector section.
        /// </summary>
        public InspectorSectionModel Section { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorSectionContext"/> class.
        /// </summary>
        /// <param name="sectionContext">The section to use as context.</param>
        public InspectorSectionContext(InspectorSectionModel sectionContext)
        {
            Section = sectionContext;
        }

        /// <inheritdoc />
        public bool Equals(IViewContext other)
        {
            if (other is InspectorSectionContext inspectorSectionContext)
                return ReferenceEquals(Section, inspectorSectionContext.Section);
            return false;
        }
    }
}
