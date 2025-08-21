// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>The alignement of the sprite, to determine the location of the pivot.</summary>
        public enum Alignment
        {
            /// <summary>Center alignment.</summary>
            Center = 0,

            /// <summary>Top-left alignment.</summary>
            TopLeft = 1,

            /// <summary>Top-center alignment.</summary>
            TopCenter = 2,

            /// <summary>Top-right alignment.</summary>
            TopRight = 3,

            /// <summary>Left-center alignment.</summary>
            LeftCenter = 4,

            /// <summary>Right-center alignment.</summary>
            RightCenter = 5,

            /// <summary>Bottom-left alignment.</summary>
            BottomLeft = 6,

            /// <summary>Bottom-center alignment.</summary>
            BottomCenter = 7,

            /// <summary>Bottom-right alignment.</summary>
            BottomRight = 8,

            /// <summary>Custom alignment.</summary>
            Custom = 9,

            /// <summary>SVG origin alignment.</summary>
            /// <remarks>
            /// This will use the origin of the SVG document as the origin of the sprite.
            /// </remarks>
            SVGOrigin = 10
        }

        /// <summary>Builds a sprite asset from a scene tessellation.</summary>
        /// <param name="geoms">The list of tessellated Geometry instances</param>
        /// <param name="svgPixelsPerUnit">How many SVG "pixels" map into a Unity unit</param>
        /// <param name="alignment">The position of the sprite origin</param>
        /// <param name="customPivot">If alignment is <see cref="Alignment.Custom"/>, customPivot is used to compute the sprite origin</param>
        /// <param name="gradientResolution">The maximum size of the texture holding gradient data</param>
        /// <param name="flipYAxis">True to have the positive Y axis to go downward.</param>
        /// <returns>A new Sprite containing the provided geometry. The Sprite may have a texture if the geometry has any texture and/or gradients</returns>
        public static Sprite BuildSprite(List<Geometry> geoms, float svgPixelsPerUnit, Alignment alignment, Vector2 customPivot, UInt16 gradientResolution, bool flipYAxis = false)
        {
            return BuildSprite(geoms, Rect.zero, svgPixelsPerUnit, alignment, customPivot, gradientResolution, flipYAxis);
        }

        /// <summary>Builds a sprite asset from a scene tessellation.</summary>
        /// <param name="geoms">The list of tessellated Geometry instances</param>
        /// <param name="rect">The position and size of the sprite geometry</param>
        /// <param name="svgPixelsPerUnit">How many SVG "pixels" map into a Unity unit</param>
        /// <param name="alignment">The position of the sprite origin</param>
        /// <param name="customPivot">If alignment is <see cref="Alignment.Custom"/>, customPivot is used to compute the sprite origin</param>
        /// <param name="gradientResolution">The maximum size of the texture holding gradient data</param>
        /// <param name="flipYAxis">True to have the positive Y axis to go downward.</param>
        /// <returns>A new Sprite containing the provided geometry. The Sprite may have a texture if the geometry has any texture and/or gradients</returns>
        public static Sprite BuildSprite(List<Geometry> geoms, Rect rect, float svgPixelsPerUnit, Alignment alignment, Vector2 customPivot, UInt16 gradientResolution, bool flipYAxis = false)
        {
            // Generate atlas
            var texAtlas = GenerateAtlasAndFillUVs(geoms, gradientResolution);

            List<Vector2> vertices;
            List<UInt16> indices;
            List<Color> colors;
            List<Vector2> uvs;
            List<Vector2> settingIndices;
            FillVertexChannels(geoms, 1.0f, texAtlas != null, out vertices, out indices, out colors, out uvs, out settingIndices, flipYAxis);

            Texture2D texture = texAtlas != null ? texAtlas.Texture : null;

            if (rect == Rect.zero)
            {
                rect = VectorUtils.Bounds(vertices);
                VectorUtils.RealignVerticesInBounds(vertices, rect, flipYAxis);
            }
            else if (flipYAxis)
            {
                VectorUtils.FlipVerticesInBounds(vertices, rect);

                // The provided rect should normally contain the whole geometry, but since VectorUtils.SceneNodeBounds doesn't
                // take the strokes into account, some triangles may appear outside the rect. We clamp the vertices as a workaround for now.
                VectorUtils.ClampVerticesInBounds(vertices, rect);
            }

            var pivot = GetPivot(alignment, customPivot, rect, flipYAxis);

            var sprite = Sprite.Create(rect, pivot, svgPixelsPerUnit, texture);
            sprite.OverrideGeometry(vertices.ToArray(), indices.ToArray());

            if (colors != null)
            {
                var colors32Array = new Color32[colors.Count];
                for (int i = 0; i < colors.Count; ++i)
                    colors32Array[i] = (Color32)colors[i];

                using (var nativeColors = new NativeArray<Color32>(colors32Array, Allocator.Temp))
                    sprite.SetVertexAttribute<Color32>(VertexAttribute.Color, nativeColors);
            }
            if (uvs != null)
            {
                using (var nativeUVs = new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord0, nativeUVs);
                using (var nativeSettingIndices = new NativeArray<Vector2>(settingIndices.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord2, nativeSettingIndices);
            }

            return sprite;
        }

        /// <summary>Fills a mesh geometry from a scene tessellation.</summary>
        /// <param name="mesh">The mesh object to fill</param>
        /// <param name="geoms">The list of tessellated Geometry instances, generated by TessellateNodeHierarchy</param>
        /// <param name="svgPixelsPerUnit">How many SVG "pixels" map into a Unity unit</param>
        /// <param name="flipYAxis">Set to "true" to have the positive Y axis to go downward.</param>
        public static void FillMesh(Mesh mesh, List<Geometry> geoms, float svgPixelsPerUnit, bool flipYAxis = false)
        {
            bool hasUVs = false;
            foreach (var g in geoms)
            {
                if (g.UVs != null)
                {
                    hasUVs = true;
                    break;
                }
            }

            // Generate atlas
            List<Vector2> vertices;
            List<UInt16> indices;
            List<Color> colors;
            List<Vector2> uvs;
            List<Vector2> settingIndices;
            FillVertexChannels(geoms, svgPixelsPerUnit, hasUVs, out vertices, out indices, out colors, out uvs, out settingIndices, flipYAxis);

            if (flipYAxis)
                FlipYAxis(vertices);

            mesh.Clear();

            var verts = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
                verts[i] = (Vector3)vertices[i];

            var inds = new int[indices.Count];
            for (int i = 0; i < indices.Count; ++i)
                inds[i] = indices[i];

            mesh.SetVertices(verts);
            mesh.SetTriangles(inds, 0);

            if (colors != null)
                mesh.SetColors(colors);

            if (uvs != null)
                mesh.SetUVs(0, uvs);
            if (settingIndices  != null)
                mesh.SetUVs(2, settingIndices);
        }

        private static void FlipYAxis(IList<Vector2> vertices)
        {
            var bbox = Bounds(vertices);
            var h = bbox.height;
            for (int i = 0; i < vertices.Count; ++i)
            {
                var v = vertices[i];
                v.y -= bbox.position.y;
                v.y = h - v.y;
                v.y += bbox.position.y;
                vertices[i] = v;
            }
        }

        private static void FillVertexChannels(List<Geometry> geoms, float pixelsPerUnit, bool hasUVs, out List<Vector2> vertices, out List<UInt16> indices, out List<Color> colors, out List<Vector2> uvs, out List<Vector2> settingIndices, bool flipYAxis)
        {
            int totalVerts = 0, totalIndices = 0;
            foreach (var geom in geoms)
            {
                if (geom.Indices.Length != 0)
                {
                    totalIndices += geom.Indices.Length;
                    totalVerts += geom.Vertices.Length;
                }
            }

            vertices = new List<Vector2>(totalVerts);
            indices = new List<UInt16>(totalIndices);
            colors = new List<Color>(totalVerts);
            uvs = hasUVs ? new List<Vector2>(totalVerts) : null;
            settingIndices = hasUVs ? new List<Vector2>(totalVerts) : null;

            foreach (var geom in geoms)
            {
                int indexStart = indices.Count;
                int indexEnd = indexStart + geom.Indices.Length;

                int vertexCount = vertices.Count;

                for (int i = 0; i < geom.Indices.Length; ++i)
                    indices.Add((UInt16)(geom.Indices[i] + vertexCount));

                for (int i = 0; i < geom.Vertices.Length; ++i)
                    vertices.Add((geom.WorldTransform * geom.Vertices[i]) / pixelsPerUnit);

                for (int i = 0; i < geom.Vertices.Length; ++i)
                    colors.Add(geom.Color);

                FlipRangeIfNecessary(vertices, indices, indexStart, indexEnd, flipYAxis);

                System.Diagnostics.Debug.Assert(uvs == null || geom.UVs != null);
                if (uvs != null)
                {
                    uvs.AddRange(geom.UVs);
                    for (int i = 0; i < geom.UVs.Length; i++)
                        settingIndices.Add(new Vector2(geom.SettingIndex, 0));
                }
            }
        }

        internal enum WindingDir
        {
            CW,
            CCW
        }

        internal static void AdjustWinding(Vector2[] vertices, UInt16[] indices, WindingDir dir)
        {
            int indexCount = indices.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i+1];
                var i2 = indices[i+2];
                var v0 = (Vector3)vertices[i0];
                var v1 = (Vector3)vertices[i1];
                var v2 = (Vector3)vertices[i2];

                var s = (v1 - v0);
                var t = (v2 - v0);
                var crossZ = s.x * t.y - s.y * t.x;

                bool shouldFlip = dir == WindingDir.CCW ? crossZ < 0.0f : crossZ > 0.0f;
                if (shouldFlip)
                {
                    var tmp = indices[i];
                    indices[i] = indices[i+1];
                    indices[i+1] = tmp;
                }
            }
        }

        private static void FlipRangeIfNecessary(List<Vector2> vertices, List<UInt16> indices, int indexStart, int indexEnd, bool flipYAxis)
        {
            // For the range, find the first valid triangle and check its winding order. If that triangle needs flipping, then flip the whole range.
            bool shouldFlip = false;
            for (int i = indexStart; i < (indexEnd - 2); i += 3)
            {
                var v0 = (Vector3)vertices[indices[i]];
                var v1 = (Vector3)vertices[indices[i + 1]];
                var v2 = (Vector3)vertices[indices[i + 2]];
                var s = (v1 - v0).normalized;
                var t = (v2 - v0).normalized;
                float dot = Vector3.Dot(s, t);
                if (s == Vector3.zero || t == Vector3.zero || dot > 0.99f || dot < -0.99f)
                    continue;
                var n = Vector3.Cross(s, t);
                if (n.sqrMagnitude < 0.001f)
                    continue;
                shouldFlip = flipYAxis ? n.z < 0.0f : n.z > 0.0f;
                break;
            }
            if (shouldFlip)
            {
                for (int i = indexStart; i < (indexEnd - 2); i += 3)
                {
                    var tmp = indices[i + 1];
                    indices[i + 1] = indices[i + 2];
                    indices[i + 2] = tmp;
                }
            }
        }

        internal static void RenderFromArrays(Vector2[] vertices, UInt16[] indices, Vector2[] uvs, Color[] colors, Vector2[] settings, Texture2D texture, Material mat, bool clear = true)
        {
            mat.SetTexture("_MainTex", texture);
            mat.SetPass(0);

            if (clear)
                GL.Clear(true, true, Color.clear);

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Color(new Color(1, 1, 1, 1));
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < indices.Length; ++i)
            {
                ushort index = indices[i];
                Vector2 vertex = vertices[index];
                Vector2 uv = uvs[index];
                GL.TexCoord2(uv.x, uv.y);
                if (settings != null)
                {
                    var setting = settings[index];
                    GL.MultiTexCoord2(2, setting.x, setting.y);
                }
                if (colors != null)
                    GL.Color(colors[index]);
                GL.Vertex3(vertex.x, vertex.y, 0);
            }
            GL.End();
            GL.PopMatrix();

            mat.SetTexture("_MainTex", null);
        }

        /// <summary>Draws a vector sprite using the provided material.</summary>
        /// <param name="sprite">The sprite to render</param>
        /// <param name="mat">The material used for rendering</param>
        /// <param name="clear">If true, clear the render target before rendering</param>
        public static void RenderSprite(Sprite sprite, Material mat, bool clear = true) 
        {
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;

            var uvs = sprite.uv;
            var triangles = sprite.triangles;
            var pivot = sprite.pivot;

            var vertices = new Vector2[sprite.vertices.Length];
            for (int i = 0; i < sprite.vertices.Length; ++i)
            {
                var v = sprite.vertices[i];
                vertices[i] = new Vector2((v.x * pixelsToUnits + pivot.x) / spriteWidth,
                                           (v.y * pixelsToUnits + pivot.y) / spriteHeight);
            }

            Color[] colors = null;
            if (sprite.HasVertexAttribute(VertexAttribute.Color))
            {
                var colorSlice = sprite.GetVertexAttribute<Color32>(VertexAttribute.Color);
                colors = new Color[colorSlice.Length];
                for (int i = 0; i < colorSlice.Length; ++i)
                    colors[i] = (Color)colorSlice[i];
            }

            Vector2[] settings = null;
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
                settings = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord2).ToArray();

            RenderFromArrays(vertices, sprite.triangles, sprite.uv, colors, settings, sprite.texture, mat, clear);
        }

        private static Material s_ExpandEdgesMat;
        private static Material s_DemulMat;
        private static Material s_BlendMat;

        private static Material CreateMaterialForShaderName(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                return null;
            return new Material(shader);
        }

        /// <summary>Renders a vector sprite to Texture2D.</summary>
        /// <param name="sprite">The sprite to render</param>
        /// <param name="width">The desired width of the resulting texture</param>
        /// <param name="height">The desired height of the resulting texture</param>
        /// <param name="mat">The material used to render the sprite</param>
        /// <param name="antiAliasing">The number of samples per pixel for anti-aliasing</param>
        /// <param name="expandEdges">When true, expand the edges to avoid a dark banding effect caused by filtering. This is slower to render and uses more graphics memory.</param>
        /// <returns>A Texture2D object containing the rendered vector sprite</returns>
        public static Texture2D RenderSpriteToTexture2D(Sprite sprite, int width, int height, Material mat, int antiAliasing = 1, bool expandEdges = false)
        {
            if (width <= 0 || height <= 0)
                return null;

            var oldActive = RenderTexture.active;

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0) {
                msaaSamples = antiAliasing,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };

            var sourceTex = RenderTexture.GetTemporary(desc);
            RenderTexture.active = sourceTex;
            GL.Clear(true, true, Color.clear);
            RenderSprite(sprite, mat);

            // The rendered sprite is in premultipled form, so we need to convert it back to straight alpha
            // before further processing.
            if (s_DemulMat == null)
                s_DemulMat = CreateMaterialForShaderName("Hidden/VectorGraphics/VectorDemultiply");

            desc.msaaSamples = 1;
            var demulTex = RenderTexture.GetTemporary(desc);
            RenderTexture.active = demulTex;
            GL.Clear(true, true, Color.clear);
            Graphics.Blit(sourceTex, demulTex, s_DemulMat);
            RenderTexture.ReleaseTemporary(sourceTex);

            RenderTexture tex = demulTex;

            if (expandEdges)
            {
                // Insteads of keeping the sprite blended over transparent black, we will render it over
                // an expanded version of itself, so that the bilinear filter will interpolate the colors.

                // Expand the edges and make completely transparent
                if (s_ExpandEdgesMat == null)
                    s_ExpandEdgesMat = CreateMaterialForShaderName("Hidden/VectorGraphics/VectorExpandEdges");

                var expandTex = RenderTexture.GetTemporary(desc);
                RenderTexture.active = expandTex;
                GL.Clear(false, true, Color.clear);
                Graphics.Blit(demulTex, expandTex, s_ExpandEdgesMat);

                // Draw the demultiplied sprite again, but over the expanded edges texture.
                // The VectorBlendMax shader uses a "max" blend operation, which will keep the original texture in
                // non-premultiplied alpha.
                if (s_BlendMat == null)
                    s_BlendMat = CreateMaterialForShaderName("Hidden/VectorGraphics/VectorBlendMax");

                Graphics.Blit(demulTex, expandTex, s_BlendMat);
                RenderTexture.ReleaseTemporary(demulTex);

                tex = expandTex;
            }

            RenderTexture.active = tex;

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();

            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(tex);

            return copy;
        }

        internal static Vector2 GetPivot(Alignment alignment, Vector2 customPivot, Rect bbox, bool flipYAxis)
        {
            switch (alignment)
            {
                case Alignment.Center: return new Vector2(0.5f, 0.5f);
                case Alignment.TopLeft: return new Vector2(0.0f, 1.0f);
                case Alignment.TopCenter: return new Vector2(0.5f, 1.0f);
                case Alignment.TopRight: return new Vector2(1.0f, 1.0f);
                case Alignment.LeftCenter: return new Vector2(0.0f, 0.5f);
                case Alignment.RightCenter: return new Vector2(1.0f, 0.5f);
                case Alignment.BottomLeft: return new Vector2(0.0f, 0.0f);
                case Alignment.BottomCenter: return new Vector2(0.5f, 0.0f);
                case Alignment.BottomRight: return new Vector2(1.0f, 0.0f);
                case Alignment.SVGOrigin: 
                {
                     var p = -bbox.position / bbox.size;
                     if (flipYAxis)
                        p.y = 1.0f - p.y;
                    return p;
                }
                case Alignment.Custom: return customPivot;
            }
            return Vector2.zero;
        }
    }
}
