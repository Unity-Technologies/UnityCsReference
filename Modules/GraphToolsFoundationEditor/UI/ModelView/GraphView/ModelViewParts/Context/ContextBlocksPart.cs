// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The part to build the UI  for blocks containers.
    /// </summary>
    class ContextBlocksPart : GraphElementPart
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
        public static ContextBlocksPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
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
        /// <returns>A newly created <see cref="ContextBlocksPart"/>.</returns>
        protected ContextBlocksPart(string name, ContextNodeModel nodeModel, ModelView ownerElement, string parentClassName)
            : base(name, nodeModel, ownerElement, parentClassName)
        {
            m_ValidBlocksContainerPart = new ValidBlocksContainerPart(blocksContainerName, nodeModel, ownerElement, parentClassName);
            PartList.AppendPart(m_ValidBlocksContainerPart);

            if (ContextNodeModel is ContextNodePlaceholder)
                return;

            PartList.AppendPart(new AddBlockPart(addBlockPartName, nodeModel, ownerElement, parentClassName));
            m_MissingBlocksContainerPart = new MissingBlocksContainerPart(missingBlocksPartName, nodeModel, ownerElement, parentClassName);
            PartList.AppendPart(m_MissingBlocksContainerPart);
        }

        /// <inheritdoc/>
        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_Root);
        }


        public void BlockBorderChanged()
        {
            m_SelectionLayer.MarkDirtyRepaint();
        }

        protected override void PostBuildPartUI()
        {
            m_SelectionLayer = new VisualElement(){name = selectionLayer};
            m_Root.Add(m_SelectionLayer);
            m_SelectionLayer.pickingMode = PickingMode.Ignore;
            m_SelectionLayer.generateVisualContent += GenerateSelectionVisualContent;
            m_SelectionLayer.AddToClassList(m_ParentClassName.WithUssElement(selectionLayer));
        }

        void GenerateSelectionVisualContent(MeshGenerationContext mgc)
        {
            void IterateOnBlock(BlockNode block)
            {
                var border = block.Border as DynamicBlockBorder_Internal;
                if (border == null)
                    return;

                var bounds = border.localBound;

                bounds = border.parent.ChangeCoordinatesTo(m_SelectionLayer, bounds);

                border.DrawBorder(mgc.painter2D, bounds, border.ComputedWidth, border.ComputedColor);
            }

            foreach (var element in m_ValidBlocksContainerPart.Root.Children())
            {
                if( element is BlockNode block)
                    IterateOnBlock(block);
            }

            foreach (var element in m_MissingBlocksContainerPart.Root.Children())
            {
                if( element is BlockNode block)
                    IterateOnBlock(block);
            }
        }

        /// <inheritdoc/>
        protected override void UpdatePartFromModel()
        {
        }

        /// <summary>
        /// Refresh the borders of the blocks.
        /// </summary>
        public void RefreshBorder()
        {
            m_SelectionLayer.MarkDirtyRepaint();
        }
    }
}
