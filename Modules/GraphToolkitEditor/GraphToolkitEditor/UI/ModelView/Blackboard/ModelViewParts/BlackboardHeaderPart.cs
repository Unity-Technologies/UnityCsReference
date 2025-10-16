// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the blackboard header.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardHeaderPart : BaseModelViewPart
    {
        /// <summary>
        /// The USS class name of <see cref="BlackboardHeaderPart"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-blackboard-toolbar-part";

        /// <summary>
        /// The name of <see cref="BlackboardTitlePart"/>.
        /// </summary>
        public static readonly string titlePartName = GraphElementHelper.titleName;

        /// <summary>
        /// The name of <see cref="BlackboardToolbarPart"/>.
        /// </summary>
        public static readonly string toolbarPartName = "toolbar";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardHeaderPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardHeaderPart"/>.</returns>
        public static BlackboardHeaderPart Create(string name, BlackboardContentModel model, ChildView ownerElement, string parentClassName)
        {
            return new BlackboardHeaderPart(name, model, ownerElement, parentClassName);
        }

        /// <summary>
        /// The root element.
        /// </summary>
        protected VisualElement m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardHeaderPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardHeaderPart(string name, BlackboardContentModel model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            PartList.AppendPart(new BlackboardTitlePart(titlePartName, model, ownerElement, ussClassName));

            var graphModel = model.GraphModel;

            if (graphModel.ShowDefaultSectionInBlackboard)
            {
                PartList.AppendPart(new BlackboardToolbarPart(toolbarPartName, model, ownerElement, ussClassName));
            }
        }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
        }
    }
}
