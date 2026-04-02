// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    static class DrawUtils
    {
        static readonly Vector2[] k_TextureUVs = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };

        public static void DrawRect(this MeshGenerationContext context, Rect rect, Color color, bool fill = true)
        {
            int vertexCount = fill ? 4 : 16; // if for non-opaque rectangles we need to draw 4 lines. Each line has 4 vertices
            int indexCount = fill ? 6 : 24; // if for non-opaque rectangles we need to draw 4 lines. Each line has 6 indices
            MeshWriteData mesh = context.Allocate(vertexCount, indexCount);
            mesh.DrawRect(rect, color, fill: fill);
        }

        public static void DrawRect(this MeshGenerationContext context, Rect rect, Texture2D texture)
        {
            context.DrawRect(rect, texture, Color.white);
        }

        public static void DrawRect(this MeshGenerationContext context, Rect rect, Texture2D texture, Color tint)
        {
            MeshWriteData mesh = context.Allocate(4, 6, texture);
            mesh.DrawRect(rect, tint);
        }

        public static void DrawRect(this MeshWriteData mesh, Rect rect, Color color, int vertexOffset = 0, bool fill = true)
        {
            float x0 = Mathf.Round(rect.x);
            float x1 = Mathf.Round(rect.xMax);
            float y0 = Mathf.Round(rect.y);
            float y1 = Mathf.Round(rect.yMax);

            var point0 = new Vector3(x0, y0, Vertex.nearZ);
            var point1 = new Vector3(x1, y0, Vertex.nearZ);
            var point2 = new Vector3(x0, y1, Vertex.nearZ);
            var point3 = new Vector3(x1, y1, Vertex.nearZ);

            if (fill)
            {
                mesh.SetNextVertex(new Vertex { position = point0, tint = color, uv = k_TextureUVs[0] });
                mesh.SetNextVertex(new Vertex { position = point1, tint = color, uv = k_TextureUVs[1] });
                mesh.SetNextVertex(new Vertex { position = point2, tint = color, uv = k_TextureUVs[2] });
                mesh.SetNextVertex(new Vertex { position = point3, tint = color, uv = k_TextureUVs[3] });

                mesh.SetNextIndex((ushort)vertexOffset);
                mesh.SetNextIndex((ushort)(vertexOffset + 1));
                mesh.SetNextIndex((ushort)(vertexOffset + 2));
                mesh.SetNextIndex((ushort)(vertexOffset + 1));
                mesh.SetNextIndex((ushort)(vertexOffset + 3));
                mesh.SetNextIndex((ushort)(vertexOffset + 2));
            }
            else
            {
                mesh.DrawLine2D(point0, point1, 1f, color);
                mesh.DrawLine2D(point0, point2, 1f, color, vertexOffset + 4);
                mesh.DrawLine2D(point2, point3, 1f, color, vertexOffset + 8);
                mesh.DrawLine2D(point1, point3, 1f, color, vertexOffset + 12);
            }
        }

        public static void DrawLine2D(this MeshGenerationContext context, Vector2 a, Vector2 b, float thickness, Color color)
        {
            MeshWriteData mesh = context.Allocate(4, 6);
            mesh.DrawLine2D(a, b, thickness, color);
        }

        public static void DrawLine2D(this MeshWriteData mesh, Vector2 a, Vector2 b, float thickness, Color color, int vertexOffset = 0)
        {
            Vector2 line = b - a;
            float halfLength = line.magnitude / 2.0f;
            float halfWidth = thickness / 2.0f;

            /* rect centered at origin
                * a1----(a)-----a2
                * |              |
                * |              |
                * b1----(b)-----b2  */
            var a1 = new Vector2(-halfWidth, halfLength);
            var a2 = new Vector2(halfWidth, halfLength);
            var b1 = new Vector2(-halfWidth, -halfLength);
            var b2 = new Vector2(halfWidth, -halfLength);

            float angle = Mathf.Atan(line.x / line.y);
            float cos = Mathf.Cos(-angle);
            float sin = Mathf.Sin(-angle);
            Vector2 translation = new Vector2(a.x, a.y) + line.normalized * halfLength;

            //rotate around origin, then translate to the correct position
            a1 = new Vector2(a1.x * cos - a1.y * sin, a1.y * cos + a1.x * sin) + translation;
            a2 = new Vector2(a2.x * cos - a2.y * sin, a2.y * cos + a2.x * sin) + translation;
            b1 = new Vector2(b1.x * cos - b1.y * sin, b1.y * cos + b1.x * sin) + translation;
            b2 = new Vector2(b2.x * cos - b2.y * sin, b2.y * cos + b2.x * sin) + translation;

            mesh.DrawQuad(a1, a2, b1, b2, color, vertexOffset);
        }

        public static void DrawQuad(this MeshGenerationContext context, Vector2 a, Vector2 b, Vector2 c, Vector3 d, Color color)
        {
            MeshWriteData mesh = context.Allocate(4, 6);
            mesh.DrawQuad(a, b, c, d, color);
        }

        public static void DrawQuad(this MeshWriteData mesh, Vector2 a, Vector2 b, Vector2 c, Vector3 d, Color color, int vertexOffset = 0)
        {
            mesh.SetNextVertex(new Vertex { position = a, tint = color });
            mesh.SetNextVertex(new Vertex { position = b, tint = color });
            mesh.SetNextVertex(new Vertex { position = c, tint = color });
            mesh.SetNextVertex(new Vertex { position = d, tint = color });
            mesh.SetNextIndex((ushort)(vertexOffset + 1));
            mesh.SetNextIndex((ushort)vertexOffset);
            mesh.SetNextIndex((ushort)(vertexOffset + 2));
            mesh.SetNextIndex((ushort)(vertexOffset + 2));
            mesh.SetNextIndex((ushort)(vertexOffset + 3));
            mesh.SetNextIndex((ushort)(vertexOffset + 1));
        }

        public static void DrawTriangle(this MeshWriteData mesh, Vector2 a, Vector2 b, Vector2 c, Color color, int vertexOffset = 0)
        {
            mesh.SetNextVertex(new Vertex { position = a, tint = color });
            mesh.SetNextVertex(new Vertex { position = b, tint = color });
            mesh.SetNextVertex(new Vertex { position = c, tint = color });
            mesh.SetNextIndex((ushort)vertexOffset);
            mesh.SetNextIndex((ushort)(vertexOffset + 1));
            mesh.SetNextIndex((ushort)(vertexOffset + 2));
        }

        public static void DrawTriangle(this MeshGenerationContext context, Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            MeshWriteData mesh = context.Allocate(3, 3);
            mesh.DrawTriangle(a, b, c, color);
        }

        /*
         * x: position
         * +----x----+
         * |         |
         * |         |
         * +---------+
        */
        public static void DrawVerticalLine(this MeshGenerationContext context, Vector2 position, float height, float thickness, Color color)
        {
            thickness = Mathf.Max(1.0f, thickness);
            float halfWidth = thickness / 2.0f;
            var rect = new Rect(Mathf.Floor(position.x - halfWidth), position.y, thickness, height);
            context.DrawRect(rect, color);
        }

        public static void DrawVerticalDottedLine(this MeshGenerationContext context, Vector2 position, float height, float width, float segmentHeight, Color color)
        {
            int segments = 1;
            if (segmentHeight > 0)
                segments = (int)(height / segmentHeight);

            float remainingHeight = height - segments * segmentHeight;

            Vector2 segmentPosition = position;
            for (var i = 0; i < segments; i++)
            {
                if (i % 2 == 0)
                    context.DrawVerticalLine(segmentPosition, segmentHeight, width, color);

                segmentPosition = new Vector2(position.x, segmentPosition.y + segmentHeight);
            }

            if (segments % 2 == 0)
                context.DrawVerticalLine(segmentPosition, remainingHeight, width, color);
        }

        /*
        * y: position
        * +---------+
        * |         |
        * y         |
        * |         |
        * +---------+
        */
        public static void DrawHorizontalLine(this MeshGenerationContext context, Vector2 position, float width, float thickness, Color color)
        {
            thickness = Mathf.Max(1.0f, thickness);
            float halfHeight = thickness / 2.0f;
            var rect = new Rect(position.x, Mathf.Floor(position.y - halfHeight), width, thickness);
            context.DrawRect(rect, color);
        }

        public static void DrawHorizontalDottedLine(this MeshGenerationContext context, Vector2 position, float width, float thickness, float segmentWidth, Color color)
        {
            int segments = 1;
            if (segmentWidth > 0)
                segments = (int)(width / segmentWidth);

            float remainingWidth = width - segments * segmentWidth;

            Vector2 segmentPosition = position;
            for (var i = 0; i < segments; i++)
            {
                if (i % 2 == 0)
                    context.DrawHorizontalLine(segmentPosition, segmentWidth, thickness, color);

                segmentPosition = new Vector2(segmentPosition.x + segmentWidth, segmentPosition.y);
            }

            if (segments % 2 == 0)
                context.DrawHorizontalLine(segmentPosition, remainingWidth, thickness, color);
        }

        public static void DrawRectangularArrow(this MeshGenerationContext context, Vector2 position, Vector2 direction, float width, float length, float arrowLength, Color color)
        {
            context.DrawHalfRectangularArrow(position, direction, width * 0.5f, length, arrowLength, false, color);
            context.DrawHalfRectangularArrow(position, direction, width * 0.5f, length, arrowLength, true, color);
        }

        public static void DrawHalfRectangularArrow(this MeshGenerationContext context, Vector2 position, Vector2 direction, float width, float length, float arrowLength, bool rightSideOfDirection, Color color)
        {
            float handleLength = length - arrowLength;

            float dirDotUp = Vector2.Dot(direction.normalized, Vector2.up);
            float dirDotRight = Vector2.Dot(direction.normalized, Vector2.right);
            bool vertical = Math.Round(Math.Abs(dirDotUp)) > 0;

            Rect handleRect;
            if (vertical)
            {
                handleRect = new Rect(
                    position.x - (!rightSideOfDirection ? width : 0),
                    position.y - (dirDotUp > 0 ? handleLength : 0),
                    width,
                    handleLength
                );
            }
            else
            {
                handleRect = new Rect(
                    position.x - (dirDotRight > 0 ? 0 : handleLength),
                    position.y - (rightSideOfDirection ? width : 0),
                    handleLength,
                    width
                );
            }

            context.DrawRect(handleRect, color);


            if (arrowLength < 0f)
                return;

            if (vertical)
            {
                if (dirDotUp > 0)
                {
                    //Draw triangle Up
                    context.DrawTriangle(
                        new Vector2(rightSideOfDirection ? handleRect.xMin : handleRect.xMax, handleRect.yMin - arrowLength),
                        new Vector2(handleRect.xMax, handleRect.yMin),
                        new Vector2(handleRect.xMin, handleRect.yMin),
                        color);
                    return;
                }

                //Draw triangle Down
                context.DrawTriangle(
                    new Vector2(handleRect.xMin, handleRect.yMax),
                    new Vector2(handleRect.xMax, handleRect.yMax),
                    new Vector2(rightSideOfDirection ? handleRect.xMin : handleRect.xMax, handleRect.yMax + arrowLength),
                    color);
                return;
            }

            if (dirDotRight > 0)
            {
                //Draw triangle Right
                context.DrawTriangle(
                    new Vector2(handleRect.xMax, handleRect.yMin),
                    new Vector2(handleRect.xMax + arrowLength, rightSideOfDirection ? handleRect.yMax : handleRect.yMin),
                    new Vector2(handleRect.xMax, handleRect.yMax),
                    color);
                return;
            }

            //Draw triangle Left
            context.DrawTriangle(
                new Vector2(handleRect.xMin, handleRect.yMax),
                new Vector2(handleRect.xMin - arrowLength, rightSideOfDirection ? handleRect.yMax : handleRect.yMin),
                new Vector2(handleRect.xMin, handleRect.yMin),
                color);
        }
    }
}
