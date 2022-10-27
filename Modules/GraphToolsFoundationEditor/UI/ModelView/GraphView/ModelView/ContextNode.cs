// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ItemLibrary.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// VisualElement to display a <see cref="ContextNodeModel"/>.
    /// </summary>
    class ContextNode : CollapsibleInOutNode, IShowItemLibraryUI_Internal
    {
        /// <summary>
        /// The <see cref="ContextNodeModel"/> this ContextNode displays.
        /// </summary>
        public ContextNodeModel ContextNodeModel => Model as ContextNodeModel;

        /// <summary>
        /// The USS class name used for context nodes
        /// </summary>
        public new static readonly string ussClassName = "ge-context-node";

        /// <summary>
        /// The name of the part containing the blocks
        /// </summary>
        public static readonly string blocksPartName = "blocks-container";

        static readonly string k_ContextBorderName = "context-border";
        static readonly string k_ContextBorderTitleName = "context-border-title";

        /// <summary>
        /// The USS class name used for the context borders element.
        /// </summary>
        public static readonly string contextBorderUssClassName = ussClassName.WithUssElement(k_ContextBorderName);

        /// <summary>
        /// The USS class name used for the title element in the context border.
        /// </summary>
        public static readonly string contextBorderTitleUssClassName = ussClassName.WithUssElement(k_ContextBorderTitleName);

        /// <summary>
        /// The USS class name used for the context borders element when the drag is refused.
        /// </summary>
        public static readonly string contextBorderRefusedUssClassName = contextBorderUssClassName.WithUssModifier("refused");

        /// <summary>
        /// The USS class name used for the context borders element when the drag is accepted.
        /// </summary>
        public static readonly string contextBorderAcceptedUssClassName = contextBorderUssClassName.WithUssModifier("accepted");

        VisualElement m_ContextBorder;
        VisualElement m_ContextTitleBkgnd;

        VisualElement m_DragBlock;

        /// <summary>
        /// The root element of the context blocks.
        /// </summary>
        public VisualElement ContextBlocksRoot { get; private set; }

        /// <inheritdoc/>
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var selectionBorder = this.SafeQ(selectionBorderElementName);
            var selectionBorderParent = selectionBorder.parent;

            //Move the selection border from being the entire container for the node to being on top of the context-border
            int cpt = 0;
            while (selectionBorder.childCount > 0)
            {
                var elementAt = selectionBorder.ElementAt(0);
                selectionBorderParent.hierarchy.Insert(cpt++, elementAt); // use hierarchy because selectionBorderParent has a content container defined
            }

            m_ContextBorder = new VisualElement { name = k_ContextBorderName };
            m_ContextBorder.AddToClassList(contextBorderUssClassName);
            contentContainer.Insert(0, m_ContextBorder);
            m_ContextBorder.Add(selectionBorder);

            m_ContextTitleBkgnd = new VisualElement() { name = k_ContextBorderTitleName };
            m_ContextTitleBkgnd.AddToClassList(contextBorderTitleUssClassName);
            m_ContextBorder.Add(m_ContextTitleBkgnd);

            m_DragBlock = new VisualElement() { name = "drag-block" };
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet_Internal("ContextNode.uss");
            AddToClassList(ussClassName);

            ContextBlocksRoot = PartList.GetPart(blocksPartName)?.Root;

            m_ContextBorder.Add(Border);
        }

        /// <inheritdoc/>
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var hasVerticalInput = ContextNodeModel?.InputsById.Values.Any(t => t.Orientation == PortOrientation.Vertical) ?? false;
            var hasVerticalOutput = ContextNodeModel?.OutputsById.Values.Any(t => t.Orientation == PortOrientation.Vertical) ?? false;

            EnableInClassList(ussClassName.WithUssModifier("no-vertical-input"), !hasVerticalInput);
            EnableInClassList(ussClassName.WithUssModifier("no-vertical-output"), !hasVerticalOutput);
        }

        /// <inheritdoc/>
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartBefore(bottomPortContainerPartName, ContextBlocksPart.Create(blocksPartName, NodeModel, this, ussClassName));

            PartList.MovePartBefore(nodeCachePartName, blocksPartName);
        }

        internal void StartBlockDragging_Internal(float blocksHeight)
        {
            m_DragBlock.style.height = blocksHeight;
        }

        int GetBlockIndex(Vector2 posInContext)
        {
            var blockContainer = ContextBlocksRoot;
            if (blockContainer == null)
                return 0;

            var blocks = blockContainer.Children().OfType<BlockNode>().ToList();

            if (blocks.Count > 0)
            {
                var firstBlock = blocks.Last();

                int i = blocks.Count - 1;
                Rect firstLayout = firstBlock.parent.ChangeCoordinatesTo(this, firstBlock.layout);
                float y = firstLayout.y;
                for (; i >= 0; --i)
                {
                    float blockY = blocks[i].layout.height;
                    if (y + blockY * 0.5f > posInContext.y)
                        break;

                    y += blockY + blocks[i].resolvedStyle.marginTop + blocks[i].resolvedStyle.marginBottom;
                }

                return i + 1;
            }

            return 0;
        }

        internal void BlockDraggingRefused_Internal()
        {
            m_ContextBorder.AddToClassList(contextBorderRefusedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
        }

        internal void BlocksDragging_Internal(Vector2 posInContext, IEnumerable<BlockNodeModel> blocks, bool copy)
        {
            var blockContainer = ContextBlocksRoot;
            if (blockContainer == null)
                return;

            m_ContextBorder.AddToClassList(contextBorderAcceptedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderRefusedUssClassName);

            int index = GetBlockIndex(posInContext);

            if (index >= blockContainer.childCount)
                blockContainer.Add(m_DragBlock);
            else
                blockContainer.Insert((index < 0 ? 0 : index) + 1, m_DragBlock);
        }

        internal void BlocksDropped_Internal(Vector2 posInContext, IEnumerable<BlockNodeModel> blocks, bool copy)
        {
            int index = GetBlockIndex(posInContext);

            int realIndex = ContextNodeModel.GraphElementModels.Count() - index - (copy ? 0 : blocks.Count(t => t.ContextNodeModel == ContextNodeModel));

            GraphView.Dispatch(new InsertBlocksInContextCommand(ContextNodeModel, realIndex, blocks, copy));

            StopBlockDragging_Internal();
        }

        internal void StopBlockDragging_Internal()
        {
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderRefusedUssClassName);
            m_DragBlock.RemoveFromHierarchy();
        }

        /// <inheritdoc/>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if ((evt.target as VisualElement)?.GetFirstOfType<BlockNode>() == null)
            {
                evt.menu.AppendAction("Create Block",
                    action =>
                    {
                        var mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                        ShowItemLibrary(mousePosition);
                    });
            }
        }

        /// <inheritdoc/>
        public virtual bool ShowItemLibrary(Vector2 mousePosition)
        {
            var posInContext = this.WorldToLocal(mousePosition);

            int index = GetBlockIndex(posInContext);

            int realIndex = ContextNodeModel.GraphElementModels.Count() - index;

            return ShowItemLibrary_Internal(mousePosition, realIndex);
        }

        /// <summary>
        /// Display the library for insertion of a new block at the given index.
        /// </summary>
        /// <param name="mousePosition">The mouse position in window coordinates.</param>
        /// <param name="index">The index in the context at which the new block will be added.</param>
        /// <returns>True if a <see cref="ItemLibraryWindow"/> could be displayed.</returns>
        internal bool ShowItemLibrary_Internal(Vector2 mousePosition, int index)
        {
            var stencil = (Stencil)GraphView.GraphModel.Stencil;
            var filter = stencil.GetLibraryFilterProvider()?.GetContextFilter(ContextNodeModel);
            var adapter = stencil.GetItemLibraryAdapter(GraphView.GraphModel, "Add a block", GraphView.GraphTool.Name);
            var dbProvider = stencil.GetItemDatabaseProvider();

            if (dbProvider == null)
                return false;

            var dbs = dbProvider.GetGraphElementContainerDatabases(GraphView.GraphModel, ContextNodeModel);
            if (dbs == null)
                return false;

            ItemLibraryService.ShowDatabases_Internal(GraphView, mousePosition, item =>
            {
                GraphView.Dispatch(new CreateBlockFromItemLibraryCommand(item, ContextNodeModel, index));
            }, dbs, filter, adapter, "CreateContextNode");

            return true;
        }

        /// <inheritdoc/>
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!copyPasteData.m_Nodes_Internal.All(t => t is BlockNodeModel))
                return false;

            GraphView.Dispatch(new InsertBlocksInContextCommand(ContextNodeModel,
                -1,
                copyPasteData.m_Nodes_Internal.OfType<BlockNodeModel>().ToList(), true, operationName));

            return true;
        }
    }
}
