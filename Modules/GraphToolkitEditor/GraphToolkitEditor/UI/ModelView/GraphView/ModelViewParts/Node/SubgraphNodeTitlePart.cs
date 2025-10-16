// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the <see cref="NodeTitlePart"/> of a <see cref="SubgraphNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class SubgraphNodeTitlePart : NodeTitlePart
    {
        public static readonly string localSubgraphUssClassName = "ge-local-subgraph";
        public static readonly string localSubgraphIconUssClassName = localSubgraphUssClassName.WithUssElement(GraphElementHelper.iconName);
        Image m_WarningIcon;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphNodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="NodeTitlePart.Options"/>.</param>
        /// <returns>A new instance of <see cref="SubgraphNodeTitlePart"/>.</returns>
        public new static SubgraphNodeTitlePart Create(string name, Model model, ChildView ownerElement, string parentClassName, int options = Options.Default)
        {
            return model is SubgraphNodeModel subgraphNode ? new SubgraphNodeTitlePart(name, subgraphNode, ownerElement, parentClassName, options) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphNodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="NodeTitlePart.Options"/>.</param>
        SubgraphNodeTitlePart(string name, GraphElementModel model, ChildView ownerElement, string parentClassName, int options)
            : base(name, model, ownerElement, parentClassName, options)
        { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            if (m_Model is not SubgraphNodeModel subgraphNodeModel)
                return;

            base.BuildUI(container);

            if (subgraphNodeModel.GetSubgraphModel() is null)
            {
                // Subgraph node that is missing its subgraph reference.
                InsertWarningIcon();
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is not SubgraphNodeModel subgraphNodeModel)
                return;

            base.UpdateUIFromModel(visitor);

            if (TitleLabel is EditableLabel editableLabel && visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                editableLabel.SetValueWithoutNotify(subgraphNodeModel.Title);
                SetupWidthFromOriginalSize();
            }

            if (subgraphNodeModel.GetSubgraphModel() == null)
            {
                InsertWarningIcon();
            }
            else
            {
                if (m_WarningIcon != null)
                {
                    TitleContainer.Remove(m_WarningIcon);
                    m_WarningIcon = null;
                }
            }
        }

        void InsertWarningIcon()
        {
            if (m_WarningIcon is not null)
                return;

            m_WarningIcon = CreateMissingWarningIcon();
            var index = TitleContainer.IndexOf(LabelContainer);
            TitleContainer.Insert(index, m_WarningIcon);
        }
    }
}
