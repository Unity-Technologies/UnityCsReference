// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The part to build the UI for block containers.
    /// </summary>
    /// <remarks>
    /// 'ContextBlocksPart' constructs the UI for block containers. This includes <see cref="ValidBlocksContainerPart"/> and <see cref="MissingBlocksContainerPart"/>.
    /// It defines how blocks are visually organized and presented within the graph.
    /// </remarks>
    [UnityRestricted]
    internal class ContextBlocksPart : GraphElementPart
    {
        /// <summary>
        /// The <see cref="ContextNodeModel"/> displayed in this part.
        /// </summary>
        public ContextNodeModel ContextNodeModel => m_Model as ContextNodeModel;

        protected VisualElement m_Root;
        protected VisualElement m_SelectionLayer;

        /// <inheritdoc/>
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Creates a new <see cref="ContextBlocksPart"/>.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A new instance of <see cref="ContextBlocksPart"/>.</returns>
        public static ContextBlocksPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is ContextNodeModel contextModel)
            {
                return new ContextBlocksPart(name, contextModel, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The name of the valid blocks container part.
        /// </summary>
        public static readonly string blocksContainerName = "blocks-container";

        /// <summary>
        /// The name of the add block part.
        /// </summary>
        public static readonly string addBlockPartName = "add-block";

        /// <summary>
        /// The name of the selection layer element.
        /// </summary>
        public static readonly string selectionLayer = "selection-layer";

        /// <summary>
        /// The name of the missing blocks part.
        /// </summary>
        public static readonly string missingBlocksPartName = "missing-blocks";

        ValidBlocksContainerPart m_ValidBlocksContainerPart;
        MissingBlocksContainerPart m_MissingBlocksContainerPart;

        /// <summary>
        /// Creates a new ContextBlocksPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="nodeModel">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        protected ContextBlocksPart(string name, ContextNodeModel nodeModel, ChildView ownerElement, string parentClassName)
            : base(name, nodeModel, ownerElement, parentClassName)
        {
            m_ValidBlocksContainerPart = new ValidBlocksContainerPart(blocksContainerName, nodeModel, ownerElement, parentClassName);
            PartList.AppendPart(m_ValidBlocksContainerPart);

            if (ContextNodeModel is ContextNodePlaceholder)
                return;

            PartList.AppendPart(AddBlockPart(nodeModel, ownerElement, parentClassName));
            m_MissingBlocksContainerPart = new MissingBlocksContainerPart(missingBlocksPartName, nodeModel, ownerElement, parentClassName);
            PartList.AppendPart(m_MissingBlocksContainerPart);
        }

        /// <summary>
        /// Creates the add block button part
        /// </summary>
        /// <param name="nodeModel">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created <see cref="AddBlockPart"/>.</returns>
        protected virtual AddBlockPart AddBlockPart(ContextNodeModel nodeModel, ChildView ownerElement, string parentClassName)
        {
            return new AddBlockPart(addBlockPartName, nodeModel, ownerElement, parentClassName);
        }

        /// <inheritdoc/>
        protected override void BuildUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_Root);
        }

        public void BlockBorderChanged()
        {
            m_SelectionLayer.MarkDirtyRepaint();
        }

        protected override void PostBuildUI()
        {
            m_SelectionLayer = new VisualElement() { name = selectionLayer };
            m_Root.Add(m_SelectionLayer);
            m_SelectionLayer.pickingMode = PickingMode.Ignore;
            m_SelectionLayer.generateVisualContent += GenerateSelectionVisualContent;
            m_SelectionLayer.AddToClassList(m_ParentClassName.WithUssElement(selectionLayer));
        }

        void GenerateSelectionVisualContent(MeshGenerationContext mgc)
        {
            void IterateOnBlock(BlockNodeView block)
            {
                var border = block.Border as DynamicBlockBorder;
                if (border == null)
                    return;

                var bounds = border.localBound;

                bounds = border.parent.ChangeCoordinatesTo(m_SelectionLayer, bounds);

                border.DrawBorder(mgc.painter2D, bounds, border.GetComputedWidth(), border.ComputedColor);
            }

            foreach (var part in PartList.Parts)
            {
                foreach (var element in part.Root.Children())
                {
                    if (element is BlockNodeView block)
                        IterateOnBlock(block);
                }
            }
        }

        /// <inheritdoc/>
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
        }

        /// <summary>
        /// Refresh the borders of the blocks.
        /// </summary>
        public void RefreshBorder()
        {
            m_SelectionLayer.MarkDirtyRepaint();
        }

        /// <inheritdoc/>
        public override bool SupportsCulling() => false;
    }
}
