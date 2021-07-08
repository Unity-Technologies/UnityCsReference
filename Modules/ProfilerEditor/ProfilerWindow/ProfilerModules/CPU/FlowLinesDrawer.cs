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
        const int k_EarlyNextEventArrowHorizontalPadding = 8;

        static readonly System.Comparison<FlowEventData> k_FlowEventStartTimeComparer = (FlowEventData x, FlowEventData y) =>
        {
            var xPos = x.rect.xMin;
            var yPos = y.rect.xMin;

            // Always sort the end event using the time it was completed.
            if (x.flowEventType == ProfilerFlowEventType.End)
            {
                xPos = x.rect.xMax;
            }
            else if (y.flowEventType == ProfilerFlowEventType.End)
            {
                yPos = y.rect.xMax;
            }

            return xPos.CompareTo(yPos);
        };

        static readonly System.Comparison<FlowEventData> k_FlowEventCompletionTimeComparer = (FlowEventData x, FlowEventData y) =>
        {
            var xPos = x.rect.xMax;
            var yPos = y.rect.xMax;

            // Always sort the begin event using the time it was started.
            if (x.flowEventType == ProfilerFlowEventType.Begin)
            {
                xPos = x.rect.xMin;
            }
            else if (y.flowEventType == ProfilerFlowEventType.Begin)
            {
                yPos = y.rect.xMin;
            }

            return xPos.CompareTo(yPos);
        };

        Dictionary<uint, List<FlowEventData>> m_Flows = new Dictionary<uint, List<FlowEventData>>();
        List<Vector3> m_CachedLinePoints = new List<Vector3>();
        HashSet<float> m_CachedParallelNextVerticalPositions = new HashSet<float>();
        List<Vector3> m_CachedSelectionLinePoints = new List<Vector3>();

        public bool hasSelectedEvent { get; private set; }

        public void AddFlowEvent(ProfilerTimelineGUI.ThreadInfo.FlowEventData flowEventData, Rect sampleRect, bool isSelectedSample)
        {
            var flowEvent = flowEventData.flowEvent;
            var flowEventId = flowEvent.FlowId;
            var flowEventType = flowEvent.FlowEventType;
            if (!m_Flows.TryGetValue(flowEventId, out var flowEvents))
            {
                flowEvents = new List<FlowEventData>();
                m_Flows[flowEventId] = flowEvents;
            }

            flowEvents.Add(new FlowEventData() {
                rect = sampleRect,
                flowEventType = flowEventType,
                isSelected = isSelectedSample
            });

            if (isSelectedSample)
            {
                hasSelectedEvent = true;
            }
        }

        public void Draw()
        {
            Handles.BeginGUI();

            foreach (var flowKvp in m_Flows)
            {
                var flowEvents = flowKvp.Value;
                DrawFlow(flowEvents);
            }

            Handles.EndGUI();
        }

        void DrawFlow(List<FlowEventData> flowEvents)
        {
            m_CachedLinePoints.Clear();
            m_CachedSelectionLinePoints.Clear();

            ProduceLinePointsForNextFlowEventsInCollection(flowEvents, ref m_CachedLinePoints, ref m_CachedSelectionLinePoints);
            ProduceLinePointsForParallelNextAndEndFlowEventsInCollection(flowEvents, ref m_CachedLinePoints, ref m_CachedSelectionLinePoints);

            DrawLines(m_CachedLinePoints.ToArray());
            DrawSelectedLineIfNecessary(m_CachedSelectionLinePoints.ToArray());
        }

        void ProduceLinePointsForNextFlowEventsInCollection(List<FlowEventData> flowEvents, ref List<Vector3> linePoints, ref List<Vector3> selectionLinePoints)
        {
            if (flowEvents.Count == 0)
            {
                return;
            }

            // We draw next event lines in start time order.
            SortFlowEventsInStartTimeOrder(flowEvents);

            for (int i = 0; i < flowEvents.Count; i++)
            {
                var flowEvent = flowEvents[i];
                var flowEventRect = flowEvent.rect;
                switch (flowEvent.flowEventType)
                {
                    case ProfilerFlowEventType.Next:
                    {
                        var previousFlowEventIndex = i - 1;
                        if (previousFlowEventIndex < 0)
                        {
                            continue;
                        }

                        var previousFlowEvent = flowEvents[previousFlowEventIndex];
                        var previousFlowEventRect = previousFlowEvent.rect;
                        var previousFlowEventRectXMax = previousFlowEventRect.xMax;
                        var flowEventRectXMin = flowEventRect.xMin;
                        var numberOfLinePointsAdded = 0;

                        // Draw a right angle line when the previous event is a Begin.
                        if (previousFlowEvent.flowEventType == ProfilerFlowEventType.Begin)
                        {
                            var halfBeginIndicatorSize = FlowIndicatorDrawer.textureVisualSize * 0.5f;
                            var origin = new Vector2(previousFlowEventRect.xMin + halfBeginIndicatorSize.x, previousFlowEventRect.yMax + halfBeginIndicatorSize.y);
                            var destination = new Vector2(flowEventRect.xMin, flowEventRect.yMax);
                            numberOfLinePointsAdded = AddRightAngleLineToPoints(origin, destination, false, ref linePoints);
                        }
                        else
                        {
                            // Verify if the previous *completed* event was a parallel next. This allows us to draw from the last parallel next that completed, rather than the last parallel next that started.
                            var hasPreviousCompletedFlowEvent = flowEvent.TryGetPreviousCompletedFlowEventInCollection(flowEvents, out var previousCompletedFlowEvent);
                            var previousCompletedFlowEventWasAParallelNext = (hasPreviousCompletedFlowEvent && previousCompletedFlowEvent.flowEventType == ProfilerFlowEventType.ParallelNext);
                            if (previousCompletedFlowEventWasAParallelNext)
                            {
                                // Draw a step-change style line from the previous completed flow event to this Next event.
                                var previousCompletedFlowEventRect = previousCompletedFlowEvent.rect;
                                var origin = previousCompletedFlowEventRect.max;
                                var destination = new Vector2(flowEventRect.xMin, flowEventRect.yMax);
                                numberOfLinePointsAdded = AddPointsForStepChangeFlowLineBetweenPointsToCollection(origin, destination, ref linePoints);
                            }
                            else
                            {
                                // If the previous event finished after this event started, draw from earlier in the previous event.
                                if (previousFlowEventRectXMax > flowEventRectXMin)
                                {
                                    var availableSpaceX = flowEventRectXMin - previousFlowEventRect.xMin;
                                    var offsetX = Mathf.Min(k_EarlyNextEventArrowHorizontalPadding, availableSpaceX * 0.5f);

                                    var originY = previousFlowEventRect.yMax;
                                    var destinationY = flowEventRect.yMax;
                                    if (destinationY < originY)
                                    {
                                        // Draw from the top of the marker if drawing upwards.
                                        originY = previousFlowEventRect.yMin;
                                    }

                                    var origin = new Vector2(flowEventRect.xMin - offsetX, originY);
                                    var destination = new Vector2(flowEventRect.xMin, flowEventRect.yMax);
                                    numberOfLinePointsAdded = AddRightAngleLineToPoints(origin, destination, false, ref linePoints);
                                }
                                else
                                {
                                    // Draw a step-change style line from the previous event to this Next event.
                                    var origin = previousFlowEventRect.max;
                                    var destination = new Vector2(flowEventRect.xMin, flowEventRect.yMax);
                                    numberOfLinePointsAdded = AddPointsForStepChangeFlowLineBetweenPointsToCollection(origin, destination, ref linePoints);
                                }
                            }
                        }

                        // If the next event is selected, add its line points to the selection line.
                        if (flowEvent.isSelected)
                        {
                            int newPointsStartIndex = linePoints.Count - numberOfLinePointsAdded;
                            ExtractSelectionLinePointsFromEndOfCollection(linePoints, newPointsStartIndex, ref m_CachedSelectionLinePoints);
                        }

                        break;
                    }
                }
            }
        }

        void ProduceLinePointsForParallelNextAndEndFlowEventsInCollection(List<FlowEventData> flowEvents, ref List<Vector3> linePoints, ref List<Vector3> selectionLinePoints)
        {
            if (flowEvents.Count == 0)
            {
                return;
            }

            // We draw parallel next and end event lines using completion time sorting. For example, the end event line should point to last *completed* event, not the last started. We also loop backwards so we only draw one horizontal line per thread when there are multiple parallel next events on one thread.
            SortFlowEventsInCompletionTimeOrder(flowEvents);

            var halfBeginIndicatorSize = FlowIndicatorDrawer.textureVisualSize * 0.5f;
            m_CachedParallelNextVerticalPositions.Clear();

            var hasParallelNextEvents = false;
            var firstFlowEvent = flowEvents[0];
            var firstFlowEventRect = firstFlowEvent.rect;
            var firstFlowEventPosition = new Vector2(firstFlowEventRect.xMin + halfBeginIndicatorSize.x, firstFlowEventRect.yMax + halfBeginIndicatorSize.y);
            var parallelNextVerticalMin = firstFlowEventPosition.y;
            var parallelNextVerticalMax = firstFlowEventPosition.y;
            for (int i = flowEvents.Count - 1; i >= 0; i--)
            {
                var flowEvent = flowEvents[i];
                var flowEventRect = flowEvent.rect;
                switch (flowEvent.flowEventType)
                {
                    case ProfilerFlowEventType.ParallelNext:
                    {
                        var flowEventRectYMax = flowEventRect.yMax;
                        var origin = new Vector2(firstFlowEventRect.xMin + halfBeginIndicatorSize.x, flowEventRectYMax);
                        var destination = new Vector2(flowEventRect.xMin, flowEventRectYMax);
                        if (!m_CachedParallelNextVerticalPositions.Contains(flowEventRectYMax)) // Only draw one horizontal line per thread.
                        {
                            linePoints.Add(origin);
                            linePoints.Add(destination);

                            m_CachedParallelNextVerticalPositions.Add(flowEventRectYMax);

                            hasParallelNextEvents = true;
                            if (flowEventRectYMax < parallelNextVerticalMin)
                            {
                                parallelNextVerticalMin = flowEventRectYMax;
                            }
                            else if (flowEventRectYMax > parallelNextVerticalMax)
                            {
                                parallelNextVerticalMax = flowEventRectYMax;
                            }
                        }

                        // If the parallel next event is selected, add its line points to the selection line.
                        if (flowEvent.isSelected)
                        {
                            m_CachedSelectionLinePoints.Add(firstFlowEventPosition);
                            m_CachedSelectionLinePoints.Add(new Vector2(firstFlowEventPosition.x, origin.y));
                            m_CachedSelectionLinePoints.Add(destination);
                        }

                        break;
                    }

                    case ProfilerFlowEventType.End:
                    {
                        // Draw a right-angled line from the previous event to the completion time of the end event.
                        var previousFlowEventIndex = i - 1;
                        if (previousFlowEventIndex < 0)
                        {
                            continue;
                        }

                        var previousFlowEvent = flowEvents[previousFlowEventIndex];
                        var previousFlowEventRect = previousFlowEvent.rect;
                        var previousFlowEventRectXMax = previousFlowEventRect.xMax;
                        var numberOfLinePointsAdded = 0;

                        var origin = previousFlowEventRect.max;
                        var destination = flowEventRect.max;
                        numberOfLinePointsAdded = AddRightAngleLineToPoints(origin, destination, true, ref linePoints);

                        // If the end event is selected, add its line points to the selection line.
                        if (flowEvent.isSelected)
                        {
                            int newPointsStartIndex = linePoints.Count - numberOfLinePointsAdded;
                            ExtractSelectionLinePointsFromEndOfCollection(linePoints, newPointsStartIndex, ref m_CachedSelectionLinePoints);
                        }

                        break;
                    }
                }
            }

            if (hasParallelNextEvents)
            {
                // Draw vertical lines to the highest and lowest parallel next events.
                if (!Mathf.Approximately(parallelNextVerticalMin, firstFlowEventPosition.y))
                {
                    linePoints.Add(firstFlowEventPosition);
                    linePoints.Add(new Vector2(firstFlowEventPosition.x, parallelNextVerticalMin));
                }

                if (!Mathf.Approximately(parallelNextVerticalMax, firstFlowEventPosition.y))
                {
                    linePoints.Add(firstFlowEventPosition);
                    linePoints.Add(new Vector2(firstFlowEventPosition.x, parallelNextVerticalMax));
                }
            }
        }

        void SortFlowEventsInStartTimeOrder(List<FlowEventData> flowEvents)
        {
            if (flowEvents.Count > 0)
            {
                flowEvents.Sort(k_FlowEventStartTimeComparer);
            }
        }

        void SortFlowEventsInCompletionTimeOrder(List<FlowEventData> flowEvents)
        {
            if (flowEvents.Count > 0)
            {
                flowEvents.Sort(k_FlowEventCompletionTimeComparer);
            }
        }

        int AddRightAngleLineToPoints(Vector2 origin, Vector2 destination, bool horizontalFirst, ref List<Vector3> linePoints)
        {
            int previousLinePointsCount = linePoints.Count;

            Vector2 destinationMidpoint = (horizontalFirst) ? new Vector2(destination.x, origin.y) : new Vector2(origin.x, destination.y);
            linePoints.Add(origin);
            linePoints.Add(destinationMidpoint);
            linePoints.Add(destinationMidpoint);
            linePoints.Add(destination);

            return linePoints.Count - previousLinePointsCount;
        }

        int AddPointsForStepChangeFlowLineBetweenPointsToCollection(Vector2 origin, Vector2 destination, ref List<Vector3> linePoints)
        {
            int previousLinePointsCount = linePoints.Count;

            if (!Mathf.Approximately(destination.y, origin.y))
            {
                var horizontalADestination = new Vector2(Mathf.Lerp(origin.x, destination.x, 0.5f), origin.y);
                linePoints.Add(origin);
                linePoints.Add(horizontalADestination);

                var verticalOrigin = horizontalADestination;
                var verticalDestination = new Vector2(horizontalADestination.x, destination.y);
                linePoints.Add(verticalOrigin);
                linePoints.Add(verticalDestination);

                linePoints.Add(verticalDestination);
                linePoints.Add(destination);
            }
            else
            {
                // If the destination is at the same height as the origin, just create a single line.
                linePoints.Add(origin);
                linePoints.Add(destination);
            }

            return linePoints.Count - previousLinePointsCount;
        }

        void ExtractSelectionLinePointsFromEndOfCollection(List<Vector3> linePoints, int pointsStartIndex, ref List<Vector3> selectionLinePoints)
        {
            // The selection line is drawn as one continuous line, as opposed to the flow lines which are many individual lines. Therefore, remove all duplicate overlapping points by taking each line's starting point.
            for (int j = pointsStartIndex; j < linePoints.Count; j += 2)
            {
                selectionLinePoints.Add(linePoints[j]);
            }

            // Add the last point, as there is no subsequent line beginning from here to capture this point.
            selectionLinePoints.Add(linePoints[linePoints.Count - 1]);
        }

        void DrawLines(Vector3[] points)
        {
            var color = Handles.color;
            Handles.color = Styles.color;
            Handles.DrawLines(points);
            Handles.color = color;
        }

        void DrawSelectedLineIfNecessary(Vector3[] selectedLinePositions)
        {
            if (m_CachedSelectionLinePoints.Count > 0)
            {
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, Styles.selectedWidth, selectedLinePositions);
            }
        }

        struct FlowEventData
        {
            public Rect rect;
            public ProfilerFlowEventType flowEventType;
            public bool isSelected;

            // Find the last flow event that completed prior to this flow event starting.
            public bool TryGetPreviousCompletedFlowEventInCollection(List<FlowEventData> collection, out FlowEventData previousCompletedFlowEvent)
            {
                var hasPreviousCompletedFlowEvent = false;
                previousCompletedFlowEvent = default;
                foreach (var flowEvent in collection)
                {
                    var flowEventRect = flowEvent.rect;

                    // Did it complete before we started?
                    if (flowEventRect.xMax < rect.xMin)
                    {
                        hasPreviousCompletedFlowEvent = true;
                        // Is it later than the previously stored value?
                        if (flowEventRect.xMax > previousCompletedFlowEvent.rect.xMax)
                        {
                            previousCompletedFlowEvent = flowEvent;
                        }
                    }
                }

                return hasPreviousCompletedFlowEvent;
            }
        }

        static class Styles
        {
            public static readonly Color color = new Color(220f, 220f, 220f, 1f);
            public static readonly Color selectedColor = new Color(255f, 255f, 255f);
            public static readonly float selectedWidth = 2f;
        }
    }
}
