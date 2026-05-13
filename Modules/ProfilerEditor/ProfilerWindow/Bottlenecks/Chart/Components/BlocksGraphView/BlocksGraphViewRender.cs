// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // A graph view that draws horizontal blocks for any data value over a threshold. Each data series is
    // plotted on a new line, with all data series being fitted equally into the available vertical space.
    // Contiguous values that exceed the threshold are grouped into a single block.
    [UxmlElement]
    internal partial class BlocksGraphViewRender : GraphView
    {
        public BlocksGraphViewRender()
        {
            generateVisualContent += Render;
        }

        // The graph's data source.
        public IDataSource DataSource { private get; set; }

        float ComputeGraphUnitWidth()
        {
            if (DataSource == null)
                return 0.5f;

            return contentRect.width / DataSource.LengthForEachDataSeriesInGraphView();
        }

        void Render(MeshGenerationContext mgc)
        {
            if (DataSource == null)
            {
                return;
            }

            var dataSeriesCount = DataSource.NumberOfDataSeriesForGraphView();
            if (dataSeriesCount == 0)
                return;

            var dataSeriesHeight = contentRect.height / dataSeriesCount;
            var dataValueThreshold = DataSource.DataValueThresholdInGraphView();

            var dataSeriesCapacity = DataSource.LengthForEachDataSeriesInGraphView();
            // The maximum number of blocks could only occur if data values alternated between being invalid and being
            // over the threshold, because consecutive values of either type are combined into a single block.
            var maximumNumberOfBlocks = Mathf.CeilToInt(dataSeriesCapacity * dataSeriesCount);
            var maximumNumberOfVertices = maximumNumberOfBlocks * BlocksGraphViewMeshBuilder.NumberOfVerticesPerBlock;
            var maximumNumberOfIndices = maximumNumberOfBlocks * BlocksGraphViewMeshBuilder.NumberOfIndicesPerBlock;
            using var vertices = new NativeArray<Vertex>(maximumNumberOfVertices, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            using var indices = new NativeArray<ushort>(maximumNumberOfIndices, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var verticesIndex = 0;
            var indicesIndex = 0;
            var meshBuilder = new BlocksGraphViewMeshBuilder(UnitWidth == 20.0f ? ComputeGraphUnitWidth() : UnitWidth, HorizontalOffset, dataValueThreshold);
            for (var i = 0; i < dataSeriesCount; i++)
            {
                var dataSeries = DataSource.ValuesForDataSeriesInGraphView(i);
                if (dataSeries.Length == 0)
                    continue;

                var color = DataSource.ColorForDataSeriesInGraphView(i);
                var invalidColor = DataSource.InvalidColorForDataSeriesInGraphView();

                // Calculate the vertical vertex positions once per series.
                var blockYMin = i * dataSeriesHeight;
                var blockYMax = (i + 1) * dataSeriesHeight;

                // Build block meshes for all data values over the threshold.
                meshBuilder.BuildBlockMeshesForDataOverThresholdInDataSeries(
                    dataSeries,
                    blockYMin,
                    blockYMax,
                    color,
                    vertices,
                    indices,
                    ref verticesIndex,
                    ref indicesIndex);

                // Build block meshes for all invalid data values.
                meshBuilder.BuildBlockMeshesForInvalidDataInDataSeries(
                    dataSeries,
                    blockYMin,
                    blockYMax,
                    invalidColor,
                    vertices,
                    indices,
                    ref verticesIndex,
                    ref indicesIndex);
            }

            if (verticesIndex > 0)
            {
                var meshWriteData = mgc.Allocate(verticesIndex, indicesIndex);
                meshWriteData.SetAllVertices(vertices.Slice(0, verticesIndex));
                meshWriteData.SetAllIndices(indices.Slice(0, indicesIndex));
            }
        }

        public interface IDataSource
        {
            // The number of data series displayed by the graph view.
            int NumberOfDataSeriesForGraphView();

            // The color for the specified data series.
            Color ColorForDataSeriesInGraphView(int dataSeriesIndex);

            // The color for invalid data values in any data series.
            Color InvalidColorForDataSeriesInGraphView();

            // The threshold at which a block should be drawn for a data value.
            float DataValueThresholdInGraphView();

            // The length for each data series displayed by the graph view.
            int LengthForEachDataSeriesInGraphView();

            // The values for the specified data series.
            NativeSlice<float> ValuesForDataSeriesInGraphView(int dataSeriesIndex);
            float PercentageFramesOverTarget(int dataSeriesIndex);
        }
    }
}
