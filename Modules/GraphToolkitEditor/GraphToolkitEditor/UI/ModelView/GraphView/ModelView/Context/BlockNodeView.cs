// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The class for block nodes UI.
    /// </summary>
    [UnityRestricted]
    internal class BlockNodeView : CollapsibleInOutNodeView
    {
        /// <summary>
        /// The USS class name added to blocks
        /// </summary>
        public new static readonly string ussClassName = "ge-block-node";

        /// <summary>
        /// The USS class of the first block node.
        /// </summary>
        public static readonly string firstBlockUssClassName = ussClassName.WithUssModifier("first");

        /// <summary>
        /// The USS class of the last block node.
        /// </summary>
        public static readonly string lastBlockUssClassName = ussClassName.WithUssModifier("last");

        /// <summary>
        /// The USS class for a block without title.
        /// </summary>
        public static readonly string blockWithoutTitleUssClassName = ussClassName.WithUssModifier("without-title");

        float m_Border = 1.0f;
        BlockDrawParams m_DrawParams = BlockDrawParams.Default;

        internal static readonly CustomStyleProperty<Color> BlocksBackgroundColorStyle = new CustomStyleProperty<Color>("--block--background-color");
        internal static readonly CustomStyleProperty<Color> DisabledBackgroundColorStyle = new CustomStyleProperty<Color>("--disabled-background-color");
        internal static readonly CustomStyleProperty<float> BlockBorderStyle = new CustomStyleProperty<float>("--block--border");
        internal static readonly CustomStyleProperty<Color> NodeOutputBackgroundColorStyle = new CustomStyleProperty<Color>("--block-output--background-color");

        /// <summary>
        /// The <see cref="BlockNodeModel"/> this <see cref="BlockNodeView"/> displays.
        /// </summary>
        public BlockNodeModel BlockNodeModel => Model as BlockNodeModel;

        BlockDragInfos m_BlockDragInfos;

        Color m_BkgndColor;
        Color m_OutputBkgndColor;
        Color m_DisabledColor;
        bool m_Attached;

        /// <summary>
        /// The color line element on the block.
        /// </summary>
        protected VisualElement m_ColorLine;

        protected internal override int NodeTitleOptions => EditableTitlePart.Options.ClickToEditDisabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockNodeView" /> class.
        /// </summary>
        public BlockNodeView()
        {
            // Excluse blocks from tab navigation as users should navigate from property fields to property fields.
            tabIndex = -1;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            m_DisplayEtch = true;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var changed = false;
            if (e.customStyle.TryGetValue(BlocksBackgroundColorStyle, out Color bkgndColor))
            {
                m_BkgndColor = bkgndColor;
                changed = true;
            }
            
            if (e.customStyle.TryGetValue(NodeOutputBackgroundColorStyle, out Color outColor))
            {
                m_OutputBkgndColor = outColor;
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
                if (Border is DynamicBlockBorder blockBorder)
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
            if (BlockNodeModel is IPlaceholder || !BlockNodeModel.IsDroppable())
                return;

            if (e.button == (int)MouseButton.LeftMouse)
            {
                e.StopPropagation();
                m_BlockDragInfos = new BlockDragInfos(this);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<KeyDownEvent>(OnDragKey);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseDownEvent>(OnOtherMouseDown);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext.RegisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
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
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseDownEvent>(OnOtherMouseDown);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<KeyDownEvent>(OnDragKey);
                m_BlockDragInfos.DraggedBlockContext.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
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
            if (e.button != (int)MouseButton.LeftMouse && m_BlockDragInfos != null)
            {
                m_BlockDragInfos?.ReleaseDragging();
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
        protected override void BuildUI()
        {
            base.BuildUI();

            if (BlockNodeModel.IsColorable())
            {
                m_ColorLine = new VisualElement { name = NodeTitlePart.colorLineName };
                m_ColorLine.AddToClassList(ussClassName.WithUssElement(NodeTitlePart.colorLineName));
                m_ColorLine.generateVisualContent = OnGenerateColorLineVisualContent;
                hierarchy.Add(m_ColorLine);
            }
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddPackageStylesheet("BlockNode.uss");
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
                            groupedBlocks.Add(blockNodeModel.ContextNodeModel, list);
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
                foreach (var element in selection)
                {
                    if (element is not BlockNodeModel blockNodeModel)
                        continue;
                    if (blockNodeModel.ContextNodeModel == BlockNodeModel.ContextNodeModel)
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
            var blockBorder =  new DynamicBlockBorder(this);
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
            GetFirstAncestorOfType<ContextNodeView>()?.ContextBlocksPart?.BlockBorderChanged();
        }

        bool m_WasSelected;
        bool m_WasFirst;

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (BlockNodeModel == null || BlockNodeModel.ContextNodeModel == null)
                return;

            UpdateDisplayEtch();

            bool isSelected = IsSelected();
            bool isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.FirstBlock;

            if (isSelected != m_WasSelected || isFirst != m_WasFirst)
            {
                m_WasSelected = isSelected;
                m_WasFirst = isFirst;
                BorderChanged();
            }

            if (Border is DynamicBlockBorder blockBorder)
            {
                blockBorder.IsFirst = isFirst;
            }

            EnableInClassList(blockWithoutTitleUssClassName, !BlockNodeModel.ShouldShowTitle);

            if (visitor.ChangeHints.HasChange(ChangeHint.Style) && BlockNodeModel.IsColorable() && m_ColorLine != null)
            {
                m_ColorLine.MarkDirtyRepaint();
            }
        }

        GraphViewZoomMode m_CurrentZoomMode;

        /// <inheritdoc/>
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            if (newZoomMode != oldZoomMode)
            {
                m_CurrentZoomMode = newZoomMode;
                UpdateDisplayEtch();
            }
        }

        void UpdateDisplayEtch()
        {
            bool newDisplayEtch = !(m_CurrentZoomMode is GraphViewZoomMode.Small or GraphViewZoomMode.VerySmall) && BlockNodeModel.ContextNodeModel.BlocksHaveEtches;
            if (newDisplayEtch != m_DisplayEtch)
            {
                m_DisplayEtch = newDisplayEtch;
                ((DynamicBlockBorder)Border).DisplayEtch = m_DisplayEtch;
                MarkDirtyRepaint();
                BorderChanged();

                if (BlockNodeModel.ElementColor.HasUserColor)
                {
                    m_ColorLine.MarkDirtyRepaint();
                }
            }
        }

        internal static void Inset(ref Rect r, Vector2 inset)
        {
            r.position += inset;
            r.size -= inset * 2.0f;
        }

        protected void OnGenerateOverlayVisualContent(MeshGenerationContext mgc)
        {
            bool isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.FirstBlock;
            var p2d = mgc.painter2D;
            p2d.fillColor = m_DisabledColor;
            var bounds = m_DisabledOverlay.localBound;
            bounds.position = Vector2.zero;
            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            DrawBlock(ref bounds, p2d, m_DisplayEtch, false, isFirst, false, ref m_DrawParams);

            p2d.Fill(FillRule.OddEven);
        }

        /// <summary>
        /// Generates the visuals of the border.
        /// </summary>
        /// <param name="mgc">The mesh generation context.</param>
        /// <remarks>
        /// This method generates the visuals for the border of a block node. By default, the block node is drawn with an etch in the bottom right and resembles a puzzle piece
        /// to visually indicate the order of execution. The border follows that shape. It uses <see cref="Painter2D"/>, which is part of the provided <see cref="MeshGenerationContext"/>, to render the visual content.
        /// </remarks>
        protected void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var bounds = localBound;
            bounds.position = Vector2.zero;
            bounds.height -= resolvedStyle.paddingBottom;

            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            Inset(ref bounds, Vector2.one * (m_Border * 0.5f));

            var actualParams = m_DrawParams;
            actualParams.topEtchMargin -= m_Border;
            actualParams.topEtchWidth += m_Border;
            actualParams.bottomEtchMargin -= m_Border;
            actualParams.bottomEtchWidth += m_Border;

            var p2d = mgc.painter2D;
            var isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.FirstBlock;

            p2d.fillColor = m_BkgndColor;
            DrawBlock(ref bounds, p2d, m_DisplayEtch, false, isFirst, false, ref actualParams);
            p2d.Fill(FillRule.OddEven);

            bool isOutputOnlyState = m_DisplayEtch &&
                                     ClassListContains(CollapsibleInOutNodeView.collapsedUssClassName) &&
                                     ClassListContains(CollapsibleInOutNodeView.hasConnectedOutputUssClassName) &&
                                     !ClassListContains(CollapsibleInOutNodeView.hasConnectedInputUssClassName);

            // if the block is collapsed, and only output connected, we want the bottom etch to be the color of the
            // output port background.
            if (isOutputOnlyState)
            {
                p2d.BeginPath();
                p2d.fillColor = m_OutputBkgndColor;

                float startX = bounds.xMin + actualParams.bottomEtchMargin + actualParams.bottomEtchWidth + actualParams.etchInnerRadius;
                p2d.MoveTo(new Vector2(startX, bounds.yMax));

                AppendBottomEtchGeometry(p2d, ref bounds, ref actualParams, 0, 0);

                // Close it to fill
                p2d.ClosePath();
                p2d.Fill(FillRule.OddEven);
            }

            p2d.lineWidth = m_Border;
            p2d.strokeColor = resolvedStyle.borderLeftColor;
            DrawBlock(ref bounds, p2d, m_DisplayEtch, false, isFirst, false, ref actualParams);
            p2d.Stroke();
        }

        /// <summary>
        /// Generates the visuals of the color line on the block.
        /// </summary>
        /// <param name="mgc">The <see cref="MeshGenerationContext"/>.</param>
        /// <remarks>
        /// 'OnGenerateColorLineVisualContent' generates the visual representation of the color line on the block. The color line is
        /// a decorative element used to categorize or distinguish blocks visually. By default, it is drawn at the top of the
        /// <see cref="BlockNodeView"/>, spanning its full width and using the color defined in <see cref="ElementColor.Color"/>
        /// of the <see cref="BlockNodeModel"/>.
        /// </remarks>
        protected virtual void OnGenerateColorLineVisualContent(MeshGenerationContext mgc)
        {
            if (BlockNodeModel is null)
                return;

            var bounds = localBound;
            var actualParams = m_DrawParams;
            bounds.position = Vector2.zero;

            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return;

            actualParams.topEtchMargin -= m_Border;
            actualParams.topEtchWidth += m_Border;
            actualParams.bottomEtchMargin -= m_Border;
            actualParams.bottomEtchWidth += m_Border;

            var p2d = mgc.painter2D;
            var colorLineBound = new Rect(bounds) { height = actualParams.extremeBlockRadius, width = bounds.width - actualParams.etchInnerRadius };
            var isFirst = BlockNodeModel == BlockNodeModel?.ContextNodeModel?.FirstBlock;

            p2d.lineWidth = m_Border;

            DrawBlock(ref colorLineBound, p2d, m_DisplayEtch, false, isFirst, true, ref actualParams);

            p2d.fillColor = BlockNodeModel.ElementColor.Color;
            p2d.Fill(FillRule.OddEven);
        }

        internal static void DrawBlock(ref Rect r, Painter2D p2d, bool displayEtch, bool isLast, bool isFirst, bool isColorLine, ref BlockDrawParams drawParams)
        {
            var borderWidth = isColorLine ? p2d.lineWidth : 0;
            var colorLineWidth = isColorLine ? r.height : 0;

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
                    new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin),
                    new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin + drawParams.etchInnerRadius));
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin - drawParams.etchOuterRadius + drawParams.etchHeight));
                p2d.BezierCurveTo(
                    new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin - borderWidth, r.yMin + drawParams.etchHeight),
                    new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.etchOuterRadius - borderWidth, r.yMin + drawParams.etchHeight));
                p2d.LineTo(new Vector2(r.xMin + drawParams.topEtchMargin + drawParams.topEtchWidth - drawParams.etchOuterRadius - borderWidth, r.yMin  + drawParams.etchHeight));
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
            {
                p2d.LineTo(new Vector2(r.xMax - drawParams.extremeBlockRadius, r.yMin));
                p2d.ArcTo(new Vector2(r.xMax, r.yMin),
                    new Vector2(r.xMax, isColorLine ? r.yMax : r.yMin + drawParams.extremeBlockRadius),
                    drawParams.extremeBlockRadius);
            }
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
                AppendBottomEtchGeometry(p2d, ref r, ref drawParams, borderWidth, colorLineWidth);
            }

            if (isLast)
                p2d.ArcTo(new Vector2(r.xMin, r.yMax),
                    new Vector2(r.xMin, r.yMax - drawParams.extremeBlockRadius),
                    drawParams.extremeBlockRadius);

            p2d.ClosePath();
        }
        
        /// <summary>
        /// Appends only the bottom etch geometry commands to the current painter path.
        /// </summary>
        internal static void AppendBottomEtchGeometry(Painter2D p2d, ref Rect r, ref BlockDrawParams drawParams, float borderWidth, float colorLineWidth)
        {
            p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + drawParams.etchInnerRadius + colorLineWidth, r.yMax));
            p2d.BezierCurveTo(
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax),
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax),
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax + drawParams.etchInnerRadius));

            p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax + drawParams.etchHeight - drawParams.etchOuterRadius));
            p2d.BezierCurveTo(
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax + drawParams.etchHeight),
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth + colorLineWidth, r.yMax + drawParams.etchHeight),
                new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.bottomEtchWidth - drawParams.etchOuterRadius + colorLineWidth, r.yMax + drawParams.etchHeight));

            p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin + drawParams.etchOuterRadius - colorLineWidth, r.yMax + drawParams.etchHeight));
            p2d.BezierCurveTo(
                new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax + drawParams.etchHeight),
                new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax + drawParams.etchHeight),
                new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax + drawParams.etchHeight - drawParams.etchOuterRadius));
            p2d.LineTo(new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax + drawParams.etchInnerRadius));
            p2d.BezierCurveTo(
                new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax),
                new Vector2(r.xMin + drawParams.bottomEtchMargin - colorLineWidth - borderWidth, r.yMax),
                new Vector2(r.xMin + drawParams.bottomEtchMargin - drawParams.etchInnerRadius - colorLineWidth - borderWidth, r.yMax));
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

        bool SetupBlockBoundsAndDrawParams(ref Rect bounds, ref BlockDrawParams actualParams)
        {
            bounds.position = Vector2.zero;
            bounds.height -= resolvedStyle.paddingBottom;
            if (!float.IsFinite(bounds.width) || !float.IsFinite(bounds.height))
                return false;

            Inset(ref bounds, Vector2.one * (m_Border * 0.5f));

            actualParams.topEtchMargin -= m_Border;
            actualParams.topEtchWidth += m_Border;
            actualParams.bottomEtchMargin -= m_Border;
            actualParams.bottomEtchWidth += m_Border;

            return true;
        }
    }
}
