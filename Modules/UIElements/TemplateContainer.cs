// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class TemplateContainer : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<TemplateContainer, UxmlTraits>
        {
            internal const string k_ElementName = "Instance";

            public override string uxmlName => k_ElementName;

            public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            internal const string k_TemplateAttributeName = "template";

            UxmlStringAttributeDescription m_Template = new UxmlStringAttributeDescription { name = k_TemplateAttributeName, use = UxmlAttributeDescription.Use.Required };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

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
