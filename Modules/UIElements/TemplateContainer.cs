// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class TemplateContainer : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<TemplateContainer, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Template = new UxmlStringAttributeDescription { name = "template", use = UxmlAttributeDescription.Use.Required };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
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
                    vea.CloneTree(templateContainer, cc.slotInsertionPoints);

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
