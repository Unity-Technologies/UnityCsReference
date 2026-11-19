// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A section in the inspector.
    /// </summary>
    [UnityRestricted]
    internal class InspectorSection : ModelView
    {
        /// <summary>
        /// The USS class name added to a <see cref="InspectorSection"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-inspector-section";

        /// <summary>
        /// The USS class name added to the content container of a <see cref="InspectorSection"/>.
        /// </summary>
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.contentContainerName);

        /// <summary>
        /// The content container.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            m_ContentContainer = new VisualElement { name = GraphElementHelper.contentContainerName };
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("InspectorSection.uss");
        }
    }
}
