// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator to select elements by drawing a lasso around them.
    /// </summary>
    [UnityRestricted]
    internal class FreehandSelector : MouseManipulator
    {
        static readonly List<GraphElement> k_OnMouseUpAllUIs = new();

        readonly FreehandElement m_FreehandElement;
        bool m_Active;
        GraphView m_GraphView;

        HashSet<GraphElementModel> m_SelectedModels = new();

        GraphViewPanHelper m_PanHelper = new GraphViewPanHelper();

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
            target.RegisterCallback<KeyDownEvent>(OnKey);
            target.RegisterCallback<KeyUpEvent>(OnKey);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<KeyDownEvent>(OnKey);
            target.UnregisterCallback<KeyUpEvent>(OnKey);

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
                m_SelectedModels.Clear();
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

            SelectElementsUnderLasso(m_FreehandElement.Points, e.altKey);
            m_GraphView.StopMergingUndoableCommands();
            ResetSelection();

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
        protected void SelectElementsUnderLasso(List<Vector2> lassoPoints, bool deleteElements)
        {
            var selectedModels = GetElementsUnderLasso(lassoPoints, 0);

            if (deleteElements)
            {
                m_GraphView.Dispatch(new DeleteElementsCommand(selectedModels));
            }
            else
            {
                m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, selectedModels));
            }
        }

        List<GraphElementModel> GetElementsUnderLasso(List<Vector2> lassoPoints, int startIndex)
        {
            List<ChildView> newSelection = new List<ChildView>();
            for (var i = startIndex + 1; i < lassoPoints.Count; i++)
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
                m_GraphView.GetGraphElementsInRegion(rect, k_OnMouseUpAllUIs, GraphView.PartitioningMode.PlacematTitle);
                foreach (var graphElement in k_OnMouseUpAllUIs)
                {
                    if (graphElement == null || !graphElement.GraphElementModel.IsSelectable())
                        continue;
                    newSelection.Add(graphElement);
                }
                k_OnMouseUpAllUIs.Clear();
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedModels = newSelection.OfType<ModelView>().Select(elem => elem.Model).OfType<GraphElementModel>().ToList();
#pragma warning restore UA2001
            return selectedModels;
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

            var selectedModels = GetElementsUnderLasso(m_FreehandElement.Points, m_FreehandElement.Points.Count - 2);

            using var dispose = ListPool<ChildView>.Get(out var views);
            foreach (var selectedModel in selectedModels)
            {
                if (!m_SelectedModels.Contains(selectedModel))
                {
                    m_SelectedModels.Add(selectedModel);

                    selectedModel.AppendAllViews(m_GraphView, t => t is GraphElement, views);
                }
            }

            foreach (var view in views)
            {
                ((GraphElement)view).UpdateSelectionVisuals(true);
            }

            e.StopPropagation();
            m_PanHelper.OnMouseMove(e);
        }


        void ResetSelection()
        {
            using var dispose = ListPool<ChildView>.Get(out var views);
            foreach (var selectedModel in m_SelectedModels)
            {
                selectedModel.AppendAllViews(m_GraphView, t => t is GraphElement, views);
            }
            foreach (var view in views)
            {
                var ge = ((GraphElement)view);
                ge.UpdateSelectionVisuals(ge.IsSelected());
            }
            m_SelectedModels.Clear();
        }

        /// <summary>
        /// Callback for the KeyDown or KeyUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKey(IKeyboardEvent e)
        {
            if (m_Active)
            {
                if (m_FreehandElement.DeleteModifier != e.altKey)
                {
                    m_FreehandElement.DeleteModifier = e.altKey;
                    m_FreehandElement.MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Marks the freehand element as dirty for repaint.
        /// </summary>
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
                get { return m_DeleteModifier; }
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
                painter.lineWidth = 1.5f / parent.contentContainer.resolvedStyle.scale.value.x;

                painter.BeginPath();

                foreach (var point in Points)
                    painter.LineTo(point);

                painter.Stroke();
            }
        }
    }
}
