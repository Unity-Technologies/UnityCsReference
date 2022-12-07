// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The class for block nodes UI.
    /// </summary>
    class BlockNode : CollapsibleInOutNode
    {
        /// <summary>
        /// The USS class name used for blocks
        /// </summary>
        public new static readonly string ussClassName = "ge-block-node";

        static readonly string k_EtchBorderName = "etch-over-border";
        static readonly string k_EtchName = "etch";
        static readonly string k_EtchBorderColorName = "etch-border-color";
        static readonly string k_EtchColorName = "etch-color";

        /// <summary>
        /// Modifier class added to class list when the BlockNode has at least one output.
        /// </summary>
        public static readonly string hasOutputModifierUssClassName = ussClassName.WithUssModifier("has-output");

        /// <summary>
        /// The USS class name used for the etch element.
        /// </summary>
        public static readonly string etchUssClassName = ussClassName.WithUssElement(k_EtchName);

        /// <summary>
        /// The USS class name used for the etch border element.
        /// </summary>
        public static readonly string etchBorderUssClassName = ussClassName.WithUssElement(k_EtchBorderName);

        /// <summary>
        /// The USS class name used for the etch color element.
        /// </summary>
        public static readonly string etchColorUssClassName = ussClassName.WithUssElement(k_EtchColorName);

        /// <summary>
        /// The USS class name used for the etch border color element.
        /// </summary>
        public static readonly string etchBorderColorUssClassName = ussClassName.WithUssElement(k_EtchBorderColorName);

        /// <summary>
        /// The <see cref="BlockNodeModel"/> this <see cref="BlockNode"/> displays.
        /// </summary>
        public BlockNodeModel BlockNodeModel => Model as BlockNodeModel;

        VisualElement m_EtchBorder;
        VisualElement m_Etch;
        VisualElement m_EtchBorderColor;
        VisualElement m_EtchColor;
        BlockDragInfos_Internal m_BlockDragInfos;


        internal VisualElement Etch_Internal => m_Etch;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNode" /> class.
        /// </summary>
        public BlockNode()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (e.button == (int)MouseButton.LeftMouse)
            {
                e.StopPropagation();
                m_BlockDragInfos = new BlockDragInfos_Internal(this);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<KeyDownEvent>(OnDragKey);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.OnMouseDown(e);
            }
        }

        void ClearDragging()
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<KeyDownEvent>(OnDragKey);
            }

            m_BlockDragInfos = null;
        }

        void OnDragKey(KeyDownEvent e)
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (e.keyCode == KeyCode.Escape)
            {
                m_BlockDragInfos.ReleaseDragging();
                ClearDragging();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (m_BlockDragInfos != null && !m_BlockDragInfos.OnMouseMove(e))
            {
                ClearDragging();
            }

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.OnMouseUp(e);
                ClearDragging();
            }
        }

        /// <inheritdoc/>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (BlockNodeModel is IPlaceholder || BlockNodeModel.ContextNodeModel is IPlaceholder)
                return;

            ContextNode context = GetFirstAncestorOfType<ContextNode>();
            if (context == null)
                return;
            evt.menu.AppendAction("Insert Block Before",
                action =>
                {
                    Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                    context.ShowItemLibrary_Internal(mousePosition, BlockNodeModel.GetIndex());
                });
            evt.menu.AppendAction("Insert Block After",
                action =>
                {
                    Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                    context.ShowItemLibrary_Internal(mousePosition, BlockNodeModel.GetIndex() + 1);
                });
            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }

        /// <inheritdoc/>
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_EtchBorder = new VisualElement() { name = k_EtchBorderName };
            m_EtchBorder.AddToClassList(etchBorderUssClassName);
            hierarchy.Add(m_EtchBorder);

            m_Etch = new VisualElement() { name = k_EtchName };
            m_Etch.AddToClassList(etchUssClassName);
            hierarchy.Add(m_Etch);

            m_EtchBorderColor = new VisualElement() { name = k_EtchBorderColorName };
            m_EtchBorderColor.AddToClassList(etchBorderColorUssClassName);
            m_EtchBorder.Add(m_EtchBorderColor);

            m_EtchColor = new VisualElement() { name = k_EtchColorName };
            m_EtchColor.AddToClassList(etchColorUssClassName);
            m_Etch.Add(m_EtchColor);

            Border.BringToFront();
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet_Internal("BlockNode.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc/>
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            bool hasInput = BlockNodeModel?.InputsByDisplayOrder.Any() ?? false;
            bool hasOutput = BlockNodeModel?.OutputsByDisplayOrder.Any() ?? false;

            if (NodeModel.HasUserColor && !hasInput && !hasOutput)
            {
                m_EtchColor.style.backgroundColor = NodeModel.Color;
                m_EtchBorderColor.style.backgroundColor = NodeModel.Color;
            }
            else
            {
                if (hasOutput)
                    AddToClassList(hasOutputModifierUssClassName);
                else
                    RemoveFromClassList(hasOutputModifierUssClassName);
                m_EtchColor.style.backgroundColor = StyleKeyword.Null;
                m_EtchBorderColor.style.backgroundColor = StyleKeyword.Null;
            }
        }

        /// <inheritdoc/>
        public override void SetPositionOverride(Vector2 position)
        {
            //Setting the position of a BlockNode does nothing.
        }

        /// <inheritdoc/>
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!copyPasteData.Nodes.All(t => t is BlockNodeModel))
                return false;

            if (operation == PasteOperation.Duplicate)
            {
                // If we duplicate we want to duplicate each selected block in its own context.
                // Duplicated blocks are added after the last selected block of each context.

                var selection = GraphView.GetSelection();
                var groupedBlocks = selection.OfType<BlockNodeModel>().GroupBy(t => t.ContextNodeModel);

                var contextDatas = new InsertBlocksInContextCommand.ContextData[groupedBlocks.Count()];

                int cpt = 0;
                foreach (var contextData in groupedBlocks)
                {
                    contextDatas[cpt++] = new InsertBlocksInContextCommand.ContextData()
                    {
                        Context = contextData.Key,
                        Blocks = contextData.ToList(),
                        Index = contextData.Max(t => t.GetIndex()) + 1
                    };
                }

                GraphView.Dispatch(new InsertBlocksInContextCommand(contextDatas, true, operationName));
            }
            else
            {
                // If we paste, we paste everything below the last selected block in the same context as this block.

                var selection = GraphView.GetSelection();
                var selectedBlocksInSameContext = selection.OfType<BlockNodeModel>().Where(t => t.ContextNodeModel == BlockNodeModel.ContextNodeModel);
                var index = selectedBlocksInSameContext.Max(t => t.GetIndex()) + 1;

                var nodesToPaste = copyPasteData.Nodes.OfType<BlockNodeModel>();

                GraphView.Dispatch(new InsertBlocksInContextCommand(BlockNodeModel.ContextNodeModel, index, nodesToPaste, true, operationName));
            }

            return true;
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBlockBorder(this);
        }

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            if (newZoomMode != oldZoomMode)
                ((DynamicBlockBorder) Border).DisplayEtch = !(newZoomMode is GraphViewZoomMode.Small or GraphViewZoomMode.VerySmall);
        }
    }
}
