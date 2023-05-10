// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the <see cref="NodeTitlePart"/> of a <see cref="SubgraphNodeModel"/>.
    /// </summary>
    class SubgraphNodeTitlePart : NodeTitlePart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphNodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SubgraphNodeTitlePart"/>.</returns>
        public static SubgraphNodeTitlePart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            return model is SubgraphNodeModel subgraphNode ? new SubgraphNodeTitlePart(name, subgraphNode, ownerElement, parentClassName) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphNodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        SubgraphNodeTitlePart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName, Options.UseEllipsis | Options.Colorable | Options.HasIcon)
        {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is SubgraphNodeModel subgraphNodeModel)
            {
                base.BuildPartUI(container);

                m_Icon.AddToClassList(ussClassName.WithUssElement("asset-graph-icon"));
                m_Icon.AddToClassList(m_ParentClassName.WithUssElement("asset-graph-icon"));

                if (subgraphNodeModel.SubgraphModel == null)
                {
                    TitleContainer.Add(CreateMissingWarningIcon());
                    TitleContainer.Add(TitleLabel);
                }
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is SubgraphNodeModel subgraphNodeModel)
            {
                base.UpdatePartFromModel();

                var warningIcon = TitleContainer.SafeQ<Image>(name: missingWarningIconName);
                if (subgraphNodeModel.SubgraphModel == null)
                {
                    if (warningIcon == null)
                    {
                        TitleContainer.Add(CreateMissingWarningIcon());
                        TitleContainer.Add(TitleLabel);
                    }
                }
                else
                {
                    if (warningIcon != null)
                    {
                        TitleContainer.Remove(warningIcon);
                        TitleContainer.Remove(TitleLabel);
                    }
                }
            }
        }
    }
}
