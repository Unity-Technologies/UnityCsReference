using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    /// <summary>
    /// Utility class that facilitates mesh allocation and building according to the provided settings
    /// </summary>
    internal static class MeshBuilder
    {
        static ProfilerMarker s_VectorGraphics9Slice = new ProfilerMarker("UIR.MakeVector9Slice");
        static ProfilerMarker s_VectorGraphicsStretch = new ProfilerMarker("UIR.MakeVectorStretch");

        internal struct AllocMeshData
        {
            internal delegate MeshWriteData Allocator(uint vertexCount, uint indexCount, ref AllocMeshData allocatorData);
            internal MeshWriteData Allocate(uint vertexCount, uint indexCount) { return alloc(vertexCount, indexCount, ref this); }

            internal Allocator alloc;

            // Additional allocation params
            internal Texture texture;
            internal Material material;
            internal MeshGenerationContext.MeshFlags flags;
        }

        internal static void MakeBorder(MeshGenerationContextUtils.BorderParams borderParams, float posZ, AllocMeshData meshAlloc)
        {
            Tessellation.TessellateBorder(borderParams, posZ, meshAlloc);
        }

        internal static void MakeSolidRect(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, AllocMeshData meshAlloc)
        {
            if (!rectParams.HasRadius(Tessellation.kEpsilon))
                MakeQuad(rectParams.rect, Rect.zero, rectParams.color, posZ, meshAlloc);
            else Tessellation.TessellateRect(rectParams, posZ, meshAlloc, false);
        }

        internal static void MakeTexturedRect(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, AllocMeshData meshAlloc)
        {
            if (rectParams.leftSlice <= Mathf.Epsilon &&
                rectParams.topSlice <= Mathf.Epsilon &&
                rectParams.rightSlice <= Mathf.Epsilon &&
                rectParams.bottomSlice <= Mathf.Epsilon)
            {
                if (!rectParams.HasRadius(Tessellation.kEpsilon))
                    MakeQuad(rectParams.rect, rectParams.uv, rectParams.color, posZ, meshAlloc);
                else Tessellation.TessellateRect(rectParams, posZ, meshAlloc, true);
            }
            else if (rectParams.texture == null)
            {
                MakeQuad(rectParams.rect, rectParams.uv, rectParams.color, posZ, meshAlloc);
            }
            else MakeSlicedQuad(ref rectParams, posZ, meshAlloc);
        }

        private static Vertex ConvertTextVertexToUIRVertex(TextCore.MeshInfo info, int index, Vector2 offset)
        {
            return new Vertex
            {
                position = new Vector3(info.vertices[index].x + offset.x, info.vertices[index].y + offset.y, UIRUtility.k_MeshPosZ),
                uv = info.uvs0[index],
                //textParms = info.uvs2[index],
                tint = info.colors32[index],
                idsFlags = new Color32(0, 0, 0, (byte)VertexFlags.IsText)
            };
        }

        private static Vertex ConvertTextVertexToUIRVertex(TextVertex textVertex, Vector2 offset)
        {
            return new Vertex
            {
                position = new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ),
                uv = textVertex.uv0,
                tint = textVertex.color,
                idsFlags = new Color32(0, 0, 0, (byte)VertexFlags.IsText) // same flag for both text engines
            };
        }

        static int LimitTextVertices(int vertexCount, bool logTruncation = true)
        {
            const int maxTextMeshVertices = 0xC000; // Max 48k vertices. We leave room for masking, borders, background, etc.

            if (vertexCount <= maxTextMeshVertices)
                return vertexCount;

            if (logTruncation)
                Debug.LogError($"Generated text will be truncated because it exceeds {maxTextMeshVertices} vertices.");

            return maxTextMeshVertices;
        }

        internal static void MakeText(TextCore.MeshInfo meshInfo, Vector2 offset, AllocMeshData meshAlloc)
        {
            int vertexCount = LimitTextVertices(meshInfo.vertexCount);
            int quadCount = vertexCount / 4;
            var mesh = meshAlloc.Allocate((uint)(quadCount * 4), (uint)(quadCount * 6));

            for (int q = 0, v = 0, i = 0; q < quadCount; ++q, v += 4, i += 6)
            {
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 0, offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 1, offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 2, offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 3, offset));

                mesh.SetNextIndex((UInt16)(v + 0));
                mesh.SetNextIndex((UInt16)(v + 1));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 3));
                mesh.SetNextIndex((UInt16)(v + 0));
            }
        }

        internal static void MakeText(NativeArray<TextVertex> uiVertices, Vector2 offset, AllocMeshData meshAlloc)
        {
            int vertexCount = LimitTextVertices(uiVertices.Length);
            int quadCount = vertexCount / 4;
            var mesh = meshAlloc.Allocate((uint)(quadCount * 4), (uint)(quadCount * 6));

            for (int q = 0, v = 0; q < quadCount; ++q, v += 4)
            {
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 0], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 1], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 2], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 3], offset));

                mesh.SetNextIndex((UInt16)(v + 0));
                mesh.SetNextIndex((UInt16)(v + 1));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 3));
                mesh.SetNextIndex((UInt16)(v + 0));
            }
        }

        internal static void UpdateText(NativeArray<TextVertex> uiVertices,
            Vector2 offset, Matrix4x4 transform,
            Color32 xformClipPages, Color32 idsFlags, Color32 opacityPageSVGSettingIndex,
            NativeSlice<Vertex> vertices)
        {
            int vertexCount = LimitTextVertices(uiVertices.Length, false);
            Debug.Assert(vertexCount == vertices.Length);
            idsFlags.a = (byte)VertexFlags.IsText;
            for (int v = 0; v < vertexCount; v++)
            {
                var textVertex = uiVertices[v];
                vertices[v] = new Vertex
                {
                    position = transform.MultiplyPoint3x4(new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ)),
                    uv = textVertex.uv0,
                    tint = textVertex.color,
                    xformClipPages = xformClipPages,
                    idsFlags = idsFlags,
                    opacityPageSVGSettingIndex = opacityPageSVGSettingIndex
                };
            }
        }

        private static void MakeQuad(Rect rcPosition, Rect rcTexCoord, Color color, float posZ, AllocMeshData meshAlloc)
        {
            var mesh = meshAlloc.Allocate(4, 6);

            float x0 = rcPosition.x;
            float x3 = rcPosition.xMax;
            float y0 = rcPosition.yMax;
            float y3 = rcPosition.y;

            var uvRegion = mesh.uvRegion;
            float u0 = rcTexCoord.x * uvRegion.width + uvRegion.xMin;
            float u3 = rcTexCoord.xMax * uvRegion.width + uvRegion.xMin;
            float v0 = rcTexCoord.y * uvRegion.height + uvRegion.yMin;
            float v3 = rcTexCoord.yMax * uvRegion.height + uvRegion.yMin;

            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x0, y0, posZ),
                tint = color,
                uv = new Vector2(u0, v0)
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x3, y0, posZ),
                tint = color,
                uv = new Vector2(u3, v0)
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x0, y3, posZ),
                tint = color,
                uv = new Vector2(u0, v3)
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x3, y3, posZ),
                tint = color,
                uv = new Vector2(u3, v3)
            });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(1);

            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
        }

        private static readonly UInt16[] slicedQuadIndices = new UInt16[]
        {
            0, 4, 1, 4, 5, 1,
            1, 5, 2, 5, 6, 2,
            2, 6, 3, 6, 7, 3,
            4, 8, 5, 8, 9, 5,
            5, 9, 6, 9, 10, 6,
            6, 10, 7, 10, 11, 7,
            8, 12, 9, 12, 13, 9,
            9, 13, 10, 13, 14, 10,
            10, 14, 11, 14, 15, 11
        };

        // Caches.
        static readonly float[] k_TexCoordSlicesX = new float[4];
        static readonly float[] k_TexCoordSlicesY = new float[4];
        static readonly float[] k_PositionSlicesX = new float[4];
        static readonly float[] k_PositionSlicesY = new float[4];

        internal static void MakeSlicedQuad(ref MeshGenerationContextUtils.RectangleParams rectParams, float posZ, AllocMeshData meshAlloc)
        {
            var mesh = meshAlloc.Allocate(16, 9 * 6);

            float pixelsPerPoint = 1;
            var texture2D = rectParams.texture as Texture2D;
            if (texture2D != null)
                pixelsPerPoint = texture2D.pixelsPerPoint;

            float texWidth = rectParams.texture.width;
            float texHeight = rectParams.texture.height;
            float uConversion = pixelsPerPoint / texWidth;
            float vConversion = pixelsPerPoint / texHeight;

            float leftSlice = Mathf.Max(0.0f, rectParams.leftSlice);
            float rightSlice = Mathf.Max(0.0f, rectParams.rightSlice);
            float bottomSlice = Mathf.Max(0.0f, rectParams.bottomSlice);
            float topSlice = Mathf.Max(0.0f, rectParams.topSlice);

            // Clamp UVs in the [0,1] range
            float uvLeftSlice = Mathf.Clamp(leftSlice * uConversion, 0.0f, 1.0f);
            float uvRightSlice = Mathf.Clamp(rightSlice * uConversion, 0.0f, 1.0f);
            float uvBottomSlice = Mathf.Clamp(bottomSlice * vConversion, 0.0f, 1.0f);
            float uvTopslice = Mathf.Clamp(topSlice * vConversion, 0.0f, 1.0f);

            k_TexCoordSlicesX[0] = rectParams.uv.min.x;
            k_TexCoordSlicesX[1] = rectParams.uv.min.x + uvLeftSlice;
            k_TexCoordSlicesX[2] = rectParams.uv.max.x - uvRightSlice;
            k_TexCoordSlicesX[3] = rectParams.uv.max.x;

            k_TexCoordSlicesY[0] = rectParams.uv.max.y;
            k_TexCoordSlicesY[1] = rectParams.uv.max.y - uvBottomSlice;
            k_TexCoordSlicesY[2] = rectParams.uv.min.y + uvTopslice;
            k_TexCoordSlicesY[3] = rectParams.uv.min.y;

            var uvRegion = mesh.uvRegion;
            for (int i = 0; i < 4; i++)
            {
                k_TexCoordSlicesX[i] = k_TexCoordSlicesX[i] * uvRegion.width + uvRegion.xMin;
                k_TexCoordSlicesY[i] = (rectParams.uv.min.y + rectParams.uv.max.y - k_TexCoordSlicesY[i]) * uvRegion.height + uvRegion.yMin;
            }

            // Prevent overlapping slices
            float sliceWidth = (leftSlice + rightSlice);
            if (sliceWidth > rectParams.rect.width)
            {
                float rescale = rectParams.rect.width / sliceWidth;
                leftSlice *= rescale;
                rightSlice *= rescale;
            }

            float sliceHeight = (bottomSlice + topSlice);
            if (sliceHeight > rectParams.rect.height)
            {
                float rescale = rectParams.rect.height / sliceHeight;
                bottomSlice *= rescale;
                topSlice *= rescale;
            }

            k_PositionSlicesX[0] = rectParams.rect.x;
            k_PositionSlicesX[1] = rectParams.rect.x + leftSlice;
            k_PositionSlicesX[2] = rectParams.rect.xMax - rightSlice;
            k_PositionSlicesX[3] = rectParams.rect.xMax;

            k_PositionSlicesY[0] = rectParams.rect.yMax;
            k_PositionSlicesY[1] = rectParams.rect.yMax - bottomSlice;
            k_PositionSlicesY[2] = rectParams.rect.y + topSlice;
            k_PositionSlicesY[3] = rectParams.rect.y;

            for (int i = 0; i < 16; ++i)
            {
                int x = i % 4;
                int y = i / 4;
                mesh.SetNextVertex(new Vertex() {
                    position = new Vector3(k_PositionSlicesX[x], k_PositionSlicesY[y], posZ),
                    uv = new Vector2(k_TexCoordSlicesX[x], k_TexCoordSlicesY[y]),
                    tint = rectParams.color
                });
            }
            mesh.SetAllIndices(slicedQuadIndices);
        }

        internal static void MakeVectorGraphics(MeshGenerationContextUtils.RectangleParams rectParams, int settingIndexOffset, AllocMeshData meshAlloc, out int finalVertexCount, out int finalIndexCount)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);

            finalVertexCount = 0;
            finalIndexCount = 0;

            // Convert the VectorImage's serializable vertices to Vertex instances
            int vertexCount = vi.vertices.Length;
            var vertices = new Vertex[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                var v = vi.vertices[i];
                vertices[i] = new Vertex() {
                    position = v.position,
                    tint = v.tint,
                    uv = v.uv,
                    opacityPageSVGSettingIndex = new Color32(0, 0, (byte)(v.settingIndex >> 8), (byte)v.settingIndex)
                };
            }

            if (rectParams.leftSlice <= Mathf.Epsilon &&
                rectParams.topSlice <= Mathf.Epsilon &&
                rectParams.rightSlice <= Mathf.Epsilon &&
                rectParams.bottomSlice <= Mathf.Epsilon)
            {
                MeshBuilder.MakeVectorGraphicsStretchBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, rectParams.uv, rectParams.scaleMode, rectParams.color, settingIndexOffset, meshAlloc, out finalVertexCount, out finalIndexCount);
            }
            else
            {
                var sliceLTRB = new Vector4(rectParams.leftSlice, rectParams.topSlice, rectParams.rightSlice, rectParams.bottomSlice);
                MeshBuilder.MakeVectorGraphics9SliceBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, sliceLTRB, true, rectParams.color, settingIndexOffset, meshAlloc);
            }
        }

        internal static void MakeVectorGraphicsStretchBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Rect sourceUV, ScaleMode scaleMode, Color tint, int settingIndexOffset, AllocMeshData meshAlloc, out int finalVertexCount, out int finalIndexCount)
        {
            // Determine position offset and scale according to scale mode
            Vector2 svgSubRectSize = new Vector2(svgWidth * sourceUV.width, svgHeight * sourceUV.height);
            Vector2 svgSubRectOffset = new Vector2(sourceUV.xMin * svgWidth, sourceUV.yMin * svgHeight);
            Rect svgSubRect = new Rect(svgSubRectOffset, svgSubRectSize);

            bool isSubRect = sourceUV.xMin != 0 || sourceUV.yMin != 0 || sourceUV.width != 1 || sourceUV.height != 1;
            float srcAspect = svgSubRectSize.x / svgSubRectSize.y;
            float destAspect = targetRect.width / targetRect.height;
            Vector2 posOffset, posScale;
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    posOffset = new Vector2(0, 0);
                    posScale.x = targetRect.width / svgSubRectSize.x;
                    posScale.y = targetRect.height / svgSubRectSize.y;
                    break;

                case ScaleMode.ScaleAndCrop:
                    // ScaleAndCrop keeps the content centered to follow the same behavior as textures
                    posOffset = new Vector2(0, 0);
                    if (destAspect > srcAspect)
                    {
                        // Fill on x and crop top/bottom sides
                        posScale.x = posScale.y = targetRect.width / svgSubRectSize.x;
                        var height = targetRect.height / posScale.y;
                        float offset = svgSubRect.height / 2.0f - height / 2.0f;
                        posOffset.y -= offset * posScale.y;
                        svgSubRect.y += offset;
                        svgSubRect.height = height;
                        isSubRect = true;
                    }
                    else if (destAspect < srcAspect)
                    {
                        // Fill on y and crop left/right sides
                        posScale.x = posScale.y = targetRect.height / svgSubRectSize.y;
                        var width = targetRect.width / posScale.x;
                        float offset = svgSubRect.width / 2.0f - width / 2.0f;
                        posOffset.x -= offset * posScale.x;
                        svgSubRect.x += offset;
                        svgSubRect.width = width;
                        isSubRect = true;
                    }
                    else posScale.x = posScale.y = targetRect.width / svgSubRectSize.x; // Just scale, cropping is not involved
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > srcAspect)
                    {
                        // Fill on y and offset on x
                        posScale.x = posScale.y = targetRect.height / svgSubRectSize.y;
                        posOffset.x = (targetRect.width - svgSubRectSize.x * posScale.x) * 0.5f;
                        posOffset.y = 0;
                    }
                    else
                    {
                        // Fill on x and offset on y
                        posScale.x = posScale.y = targetRect.width / svgSubRectSize.x;
                        posOffset.x = 0;
                        posOffset.y = (targetRect.height - svgSubRectSize.y * posScale.y) * 0.5f;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            s_VectorGraphicsStretch.Begin();

            posOffset -= svgSubRectOffset * posScale;

            int newVertexCount = svgVertices.Length;
            int newIndexCount = svgIndices.Length;
            ClipCounts cc = new ClipCounts();
            Vector4 svgSubRectMinMax = Vector4.zero;
            if (isSubRect)
            {
                if (svgSubRect.width <= 0 || svgSubRect.height <= 0)
                {
                    finalVertexCount = finalIndexCount = 0;
                    s_VectorGraphicsStretch.End();
                    return; // Totally clipped
                }
                svgSubRectMinMax = new Vector4(svgSubRect.xMin, svgSubRect.yMin, svgSubRect.xMax, svgSubRect.yMax);
                cc = UpperBoundApproximateRectClippingResults(svgVertices, svgIndices, svgSubRectMinMax);

                // We never kill vertices, just triangles.. so the clipper will only cause growth in the vertex count
                newVertexCount += cc.clippedTriangles * 6; // 6 new vertices per clipped triangle
                newIndexCount += cc.addedTriangles * 3;
                newIndexCount -= cc.degenerateTriangles * 3; // We will not add the indices of degenerate triangles, so discount them
            }

            var mwd = meshAlloc.alloc((uint)newVertexCount, (uint)newIndexCount, ref meshAlloc);

            // Copy indices straight. If clipping is involved, perform clipping. This will fill all the indices
            // as well as register some new vertices at the end of the original set of vertices
            if (isSubRect)
                RectClip(svgVertices, svgIndices, svgSubRectMinMax, mwd, cc, ref newVertexCount);
            else mwd.SetAllIndices(svgIndices);

            // Transform all original vertices, vertices generated by clipping will use those directly
            Debug.Assert(mwd.currentVertex == 0);
            var uvRegion = mwd.uvRegion;
            int vertsCount = svgVertices.Length;
            for (int i = 0; i < vertsCount; i++)
            {
                var v = svgVertices[i];
                v.position.x = v.position.x * posScale.x + posOffset.x;
                v.position.y = v.position.y * posScale.y + posOffset.y;
                v.uv.x = v.uv.x * uvRegion.width + uvRegion.xMin;
                v.uv.y = v.uv.y * uvRegion.height + uvRegion.yMin;
                v.tint *= tint;
                uint settingIndex = (uint)(((v.opacityPageSVGSettingIndex.b << 8) | v.opacityPageSVGSettingIndex.a) + settingIndexOffset);
                v.opacityPageSVGSettingIndex.b = (byte)(settingIndex >> 8);
                v.opacityPageSVGSettingIndex.a = (byte)settingIndex;
                mwd.SetNextVertex(v);
            }

            // Transform newely generated vertices as well (if any)
            for (int i = vertsCount; i < newVertexCount; i++)
            {
                var v = mwd.m_Vertices[i];
                v.position.x = v.position.x * posScale.x + posOffset.x;
                v.position.y = v.position.y * posScale.y + posOffset.y;
                v.uv.x = v.uv.x * uvRegion.width + uvRegion.xMin;
                v.uv.y = v.uv.y * uvRegion.height + uvRegion.yMin;
                v.tint *= tint;
                uint settingIndex = (uint)(((v.opacityPageSVGSettingIndex.b << 8) | v.opacityPageSVGSettingIndex.a) + settingIndexOffset);
                v.opacityPageSVGSettingIndex.b = (byte)(settingIndex >> 8);
                v.opacityPageSVGSettingIndex.a = (byte)settingIndex;
                mwd.SetNextVertex(v);
            }

            finalVertexCount = mwd.vertexCount;
            finalIndexCount = mwd.indexCount;

            s_VectorGraphicsStretch.End();
        }

        private static void MakeVectorGraphics9SliceBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Vector4 sliceLTRB, bool stretch, Color tint, int settingIndexOffset, AllocMeshData meshAlloc)
        {
            var mwd = meshAlloc.alloc((uint)svgVertices.Length, (uint)svgIndices.Length, ref meshAlloc);
            mwd.SetAllIndices(svgIndices);

            if (!stretch)
                throw new NotImplementedException("Support for repeating 9-slices is not done yet");

            s_VectorGraphics9Slice.Begin();

            var uvRegion = mwd.uvRegion;
            int vertsCount = svgVertices.Length;
            Vector2 sliceInvSize = new Vector2(1.0f / (svgWidth - sliceLTRB.z - sliceLTRB.x), 1.0f / (svgHeight - sliceLTRB.w - sliceLTRB.y));
            Vector2 stretchAmount = new Vector2(targetRect.width - svgWidth, targetRect.height - svgHeight);
            for (int i = 0; i < vertsCount; i++)
            {
                var v = svgVertices[i];
                Vector2 skinWeight;
                skinWeight.x = Mathf.Clamp01((v.position.x - sliceLTRB.x) * sliceInvSize.x);
                skinWeight.y = Mathf.Clamp01((v.position.y - sliceLTRB.y) * sliceInvSize.y);

                v.position.x += skinWeight.x * stretchAmount.x;
                v.position.y += skinWeight.y * stretchAmount.y;
                v.uv.x = v.uv.x * uvRegion.width + uvRegion.xMin;
                v.uv.y = v.uv.y * uvRegion.height + uvRegion.yMin;
                v.tint *= tint;
                uint settingIndex = (uint)(((v.opacityPageSVGSettingIndex.b << 8) | v.opacityPageSVGSettingIndex.a) + settingIndexOffset);
                v.opacityPageSVGSettingIndex.b = (byte)(settingIndex >> 8);
                v.opacityPageSVGSettingIndex.a = (byte)settingIndex;
                mwd.SetNextVertex(v);
            }

            s_VectorGraphics9Slice.End();
        }

        struct ClipCounts
        {
            public int firstClippedIndex;
            public int firstDegenerateIndex; // After which all triangles are degenerate until the end of the mesh
            public int lastClippedIndex;
            public int clippedTriangles;
            public int addedTriangles;
            public int degenerateTriangles;
        }

        static ClipCounts UpperBoundApproximateRectClippingResults(Vertex[] vertices, UInt16[] indices, Vector4 clipRectMinMax)
        {
            ClipCounts cc = new ClipCounts();
            cc.firstClippedIndex = int.MaxValue;
            cc.firstDegenerateIndex = -1;
            cc.lastClippedIndex = -1;

            int indexCount = indices.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                Vector3 v0 = vertices[indices[i]].position;
                Vector3 v1 = vertices[indices[i + 1]].position;
                Vector3 v2 = vertices[indices[i + 2]].position;

                Vector4 triRectMinMax;
                triRectMinMax.x = v0.x < v1.x ? v0.x : v1.x;
                triRectMinMax.x = triRectMinMax.x < v2.x ? triRectMinMax.x : v2.x;
                triRectMinMax.y = v0.y < v1.y ? v0.y : v1.y;
                triRectMinMax.y = triRectMinMax.y < v2.y ? triRectMinMax.y : v2.y;
                triRectMinMax.z = v0.x > v1.x ? v0.x : v1.x;
                triRectMinMax.z = triRectMinMax.z > v2.x ? triRectMinMax.z : v2.x;
                triRectMinMax.w = v0.y > v1.y ? v0.y : v1.y;
                triRectMinMax.w = triRectMinMax.w > v2.y ? triRectMinMax.w : v2.y;

                // Test if the rect is outside (degenerate triangle), or totally inside (not clipped),
                // else it is _probably_ clipped, but can still be either degenerate or unclipped
                if ((triRectMinMax.x >= clipRectMinMax.x) &&
                    (triRectMinMax.z <= clipRectMinMax.z) &&
                    (triRectMinMax.y >= clipRectMinMax.y) &&
                    (triRectMinMax.w <= clipRectMinMax.w))
                {
                    cc.firstDegenerateIndex = -1;
                    continue; // Not clipped
                }

                cc.firstClippedIndex = cc.firstClippedIndex < i ? cc.firstClippedIndex : i;
                cc.lastClippedIndex = i + 2;
                if ((triRectMinMax.x >= clipRectMinMax.z) ||
                    (triRectMinMax.z <= clipRectMinMax.x) ||
                    (triRectMinMax.y >= clipRectMinMax.w) ||
                    (triRectMinMax.w <= clipRectMinMax.y))
                {
                    cc.firstDegenerateIndex = cc.firstDegenerateIndex == -1 ? i : cc.firstDegenerateIndex;
                    cc.degenerateTriangles++;
                }

                cc.firstDegenerateIndex = -1;
                cc.clippedTriangles++;
                cc.addedTriangles += 4; // Triangles clipping against corners may spawn more triangles
            }
            return cc;
        }

        unsafe static void RectClip(Vertex[] vertices, UInt16[] indices, Vector4 clipRectMinMax, MeshWriteData mwd, ClipCounts cc, ref int newVertexCount)
        {
            int lastEffectiveClippedIndex = cc.lastClippedIndex;
            if (cc.firstDegenerateIndex != -1 && cc.firstDegenerateIndex < lastEffectiveClippedIndex)
                lastEffectiveClippedIndex = cc.firstDegenerateIndex;
            UInt16 nextNewVertex = (UInt16)vertices.Length;

            // Copy all non-clipped indices
            for (int i = 0; i < cc.firstClippedIndex; i++)
                mwd.SetNextIndex(indices[i]);

            // Clipped triangles
            UInt16* it = stackalloc UInt16[3]; // Indices of the triangle
            Vertex* vt = stackalloc Vertex[3]; // Vertices of the triangle
            for (int i = cc.firstClippedIndex; i < lastEffectiveClippedIndex; i += 3)
            {
                it[0] = indices[i];
                it[1] = indices[i + 1];
                it[2] = indices[i + 2];
                vt[0] = vertices[it[0]];
                vt[1] = vertices[it[1]];
                vt[2] = vertices[it[2]];

                Vector4 triRectMinMax;
                triRectMinMax.x = vt[0].position.x < vt[1].position.x ? vt[0].position.x : vt[1].position.x;
                triRectMinMax.x = triRectMinMax.x < vt[2].position.x ? triRectMinMax.x : vt[2].position.x;
                triRectMinMax.y = vt[0].position.y < vt[1].position.y ? vt[0].position.y : vt[1].position.y;
                triRectMinMax.y = triRectMinMax.y < vt[2].position.y ? triRectMinMax.y : vt[2].position.y;
                triRectMinMax.z = vt[0].position.x > vt[1].position.x ? vt[0].position.x : vt[1].position.x;
                triRectMinMax.z = triRectMinMax.z > vt[2].position.x ? triRectMinMax.z : vt[2].position.x;
                triRectMinMax.w = vt[0].position.y > vt[1].position.y ? vt[0].position.y : vt[1].position.y;
                triRectMinMax.w = triRectMinMax.w > vt[2].position.y ? triRectMinMax.w : vt[2].position.y;

                // Test if the rect is outside (degenerate triangle), or totally inside (not clipped),
                // else it is _probably_ clipped, but can still be either degenerate or unclipped
                if ((triRectMinMax.x >= clipRectMinMax.x) &&
                    (triRectMinMax.z <= clipRectMinMax.z) &&
                    (triRectMinMax.y >= clipRectMinMax.y) &&
                    (triRectMinMax.w <= clipRectMinMax.w))
                {
                    // Clean triangle
                    mwd.SetNextIndex(it[0]);
                    mwd.SetNextIndex(it[1]);
                    mwd.SetNextIndex(it[2]);
                    continue;
                }
                if ((triRectMinMax.x >= clipRectMinMax.z) ||
                    (triRectMinMax.z <= clipRectMinMax.x) ||
                    (triRectMinMax.y >= clipRectMinMax.w) ||
                    (triRectMinMax.w <= clipRectMinMax.y))
                {
                    continue;  // Skip this triangle. It is fully clipped.
                }

                // The full shabang
                RectClipTriangle(vt, it, clipRectMinMax, mwd, ref nextNewVertex);
            }

            // Copy remaining non-clipped indices
            int indexCount = indices.Length;
            for (int i = cc.lastClippedIndex + 1; i < indexCount; i++)
                mwd.SetNextIndex(indices[i]);

            newVertexCount = nextNewVertex;
            mwd.m_Vertices = mwd.m_Vertices.Slice(0, newVertexCount);
            mwd.m_Indices = mwd.m_Indices.Slice(0, mwd.currentIndex);
        }

        unsafe static void RectClipTriangle(Vertex* vt, UInt16* it, Vector4 clipRectMinMax, MeshWriteData mwd, ref UInt16 nextNewVertex)
        {
            Vertex* newVerts = stackalloc Vertex[4 + 3 + 6];
            int newVertsCount = 0;

            // First check, add original triangle vertices if they all fall within the clip rect, and early out if they are 3.
            // This hopefully will trap the majority of triangles.
            for (int i = 0; i < 3; i++)
            {
                if ((vt[i].position.x >= clipRectMinMax.x) &&
                    (vt[i].position.y >= clipRectMinMax.y) &&
                    (vt[i].position.x <= clipRectMinMax.z) &&
                    (vt[i].position.y <= clipRectMinMax.w))
                {
                    newVerts[newVertsCount++] = vt[i];
                }
            }

            if (newVertsCount == 3)
            {
                // Entire triangle is contained within the clip rect, just reference the original triangle verts and sayonara
                mwd.SetNextIndex(it[0]);
                mwd.SetNextIndex(it[1]);
                mwd.SetNextIndex(it[2]);
                return;
            }

            // Next, check if any clip rect vertices are within the triangle, and register those
            Vector3 uvwTL = GetVertexBaryCentricCoordinates(vt, clipRectMinMax.x, clipRectMinMax.y);
            Vector3 uvwTR = GetVertexBaryCentricCoordinates(vt, clipRectMinMax.z, clipRectMinMax.y);
            Vector3 uvwBL = GetVertexBaryCentricCoordinates(vt, clipRectMinMax.x, clipRectMinMax.w);
            Vector3 uvwBR = GetVertexBaryCentricCoordinates(vt, clipRectMinMax.z, clipRectMinMax.w);
            const float kEpsilon = 0.0000001f; // Better be safe and have more verts than miss some
            const float kMin = -kEpsilon;
            const float kMax = 1 + kEpsilon;

            if ((uvwTL.x >= kMin && uvwTL.x <= kMax) && (uvwTL.y >= kMin && uvwTL.y <= kMax) && (uvwTL.z >= kMin && uvwTL.z <= kMax))
                newVerts[newVertsCount++] = InterpolateVertexInTriangle(vt, clipRectMinMax.x, clipRectMinMax.y, uvwTL);
            if ((uvwTR.x >= kMin && uvwTR.x <= kMax) && (uvwTR.y >= kMin && uvwTR.y <= kMax) && (uvwTR.z >= kMin && uvwTR.z <= kMax))
                newVerts[newVertsCount++] = InterpolateVertexInTriangle(vt, clipRectMinMax.z, clipRectMinMax.y, uvwTR);
            if ((uvwBL.x >= kMin && uvwBL.x <= kMax) && (uvwBL.y >= kMin && uvwBL.y <= kMax) && (uvwBL.z >= kMin && uvwBL.z <= kMax))
                newVerts[newVertsCount++] = InterpolateVertexInTriangle(vt, clipRectMinMax.x, clipRectMinMax.w, uvwBL);
            if ((uvwBR.x >= kMin && uvwBR.x <= kMax) && (uvwBR.y >= kMin && uvwBR.y <= kMax) && (uvwBR.z >= kMin && uvwBR.z <= kMax))
                newVerts[newVertsCount++] = InterpolateVertexInTriangle(vt, clipRectMinMax.z, clipRectMinMax.w, uvwBR);

            // Next, test triangle edges against rect sides (12 tests)
            float t;
            t = IntersectSegments(vt[0].position.x, vt[0].position.y, vt[1].position.x, vt[1].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.y); // Edge 1 against top side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 0, 1, t);

            t = IntersectSegments(vt[1].position.x, vt[1].position.y, vt[2].position.x, vt[2].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.y); // Edge 2 against top side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 1, 2, t);

            t = IntersectSegments(vt[2].position.x, vt[2].position.y, vt[0].position.x, vt[0].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.y); // Edge 3 against top side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 2, 0, t);


            t = IntersectSegments(vt[0].position.x, vt[0].position.y, vt[1].position.x, vt[1].position.y, clipRectMinMax.z, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.w); // Edge 1 against right side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 0, 1, t);

            t = IntersectSegments(vt[1].position.x, vt[1].position.y, vt[2].position.x, vt[2].position.y, clipRectMinMax.z, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.w); // Edge 2 against right side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 1, 2, t);

            t = IntersectSegments(vt[2].position.x, vt[2].position.y, vt[0].position.x, vt[0].position.y, clipRectMinMax.z, clipRectMinMax.y, clipRectMinMax.z, clipRectMinMax.w); // Edge 3 against right side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 2, 0, t);

            t = IntersectSegments(vt[0].position.x, vt[0].position.y, vt[1].position.x, vt[1].position.y, clipRectMinMax.x, clipRectMinMax.w, clipRectMinMax.z, clipRectMinMax.w); // Edge 1 against bottom side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 0, 1, t);

            t = IntersectSegments(vt[1].position.x, vt[1].position.y, vt[2].position.x, vt[2].position.y, clipRectMinMax.x, clipRectMinMax.w, clipRectMinMax.z, clipRectMinMax.w); // Edge 2 against bottom side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 1, 2, t);

            t = IntersectSegments(vt[2].position.x, vt[2].position.y, vt[0].position.x, vt[0].position.y, clipRectMinMax.x, clipRectMinMax.w, clipRectMinMax.z, clipRectMinMax.w); // Edge 3 against bottom side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 2, 0, t);

            t = IntersectSegments(vt[0].position.x, vt[0].position.y, vt[1].position.x, vt[1].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.x, clipRectMinMax.w); // Edge 1 against left side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 0, 1, t);

            t = IntersectSegments(vt[1].position.x, vt[1].position.y, vt[2].position.x, vt[2].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.x, clipRectMinMax.w); // Edge 2 against left side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 1, 2, t);

            t = IntersectSegments(vt[2].position.x, vt[2].position.y, vt[0].position.x, vt[0].position.y, clipRectMinMax.x, clipRectMinMax.y, clipRectMinMax.x, clipRectMinMax.w); // Edge 3 against left side
            if (t != float.MaxValue)
                newVerts[newVertsCount++] = InterpolateVertexInTriangleEdge(vt, 2, 0, t);

            if (newVertsCount == 0)
                return; // This should be rare. It means the bounding box test intersected but the accurate test found no intersection. It's ok.

            // The first added vertex will be our anchor for the fan

            // All vertices involved in the result are accumulated in newVerts. Calculate angles of vertices with regards to the first vertex to sort against.
            float* vertAngles = stackalloc float[newVertsCount];
            vertAngles[0] = 0; // Doesn't matter, unused
            const float k2PI = Mathf.PI * 2.0f;
            for (int i = 1; i < newVertsCount; i++)
            {
                vertAngles[i] = Mathf.Atan2(newVerts[i].position.y - newVerts[0].position.y, newVerts[i].position.x - newVerts[0].position.x);
                if (vertAngles[i] < 0.0f)
                    vertAngles[i] += k2PI;
            }

            // Sort vertices in angle order
            int* sortedVerts = stackalloc int[newVertsCount];
            sortedVerts[0] = 0;
            uint addedFlag = 0; // Bit field for each vertex, max 32.. definitely enough
            for (int i = 1; i < newVertsCount; i++)
            {
                int minIndex = -1;
                float minAngle = float.MaxValue;
                for (int j = 1; j < newVertsCount; j++)
                {
                    if (((addedFlag & (1 << j)) == 0) && (vertAngles[j] < minAngle))
                    {
                        minAngle = vertAngles[j];
                        minIndex = j;
                    }
                }
                sortedVerts[i] = minIndex;
                addedFlag = addedFlag | (1U << minIndex);
            }

            // Register new vertices
            UInt16 newVerticesIndex = nextNewVertex;
            for (int i = 0; i < newVertsCount; i++)
                mwd.m_Vertices[newVerticesIndex + i] = newVerts[sortedVerts[i]];
            nextNewVertex += (UInt16)newVertsCount;

            // Build a fan, our selection of first edge might be crossing the middle of the tessellated polygon
            // so the fan is not starting from side to side, but from middle to middle. This is why wrapAroundHandled is there.
            int newTriCount = newVertsCount - 2;
            bool wrapAroundHandled = false;
            Vector3 p0 = mwd.m_Vertices[newVerticesIndex].position;
            for (int i = 0; i < newTriCount; i++)
            {
                int index1 = newVerticesIndex + i + 1;
                int index2 = newVerticesIndex + i + 2;
                if (!wrapAroundHandled)
                {
                    float angle1 = vertAngles[sortedVerts[i + 1]];
                    float angle2 = vertAngles[sortedVerts[i + 2]];
                    if (angle2 - angle1 >= Mathf.PI)
                    {
                        index1 = newVerticesIndex + 1;
                        index2 = newVerticesIndex + newVertsCount - 1;
                        wrapAroundHandled = true;
                    }
                }

                Vector3 p1 = mwd.m_Vertices[index1].position;
                Vector3 p2 = mwd.m_Vertices[index2].position;
                Vector3 c = Vector3.Cross(p1 - p0, p2 - p0);

                // Add the indices in the right winding order
                mwd.SetNextIndex((UInt16)(newVerticesIndex));
                if (c.z < 0)
                {
                    mwd.SetNextIndex((UInt16)index2);
                    mwd.SetNextIndex((UInt16)index1);
                }
                else
                {
                    mwd.SetNextIndex((UInt16)index1);
                    mwd.SetNextIndex((UInt16)index2);
                }
            } // For each new triangle
        }

        unsafe static Vector3 GetVertexBaryCentricCoordinates(Vertex* vt, float x, float y)
        {
            // Get barycentric coordinates
            float v0x = vt[1].position.x - vt[0].position.x;
            float v0y = vt[1].position.y - vt[0].position.y;
            float v1x = vt[2].position.x - vt[0].position.x;
            float v1y = vt[2].position.y - vt[0].position.y;
            float v2x = x - vt[0].position.x;
            float v2y = y - vt[0].position.y;
            float d00 = v0x * v0x + v0y * v0y;
            float d01 = v0x * v1x + v0y * v1y;
            float d11 = v1x * v1x + v1y * v1y;
            float d20 = v2x * v0x + v2y * v0y;
            float d21 = v2x * v1x + v2y * v1y;
            float denom = d00 * d11 - d01 * d01;

            Vector3 uvw;
            uvw.y = (d11 * d20 - d01 * d21) / denom;
            uvw.z = (d00 * d21 - d01 * d20) / denom;
            uvw.x = 1.0f - uvw.y - uvw.z;
            return uvw;
        }

        unsafe static Vertex InterpolateVertexInTriangle(Vertex* vt, float x, float y, Vector3 uvw)
        {
            Vertex iv = vt[0];
            iv.position.x = x;
            iv.position.y = y;
            iv.tint = (Color)vt[0].tint * uvw.x + (Color)vt[1].tint * uvw.y + (Color)vt[2].tint * uvw.z;
            iv.uv = vt[0].uv * uvw.x + vt[1].uv * uvw.y + vt[2].uv * uvw.z;
            return iv;
        }

        unsafe static Vertex InterpolateVertexInTriangleEdge(Vertex* vt, int e0, int e1, float t)
        {
            Vertex iv = vt[0];
            iv.position.x = vt[e0].position.x + t * (vt[e1].position.x - vt[e0].position.x);
            iv.position.y = vt[e0].position.y + t * (vt[e1].position.y - vt[e0].position.y);
            iv.tint = Color.LerpUnclamped(vt[e0].tint, vt[e1].tint, t);
            iv.uv = Vector2.LerpUnclamped(vt[e0].uv, vt[e1].uv, t);
            return iv;
        }

        unsafe static float IntersectSegments(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
        {
            float a1 = (ax - dx) * (by - dy) - (ay - dy) * (bx - dx);
            float a2 = (ax - cx) * (by - cy) - (ay - cy) * (bx - cx);
            if (a1 * a2 >= 0)
                return float.MaxValue; // No intersection
            // Return t on AB
            float a3 = (cx - ax) * (dy - ay) - (cy - ay) * (dx - ax);
            float a4 = a3 + a2 - a1;
            if (a3 * a4 >= 0)
                return float.MaxValue; // No intersection
            return a3 / (a3 - a4); // t [0,1] interpolates between A and B respectively as such: p=a+t*(b-a)
        }
    }
}
