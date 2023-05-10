// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

abstract class BlocksContainerPart : GraphElementPart
{
    /// <summary>
    /// The <see cref="ContextNodeModel"/> displayed in this part.
    /// </summary>
    public ContextNodeModel ContextNodeModel => m_Model as ContextNodeModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlocksContainerPart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    public BlocksContainerPart(string name, ContextNodeModel model, ModelView ownerElement, string parentClassName)
        : base(name, model, ownerElement, parentClassName) { }

    protected VisualElement m_BlocksContainer;

    /// <inheritdoc />
    public override VisualElement Root => m_BlocksContainer;

    /// <inheritdoc />
    protected override void BuildPartUI(VisualElement parent)
    {
        m_BlocksContainer = new VisualElement(){name = PartName};
        m_BlocksContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

        parent.Add(Root);
    }

    ModelView m_PreviousFirstBlockNode;
    ModelView m_PreviousLastBlockNode;

    /// <summary>
    /// Update the blocks given the <see cref="blockModels"/>s passed.
    /// </summary>
    /// <param name="blockModels">The block models to update.</param>
    /// <param name="existingBlockViews">The already existing views for block models.</param>
    /// <returns>True if there are no blocks.</returns>
    protected bool UpdateBlocks(IReadOnlyList<BlockNodeModel> blockModels, List<ModelView> existingBlockViews)
    {
        foreach (var block in existingBlockViews)
        {
            if (!(block.Model is BlockNodeModel blockModel) || blockModels.ContainsReference(blockModel))
                continue;

            // Only remove it from the root view if the block is not part of another context node.
            if (blockModel.ContextNodeModel == null || blockModel.ContextNodeModel == ContextNodeModel)
                block.RemoveFromRootView();
            block.RemoveFromHierarchy();
        }

        if (blockModels.Count == 0)
            return true;

        var orderedNewBlockModels = new List<KeyValuePair<int, BlockNodeModel>>();
        var existingModels = new HashSet<Model>();
        foreach (var view in existingBlockViews)
            existingModels.Add(view.Model);

        for(int i = 0 ; i < blockModels.Count ; ++i)
        {
            if (existingModels.Contains(blockModels[i]))
                continue;
            orderedNewBlockModels.Add(new KeyValuePair<int,BlockNodeModel>(i, blockModels[i]));
        }

        // Add blocks that are new in the model
        foreach (var blockModel in orderedNewBlockModels)
        {
            int index = blockModel.Key;
            ModelView newBlockNode = ModelViewFactory.CreateUI<ModelView>(m_OwnerElement.RootView, blockModel.Value);

            if (newBlockNode != null)
            {
                newBlockNode.AddToRootView(m_OwnerElement.RootView);
                if (index < m_BlocksContainer.childCount - 1) //childCount - add button.
                    m_BlocksContainer.Insert(index, newBlockNode);
                else
                    m_BlocksContainer.Add(newBlockNode); // last element is the add block button container

                if (newBlockNode is GraphElement ge)
                    ge.SetLevelOfDetail(ge.GraphView.ViewTransform.scale.x, GraphViewZoomMode.Normal, GraphViewZoomMode.Unknown);
            }
        }

        // Sort blocks through the models order the idea is to minimize change since most of the time block order will still be valid
        // they are sorted as reverse order in the ui and then flex-direction: reverse-column is used so that the top blocks are closer than the bottom blocks
        existingBlockViews = m_BlocksContainer.Children().OfTypeToList<ModelView,VisualElement>();

        ModelView firstBlockNode = existingBlockViews.Count == 0 ? null : existingBlockViews[0];
        BlockNodeModel firstModel = blockModels[0];
        if (firstBlockNode == null || !ReferenceEquals(firstBlockNode.Model, firstModel))
        {
            foreach (var view in existingBlockViews)
            {
                if (ReferenceEquals(view.Model, firstModel))
                {
                    firstBlockNode = view;
                    break;
                }
            }
            firstBlockNode.SendToBack();
            existingBlockViews.Remove(firstBlockNode);
            existingBlockViews.Insert(0, firstBlockNode);
        }

        ModelView prevBlockNode = firstBlockNode;
        for (int i = 1; i < blockModels.Count; ++i)
        {
            if (blockModels[i] == null || existingBlockViews[i] == null)
                continue;

            ModelView currentBlockNode = null;
            foreach (var view in existingBlockViews)
            {
                if (ReferenceEquals(view.Model, blockModels[i]))
                {
                    currentBlockNode = view;
                    break;
                }
            }

            if (existingBlockViews[i] != currentBlockNode)
            {
                currentBlockNode.PlaceInFront(prevBlockNode);
                existingBlockViews.Remove(currentBlockNode);
                existingBlockViews.Insert(i, currentBlockNode);
            }

            prevBlockNode = currentBlockNode;
        }

        if (existingBlockViews.Count > 0)
        {
            if (existingBlockViews[0] != m_PreviousFirstBlockNode)
            {
                m_PreviousFirstBlockNode?.RemoveFromClassList(BlockNode.firstBlockUssClassName);

                m_PreviousFirstBlockNode = existingBlockViews[0];
                m_PreviousFirstBlockNode.AddToClassList(BlockNode.firstBlockUssClassName);
            }

            if (existingBlockViews[^1] != m_PreviousLastBlockNode)
            {
                m_PreviousLastBlockNode?.RemoveFromClassList(BlockNode.lastBlockUssClassName);

                m_PreviousLastBlockNode = existingBlockViews[^1];
                m_PreviousLastBlockNode.AddToClassList(BlockNode.lastBlockUssClassName);
            }
        }
        else
        {
            m_PreviousLastBlockNode?.RemoveFromClassList(BlockNode.lastBlockUssClassName);
            m_PreviousFirstBlockNode?.RemoveFromClassList(BlockNode.firstBlockUssClassName);
        }

        return false;
    }
}

