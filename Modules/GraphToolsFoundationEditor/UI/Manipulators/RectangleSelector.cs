// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manipulator to select elements by drawing a rectangle around them.
    /// </summary>
    class RectangleSelector : MouseManipulator
    {
        static readonly List<ModelView> k_OnMouseUpAllUIs = new List<ModelView>();

        readonly RectangleSelect m_Rectangle;
        bool m_Active;

        GraphViewPanHelper_Internal m_PanHelper = new GraphViewPanHelper_Internal();

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleSelector"/> class.
        /// </summary>
        public RectangleSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
            m_Rectangle = new RectangleSelect();
            m_Rectangle.style.position = Position.Absolute;
            m_Rectangle.style.top = 0f;
            m_Rectangle.style.left = 0f;
            m_Rectangle.style.bottom = 0f;
            m_Rectangle.style.right = 0f;
            m_Active = false;
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                m_Rectangle.RemoveFromHierarchy();
                m_Active = false;
            }
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (e.target != target)
            {
                return;
            }

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (CanStartManipulation(e))
            {
                graphView.StartMergingUndoableCommands();
                if (!e.actionKey)
                {
                    graphView.Dispatch(new ClearSelectionCommand());
                }

                graphView.ContentViewContainer.Add(m_Rectangle);

                m_Rectangle.Start = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition);
                m_Rectangle.End = m_Rectangle.Start;

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
                m_PanHelper.OnMouseDown(e, graphView, Pan);
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (!CanStopManipulation(e))
                return;

            graphView.ContentViewContainer.Remove(m_Rectangle);

            m_Rectangle.End = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition);

            var selectionRect = new Rect()
            {
                min = new Vector2(Math.Min(m_Rectangle.Start.x, m_Rectangle.End.x), Math.Min(m_Rectangle.Start.y, m_Rectangle.End.y)),
                max = new Vector2(Math.Max(m_Rectangle.Start.x, m_Rectangle.End.x), Math.Max(m_Rectangle.Start.y, m_Rectangle.End.y))
            };

            SelectElementsInRegion(selectionRect, e.actionKey ? SelectElementsCommand.SelectionMode.Toggle : SelectElementsCommand.SelectionMode.Replace, graphView);
            graphView.StopMergingUndoableCommands();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
            m_PanHelper.OnMouseUp(e);
        }

        /// <summary>
        /// Performs the selection of all elements inside a specific region.
        /// </summary>
        /// <param name="selectionRegion">The region where to look for the elements.</param>
        /// <param name="selectionMode">The selection mode.</param>
        /// <param name="graphView">The <see cref="GraphView"/> in which to search for the elements.</param>
        protected static void SelectElementsInRegion(Rect selectionRegion, SelectElementsCommand.SelectionMode selectionMode, GraphView graphView)
        {
            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ModelView> newSelection = new List<ModelView>();
            graphView.GraphModel?.GraphElementModels
                .Where(ge => ge.IsSelectable())
                .GetAllViewsInList_Internal(graphView, null, k_OnMouseUpAllUIs);
            foreach (var child in k_OnMouseUpAllUIs)
            {
                var localSelRect = graphView.ContentViewContainer.ChangeCoordinatesTo(child, selectionRegion);
                if (child.Overlaps(localSelRect))
                {
                    newSelection.Add(child);
                }
            }
            k_OnMouseUpAllUIs.Clear();

            var allSelectedModels = newSelection
                .Select(elem => elem.Model)
                .OfType<GraphElementModel>()
                .ToList();
            bool onlyWiresSelected = allSelectedModels.All(m => m is WireModel);
            var selectedNodes = allSelectedModels
                .OfType<PortNodeModel>()
                .ToHashSet();

            foreach (var node in selectedNodes.ToList()) // need to copy the list as it will be changed while iterating.
                RecurseAddGraphContainerChildren(node, selectedNodes);

            bool PortIsSelected(PortModel p) => p != null && selectedNodes.Contains(p.NodeModel);

            // don't select wires unless they link selected models or if only wires are selected
            var modelsToSelect = onlyWiresSelected ?
                allSelectedModels :
                allSelectedModels
                    .Where(m => m is not WireModel wire
                        || PortIsSelected(wire.FromPort) && PortIsSelected(wire.ToPort))
                    .ToList();

            graphView.Dispatch(new SelectElementsCommand(selectionMode, modelsToSelect));
        }

        static void RecurseAddGraphContainerChildren(PortNodeModel node, HashSet<PortNodeModel> nodeModels)
        {
            if (node is IGraphElementContainer container)
            {
                foreach (var child in container.GraphElementModels.OfType<PortNodeModel>())
                {
                    nodeModels.Add(child);
                    RecurseAddGraphContainerChildren(child, nodeModels);
                }
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            if (target is not GraphView graphView)
                return;

            m_Rectangle.End = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition);
            e.StopPropagation();
            m_PanHelper.OnMouseMove(e);
        }

        public void MarkDirtyRepaint()
        {
            m_Rectangle?.MarkDirtyRepaint();
        }

        void Pan(TimerState timerState)
        {
            if (target is not GraphView graphView)
                return;

            m_Rectangle.End = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, m_PanHelper.LastLocalMousePosition);
        }

        class RectangleSelect : VisualElement
        {
            static readonly CustomStyleProperty<Color> k_FillColorProperty = new CustomStyleProperty<Color>("--fill-color");
            static readonly CustomStyleProperty<Color> k_BorderColorProperty = new CustomStyleProperty<Color>("--border-color");

            Vector2 m_End;
            Vector2 m_Start;

            static Color DefaultFillColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.11f);
                    }

                    return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.32f);
                }
            }

            static Color DefaultBorderColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.38f);
                    }

                    return new Color(255 / 255f, 255 / 255f, 255 / 255f, 0.67f);
                }
            }

            Color FillColor { get; set; } = DefaultFillColor;
            Color BorderColor { get; set; } = DefaultBorderColor;

            public Vector2 Start
            {
                get => m_Start;
                set
                {
                    m_Start = value;
                    MarkDirtyRepaint();
                }
            }

            public Vector2 End
            {
                get => m_End;
                set
                {
                    m_End = value;
                    MarkDirtyRepaint();
                }
            }

            public RectangleSelect()
            {
                generateVisualContent += OnGenerateVisualContent;
                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                ICustomStyle styles = e.customStyle;

                if (styles.TryGetValue(k_BorderColorProperty, out var borderColor))
                    BorderColor = borderColor;

                if (styles.TryGetValue(k_FillColorProperty, out var fillColor))
                    FillColor = fillColor;
            }

            void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                // Avoid drawing useless information
                if (Start == End)
                    return;

                var r = new Rect
                {
                    min = new Vector2(Math.Min(Start.x, End.x), Math.Min(Start.y, End.y)),
                    max = new Vector2(Math.Max(Start.x, End.x), Math.Max(Start.y, End.y))
                };

                var width = 1f / parent.transform.scale.x;

                MeshDrawingHelpers_Internal.SolidRectangle(mgc, r, FillColor, ContextType.Editor);
                MeshDrawingHelpers_Internal.Border(mgc, r, BorderColor, width, ContextType.Editor);
            }
        }
    }
}
