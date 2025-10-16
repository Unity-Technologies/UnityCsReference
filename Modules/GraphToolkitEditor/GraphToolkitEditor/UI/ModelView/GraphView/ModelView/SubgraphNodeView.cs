// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for a <see cref="SubgraphNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class SubgraphNodeView : CollapsibleInOutNodeView
    {
        /// <summary>
        /// The USS class name added to a <see cref="SubgraphNodeView"/>.
        /// </summary>
        public static readonly string subgraphNodeUssClassName = "ge-subgraph-node";

        /// <summary>
        /// The USS class name added to the tab element on the subgraph node.
        /// </summary>
        public static readonly string tabUssClassName = subgraphNodeUssClassName.WithUssElement("tab");

        /// <summary>
        /// The USS class name added to the element that hides the bottom border of the tab on the subgraph node.
        /// </summary>
        public static readonly string hideTabUssClassName = subgraphNodeUssClassName.WithUssElement("hide-tab-bottom");

        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new CustomStyleProperty<Color>("--border-color");

        Color m_TabBorderColor;
        Color m_TabColor;
        Color m_DefaultTabColor;
        VisualElement m_TabElement;
        VisualElement m_HideTabBottomBorderElement;
        VisualElement m_TabDraggableAreaElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphNodeView"/> class.
        /// </summary>
        public SubgraphNodeView()
        {
            var clickable = new Clickable(OpenSubgraph);
            clickable.activators.Clear();
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);

            m_DefaultTabColor = new Color(107 / 255f, 204 / 255f, 134 / 255f, 1f);
            m_TabColor = m_DefaultTabColor;
        }

        /// <inheritdoc/>
        protected internal override int NodeTitleOptions => EditableTitlePart.Options.SetWidth | NodeTitlePart.Options.ShouldDisplayColor | NodeTitlePart.Options.HasIcon | EditableTitlePart.Options.UseEllipsis;

        /// <inheritdoc/>
        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.ReplacePart(titleIconContainerPartName, SubgraphNodeTitlePart.Create(titleIconContainerPartName, Model, this, ussClassName, NodeTitleOptions));
        }

        /// <summary>
        /// Loads the subgraph associated with this subgraph node.
        /// </summary>
        public virtual void OpenSubgraph()
        {
            if (Model is SubgraphNodeModel subgraphNodeModel && subgraphNodeModel.GetSubgraphModel() != null)
            {
                GraphView.Dispatch(new LoadGraphCommand(subgraphNodeModel.GetSubgraphModel(), LoadGraphCommand.LoadStrategies.PushOnStack, title: subgraphNodeModel.Title));
                if (GraphView.Window is GraphViewEditorWindow graphViewWindow)
                    graphViewWindow.UpdateWindowsWithSameCurrentGraph(false);
            }
        }

        /// <summary>
        /// Creates the <see cref="NodeToolbarButton"/> used to open the subgraph.
        /// </summary>
        /// <returns>The button to open the subgraph.</returns>
        protected NodeToolbarButton CreateOpenSubgraphButton() => new OpenGraphButton(OpenGraphButton.openGraphButtonName, OpenSubgraph) { tooltip = "Enter Subgraph" };

        /// <inheritdoc/>
        protected override void BuildNodeToolbarButtons()
        {
            AddNodeToolbarButton(CreateOpenSubgraphButton());
            base.BuildNodeToolbarButtons();
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            /* Subgraph tab: consists of the tab itself and a rectangle below the tab. */
            // Element that makes it possible to drag the node when clicking on the tab.
            m_TabDraggableAreaElement = new VisualElement { name = "tab-drag-area" };
            m_TabDraggableAreaElement.AddToClassList(subgraphNodeUssClassName.WithUssElement("tab-drag-area"));
            hierarchy.Insert(0, m_TabDraggableAreaElement);

            // Element that contain the drawing of the tab.
            m_TabElement = new VisualElement { name = tabUssClassName };
            m_TabElement.AddToClassList(tabUssClassName);
            m_TabElement.generateVisualContent = OnGenerateTabVisualContent;
            m_TabElement.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(k_BorderColorProperty, out var borderColorValue))
                    m_TabBorderColor = borderColorValue;
            });
            hierarchy.Add(m_TabElement);

            // Element to hide the bottom border of the tab.
            m_HideTabBottomBorderElement = new VisualElement { name = "hide-tab" };
            m_HideTabBottomBorderElement.AddToClassList(hideTabUssClassName);
            m_TabElement.Add(m_HideTabBottomBorderElement);
        }

        /// <inheritdoc/>
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (Model is SubgraphNodeModel subgraphNodeModel && subgraphNodeModel.IsColorable() && visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                var color = subgraphNodeModel.ElementColor.Color;
                if (color != m_TabColor)
                {
                    m_TabColor = subgraphNodeModel.ElementColor.HasUserColor ? subgraphNodeModel.ElementColor.Color : m_DefaultTabColor;
                    m_TabElement.MarkDirtyRepaint();
                }
            }
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
            AddToClassList(subgraphNodeUssClassName);
            this.AddPackageStylesheet("SubgraphNode.uss");
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicSubgraphNodeBorder(this);
        }

        void OnGenerateTabVisualContent(MeshGenerationContext mgc)
        {
            const int distBeforeTab = 8;

            // Tab only
            var tabRect = new Rect(
                distBeforeTab,
                m_TabElement.layout.y,
                m_TabElement.layout.width * 0.25f,
                m_TabElement.layout.height * 0.5f);

            // Rectangle area below the tab
            var bottomRect = new Rect(
                .5f,
                m_TabElement.layout.center.y,
                m_TabElement.layout.width - 1f,
                tabRect.height);

            DrawSubgraphTab(mgc.painter2D, tabRect, bottomRect);
            mgc.painter2D.fillColor = m_TabColor;
            mgc.painter2D.strokeColor = m_TabBorderColor;
            mgc.painter2D.Fill();
            mgc.painter2D.Stroke();
        }

        internal static void DrawSubgraphTab(Painter2D p2d, Rect tabRect, Rect bottomRect, Vector2 brr = default, Vector2 blr = default)
        {
            const int radius = 3;
            const int tabSideLenght = 6;

            p2d.BeginPath();

            // Top left corner
            p2d.MoveTo(new Vector2(bottomRect.xMin, bottomRect.center.y));
            p2d.ArcTo(new Vector2(bottomRect.xMin, tabRect.yMax), new Vector2(bottomRect.xMin + radius * 0.5f, tabRect.yMax), radius);
            p2d.ArcTo(new Vector2(tabRect.xMin - radius * 0.5f, tabRect.yMax), new Vector2(tabRect.xMin + tabSideLenght, tabRect.yMin), radius);

            // Tab
            p2d.ArcTo(new Vector2(tabRect.xMin + tabSideLenght - radius * 0.5f, tabRect.yMin), new Vector2(tabRect.xMax - tabSideLenght, tabRect.yMin), radius);
            p2d.ArcTo(new Vector2(tabRect.xMax - tabSideLenght + radius * 0.5f, tabRect.yMin), new Vector2(tabRect.xMax, tabRect.yMax), radius);

            // Top right corner
            p2d.ArcTo(new Vector2(tabRect.xMax + radius * 0.5f, tabRect.yMax), new Vector2(bottomRect.xMax, tabRect.yMax), radius);
            p2d.ArcTo(new Vector2(bottomRect.xMax, tabRect.yMax), new Vector2(bottomRect.xMax, tabRect.yMax + radius * 0.5f), radius);

            // Bottom corners
            p2d.ArcTo(new Vector2(bottomRect.xMax, bottomRect.yMax), new Vector2(bottomRect.xMax - brr.x, bottomRect.yMax), brr.x);
            p2d.ArcTo(new Vector2(bottomRect.xMin, bottomRect.yMax), new Vector2(bottomRect.xMin, bottomRect.yMax - blr.y), blr.y);

            p2d.ClosePath();
        }
    }
}
