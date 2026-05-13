// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Serialization;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents the root <see cref="VisualElement"/> of UXML file.
    /// </summary>
    /// <remarks>
    /// A TemplateContainer instance is created by Unity to represent the root of the UXML file and acts as the parent for all elements in the file.
    /// Users typically don't create TemplateContainer objects directly.
    /// When using <see cref="VisualTreeAsset.Instantiate()"/>, a TemplateContainer instance is returned to you to represent the root of the hierarchy.
    /// When using UXML templates, a TemplateContainer is generated for the template instance and inserted into the hierarchy of the parent UXML file.
    /// </remarks>
    [UxmlElement("Instance"), HideInInspector]
    [Icon("UIToolkit/Icons/TemplateContainer.png")]
    public partial class TemplateContainer : BindableElement
    {
        internal static readonly BindingId templateIdProperty = nameof(templateId);
        internal static readonly BindingId templateSourceProperty = nameof(templateSource);
        internal const string k_ElementName = "Instance";

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        [Serializable]
        internal class TemplateUXML
        {
            public VisualTreeAsset templateAsset;
            public string templateId;
        }

        [UxmlAttribute("template"), HideInInspector]
        internal TemplateUXML templateUXML
        {
            get => new TemplateUXML { templateAsset = templateSource, templateId = templateId };
            set
            {
                templateSource = value.templateAsset;
                templateId = value.templateId;
            }
        }

        /// <summary>
        /// The local ID of the template in the parent UXML file (RO).
        /// </summary>
        /// <remarks>This value is null, unless the TemplateContainer represents a UXML template within another UXML file.</remarks>
        [CreateProperty(ReadOnly = true)]
        public string templateId { get; private set; }
        private VisualElement m_ContentContainer;

        private VisualTreeAsset m_TemplateSource;

        /// <summary>
        /// Stores the template asset reference, if the generated element is cloned from a VisualTreeAsset as a
        /// Template declaration inside another VisualTreeAsset.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public VisualTreeAsset templateSource
        {
            get => m_TemplateSource;
            internal set => m_TemplateSource = value;
        }

        /// <undoc/>
        public TemplateContainer() : this(null) {}

        /// <undoc/>
        public TemplateContainer(string templateId)
            : this(templateId, null)
        { }

        internal TemplateContainer(string templateId, VisualTreeAsset templateSource)
        {
            this.templateId = templateId;
            this.templateSource = templateSource;
            m_ContentContainer = this;
        }

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetContentContainer(VisualElement content)
        {
            m_ContentContainer = content;
        }
    }
}
