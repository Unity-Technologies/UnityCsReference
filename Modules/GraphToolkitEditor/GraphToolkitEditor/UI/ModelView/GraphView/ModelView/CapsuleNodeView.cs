// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for <see cref="ISingleInputPortNodeModel"/> and <see cref="ISingleOutputPortNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class CapsuleNodeView : NodeView
    {
        /// <summary>
        /// The USS name for capsules.
        /// </summary>
        public static readonly string capsuleName = "capsule";

        /// <summary>
        /// The USS modifier name for when we have visible sub ports.
        /// </summary>
        public static readonly string withSubportsName = "with-sub-ports";

        /// <summary>
        /// The USS class name added to nodes that are <see cref="CapsuleNodeView"/>.
        /// </summary>
        public static readonly string capsuleUssClassName = ussClassName.WithUssModifier(capsuleName);

        /// <summary>
        /// The USS class name added to capsule nodes that are constant nodes.
        /// </summary>
        public static readonly string constantUssClassName = ussClassName.WithUssModifier("constant");

        /// <summary>
        /// The USS class name added to capsule nodes that are variable nodes.
        /// </summary>
        public static readonly string variableUssClassName = ussClassName.WithUssModifier("variable");

        /// <summary>
        /// The USS class name added to capsule nodes that are portal nodes.
        /// </summary>
        public static readonly string portalUssClassName = ussClassName.WithUssModifier("portal");

        /// <summary>
        /// The USS class name added to entry portal nodes.
        /// </summary>
        public static readonly string portalEntryUssClassName = ussClassName.WithUssModifier("portal-entry");

        /// <summary>
        /// The USS class name added to exit portal nodes.
        /// </summary>
        public static readonly string portalExitUssClassName = ussClassName.WithUssModifier("portal-exit");

        /// <summary>
        /// The USS class name added to nodes that are <see cref="CapsuleNodeView"/> that have visible sub ports.
        /// </summary>
        public static readonly string capsuleWithSubportsUssClassName = capsuleUssClassName.WithUssModifier(withSubportsName);

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the variable title.
        /// </summary>
        public static readonly string titlePartName = "variable-title-part";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the sub port container.
        /// </summary>
        public static readonly string subPortContainerPartName = "sub-port-container-part";

        protected CapsuleNodePortContainerPart m_CapsuleNodePortContainerPart;
        CapsuleNodeLodCachePart m_CapsuleNodeLodCachePart;

        internal CapsuleNodePortContainerPart CapsuleNodePortContainerPart => m_CapsuleNodePortContainerPart;

        internal bool IsHighlighted() => Border.Highlighted;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            m_CapsuleNodePortContainerPart = new CapsuleNodePortContainerPart(subPortContainerPartName, Model, this, ussClassName);
            PartList.AppendPart(m_CapsuleNodePortContainerPart);
            m_CapsuleNodeLodCachePart = CapsuleNodeLodCachePart.Create(cachePartName, Model, this, capsuleUssClassName);
            PartList.AppendPart(m_CapsuleNodeLodCachePart);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(capsuleUssClassName);
            this.AddPackageStylesheet("CapsuleNode.uss");

            switch (Model)
            {
                case WirePortalModel and ISingleInputPortNodeModel:
                    AddToClassList(portalUssClassName);
                    AddToClassList(portalEntryUssClassName);
                    break;
                case WirePortalModel and ISingleOutputPortNodeModel:
                    AddToClassList(portalUssClassName);
                    AddToClassList(portalExitUssClassName);
                    break;
                case ConstantNodeModel:
                    AddToClassList(constantUssClassName);
                    break;
                case VariableNodeModel:
                    AddToClassList(variableUssClassName);
                    break;
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            Border.Highlighted = ShouldBeHighlighted();

            if (NodeModel is NodeModel nodeModel)
            {
                var visiblePorts = (nodeModel is ISingleOutputPortNodeModel outputPortNodeModel && outputPortNodeModel.OutputPort != null)  ? nodeModel.VisibleOutputsByDisplayOrder : nodeModel.VisibleInputsByDisplayOrder;
                EnableInClassList(capsuleWithSubportsUssClassName, visiblePorts.Count > 1);
            }
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged()
        {
            return Model is VariableNodeModel;
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            if (Model is VariableNodeModel variableNodeModel)
                Dependencies.AddModelDependency(variableNodeModel.VariableDeclarationModel);
        }

        /// <inheritdoc />
        public override bool SupportsCulling(GraphViewCullingSource cullingSource)
        {
            // TODO: GTF-1278
            // There are some timing issues with this node, so disable culling for now.
            return false;
        }
    }
}
