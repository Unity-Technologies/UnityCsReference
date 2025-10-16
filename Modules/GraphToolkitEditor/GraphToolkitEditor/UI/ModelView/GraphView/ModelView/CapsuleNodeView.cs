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
        /// The name of the <see cref="ModelViewPart"/> for the title container.
        /// </summary>
        public static readonly string titleIconContainerPartName = "title-icon-container";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the constant editor.
        /// </summary>
        public static readonly string constantEditorPartName = "constant-editor";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the input ports container.
        /// </summary>
        public static readonly string inputPortContainerPartName = "inputs";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the output ports container.
        /// </summary>
        public static readonly string outputPortContainerPartName = "outputs";

        internal bool IsHighlighted() => Border.Highlighted;

        SinglePortContainerPart m_InputPart;
        SinglePortContainerPart m_OutputPart;
        CapsuleNodeLodCachePart m_CapsuleNodeLodCachePart;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(NodeTitlePart.Create(titleIconContainerPartName, NodeModel, this, ussClassName, EditableTitlePart.Options.UseEllipsis | EditableTitlePart.Options.SetWidth));
            PartList.AppendPart(ConstantNodeEditorPart.Create(constantEditorPartName, Model, this, ussClassName));

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

            var inputPort = ExtractInputPortModel(Model);
            if (inputPort != null && m_InputPart == null)
            {
                m_InputPart = SinglePortContainerPart.Create(inputPortContainerPartName, inputPort, this, ussClassName);
                PartList.InsertPartBefore(titleIconContainerPartName, m_InputPart);

                m_InputPart.BuildUITree(this);
                m_InputPart.PostBuildUITree();
                m_InputPart.Root.SendToBack();
            }
            else if (inputPort == null && m_InputPart != null)
            {
                PartList.RemovePart(m_InputPart.PartName);
                m_InputPart.Root.RemoveFromHierarchy();
                m_InputPart = null;
            }

            var outputPort = ExtractOutputPortModel(Model);
            if (outputPort != null && m_OutputPart == null)
            {
                m_OutputPart = SinglePortContainerPart.Create(outputPortContainerPartName, outputPort, this, ussClassName);
                PartList.InsertPartBefore(cachePartName, m_OutputPart);

                m_OutputPart.BuildUITree(this);
                m_OutputPart.PostBuildUITree();
                m_OutputPart.Root.PlaceBehind(m_CapsuleNodeLodCachePart.Root);
            }
            else if (outputPort == null && m_OutputPart != null)
            {
                PartList.RemovePart(m_OutputPart.PartName);
                m_OutputPart.Root.RemoveFromHierarchy();
                m_OutputPart = null;
            }

            Border.Highlighted = ShouldBeHighlighted();
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

        protected static Model ExtractInputPortModel(Model model)
        {
            if (model is ISingleInputPortNodeModel inputPortHolder && inputPortHolder.InputPort != null)
            {
                Debug.Assert(inputPortHolder.InputPort.Direction == PortDirection.Input);
                return inputPortHolder.InputPort;
            }

            return null;
        }

        protected static Model ExtractOutputPortModel(Model model)
        {
            if (model is ISingleOutputPortNodeModel outputPortHolder && outputPortHolder.OutputPort != null)
            {
                Debug.Assert(outputPortHolder.OutputPort.Direction == PortDirection.Output);
                return outputPortHolder.OutputPort;
            }

            return null;
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
