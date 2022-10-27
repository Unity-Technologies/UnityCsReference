// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A section in the inspector.
    /// </summary>
    class InspectorSection : ModelView
    {
        public static readonly string ussClassName = "ge-inspector-section";
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement("content-container");

        /// <summary>
        /// The content container.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_ContentContainer = new VisualElement { name = "content-container" };
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("InspectorSection.uss");
        }
    }
}
