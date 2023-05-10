// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The uss class of the first block node.
        /// </summary>
        public static readonly string firstBlockUssClassName = ussClassName.WithUssModifier("first");

        /// <summary>
        /// The uss class of the last block node.
        /// </summary>
        public static readonly string lastBlockUssClassName = ussClassName.WithUssModifier("last");

        /// <summary>
        /// The uss class for a block without title.
        /// </summary>
        public static readonly string blockWithoutTitleUssClassName = ussClassName.WithUssModifier("without-title");

        float m_Border = 1.0f;
        BlockDrawParams_Internal m_DrawParams = BlockDrawParams_Internal.Default;

        internal static readonly CustomStyleProperty<Color> BlocksBackgroundColorStyle = new CustomStyleProperty<Color>("--block--background-color");
        internal static readonly CustomStyleProperty<Color> DisabledBackgroundColorStyle = new CustomStyleProperty<Color>("--disabled-background-color");
        internal static readonly CustomStyleProperty<float> BlockBorderStyle = new CustomStyleProperty<float>("--block--border");

        /// <summary>
        /// The <see cref="BlockNodeModel"/> this <see cref="BlockNode"/> displays.
        /// </summary>
        public BlockNodeModel BlockNodeModel => Model as BlockNodeModel;

        BlockDragInfos_Internal m_BlockDragInfos;

        Color m_BkgndColor;
        Color m_DisabledColor;

        protected internal override int NodeTitleOptions => EditableTitlePart.Options.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNode" /> class.
        /// </summary>
        public BlockNode()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var changed = false;
            if (e.customStyle.TryGetValue(BlocksBackgroundColorStyle, out Color bkgndColor))
            {
                m_BkgndColor = bkgndColor;
                changed = true;
            }

            {
                if (e.customStyle.TryGetValue(BlockBorderStyle, out float value))
                {
                    m_Border = value;
                    changed = true;
                }
            }

            changed = m_DrawParams.CustomStyleResolved(e) || changed;

            if (changed)
            {
                if (Border is DynamicBlockBorder_Internal blockBorder)
                {
                    blockBorder.SetDrawParams(ref m_DrawParams);
                }
                MarkDirtyRepaint();
            }

            if (e.customStyle.TryGetValue(DisabledBackgroundColorStyle, out Color color))
            {
                m_DisabledColor = color;
                m_DisabledOverlay.MarkDirtyRepaint();
            }
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
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseDownEvent>(OnOtherMouseDown);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext_Internal.RegisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
                m_BlockDragInfos.OnMouseDown(e);
            }
        }

        void OnOtherMouseDown(MouseDownEvent e)
        {
            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.ReleaseDragging();
                ClearDragging();
                e.StopPropagation();
            }
        }

        void OnCaptureLost(MouseCaptureOutEvent e)
        {
            m_BlockDragInfos.ReleaseDragging();
            ClearDragging();
        }

        void ClearDragging()
        {
            if (BlockNodeModel is IPlaceholder)
                return;

            if (m_BlockDragInfos != null)
            {
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseDownEvent>(OnOtherMouseDown);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<KeyDownEvent>(OnDragKey);
                m_BlockDragInfos.DraggedBlockContext_Internal.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
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
            if (e.button != (int)MouseButton.LeftMouse)
            {
                m_BlockDragInfos.ReleaseDragging();
                ClearDragging();
                e.StopPropagation();
                return;
            }

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
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet_Internal("BlockNode.uss");
            AddToClassList(ussClassName);

            m_DisabledOverlay = this.SafeQ(disabledOverlayElementName);
            m_DisabledOverlay.generateVisualContent = OnGenerateOverlayVisualContent;
        }

        /// <inheritdoc/>
        public override void SetPositionOverride(Vector2 position)
        {
            //Setting the position of a BlockNode does nothing.
        }

        /// <inheritdoc/>
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            foreach (var node in copyPasteData.Nodes)
            {
                if (node is not Editor.BlockNodeModel)
                    return false;
            }

            int MyMax(List<BlockNodeModel> blockNodeModels)
            {
                int max = 0;
                foreach (var model in blockNodeModels)
                {
                    var index = model.GetIndex();
                    if (max < index)
                        max = index;
                }

                return max;
            }

            if (operation == PasteOperation.Duplicate)
            {
                // If we duplicate we want to duplicate each selected block in its own context.
                // Duplicated blocks are added after the last selected block of each context.

                var selection = GraphView.GetSelection();

                Dictionary<ContextNodeModel, List<BlockNodeModel>> groupedBlocks = new();
                foreach (var element in selection)
                {
                    if (element is BlockNodeModel blockNodeModel)
                    {
                        if (!groupedBlocks.TryGetValue(blockNodeModel.ContextNodeModel, out List<BlockNodeModel> list))
                        {
                            list = new();
                            groupedBlocks.Add(blockNodeModel.ContextNodeModel,list);
                        }
                        list.Add(blockNodeModel);
                    }
                }

                var contextDatas = new InsertBlocksInContextCommand.ContextData[groupedBlocks.Count];

                int cpt = 0;
                foreach (var contextData in groupedBlocks)
                {
                    contextDatas[cpt++] = new InsertBlocksInContextCommand.ContextData()
                    {
                        Context = contextData.Key,
                        Blocks = contextData.Value,
                        Index = MyMax(contextData.Value) + 1
                    };
                }

                GraphView.Dispatch(new InsertBlocksInContextCommand(contextDatas, false, true, operationName));
            }
            else
            {
                // If we paste, we paste everything below the last selected block in the same context as this block.

                var selection = GraphView.GetSelection();
                var selectedBlocksInSameContext = new List<BlockNodeModel>();
                foreach(var element in selection)
                {
                    if(element is not BlockNodeModel blockNodeModel)
                        continue;
                    if(blockNodeModel.ContextNodeModel == BlockNodeModel.ContextNodeModel)
                        selectedBlocksInSameContext.Add(blockNodeModel);
                }

                var index = MyMax(selectedBlocksInSameContext) + 1;

                var blocksToPaste = new BlockNodeModel[copyPasteData.Nodes.Count];
                int cpt = 0;
                foreach (var node in copyPasteData.Nodes)
                    blocksToPaste[cpt++] = (BlockNodeModel)node;

                GraphView.Dispatch(new InsertBlocksInContextCommand(BlockNodeModel.ContextNodeModel, index, blocksToPaste, false, true, operationName));
            }

            return true;
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            var blockBorder =  new DynamicBlockBorder_Internal(this);
            blockBorder.IsLast = false;
            blockBorder.SetDrawParams(ref m_DrawParams);

            RegisterCallback<MouseEnterEvent>(_ => BorderChanged());
            RegisterCallback<MouseLeaveEvent>(_ => BorderChanged());

            return blockBorder;
        }

        bool m_DisplayEtch;
        VisualElement m_DisabledOverlay;

        void BorderChanged()
        {
            GetFirstAncestorOfType<ContextNode>()?.ContextBlocksPart?.BlockBorderChanged();
        }


        bool m_WasSelected;
        bool m_WasFirst;

        public override void UpdateFromModel()
        {
            base.UpdateFromModel();

            if (BlockNodeModel == null || BlockNodeModel.ContextNodeModel == null)
                return;

            bool isSelected = IsSelected();
            bool isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.GetBlock(0);

            if (isSelected != m_WasSelected || isFirst != m_WasFirst)
            {
                m_WasSelected = isSelected;
                m_WasFirst = isFirst;
                BorderChanged();
            }

            if (Border is DynamicBlockBorder_Internal blockBorder)
            {
                blockBorder.IsFirst = isFirst;
            }

            EnableInClassList(blockWithoutTitleUssClassName,!BlockNodeModel.ShouldShowTitle);

        }

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            if (newZoomMode != oldZoomMode)
            {
                var newDisplayEtch = !(newZoomMode is GraphViewZoomMode.Small or GraphViewZoomMode.VerySmall);
                if (newDisplayEtch != m_DisplayEtch)
                {
                    m_DisplayEtch = newDisplayEtch;
                    ((DynamicBlockBorder_Internal)Border).DisplayEtch = m_DisplayEtch;
                    MarkDirtyRepaint();
                    BorderChanged();
                }
            }
        }

        internal static void Inset_Internal(ref Rect r, Vector2 inset)
        {
            r.position += inset;
            r.size -= inset * 2.0f;
        }

        protected void OnGenerateOverlayVisualContent(MeshGenerationContext mgc)
        {
            bool isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.GetBlock(0);
            var p2d = mgc.painter2D;
            p2d.fillColor = m_DisabledColor;
            var bounds = m_DisabledOverlay.localBound;
            bounds.position = Vector2.zero;
            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            DrawBlock_Internal(ref bounds, p2d, m_DisplayEtch, false, isFirst, ref m_DrawParams);

            p2d.Fill(FillRule.OddEven);
        }




        /// <summary>
        /// Generate the visuals of the border
        /// </summary>
        /// <param name="mgc">The mesh generation context.</param>
        protected void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            bool isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.GetBlock(0);

            var bounds = localBound;
            bounds.position = Vector2.zero;
            bounds.height -= resolvedStyle.paddingBottom;
            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            Inset_Internal(ref bounds, Vector2.one * (m_Border * 0.5f));


            var actualParams = m_DrawParams;

            actualParams.topEtchMargin -= m_Border;
            actualParams.topEtchWidth += m_Border;
            actualParams.bottomEtchMargin -= m_Border;
            actualParams.bottomEtchWidth += m_Border;

            var p2d = mgc.painter2D;
            p2d.fillColor = m_BkgndColor;

            DrawBlock_Internal(ref bounds, p2d, m_DisplayEtch, false, isFirst, ref actualParams);

            p2d.lineWidth = m_Border;
            p2d.strokeColor = resolvedStyle.borderLeftColor;

            p2d.Fill(FillRule.OddEven);
            p2d.Stroke();

        }

        internal static void DrawBlock_Internal(ref Rect r, Painter2D p2d, bool displayEtch, bool isLast, bool isFirst,ref BlockDrawParams_Internal drawParams)
        {
            p2d.BeginPath();
            if (isLast)
                p2d.MoveTo(new Vector2(r.xMin, r.yMax - drawParams.extremeBlockRadius));
            else
                p2d.MoveTo(new Vector2(r.xMin, r.yMax));

            if (isFirst)
                p2d.ArcTo(new Vector2(r.xMin, r.yMin),
                    new Vector2(r.xMin + drawParams.extremeBlockRadius, r.yMin),
                    drawParams.extremeBlockRadius);
            else
                p2d.LineTo(new Vector2(r.xMin, r.yMin));

            if (displayEtch)
            {
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin - drawParams.etchInnerRadius, r.yMin));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin + drawParams.etchInnerRadius));
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin - drawParams.etchOuterRadius + drawParams.etchHeight));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.etchOuterRadius, r.yMin + drawParams.etchHeight));
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth - drawParams.etchOuterRadius, r.yMin  + drawParams.etchHeight));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin + drawParams.etchHeight - drawParams.etchOuterRadius));
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin  + drawParams.etchInnerRadius));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth + drawParams.etchInnerRadius, r.yMin));
            }

            if (isFirst)
                p2d.ArcTo(new Vector2(r.xMax, r.yMin),
                    new Vector2(r.xMax, r.yMin + drawParams.extremeBlockRadius),
                    drawParams.extremeBlockRadius);
            else
                p2d.LineTo(new Vector2(r.xMax, r.yMin));

            if (isLast)
                p2d.ArcTo(new Vector2(r.xMax, r.yMax),
                    new Vector2(r.xMax - drawParams.extremeBlockRadius, r.yMax),
                    drawParams.extremeBlockRadius);
            else
                p2d.LineTo(new Vector2(r.xMax, r.yMax));

            if (displayEtch)
            {
                p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + drawParams.etchInnerRadius, r.yMax));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax + drawParams.etchInnerRadius));

                p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax + drawParams.etchHeight - drawParams.etchOuterRadius));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth, r.yMax + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth - drawParams.etchOuterRadius, r.yMax + drawParams.etchHeight));

                p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.etchOuterRadius, r.yMax + drawParams.etchHeight));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax + drawParams.etchHeight - drawParams.etchOuterRadius));
                p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax + drawParams.etchInnerRadius));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin, r.yMax),
                    new Vector2(r.xMin + drawParams.bottomEtchMargin - drawParams.etchInnerRadius, r.yMax));

            }

            if (isLast)
                p2d.ArcTo(new Vector2(r.xMin, r.yMax),
                    new Vector2(r.xMin, r.yMax - drawParams.extremeBlockRadius),
                    drawParams.extremeBlockRadius);

            p2d.ClosePath();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            var bounds = localBound;
            bounds.position = Vector2.zero;
            var topEtch = new Rect(m_DrawParams.topEtchMargin, 0, m_DrawParams.topEtchWidth, m_DrawParams.etchHeight);
            if (topEtch.Contains(localPoint))
                return false;

            var beforeBottomEtch = new Rect(0, bounds.yMax - m_DrawParams.etchHeight, m_DrawParams.bottomEtchMargin, bounds.yMax);
            if (beforeBottomEtch.Contains(localPoint))
                return false;

            var afterBottomEtch = new Rect(m_DrawParams.bottomEtchWidth + m_DrawParams.bottomEtchWidth, bounds.yMax - m_DrawParams.etchHeight, bounds.yMax - m_DrawParams.bottomEtchWidth - m_DrawParams.bottomEtchWidth, bounds.yMax);
            if (afterBottomEtch.Contains(localPoint))
                return false;

            return base.ContainsPoint(localPoint);
        }
    }
}
