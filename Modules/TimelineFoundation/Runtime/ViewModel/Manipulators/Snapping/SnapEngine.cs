// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class SnapEngine
    {
        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal enum Location
        {
            None,
            Start,
            End,
            Both
        }

        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal readonly struct Result<T>
        {
            public readonly bool isSnapped;
            public readonly T value;
            public readonly Location location;

            public Result(bool isSnapped, T value, Location location = Location.None)
            {
                this.isSnapped = isSnapped;
                this.value = value;
                this.location = location;
            }
        }

        List<DiscreteTime> m_Edges;

        public IEnumerable<DiscreteTime> edges => m_Edges;

        public SnapEngine()
        {
            m_Edges = new List<DiscreteTime>();
        }

        public SnapEngine AddEdge(DiscreteTime edge)
        {
            int result = m_Edges.BinarySearch(edge, DiscreteTimeComparer.instance);
            if (~result == m_Edges.Count)
                m_Edges.Add(edge);
            else if (result < 0)
                m_Edges.Insert(~result, edge);
            return this;
        }

        public SnapEngine AddEdges(IEnumerable<DiscreteTime> newEdges)
        {
            var uniqueEdges = new HashSet<DiscreteTime>(newEdges, DiscreteTimeComparer.instance);
            m_Edges.AddRange(uniqueEdges);
            m_Edges.Sort(DiscreteTimeComparer.instance);
            return this;
        }

        public void RemoveAllEdges()
        {
            m_Edges.Clear();
        }

        public Result<DiscreteTime> FindEdge(DiscreteTime candidate, DiscreteTime attractionDuration)
        {
            (int startIndex, int endIndex) = GetValidEdgeIndexes(m_Edges, candidate, attractionDuration);
            if (startIndex >= m_Edges.Count || endIndex < 0) //invalid range
                return new Result<DiscreteTime>(false, candidate);

            var distance = DiscreteTime.MaxValue;
            var result = new Result<DiscreteTime>(false, candidate);

            for (int i = startIndex; i <= endIndex; i++)
            {
                //check if this edge is closest than the last one
                DiscreteTime newDistance = candidate - m_Edges[i];
                if (newDistance.Abs() < distance)
                {
                    distance = newDistance;
                    result = new Result<DiscreteTime>(true, m_Edges[i]);
                }
            }

            return result;
        }

        public Result<TimeRange> FindEdge(TimeRange candidate, DiscreteTime attractionDuration)
        {
            Result<DiscreteTime> startResult = FindEdge(candidate.start, attractionDuration);
            Result<DiscreteTime> endResult = FindEdge(candidate.end, attractionDuration);

            return MergeResults(candidate, startResult, endResult);
        }

        static (int, int) GetValidEdgeIndexes(List<DiscreteTime> edges, DiscreteTime candidate, DiscreteTime attractionDuration)
        {
            DiscreteTime lowerBound = candidate - attractionDuration;
            DiscreteTime higherBound = candidate + attractionDuration;

            int lowerRes = edges.BinarySearch(lowerBound, DiscreteTimeComparer.instance);
            if (lowerRes < 0) //not found
                lowerRes = ~lowerRes;

            int higherRes = edges.BinarySearch(higherBound, DiscreteTimeComparer.instance);
            if (higherRes < 0) //not found
                higherRes = ~higherRes - 1;

            return (lowerRes, higherRes);
        }

        static Result<TimeRange> MergeResults(TimeRange candidate, in Result<DiscreteTime> start, in Result<DiscreteTime> end)
        {
            var result = new Result<TimeRange>(false, candidate);

            if (start.isSnapped && end.isSnapped) // find closest edge
            {
                DiscreteTime leftDistance = (candidate.start - start.value).Abs();
                DiscreteTime rightDistance = (candidate.end - end.value).Abs();

                if (leftDistance == rightDistance)
                    result = BuildBothResult(candidate, start.value);
                else if (leftDistance < rightDistance) //start is closest
                    result = BuildStartResult(candidate, start.value);
                else //end is closest
                    result = BuildEndResult(candidate, end.value);
            }
            else if (start.isSnapped)
                result = BuildStartResult(candidate, start.value);
            else if (end.isSnapped)
                result = BuildEndResult(candidate, end.value);

            return result;
        }

        static Result<TimeRange> BuildStartResult(TimeRange candidate, DiscreteTime snappedTime)
        {
            var range = new TimeRange(snappedTime, snappedTime + candidate.duration);
            return new Result<TimeRange>(true, range, Location.Start);
        }

        static Result<TimeRange> BuildEndResult(TimeRange candidate, DiscreteTime snappedTime)
        {
            var range = new TimeRange(snappedTime - candidate.duration, snappedTime);
            return new Result<TimeRange>(true, range, Location.End);
        }

        static Result<TimeRange> BuildBothResult(TimeRange candidate, DiscreteTime snappedTime)
        {
            var range = new TimeRange(snappedTime, snappedTime + candidate.duration);
            return new Result<TimeRange>(true, range, Location.Both);
        }
    }
}
