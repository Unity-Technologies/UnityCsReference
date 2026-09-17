// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.UIElements;
using System;
using Unity.Burst.CompilerServices;

namespace UnityEditor.JobsProfiling;

internal struct PrimitiveRenderer
{
    /// This is used to store the number of vertices for each batch. UIToolkit uses ushort vertices
    /// only so we need to manually handle if we run out and submit more than one batch. If this is
    /// empty we just use the regular vertices count.
    internal struct Range
    {
        internal int indexStart;
        internal int indexCount;
        internal int vertexStart;
        internal int vertexCount;
    }

    // Maximum allowed vertices pre chunk
    internal const int kMaxVertsPerChunk = 65535;

    internal NativeList<Vertex> vertices;
    internal NativeList<ushort> indices;
    internal NativeList<Range> drawRanges;
    bool needDispose;
    internal int vertexIndex;

    internal PrimitiveRenderer(int preAllocSize)
    {
        vertices = new NativeList<Vertex>(preAllocSize, Allocator.Persistent);
        indices = new NativeList<ushort>(preAllocSize, Allocator.Persistent);
        drawRanges = new NativeList<Range>(4, Allocator.Persistent);
        vertexIndex = 0;
        needDispose = true;
    }

    internal void Begin()
    {
        vertices.Clear();
        indices.Clear();
        drawRanges.Clear();
        vertexIndex = 0;
    }

    internal enum ArrowDirection
    {
        Right,
        Down,
        Up,
    }

    ushort UpdateVertexIndex(int count)
    {
        // Validate that we can fit the within new new count
        if (Hint.Unlikely(vertexIndex + count > kMaxVertsPerChunk))
        {
            // if we haven't added any ranges we can just use the current values as is
            if (drawRanges.Length == 0)
            {
                var range = new Range
                {
                    indexStart = 0,
                    indexCount = indices.Length,
                    vertexStart = 0,
                    vertexCount = vertices.Length,
                };

                drawRanges.Add(range);
            }
            else
            {
                var prevRange = drawRanges[drawRanges.Length - 1];

                int indexStart = prevRange.indexStart + prevRange.indexCount;
                int vertexStart = prevRange.vertexStart + prevRange.vertexCount;

                var range = new Range
                {
                    indexStart = indexStart,
                    indexCount = indices.Length - indexStart,
                    vertexStart = vertexStart,
                    vertexCount = vertices.Length - vertexStart,
                };

                drawRanges.Add(range);
            }

            vertexIndex = 0;
        }

        int retValue = vertexIndex;

        vertexIndex += count;

        return (ushort)retValue;
    }

