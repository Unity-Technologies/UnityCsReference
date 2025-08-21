// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // A BlocksGraphViewRender that includes support for mouse interactions.
    class BlocksGraphView : BlocksGraphViewRender
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new BlocksGraphView();
        }

        const string k_UssClass_HoverIndicator = "blocks-graph-view__hover-indicator";

        readonly VisualElement m_HoverIndicator;
        int? m_PointerDownUnit;

        public BlocksGraphView()
        {
            // Add a child element to provide hover feedback. This prevents redrawing the whole graph just to change the hover state.
            m_HoverIndicator = new VisualElement()
            {
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.DynamicTransform,
            };
            m_HoverIndicator.AddToClassList(k_UssClass_HoverIndicator);
            UIUtility.SetElementDisplay(m_HoverIndicator, false);
            Add(m_HoverIndicator);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        // The graph's responder.
        public IResponder Responder { private get; set; }

        void OnPointerDown(PointerDownEvent evt)
        {
            this.CapturePointer(evt.pointerId);
            var hoveredUnit = UnitAtPosition(evt.localPosition);
            m_PointerDownUnit = hoveredUnit;

            var selectionRange = new Range(hoveredUnit, hoveredUnit + 1);
            Responder?.GraphViewUpdatedPendingSelection(selectionRange);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            var hoveredUnit = UnitAtPosition(evt.localPosition);
            var left = hoveredUnit * UnitWidth;
            m_HoverIndicator.style.left = Mathf.RoundToInt(left);

            Responder?.GraphViewPointerHoverMoved(hoveredUnit, evt.position);

            if (m_PointerDownUnit.HasValue)
            {
                var selectionStartUnit = m_PointerDownUnit.Value;
                var selectionRange = SelectionRangeFromUnits(selectionStartUnit, hoveredUnit);
                Responder?.GraphViewUpdatedPendingSelection(selectionRange);
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (m_PointerDownUnit.HasValue)
            {
                var selectionStartUnit = m_PointerDownUnit.Value;
                var selectionEndUnit = UnitAtPosition(evt.localPosition);
                var selectionRange = SelectionRangeFromUnits(selectionStartUnit, selectionEndUnit);
                Responder?.GraphViewSelectedUnitRange(selectionRange);
            }

            m_PointerDownUnit = null;
            this.ReleasePointer(evt.pointerId);
        }

        Range SelectionRangeFromUnits(int startUnit, int endUnit)
        {
            // Always place lowest unit at the start of the range (if user drags
            // from right to left). One is added to the end because C# ranges have
            // an exclusive end index.
            Range selectionRange;
            if (startUnit <= endUnit)
                selectionRange = new Range(startUnit, endUnit + 1);
            else
                selectionRange = new Range(endUnit, startUnit + 1);

            return selectionRange;
        }

        void OnPointerOver(PointerOverEvent evt)
        {
            m_HoverIndicator.style.width = UnitWidth;
            UIUtility.SetElementDisplay(m_HoverIndicator, true);

            var hoveredUnit = UnitAtPosition(evt.localPosition);
            Responder?.GraphViewPointerHoverBegan(hoveredUnit, evt.position);
        }

        void OnPointerOut(PointerOutEvent evt)
        {
            UIUtility.SetElementDisplay(m_HoverIndicator, false);
            Responder?.GraphViewPointerHoverEnded();
        }

        int UnitAtPosition(Vector2 localPosition)
        {
            return Math.Max(Mathf.FloorToInt(localPosition.x / UnitWidth), 0);
        }

        public interface IResponder
        {
            void GraphViewUpdatedPendingSelection(Range unitRange);

            void GraphViewSelectedUnitRange(Range unitRange);

            void GraphViewPointerHoverBegan(int unit, Vector2 position);

            void GraphViewPointerHoverMoved(int unit, Vector2 position);

            void GraphViewPointerHoverEnded();
        }
    }
}
