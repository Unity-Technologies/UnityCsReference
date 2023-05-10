// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class BlocksGraphViewMeshBuilder
    {
        public const int NumberOfVerticesPerBlock = 4;
        public const int NumberOfIndicesPerBlock = 6;

        const float k_MinimumBlockWidth = 1f;
        const float k_HalfMinimumBlockWidth = k_MinimumBlockWidth * 0.5f;

        readonly float m_UnitWidth;
        readonly float m_HorizontalOffset;
        readonly float m_DataValueThreshold;

        public BlocksGraphViewMeshBuilder(
            float unitWidth,
            float horizontalOffset,
            float dataValueThreshold)
        {
            m_UnitWidth = unitWidth;
            m_HorizontalOffset = horizontalOffset;
            m_DataValueThreshold = dataValueThreshold;
        }

        public void BuildBlockMeshesForDataOverThresholdInDataSeries(
            NativeSlice<float> dataSeries,
            float blockYMin,
            float blockYMax,
            Color blockColor,
            NativeArray<Vertex> vertices,
            NativeArray<ushort> indices,
            ref int verticesIndex,
            ref int indicesIndex)
        {
            BuildBlockMeshesForDataSeries(
                Mode.ValueOverThreshold,
                dataSeries,
                blockYMin,
                blockYMax,
                blockColor,
                vertices,
                indices,
                ref verticesIndex,
                ref indicesIndex);
        }

        public void BuildBlockMeshesForInvalidDataInDataSeries(
            NativeSlice<float> dataSeries,
            float blockYMin,
            float blockYMax,
            Color blockColor,
            NativeArray<Vertex> vertices,
            NativeArray<ushort> indices,
            ref int verticesIndex,
            ref int indicesIndex)
        {
            BuildBlockMeshesForDataSeries(
                Mode.InvalidValue,
                dataSeries,
                blockYMin,
                blockYMax,
                blockColor,
                vertices,
                indices,
                ref verticesIndex,
                ref indicesIndex);
        }

        void BuildBlockMeshesForDataSeries(
            Mode mode,
            NativeSlice<float> dataSeries,
            float blockYMin,
            float blockYMax,
            Color blockColor,
            NativeArray<Vertex> vertices,
            NativeArray<ushort> indices,
            ref int verticesIndex,
            ref int indicesIndex)
        {
            // Iterate over the series's data points to find the ranges where they meet the condition. Draw
            // blocks for each of these ranges. If multiple consecutive data points meet the condition, continue
            // iterating to find the end of the range, and draw a single block mesh for the whole range.
            int? blockStartIndex = null;
            for (var j = 0; j < dataSeries.Length; j++)
            {
                var dataValue = dataSeries[j];

                bool condition;
                if (mode == Mode.ValueOverThreshold)
                {
                    condition = (dataValue > m_DataValueThreshold);
                }
                else
                {
                    // A value of -1 is what our existing counters API returns when there is no counter data in the selected frame.
                    // A value of 0 is what FTM will write when GPU misses the deadline and does not record a measurement.
                    condition = Mathf.Approximately(dataValue, -1f) || Mathf.Approximately(dataValue, 0f);
                }

                if (condition)
                {
                    if (!blockStartIndex.HasValue)
                        blockStartIndex = j;
                }
                else
                {
                    if (blockStartIndex.HasValue)
                    {
                        BuildBlockMesh(
                            blockStartIndex.Value,
                            j,
                            blockYMin,
                            blockYMax,
                            blockColor,
                            vertices,
                            indices,
                            ref verticesIndex,
                            ref indicesIndex);

                        blockStartIndex = null;
                    }
                }
            }

            // Complete the last block if it extends into the last data point (without placing branch check on every iteration).
            if (blockStartIndex.HasValue)
            {
                BuildBlockMesh(
                    blockStartIndex.Value,
                    dataSeries.Length,
                    blockYMin,
                    blockYMax,
                    blockColor,
                    vertices,
                    indices,
                    ref verticesIndex,
                    ref indicesIndex);

                blockStartIndex = null;
            }
        }

        void BuildBlockMesh(
            int blockStartIndex,
            int blockEndIndex,
            float blockYMin,
            float blockYMax,
            Color color,
            NativeArray<Vertex> vertices,
            NativeArray<ushort> indices,
            ref int verticesIndex,
            ref int indicesIndex)
        {
            var blockXMin = (blockStartIndex * m_UnitWidth) + m_HorizontalOffset;
            var blockXMax = (blockEndIndex * m_UnitWidth) + m_HorizontalOffset;

            // Enforce a minimum block width. The current Profiler Window allows users to render many more data points than pixels.
            var blockLength = blockXMax - blockXMin;
            if (blockLength < k_MinimumBlockWidth)
            {
                var blockXMid = Mathf.Lerp(blockXMin, blockXMax, 0.5f);
                blockXMin = blockXMid - k_HalfMinimumBlockWidth;
                blockXMax = blockXMid + k_HalfMinimumBlockWidth;
            }

            vertices[verticesIndex + 0] = new Vertex { position = new Vector3(blockXMin, blockYMin, Vertex.nearZ), tint = color };
            vertices[verticesIndex + 1] = new Vertex { position = new Vector3(blockXMin, blockYMax, Vertex.nearZ), tint = color };
            vertices[verticesIndex + 2] = new Vertex { position = new Vector3(blockXMax, blockYMin, Vertex.nearZ), tint = color };
            vertices[verticesIndex + 3] = new Vertex { position = new Vector3(blockXMax, blockYMax, Vertex.nearZ), tint = color };

            indices[indicesIndex + 0] = (ushort)(verticesIndex + 0);
            indices[indicesIndex + 1] = (ushort)(verticesIndex + 2);
            indices[indicesIndex + 2] = (ushort)(verticesIndex + 1);
            indices[indicesIndex + 3] = (ushort)(verticesIndex + 1);
            indices[indicesIndex + 4] = (ushort)(verticesIndex + 2);
            indices[indicesIndex + 5] = (ushort)(verticesIndex + 3);

            verticesIndex += NumberOfVerticesPerBlock;
            indicesIndex += NumberOfIndicesPerBlock;
        }

        enum Mode
        {
            ValueOverThreshold,
            InvalidValue
        }
    }
}