    void AddQuad(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
    {
        ushort vertexPos = UpdateVertexIndex(4);

        // We use this unsafe block here to reduce the number of bounds checks inside the NativeList code
        unsafe
        {
            Vertex* tempVerts = stackalloc[] { v0, v1, v2, v3 };
            vertices.AddRange(tempVerts, 4);

            ushort* temp = stackalloc[]
            {
                (ushort)(vertexPos + 0),
                (ushort)(vertexPos + 1),
                (ushort)(vertexPos + 2),
                (ushort)(vertexPos + 2),
                (ushort)(vertexPos + 3),
                (ushort)(vertexPos + 0),
            };

            indices.AddRange(temp, 6);
        }
    }
    internal void AddTriangle(Vertex v0, Vertex v1, Vertex v2)
    {
        ushort vertexPos = UpdateVertexIndex(3);

        // We use this unsafe block here to reduce the number of bounds checks inside the NativeList code
        unsafe
        {
            Vertex* tempVerts = stackalloc[] { v0, v1, v2 };
            vertices.AddRange(tempVerts, 3);

            ushort* temp = stackalloc[]
            {
                (ushort)(vertexPos + 0),
                (ushort)(vertexPos + 1),
                (ushort)(vertexPos + 2),
            };

            indices.AddRange(temp, 3);
        }
    }

    internal void DrawQuadColor(float2 c0, float2 c1, Color32 color)
    {
        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();
        Vertex v3 = new Vertex();

        v0.tint = color;
        v1.tint = color;
        v2.tint = color;
        v3.tint = color;

        v0.position.x = c0.x;
        v0.position.y = c0.y;

        v1.position.x = c1.x;
        v1.position.y = c0.y;

        v2.position.x = c1.x;
        v2.position.y = c1.y;

        v3.position.x = c0.x;
        v3.position.y = c1.y;

        AddQuad(v0, v1, v2, v3);
    }

    internal void DrawHorizontalLine(float2 start, float2 end, float width, Color32 color)
    {
        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();
        Vertex v3 = new Vertex();

        v0.tint = color;
        v1.tint = color;
        v2.tint = color;
        v3.tint = color;

        float halfWidth = width * 0.5f;

        v0.position.x = start.x;
        v0.position.y = start.y + halfWidth;

        v1.position.x = start.x;
        v1.position.y = start.y - halfWidth;

        v3.position.x = end.x;
        v3.position.y = end.y + halfWidth;

        v2.position.x = end.x;
        v2.position.y = end.y - halfWidth;

        if (v0.position.y > v3.position.y)
            AddQuad(v0, v1, v2, v3);
        else
            AddQuad(v2, v3, v0, v1);
    }
    internal void DrawVerticalLine(float2 start, float2 end, float width, Color32 color)
    {
        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();
        Vertex v3 = new Vertex();

        v0.tint = color;
        v1.tint = color;
        v2.tint = color;
        v3.tint = color;

        float halfWidth = width * 0.5f;

        if (end.y < start.y)
        {
            float2 t = start;
            start = end;
            end = t;
        }

        v0.position.x = start.x - halfWidth;
        v0.position.y = start.y;

        v1.position.x = start.x + halfWidth;
        v1.position.y = start.y;

        v2.position.x = end.x + halfWidth;
        v2.position.y = end.y;

        v3.position.x = end.x - halfWidth;
        v3.position.y = end.y;

        AddQuad(v0, v1, v2, v3);
    }


    void DrawArrowInternal(float2 pos, float size, Color32 color, ArrowDirection dir)
    {
        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();

        v0.tint = color;
        v1.tint = color;
        v2.tint = color;

        switch (dir)
        {
            case ArrowDirection.Right:
                {
                    v0.position.x = pos.x;
                    v0.position.y = pos.y - size;
                    v1.position.x = pos.x + size;
                    v1.position.y = pos.y;
                    v2.position.x = pos.x;
                    v2.position.y = pos.y + size;
                    break;
                }

            case ArrowDirection.Down:
                {
                    v0.position.x = pos.x - size;
                    v0.position.y = pos.y;
                    v1.position.x = pos.x + size;
                    v1.position.y = pos.y;
                    v2.position.x = pos.x;
                    v2.position.y = pos.y + size;
                    break;
                }

            case ArrowDirection.Up:
                {
                    v0.position.x = pos.x - size;
                    v0.position.y = pos.y;
                    v1.position.x = pos.x;
                    v1.position.y = pos.y - size;
                    v2.position.x = pos.x + size;
                    v2.position.y = pos.y;
                    break;
                }
        }

        AddTriangle(v0, v1, v2);
    }

    internal void DrawArrow(float2 pos, Color32 color, ArrowDirection direction)
    {
        Color32 black = new Color32(0, 0, 0, 127);
        DrawArrowInternal(pos, 6.0f, black, direction); // black outline
        DrawArrowInternal(pos, 5.0f, color, direction); // inner color
    }
    internal void DrawArrowWithSize(float2 pos, Color32 color, float size, ArrowDirection direction)
    {
        Color32 black = new Color32(0, 0, 0, 127);
        DrawArrowInternal(pos, size, black, direction); // black outline
        DrawArrowInternal(pos, size - 1.0f, color, direction); // inner color
    }

    internal void Dispose()
    {
        if (needDispose)
        {
            needDispose = false;
            vertices.Dispose();
            indices.Dispose();
            drawRanges.Dispose();
        }
    }
}
