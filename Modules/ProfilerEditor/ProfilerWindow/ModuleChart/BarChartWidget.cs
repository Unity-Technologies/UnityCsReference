// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Profiling.Editor
{
    internal class BarChartWidget : ChartWidget
    {
        static readonly int kIndicesPerQuad = 6;
        static readonly int kVerticesPerQuad = 4;
        static readonly int kMaxQuadsCount = 65535 / kVerticesPerQuad;
        static readonly float kMinBarSizeWithGap = 3f;
        static readonly float kInvalidFrameValue = -1f;
        static readonly Color k_OverlayBackgroundDimFactor = new(0.9f, 0.9f, 0.9f, 0.4f);

        static readonly ProfilerMarker k_GeometryUpdate = new($"{nameof(BarChartWidget)}.UpdateGeometry");

        public BarChartWidget(ChartModel model, VisualElement root) :
            base(model, root)
        {
        }

        protected override void UpdateGeometry(MeshGenerationContext mgc)
        {
            using var _ = k_GeometryUpdate.Auto();

            int totalQuadsCount = CalcQuadsCount();
            if (totalQuadsCount == 0)
                return;

            // MakeGeometry now handles batching internally
            MakeGeometry(mgc, Root, Model, totalQuadsCount);
        }

        int CalcQuadsCount()
        {
            int count = 0;
            var model = Model;
            for (int i = 0; i < model.numSeries; i++)
            {
                if (!model.series[i].enabled)
                    continue;

                count += CalcSeriesQuadsCount(model.series[i].yValues);

                if (model.hasOverlay)
                    count += CalcSeriesQuadsCount(model.overlays[i].yValues);
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IgnoreSeriesValue(float value)
        {
            if (value == kInvalidFrameValue)
                return true;
            // Avoid drawing zero-sized quads
            if (value < Mathf.Epsilon)
                return true;
            return false;
        }

        static int CalcSeriesQuadsCount(float[] series)
        {
            int count = 0;
            for (int i = 0; i < series.Length; i++)
            {
                if (IgnoreSeriesValue(series[i]))
                    continue;
                count++;
            }

            return count;
        }

        static unsafe void MakeGeometry(MeshGenerationContext mgc, VisualElement elm, ChartModel model, int totalQuadsCount, int startOffset = 0)
        {
            var workArea = elm.contentRect;
            var dataPoints = model.series[0].numDataPoints;
            using var totalValuesArray = new NativeArray<float>(dataPoints, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Use 90% of max capacity to leave room for safety
            int maxQuadsPerMesh = (int)(kMaxQuadsCount * 0.9f);
            int remainingQuads = totalQuadsCount;
            int processedQuads = 0;

            var totalValues = (float*)totalValuesArray.GetUnsafePtr();

            while (remainingQuads > 0)
            {
                int currentMeshQuads = Math.Min(maxQuadsPerMesh, remainingQuads);
                
                var indexCount = currentMeshQuads * kIndicesPerQuad;
                var vertexCount = currentMeshQuads * kVerticesPerQuad;
                var mesh = mgc.Allocate(vertexCount, indexCount);
                var indices = new Span<UInt16>(mesh.m_Indices.GetUnsafePtr(), indexCount);
                var vertices = new Span<Vertex>(mesh.m_Vertices.GetUnsafePtr(), vertexCount);

                int quadIndex = 0;
                int globalQuadIndex = processedQuads;

                for (int orderedIndex = 0; orderedIndex < model.numSeries && quadIndex < currentMeshQuads; orderedIndex++)
                {
                    var seriesIndex = model.order[orderedIndex];
                    var seriesData = model.series[seriesIndex];
                    if (!seriesData.enabled)
                        continue;

                    // Process overlay first if it exists
                    if (model.hasOverlay)
                    {
                        MakeSeriesGeometry(ref vertices, ref indices, model, seriesData, 
                            model.overlays[seriesIndex].yValues, ref quadIndex, totalValues, workArea, 
                            0, dataPoints, currentMeshQuads, ref globalQuadIndex);
                    }

                    // Process main series
                    if (quadIndex < currentMeshQuads)
                    {
                        MakeSeriesGeometry(ref vertices, ref indices, model, seriesData, 
                            null, ref quadIndex, totalValues, workArea, 0, dataPoints, currentMeshQuads, ref globalQuadIndex);
                    }
                }

                remainingQuads -= quadIndex;
                processedQuads += quadIndex;
                
                // If we didn't fill this mesh, we're done
                if (quadIndex < currentMeshQuads)
                    break;
            }
        }

        static unsafe void MakeSeriesGeometry(ref Span<Vertex> vertices, ref Span<UInt16> indices, ChartModel model, ChartSeriesViewData series, float[] overlayValues, ref int quadIndex, float* totalValues, Rect workArea, int batchStart, int batchSize, int maxQuads, ref int globalQuadIndex)
        {
            var overlay = overlayValues != null;

            var seriesLength = series.numDataPoints;
            var seriesRange = series.rangeAxis;
            var seriesValues = overlay ? overlayValues : series.yValues;

            var seriesColor = series.color;
            if (model.hasOverlay && !overlay)
                seriesColor *= k_OverlayBackgroundDimFactor;
            var seriesColor32 = (Color32)seriesColor;

            var barStep = new Vector2(workArea.width / seriesLength, 0);
            var barGap = barStep.x > kMinBarSizeWithGap ? 1 : 0;
            var barScale = new Vector2(0, -workArea.height);
            var position = new Vector2(workArea.x, workArea.y + workArea.height);
            var baseMinValue = seriesRange.x;
            var baseValueSpan = seriesRange.y;
            
            for (int i = batchStart; i < batchStart + batchSize; i++)
            {
                // Stop if we would exceed the mesh capacity
                if (quadIndex >= maxQuads)
                    break;
                    
                var value = seriesValues[i];
                if (IgnoreSeriesValue(value))
                {
                    position += barStep;
                    continue;
                }

                // Skip quads that were already processed in previous batches
                if (globalQuadIndex > 0)
                {
                    globalQuadIndex--;
                    position += barStep;
                    continue;
                }

                // Bar segment values and range
                value *= series.yScale;
                var minValue = totalValues[i];
                var maxValue = minValue + value;
                var valueSpan = baseValueSpan;

                // Bar segment value range in unit scale ([0..1])
                float unitMin = Mathf.Max((minValue - baseMinValue) / valueSpan, 0);
                float unitMax = (maxValue - baseMinValue) / valueSpan;

                // Screen space rect
                var rectPos = position + unitMin * barScale;
                var rectSize = (unitMax - unitMin) * barScale + barStep;
                var rectXMin = Mathf.Min(rectPos.x, rectPos.x + rectSize.x) + barGap;
                var rectXMax = Mathf.Max(rectPos.x, rectPos.x + rectSize.x);
                var rectYMin = Mathf.Min(rectPos.y, rectPos.y + rectSize.y);
                var rectYMax = Mathf.Max(rectPos.y, rectPos.y + rectSize.y);

                int indicesBase = quadIndex * kIndicesPerQuad;
                int verticesBase = quadIndex * kVerticesPerQuad;

                vertices[verticesBase + 0] = new Vertex() { position = new Vector3(rectXMin, rectYMin, Vertex.nearZ), tint = seriesColor32 };
                vertices[verticesBase + 1] = new Vertex() { position = new Vector3(rectXMin, rectYMax, Vertex.nearZ), tint = seriesColor32 };
                vertices[verticesBase + 2] = new Vertex() { position = new Vector3(rectXMax, rectYMin, Vertex.nearZ), tint = seriesColor32 };
                vertices[verticesBase + 3] = new Vertex() { position = new Vector3(rectXMax, rectYMax, Vertex.nearZ), tint = seriesColor32 };

                indices[indicesBase + 0] = (ushort)(verticesBase + 0);
                indices[indicesBase + 1] = (ushort)(verticesBase + 2);
                indices[indicesBase + 2] = (ushort)(verticesBase + 1);

                indices[indicesBase + 3] = (ushort)(verticesBase + 1);
                indices[indicesBase + 4] = (ushort)(verticesBase + 2);
                indices[indicesBase + 5] = (ushort)(verticesBase + 3);

                quadIndex++;
                if (!overlay)
                    totalValues[i] += value;

                position += barStep;
            }
        }
    }
}
