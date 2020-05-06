using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Template Container.
    /// </summary>
    public class TemplateContainer : BindableElement
    {
        /// <summary>
        /// Instantiates and clones a <see cref="TemplateContainer"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<TemplateContainer, UxmlTraits>
        {
            internal const string k_ElementName = "Instance";

            public override string uxmlName => k_ElementName;

            public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;
        }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TemplateContainer"/>.
        /// </summary>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            internal const string k_TemplateAttributeName = "template";

            UxmlStringAttributeDescription m_Template = new UxmlStringAttributeDescription { name = k_TemplateAttributeName, use = UxmlAttributeDescription.Use.Required };

            /// <summary>
            /// Returns an empty enumerable, as template instance do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initialize <see cref="TemplateContainer"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TemplateContainer templateContainer = ((TemplateContainer)ve);
                templateContainer.templateId = m_Template.GetValueFromBag(bag, cc);
                VisualTreeAsset vea = cc.visualTreeAsset?.ResolveTemplate(templateContainer.templateId);

                if (vea == null)
                    templateContainer.Add(new Label(string.Format("Unknown Template: '{0}'", templateContainer.templateId)));
                else
                {
                    var bagOverrides = (bag as TemplateAsset)?.attributeOverrides;
                    var contextOverrides = cc.attributeOverrides;

                    List<TemplateAsset.AttributeOverride> attributeOverrides = null;
                    if (bagOverrides != null || contextOverrides != null)
                    {
                        // We want to add contextOverrides first here, then bagOverrides, as we
                        // want parent instances to always override child instances.
                        attributeOverrides = new List<TemplateAsset.AttributeOverride>();
                        if (contextOverrides != null)
                            attributeOverrides.AddRange(contextOverrides);
                        if (bagOverrides != null)
                            attributeOverrides.AddRange(bagOverrides);
                    }

                    vea.CloneTree(ve, cc.slotInsertionPoints, attributeOverrides);
                }

                if (vea == null)
                    Debug.LogErrorFormat("Could not resolve template with name '{0}'", templateContainer.templateId);
            }
        }

        public string templateId { get; private set; }
        private VisualElement m_ContentContainer;

        public TemplateContainer() : this(null) {}

        public TemplateContainer(string templateId)
        {
            this.templateId = templateId;
            m_ContentContainer = this;
        }

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        internal void SetContentContainer(VisualElement content)
        {
            m_ContentContainer = content;
        }
    }
}
