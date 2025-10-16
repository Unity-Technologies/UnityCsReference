// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the blackboard section list.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardSectionListPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-blackboard-section-list-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardSectionListPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is BlackboardContentModel)
            {
                return new BlackboardSectionListPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The blackboard containing this part.
        /// </summary>
        public Blackboard Blackboard => m_OwnerElement as Blackboard;

        /// <summary>
        /// The root of this part.
        /// </summary>
        protected VisualElement m_Root;

        /// <summary>
        /// A dictionary associating section names with the related <see cref="BlackboardSection"/>.
        /// </summary>
        protected Dictionary<string, BlackboardSection> m_Sections;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardSectionListPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            void AddSection(GraphModel graphModel, string sectionName)
            {
                var section = ModelViewFactory.CreateUI<BlackboardSection>(m_OwnerElement.RootView,
                    graphModel.GetSectionModel(sectionName));

                if (section != null && !m_Sections.ContainsKey(sectionName))
                {
                    section.name = sectionName;
                    m_Root.Add(section);
                    m_Sections.Add(sectionName, section);
                }
            }

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            if (m_Model is BlackboardContentModel blackboardContentModel)
            {
                m_Sections = new Dictionary<string, BlackboardSection>();

                if (blackboardContentModel.GraphModel.ShowDefaultSectionInBlackboard)
                    AddSection(blackboardContentModel.GraphModel, GraphModel.DefaultSectionName);

                foreach (var sectionName in blackboardContentModel.GraphModel.AdditionalSectionNames)
                {
                    AddSection(blackboardContentModel.GraphModel, sectionName);
                }
            }

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            if (m_Sections != null)
            {
                foreach (var section in m_Sections.Values)
                {
                    section.AddToRootView(m_OwnerElement.RootView);
                }
            }

            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            if (m_Sections != null)
            {
                foreach (var section in m_Sections.Values)
                {
                    section.RemoveFromRootView();
                }
            }

            base.PartOwnerRemovedFromView();
        }
    }
}
