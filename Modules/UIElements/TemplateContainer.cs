// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class TemplateContainer : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<TemplateContainer, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Template = new UxmlStringAttributeDescription { name = "template", use = UxmlAttributeDescription.Use.Required };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TemplateContainer templateContainer = ((TemplateContainer)ve);
                templateContainer.templateId = m_Template.GetValueFromBag(bag, cc);
                VisualTreeAsset vea = cc.visualTreeAsset.ResolveTemplate(templateContainer.templateId);

                if (vea == null)
                    templateContainer.Add(new Label(string.Format("Unknown Element: '{0}'", templateContainer.templateId)));
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

                    vea.CloneTree(templateContainer, cc.slotInsertionPoints, attributeOverrides);
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
            pickingMode = PickingMode.Ignore;
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
