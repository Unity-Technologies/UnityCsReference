// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    internal class FlowLinesDrawer
    {
        static readonly Vector2 k_InvalidPosition = Vector2.one * -1;

        readonly Vector3[] m_CachedSelectedLinePoints = new Vector3[3];
        readonly Color[] m_CachedSelectedLineColors = new Color[]
        {
            Styles.selectedColor,
            Styles.selectedColor,
            Styles.selectedColor
        };
        Vector2 m_BeginEventPosition = k_InvalidPosition;
        Dictionary<int, Vector2> m_LatestNextEventPositionsPerThread = new Dictionary<int, Vector2>();
        Dictionary<uint, Vector2> m_LatestNextEventPositionsPerFlow = new Dictionary<uint, Vector2>();
        List<Vector2> m_EndEventPositions = new List<Vector2>();
        Dictionary<uint, int> m_LatestEndEventPositionIndexesPerFlow = new Dictionary<uint, int>();
        SelectedEventData m_SelectedEvent;

        public bool hasSelectedEventPosition
        {
            get
            {
                return m_SelectedEvent != null;
            }
        }

        bool hasBeginEventPosition
        {
            get
            {
                return (m_BeginEventPosition != k_InvalidPosition);
            }
        }

        bool hasAtLeastOneNextEventPosition
        {
            get
            {
                return (m_LatestNextEventPositionsPerThread.Count > 0);
            }
        }

        bool hasEndEventPosition
        {
            get
            {
                return (m_EndEventPositions.Count > 0);
            }
        }

        bool canDraw
        {
            get
            {
                return (hasBeginEventPosition && hasAtLeastOneNextEventPosition);
            }
        }

        public void AddFlowEvent(ProfilerTimelineGUI.ThreadInfo.FlowEventData flowEventData, int threadIndex, Rect sampleRect, bool isSelectedSample)
        {
            var flowEvent = flowEventData.flowEvent;
            var flowEventType = flowEvent.FlowEventType;
            switch (flowEventType)
            {
                case ProfilerFlowEventType.Begin:
                    if (!hasBeginEventPosition)
                    {
                        m_BeginEventPosition = new Vector2(sampleRect.xMin, sampleRect.yMax);
                    }
                    break;

                case ProfilerFlowEventType.Next:
                    ProcessSampleRectForNextEvent(sampleRect, threadIndex, flowEvent.FlowId);
                    break;

                case ProfilerFlowEventType.End:
                    ProcessSampleRectForEndEvent(sampleRect, flowEvent.FlowId);
                    break;

                default:
                    break;
            }

            // Only next and end events show a selected line when selected.
            if (flowEventType != ProfilerFlowEventType.Begin)
            {
                if (isSelectedSample && !hasSelectedEventPosition)
                {
                    Vector2 selectedFlowEventPosition = k_InvalidPosition;
                    switch (flowEventType)
                    {
                        case ProfilerFlowEventType.Next:
                            selectedFlowEventPosition = new Vector2(sampleRect.xMin, sampleRect.yMax);
                            break;

                        case ProfilerFlowEventType.End:
                            selectedFlowEventPosition = new Vector2(sampleRect.xMax, sampleRect.yMax);
                            break;

                        default:
                            break;
                    }

                    m_SelectedEvent = new SelectedEventData()
                    {
                        flowEventPosition = selectedFlowEventPosition,
                        flowEventType = flowEventType,
                        flowId = flowEventData.flowEvent.FlowId,
                    };
                }
            }
        }

        public void Draw()
        {
            if (!canDraw)
            {
                return;
            }

            Handles.BeginGUI();

            // Offset the line's origin to the arrow tip of the begin indicator.
            var beginIndicatorSize = FlowIndicatorDrawer.textureVisualSize;
            var verticalLineOrigin = new Vector2(m_BeginEventPosition.x + (beginIndicatorSize.x * 0.5f), m_BeginEventPosition.y + beginIndicatorSize.y);
            DrawBeginToNextFlowLines(verticalLineOrigin);
            DrawNextToEndFlowLines();
            DrawSelectedLineIfNecessary(verticalLineOrigin);

            Handles.EndGUI();
        }

        void ProcessSampleRectForNextEvent(Rect sampleRect, int threadIndex, uint flowEventId)
        {
            // Store the position of the latest next event per thread. We use this to draw a single horizontal line to the latest next event. We can use the bottom-left position (where we draw to) as samples won't overlap on a single thread.
            var bottomLeftSampleRectPosition = new Vector2(sampleRect.xMin, sampleRect.yMax);
            Vector2 latestNextEventPositionForThread;
            if (m_LatestNextEventPositionsPerThread.TryGetValue(threadIndex, out latestNextEventPositionForThread))
            {
                if (bottomLeftSampleRectPosition.x > latestNextEventPositionForThread.x)
                {
                    m_LatestNextEventPositionsPerThread[threadIndex] = bottomLeftSampleRectPosition;
                }
            }
            else
            {
                m_LatestNextEventPositionsPerThread[threadIndex] = bottomLeftSampleRectPosition;
            }

            // Store the position of the latest next event per flow. We use this when drawing each flow's end line.
            var bottomRightSampleRectPosition = new Vector2(sampleRect.xMax, sampleRect.yMax);
            Vector2 latestNextEventPositionForFlow;
            if (m_LatestNextEventPositionsPerFlow.TryGetValue(flowEventId, out latestNextEventPositionForFlow))
            {
                if (bottomRightSampleRectPosition.x > latestNextEventPositionForFlow.x)
                {
                    m_LatestNextEventPositionsPerFlow[flowEventId] = bottomRightSampleRectPosition;
                }
            }
            else
            {
                m_LatestNextEventPositionsPerFlow[flowEventId] = bottomRightSampleRectPosition;
            }
        }

        void ProcessSampleRectForEndEvent(Rect sampleRect, uint flowEventId)
        {
            var maxSampleRectPosition = new Vector2(sampleRect.xMax, sampleRect.yMax);
            m_EndEventPositions.Add(maxSampleRectPosition);

            // Store the position of the latest end event per flow. We use this when drawing each flow's end line.
            int latestEndEventPositionIndexForFlow;
            if (m_LatestEndEventPositionIndexesPerFlow.TryGetValue(flowEventId, out latestEndEventPositionIndexForFlow))
            {
                var latestEndEventPositionForFlow = m_EndEventPositions[latestEndEventPositionIndexForFlow];
                if (maxSampleRectPosition.x > latestEndEventPositionForFlow.x)
                {
                    int currentIndex = m_EndEventPositions.Count - 1;
                    m_LatestEndEventPositionIndexesPerFlow[flowEventId] = currentIndex;
                }
            }
            else
            {
                int currentIndex = m_EndEventPositions.Count - 1;
                m_LatestEndEventPositionIndexesPerFlow[flowEventId] = currentIndex;
            }
        }

        void DrawBeginToNextFlowLines(Vector2 verticalLineOrigin)
        {
            var lowestVerticalSamplePosition = m_BeginEventPosition.y;
            var highestVerticalSamplePosition = m_BeginEventPosition.y;

            var horizontalLinePositions = CollectHorizontalBeginToNextFlowLinePositionsAndTrackVerticalRange(verticalLineOrigin.x, ref lowestVerticalSamplePosition, ref highestVerticalSamplePosition);
            DrawLines(horizontalLinePositions);

            DrawVerticalBeginToNextFlowLines(verticalLineOrigin, lowestVerticalSamplePosition, highestVerticalSamplePosition);
        }

        Vector3[] CollectHorizontalBeginToNextFlowLinePositionsAndTrackVerticalRange(float horizontalOrigin, ref float lowestVerticalSamplePosition, ref float highestVerticalSamplePosition)
        {
            const int pointsPerLine = 2;
            Vector3[] linePositions = new Vector3[m_LatestNextEventPositionsPerThread.Count * pointsPerLine];

            int index = 0;
            foreach (KeyValuePair<int, Vector2> kvp in m_LatestNextEventPositionsPerThread)
            {
                var highestHorizontalNextEventSamplePosition = kvp.Value;
                var origin = new Vector2(horizontalOrigin, highestHorizontalNextEventSamplePosition.y);
                linePositions[index] = origin;
                linePositions[index + 1] = highestHorizontalNextEventSamplePosition;

                if (origin.y < lowestVerticalSamplePosition)
                {
                    lowestVerticalSamplePosition = origin.y;
                }
                else if (origin.y > highestVerticalSamplePosition)
                {
                    highestVerticalSamplePosition = origin.y;
                }

                index += pointsPerLine;
            }

            return linePositions;
        }

        void DrawVerticalBeginToNextFlowLines(Vector2 origin, float lowestVerticalSamplePosition, float highestVerticalSamplePosition)
        {
            if (lowestVerticalSamplePosition < m_BeginEventPosition.y)
            {
                var destination = new Vector2(origin.x, lowestVerticalSamplePosition);
                DrawLine(origin, destination);
            }

            if (highestVerticalSamplePosition > m_BeginEventPosition.y)
            {
                var destination = new Vector2(origin.x, highestVerticalSamplePosition);
                DrawLine(origin, destination);
            }
        }

        void DrawNextToEndFlowLines()
        {
            if (!hasEndEventPosition)
            {
                return;
            }

            Vector3[] linePositions = CollectLatestNextEventToLatestEndEventLinePositionsPerFlow();
            DrawLines(linePositions);
        }

        Vector3[] CollectLatestNextEventToLatestEndEventLinePositionsPerFlow()
        {
            const int k_NumberOfPointsPerEndEvent = 4;
            Vector3[] linePositions = new Vector3[m_LatestEndEventPositionIndexesPerFlow.Count * k_NumberOfPointsPerEndEvent];
            int index = 0;
            foreach (KeyValuePair<uint, int> latestEndEventPositionIndexKvp in m_LatestEndEventPositionIndexesPerFlow)
            {
                var flowId = latestEndEventPositionIndexKvp.Key;
                var latestEndEventPositionIndex = latestEndEventPositionIndexKvp.Value;

                Vector2 latestNextEventPositionForFlow;
                if (m_LatestNextEventPositionsPerFlow.TryGetValue(flowId, out latestNextEventPositionForFlow))
                {
                    var latestEndEventPositionForFlow = m_EndEventPositions[latestEndEventPositionIndex];
                    var horizontalOrigin = latestNextEventPositionForFlow;
                    linePositions[index] = horizontalOrigin;
                    var horizontalDestination = new Vector2(latestEndEventPositionForFlow.x, latestNextEventPositionForFlow.y);
                    linePositions[index + 1] = horizontalDestination;
                    var verticalOrigin = new Vector2(latestEndEventPositionForFlow.x, latestNextEventPositionForFlow.y);
                    linePositions[index + 2] = verticalOrigin;
                    var verticalDestination = latestEndEventPositionForFlow;
                    linePositions[index + 3] = verticalDestination;
                }

                index += k_NumberOfPointsPerEndEvent;
            }

            return linePositions;
        }

        void DrawLines(Vector3[] points)
        {
            var color = Handles.color;
            Handles.color = Styles.color;
            Handles.DrawLines(points);
            Handles.color = color;
        }

        void DrawLine(Vector2 origin, Vector2 destination)
        {
            var color = Handles.color;
            Handles.color = Styles.color;
            Handles.DrawLine(origin, destination);
            Handles.color = color;
        }

        void DrawSelectedLineIfNecessary(Vector2 beginEventLineOrigin)
        {
            if (hasSelectedEventPosition)
            {
                Vector2 selectedLineOrigin = k_InvalidPosition;
                switch (m_SelectedEvent.flowEventType)
                {
                    case ProfilerFlowEventType.Next:
                    {
                        // If a next event is selected, draw from the begin event to the selected event.
                        selectedLineOrigin = beginEventLineOrigin;
                        break;
                    }

                    case ProfilerFlowEventType.End:
                    {
                        // If an end event is selected, draw from the last next event for this flow to the selected event.
                        var flowId = m_SelectedEvent.flowId;
                        Vector2 latestNextEventPositionForFlow;
                        if (m_LatestNextEventPositionsPerFlow.TryGetValue(flowId, out latestNextEventPositionForFlow))
                        {
                            selectedLineOrigin = latestNextEventPositionForFlow;
                        }

                        break;
                    }
                }

                if (selectedLineOrigin != k_InvalidPosition)
                {
                    DrawSelectedLine(selectedLineOrigin);
                }
            }
        }

        void DrawSelectedLine(Vector2 origin)
        {
            Vector2 destination = m_SelectedEvent.flowEventPosition;

            Vector2 midPoint = k_InvalidPosition;
            switch (m_SelectedEvent.flowEventType)
            {
                case ProfilerFlowEventType.Next:
                {
                    midPoint = new Vector2(origin.x, destination.y);
                    break;
                }

                case ProfilerFlowEventType.End:
                {
                    midPoint = new Vector2(destination.x, origin.y);
                    break;
                }
            }

            if (midPoint != k_InvalidPosition)
            {
                m_CachedSelectedLinePoints[0] = origin;
                m_CachedSelectedLinePoints[1] = midPoint;
                m_CachedSelectedLinePoints[2] = destination;
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, Styles.selectedWidth, m_CachedSelectedLinePoints);
            }
        }

        static class Styles
        {
            public static readonly Color color = new Color(220f, 220f, 220f, 1f);
            public static readonly Color selectedColor = new Color(255f, 255f, 255f);
            public static readonly float selectedWidth = 2f;
        }

        class SelectedEventData
        {
            public Vector2 flowEventPosition { get; set; }
            public ProfilerFlowEventType flowEventType { get; set; }
            public uint flowId { get; set; }
        }
    }
}
