// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// VisualElement to display a <see cref="ContextNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class ContextNodeView : CollapsibleInOutNodeView, IShowItemLibraryUI
    {
        /// <summary>
        /// The <see cref="ContextNodeModel"/> this ContextNode displays.
        /// </summary>
        public ContextNodeModel ContextNodeModel => Model as ContextNodeModel;

        /// <summary>
        /// The USS class name added to a <see cref="ContextNodeView"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-context-node";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> containing the blocks.
        /// </summary>
        public static readonly string blocksPartName = "blocks-container-part";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> showing the category at the top of the context.
        /// </summary>
        public static readonly string contextTopCategoryPartName = "top-category-part";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> showing the category at the bottom of the context.
        /// </summary>
        public static readonly string contextBottomCategoryPartName = "bottom-category-part";

        static readonly string k_ContextBorderName = "context-border";
        static readonly string k_ContextBorderTitleName = "context-border-title";

        internal static readonly CustomStyleProperty<Color> BlocksBorderColorStyle = new CustomStyleProperty<Color>("--blocks--border-color");

        /// <summary>
        /// The USS class name added to the context borders element.
        /// </summary>
        public static readonly string contextBorderUssClassName = ussClassName.WithUssElement(k_ContextBorderName);

        /// <summary>
        /// The USS class name added to the title element in the context border.
        /// </summary>
        public static readonly string contextBorderTitleUssClassName = ussClassName.WithUssElement(k_ContextBorderTitleName);

        /// <summary>
        /// The USS class name added to the context borders element when the drag is refused.
        /// </summary>
        public static readonly string contextBorderRefusedUssClassName = contextBorderUssClassName.WithUssModifier("refused");

        /// <summary>
        /// The USS class name added to the context borders element when the drag is accepted.
        /// </summary>
        public static readonly string contextBorderAcceptedUssClassName = contextBorderUssClassName.WithUssModifier("accepted");

        VisualElement m_ContextBorder;
        VisualElement m_DragBlock;
        BlockDrawParams m_DrawParams = BlockDrawParams.Default;
        Color m_BlocksBkgndColor;
        Color m_BlocksBorderColor;
        bool m_Attached;

        /// <summary>
        /// The root element of the context blocks.
        /// </summary>
        public VisualElement ContextBlocksRoot { get; private set; }

        /// <summary>
        /// The <see cref="ContextBlocksPart"/> contained in this <see cref="ContextNodeView"/>
        /// </summary>
        public ContextBlocksPart ContextBlocksPart { get; private set; }

        protected internal override int NodeTitleOptions => NodeTitlePart.Options.HasIcon;

        /// <inheritdoc/>
        protected override void BuildUI()
        {
            base.BuildUI();

            m_DragBlock = new VisualElement() { name = "drag-block" };
        }

        /// <inheritdoc/>
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartBefore(topPortContainerPartName, new CategoryPart(contextTopCategoryPartName, ContextNodeModel, this, ussClassName, true));

            ContextBlocksPart = CreateContextBlocksPart();
            PartList.InsertPartBefore(bottomPortContainerPartName, ContextBlocksPart);
            PartList.MovePartBefore(cachePartName, blocksPartName);
            PartList.AppendPart(new CategoryPart(contextBottomCategoryPartName, ContextNodeModel, this, ussClassName, false));
        }

        /// <summary>
        /// Creates the context blocks part.
        /// </summary>
        /// <returns>The newly instantiated context blocks part.</returns>
        /// <remarks>
        /// Override this method to provide a custom implementation of <see cref="ContextBlocksPart"/> if a specialized behavior or appearance is required.
        /// The returned instance manages and displays blocks within the context node.
        /// </remarks>
        protected virtual ContextBlocksPart CreateContextBlocksPart()
        {
            return ContextBlocksPart.Create(blocksPartName, NodeModel, this, ussClassName);
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddPackageStylesheet("ContextNode.uss");
            AddToClassList(ussClassName);

            ContextBlocksRoot = ContextBlocksPart.PartList.GetPart(ContextBlocksPart.blocksContainerName).Root;
            ContextBlocksPart.Root.generateVisualContent = GenerateBlocksVisualContent;

            m_ContextBorder = new VisualElement { name = k_ContextBorderName };
            m_ContextBorder.AddToClassList(contextBorderUssClassName);
            m_ContextBorder.pickingMode = PickingMode.Ignore;
            contentContainer.Add(m_ContextBorder);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <inheritdoc/>
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            var hasVerticalInput = ContextNodeModel?.InputsById.Values.HasAny(t => t.Orientation == PortOrientation.Vertical) ?? false;
            var hasVerticalOutput = ContextNodeModel?.OutputsById.Values.HasAny(t => t.Orientation == PortOrientation.Vertical) ?? false;

            EnableInClassList(ussClassName.WithUssModifier("no-vertical-input"), !hasVerticalInput);
            EnableInClassList(ussClassName.WithUssModifier("no-vertical-output"), !hasVerticalOutput);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (!m_Attached)
            {
                GraphView.RegisterElementZoomLevelClass(this, GraphViewZoomMode.Small, ussClassName.WithUssModifier(GraphElementHelper.mediumUssModifier));
                m_Attached = true;
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_Attached)
            {
                GraphView.UnregisterElementZoomLevelClass(this, GraphViewZoomMode.Small);
                m_Attached = false;
            }
        }

        internal void StartBlockDragging(float blocksHeight)
        {
            m_DragBlock.style.height = blocksHeight;
        }

        int GetBlockIndex(Vector2 posInContext)
        {
            var blockContainer = ContextBlocksRoot;
            if (blockContainer == null)
                return 0;

            using var dispose = blockContainer.Children().OfTypeToPooledList<BlockNodeView, VisualElement>(out var blocks);

            if (blocks.Count > 0)
            {
                var firstBlock = blocks[0];

                int i;
                Rect firstLayout = firstBlock.parent.ChangeCoordinatesTo(this, firstBlock.layout);
                float y = firstLayout.y;
                for (i = 0; i < blocks.Count; ++i)
                {
                    float blockY = blocks[i].layout.height;
                    if (y + blockY * 0.5f > posInContext.y)
                        break;

                    y += blockY + blocks[i].resolvedStyle.marginTop + blocks[i].resolvedStyle.marginBottom;
                }

                return i;
            }

            return 0;
        }

        internal void BlockDraggingRefused()
        {
            m_ContextBorder.AddToClassList(contextBorderRefusedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
        }

        internal void BlocksDragging(Vector2 posInContext, IEnumerable<BlockNodeModel> blocks, bool copy)
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
                blockContainer.Insert(index, m_DragBlock);
        }

        internal void BlocksDropped(Vector2 posInContext, IEnumerable<BlockNodeModel> blocks, bool copy)
        {
            int index = GetBlockIndex(posInContext);

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            GraphView.Dispatch(new InsertBlocksInContextCommand(ContextNodeModel, index, blocks?.ToList(), true, copy));
#pragma warning restore UA2001

            StopBlockDragging();
        }

        internal void StopBlockDragging()
        {
            m_ContextBorder.RemoveFromClassList(contextBorderAcceptedUssClassName);
            m_ContextBorder.RemoveFromClassList(contextBorderRefusedUssClassName);
            m_DragBlock.RemoveFromHierarchy();
        }

        /// <inheritdoc/>
        public virtual bool ShowItemLibrary(Vector2 mousePosition)
        {
            var posInContext = this.WorldToLocal(mousePosition);

            int index = GetBlockIndex(posInContext);

            return ShowItemLibrary(mousePosition, index);
        }

        /// <summary>
        /// Display the library for insertion of a new block at the given index.
        /// </summary>
        /// <param name="mousePosition">The mouse position in window coordinates.</param>
        /// <param name="index">The index in the context at which the new block will be added.</param>
        /// <returns>True if a <see cref="ItemLibraryWindow"/> could be displayed.</returns>
        internal bool ShowItemLibrary(Vector2 mousePosition, int index)
        {
            var libraryHelper = GraphView.GetItemLibraryHelper();
            var filter = libraryHelper.GetLibraryFilterProvider()?.GetContextFilter(ContextNodeModel);
            var adapter = libraryHelper.GetItemLibraryAdapter(ContextNodeModel.AddBlockText, GraphView.GraphTool.Name);
            var dbProvider = libraryHelper.GetItemDatabaseProvider();

            if (dbProvider == null)
                return false;

            var dbs = dbProvider.GetGraphElementContainerDatabases(ContextNodeModel);
            if (dbs == null)
                return false;

            ItemLibraryService.ShowDatabases(GraphView, mousePosition, item =>
            {
                if (item is GraphNodeModelLibraryItem nodeItem)
                    GraphView.Dispatch(new CreateBlockFromItemLibraryCommand(nodeItem, ContextNodeModel, index));
            }, dbs, filter, adapter, "CreateContextNode");

            return true;
        }

        /// <inheritdoc/>
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!copyPasteData.Nodes.Exists(t => t is BlockNodeModel))
                return false;

            using var dispose = copyPasteData.Nodes.OfTypeToPooledList<BlockNodeModel, AbstractNodeModel>(out var blocks);

            GraphView.Dispatch(new InsertBlocksInContextCommand(
                ContextNodeModel, -1, blocks, false, true, operationName));

            return true;
        }

        /// <inheritdoc/>
        public override void RefreshBorder()
        {
            base.RefreshBorder();
            ContextBlocksPart.Root.MarkDirtyRepaint();
            ContextBlocksPart.RefreshBorder();
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            var changed = false;
            if (e.customStyle.TryGetValue(BlockNodeView.BlocksBackgroundColorStyle, out Color bkgndColor))
            {
                m_BlocksBkgndColor = bkgndColor;
                changed = true;
            }
            if (e.customStyle.TryGetValue(BlocksBorderColorStyle, out Color borderColor))
            {
                m_BlocksBorderColor = borderColor;
                changed = true;
            }
            changed = m_DrawParams.CustomStyleResolved(e) || changed;

            if (changed)
            {
                ContextBlocksPart.Root.MarkDirtyRepaint();
            }
        }

        void GenerateBlocksVisualContent(MeshGenerationContext mgc)
        {
            var bounds = ContextBlocksPart.Root.localBound;
            bounds.position = Vector2.zero;
            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            var p2d = mgc.painter2D;
            p2d.fillColor = m_BlocksBkgndColor;

            const float border = 1;

            bounds.position += Vector2.one * border * 0.5f;
            bounds.size -= Vector2.one * border;

            var drawParams = m_DrawParams;

            drawParams.topEtchMargin -= border;
            drawParams.topEtchWidth += border;

            BlockNodeView.DrawBlock(ref bounds, p2d, ContextNodeModel.BlocksHaveEtches, true, true, false, ref drawParams);

            p2d.strokeColor = m_BlocksBorderColor;
            p2d.lineWidth = border;

            p2d.Fill(FillRule.OddEven);
            p2d.Stroke();
        }
    }
}
