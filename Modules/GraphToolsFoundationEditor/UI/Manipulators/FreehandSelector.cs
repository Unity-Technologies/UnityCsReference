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
    /// Manipulator to select elements by drawing a lasso around them.
    /// </summary>
    class FreehandSelector : MouseManipulator
    {
        static readonly List<GraphElement> k_OnMouseUpAllUIs = new();

        readonly FreehandElement m_FreehandElement;
        bool m_Active;
        GraphView m_GraphView;

        GraphViewPanHelper_Internal m_PanHelper = new GraphViewPanHelper_Internal();

        /// <summary>
        /// Initializes a new instance of the <see cref="FreehandSelector"/> class.
        /// </summary>
        public FreehandSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift | EventModifiers.Alt });
            m_FreehandElement = new FreehandElement();
            m_FreehandElement.StretchToParentSize();
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            m_GraphView = target as GraphView;
            if (m_GraphView == null)
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);

            m_GraphView = null;
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
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

            if (CanStartManipulation(e))
            {
                m_GraphView.StartMergingUndoableCommands();
                m_GraphView.Dispatch(new ClearSelectionCommand());

                m_GraphView.ContentViewContainer.Add(m_FreehandElement);

                m_FreehandElement.Points.Clear();
                m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));
                m_FreehandElement.DeleteModifier = e.altKey;

                m_Active = true;
                target.CaptureMouse();
                e.StopImmediatePropagation();
                m_PanHelper.OnMouseDown(e, m_GraphView, Pan);
            }
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            m_GraphView.ContentViewContainer.Remove(m_FreehandElement);

            m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));

            SelectElementsUnderLasso(m_FreehandElement.Points, e.altKey, m_GraphView);
            m_GraphView.StopMergingUndoableCommands();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
            m_PanHelper.OnMouseUp(e);
        }

        /// <summary>
        /// Selects or deletes all elements found under the lasso points.
        /// </summary>
        /// <param name="lassoPoints">Points forming the lasso.</param>
        /// <param name="deleteElements">True to delete elements, otherwise select them.</param>
        /// <param name="graphView">The <see cref="GraphView"/> in which to search for the elements.</param>
        protected static void SelectElementsUnderLasso(List<Vector2> lassoPoints, bool deleteElements, GraphView graphView)
        {
            List<ChildView> newSelection = new List<ChildView>();
            for (var i = 1; i < lassoPoints.Count; i++)
            {
                // Apply offset
                Vector2 start = lassoPoints[i - 1];
                Vector2 end = lassoPoints[i];
                float minx = Mathf.Min(start.x, end.x);
                float maxx = Mathf.Max(start.x, end.x);
                float miny = Mathf.Min(start.y, end.y);
                float maxy = Mathf.Max(start.y, end.y);

                var rect = new Rect(minx, miny, maxx - minx + 1, maxy - miny + 1);
                k_OnMouseUpAllUIs.Clear();
                graphView.GetGraphElementsInRegion(rect, k_OnMouseUpAllUIs);
                foreach (var graphElement in k_OnMouseUpAllUIs)
                {
                    if (graphElement == null || !graphElement.GraphElementModel.IsSelectable())
                        continue;
                    newSelection.Add(graphElement);
                }
                k_OnMouseUpAllUIs.Clear();
            }

            var selectedModels = newSelection.OfType<ModelView>().Select(elem => elem.Model).OfType<GraphElementModel>().ToList();

            if (deleteElements)
            {
                graphView.Dispatch(new DeleteElementsCommand(selectedModels));
            }
            else
            {
                graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, selectedModels));
            }
        }

        /// <summary>
        /// Callback for the MouseMove event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));
            m_FreehandElement.DeleteModifier = e.altKey;
            m_FreehandElement.MarkDirtyRepaint();

            e.StopPropagation();
            m_PanHelper.OnMouseMove(e);
        }

        /// <summary>
        /// Callback for the KeyDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyDown(KeyDownEvent e)
        {
            if (m_Active)
                m_FreehandElement.DeleteModifier = e.altKey;
        }

        /// <summary>
        /// Callback for the KeyUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyUp(KeyUpEvent e)
        {
            if (m_Active)
                m_FreehandElement.DeleteModifier = e.altKey;
        }

        public void MarkDirtyRepaint()
        {
            m_FreehandElement?.MarkDirtyRepaint();
        }

        void Pan(TimerState obj)
        {
            m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, m_PanHelper.LastLocalMousePosition));
            MarkDirtyRepaint();
        }

        class FreehandElement : VisualElement
        {
            public List<Vector2> Points { get; } = new List<Vector2>();

            public FreehandElement()
            {
                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                generateVisualContent += GenerateVisualContent;
            }

            bool m_DeleteModifier;
            public bool DeleteModifier
            {
                private get { return m_DeleteModifier; }
                set
                {
                    if (m_DeleteModifier == value)
                        return;
                    m_DeleteModifier = value;
                    MarkDirtyRepaint();
                }
            }

            static readonly CustomStyleProperty<float> k_SegmentSizeProperty = new CustomStyleProperty<float>("--segment-size");
            static readonly CustomStyleProperty<Color> k_SegmentColorProperty = new CustomStyleProperty<Color>("--segment-color");
            static readonly CustomStyleProperty<Color> k_DeleteSegmentColorProperty = new CustomStyleProperty<Color>("--delete-segment-color");

            static float DefaultSegmentSize => 5f;
            static Color DefaultSegmentColor
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

            static Color DefaultDeleteSegmentColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(1f, 0f, 0f);
                    }

                    return new Color(1f, 0f, 0f);
                }
            }

            public float SegmentSize { get; private set; } = DefaultSegmentSize;

            Color SegmentColor { get; set; } = DefaultSegmentColor;

            Color DeleteSegmentColor { get; set; } = DefaultDeleteSegmentColor;

            void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                ICustomStyle styles = e.customStyle;
                Color segmentColorValue;
                Color deleteColorValue;

                if (styles.TryGetValue(k_SegmentSizeProperty, out var segmentSizeValue))
                    SegmentSize = segmentSizeValue;

                if (styles.TryGetValue(k_SegmentColorProperty, out segmentColorValue))
                    SegmentColor = segmentColorValue;

                if (styles.TryGetValue(k_DeleteSegmentColorProperty, out deleteColorValue))
                    DeleteSegmentColor = deleteColorValue;
            }

            void GenerateVisualContent(MeshGenerationContext mgc)
            {
                if (Points.Count < 2)
                    return;

                var painter = mgc.painter2D;
                painter.strokeColor = DeleteModifier ? DeleteSegmentColor : SegmentColor;
                painter.lineWidth = 1.5f / parent.transform.scale.x;
                painter.BeginPath();

                foreach (var point in Points)
                    painter.LineTo(point);

                painter.Stroke();
            }
        }
    }
}
