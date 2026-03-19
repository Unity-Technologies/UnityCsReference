// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the title of the blackboard.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardTitlePart : BaseModelViewPart
    {
        /// <summary>
        /// The USS class of this part.
        /// </summary>
        public static readonly string ussClassName = "ge-blackboard-title-part";

        /// <summary>
        /// The USS class of the title element.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleName);

        /// <summary>
        /// The USS class of the subtitle element.
        /// </summary>
        public static readonly string subTitleUssClassName = ussClassName.WithUssElement("subtitle");

        /// <summary>
        /// The name of the title label element.
        /// </summary>
        public static readonly string titleLabelName = "title-label";

        /// <summary>
        /// The default title for the blackboard.
        /// </summary>
        protected static readonly string k_DefaultTitle = "Blackboard";

        /// <summary>
        /// The default sub title for the blackboard.
        /// </summary>
        protected static readonly string k_DefaultSubTitle = "";

        /// <summary>
        /// Creates an instance of the <see cref="BlackboardTitlePart"/>
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        public BlackboardTitlePart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        { }

        /// <summary>
        /// THe root element.
        /// </summary>
        VisualElement m_Root;

        /// <summary>
        /// The title label.
        /// </summary>
        protected Label m_TitleLabel;

        /// <summary>
        /// The sub title label.
        /// </summary>
        protected Label m_SubTitleLabel;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);

            m_TitleLabel = new Label { name = titleLabelName };
            m_TitleLabel.AddToClassList(titleUssClassName);
            m_SubTitleLabel = new Label { name = "sub-title-label" };
            m_SubTitleLabel.AddToClassList(subTitleUssClassName);

            m_Root.Add(m_TitleLabel);
            m_Root.Add(m_SubTitleLabel);

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is BlackboardContentModel blackboardContentModel && blackboardContentModel.IsValid())
            {
                m_TitleLabel.text = blackboardContentModel.GetTitle();
                m_SubTitleLabel.text = blackboardContentModel.GetSubTitle();
            }
            else
            {
                m_TitleLabel.text = k_DefaultTitle;
                m_SubTitleLabel.text = k_DefaultSubTitle;
            }
        }
    }
}
