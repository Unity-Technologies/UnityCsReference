// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using LibTessDotNet;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>Holds the tessellated Scene geometry and associated data.</summary>
        public class Geometry
        {
            /// <summary>The vertices of the geometry.</summary>
            public Vector2[] Vertices;

            /// <summary>The UV coordinates of the geometry.</summary>
            public Vector2[] UVs;

            /// <summary>The triangle indices of the geometry.</summary>
            public UInt16[] Indices;

            /// <summary>The color of the geometry.</summary>
            public Color Color;

            /// <summary>The world transform of the geometry.</summary>
            public Matrix2D WorldTransform;

            /// <summary>The fill of the geometry. May be null.</summary>
            public IFill Fill;

            /// <summary>The filling transform of the geometry.</summary>
            public Matrix2D FillTransform;

            /// <summary>The unclipped bounds of the geometry.</summary>
            public Rect UnclippedBounds;

            /// <summary>The setting index of the geometry.</summary>
            /// <remarks>
            /// This is used to refer to the proper texture/gradient settings inside the texture atlas.
            /// This should be set to 0 for geometries without texture or gradients.
            /// </remarks>
            public int SettingIndex;
        }

        /// <summary>Tessellates a Scene object into triangles.</summary>
        /// <param name="scene">The scene containing the hierarchy to tessellate</param>
        /// <param name="tessellationOptions">The tessellation options</param>
        /// <param name="nodeOpacities">If provided, the resulting node opacities</param>
        /// <returns>A list of tesselated geometry</returns>
        public static List<Geometry> TessellateScene(Scene scene, TessellationOptions tessellationOptions, Dictionary<SceneNode, float> nodeOpacities = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellateVectorScene");

            VectorClip.ResetClip();
            var geoms = TessellateNodeHierarchyRecursive(scene.Root, tessellationOptions, scene.Root.Transform, 1.0f, nodeOpacities);

            UnityEngine.Profiling.Profiler.EndSample();

            return geoms;
        }

        #pragma warning disable 612, 618 // Silence use of deprecated IDrawable
        private static List<Geometry> TessellateNodeHierarchyRecursive(SceneNode node, TessellationOptions tessellationOptions, Matrix2D worldTransform, float worldOpacity, Dictionary<SceneNode, float> nodeOpacities)
        {
            if (node.Clipper != null)
                VectorClip.PushClip(TraceNodeHierarchyShapes(node.Clipper, tessellationOptions), worldTransform);

            var geoms = new List<Geometry>();

            if (node.Shapes != null)
            {
                foreach (var shape in node.Shapes)
                {
                    bool isConvex = shape.IsConvex && shape.Contours.Length == 1;
                    TessellateShape(shape, geoms, tessellationOptions, isConvex);
                }
            }

            foreach (var g in geoms)
            {
                g.Color.a *= worldOpacity;
                g.WorldTransform = worldTransform;
                g.UnclippedBounds = Bounds(g.Vertices);

                VectorClip.ClipGeometry(g);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var childOpacity = 1.0f;
                    if (nodeOpacities == null || !nodeOpacities.TryGetValue(child, out childOpacity))
                        childOpacity = 1.0f;

                    var transform = worldTransform * child.Transform;
                    var opacity = worldOpacity * childOpacity;
                    var childGeoms = TessellateNodeHierarchyRecursive(child, tessellationOptions, transform, opacity, nodeOpacities);

                    geoms.AddRange(childGeoms);
                }
            }

            if (node.Clipper != null)
                VectorClip.PopClip();

            return geoms;
        }

        internal static List<Vector2[]> TraceNodeHierarchyShapes(SceneNode root, TessellationOptions tessellationOptions)
        {
            var shapes = new List<Vector2[]>();

            foreach (var nodeInfo in WorldTransformedSceneNodes(root, null))
            {
                var node = nodeInfo.Node;

                if (node.Shapes != null)
                {
                    foreach (var shape in node.Shapes)
                    {
                        foreach (var c in shape.Contours)
                        {
                            var tracedShape = VectorUtils.TraceShape(c, shape.PathProps.Stroke, tessellationOptions);
                            if (tracedShape.Length > 0)
                            {
                                var tracedShapeArray = new Vector2[tracedShape.Length];
                                for (int i = 0; i < tracedShape.Length; ++i)
                                    tracedShapeArray[i] = nodeInfo.WorldTransform * tracedShape[i];

                                shapes.Add(tracedShapeArray);
                            }
                        }
                    }
                }
            }

            return shapes;
        }
        #pragma warning restore 612, 618

        private static void TessellateShape(Shape vectorShape, List<Geometry> geoms, TessellationOptions tessellationOptions, bool isConvex)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellateShape");

            // Don't generate any geometry for pattern fills since these are generated from another SceneNode
            if (vectorShape.Fill != null && !(vectorShape.Fill is PatternFill))
            {
                Color shapeColor = Color.white;
                if (vectorShape.Fill is SolidFill)
                    shapeColor = ((SolidFill)vectorShape.Fill).Color;

                shapeColor.a *= vectorShape.Fill.Opacity;

                if (isConvex && vectorShape.Contours.Length == 1)
                {
                    TessellateConvexContour(vectorShape, vectorShape.PathProps.Stroke, shapeColor, geoms, tessellationOptions);
                }
                else
                {
                    TessellateShapeLibTess(vectorShape, shapeColor, geoms, tessellationOptions);
                }
            }

            var stroke = vectorShape.PathProps.Stroke;
            if (stroke != null && stroke.HalfThickness > VectorUtils.Epsilon)
            {
                var strokeFill = stroke.Fill;
                Color strokeColor = Color.white;
                if (strokeFill is SolidFill)
                {
                    strokeColor = ((SolidFill)strokeFill).Color;
                    strokeFill = null;
                }

                foreach (var c in vectorShape.Contours)
                {
                    Vector2[] strokeVerts;
                    UInt16[] strokeIndices;
                    VectorUtils.TessellatePath(c, vectorShape.PathProps, tessellationOptions, out strokeVerts, out strokeIndices);
                    VectorUtils.AdjustWinding(strokeVerts, strokeIndices, VectorUtils.WindingDir.CCW);
                    if (strokeIndices.Length > 0)
                    {
                        geoms.Add(new Geometry() { Vertices = strokeVerts, Indices = strokeIndices, Color = strokeColor, Fill = strokeFill, FillTransform = stroke.FillTransform });
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellateConvexContour(Shape shape, Stroke stroke, Color color, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            if (shape.Contours.Length != 1 || shape.Contours[0].Segments.Length == 0)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TessellateConvexContour");

            // Compute geometric mean
            var contour = shape.Contours[0];
            var mean = Vector2.zero;
            foreach (var seg in contour.Segments)
                mean += seg.P0;
            mean /= contour.Segments.Length;

            // Trace the shape and build triangle fan
            var tracedShape = VectorUtils.TraceShape(contour, stroke, tessellationOptions);
            var vertices = new Vector2[tracedShape.Length + 1];
            var indices = new UInt16[tracedShape.Length * 3];

            vertices[0] = mean;
            for (int i = 0; i < tracedShape.Length; ++i)
            {
                vertices[i + 1] = tracedShape[i];
                indices[i * 3] = 0;
                indices[i * 3 + 1] = (UInt16)(i + 1);
                indices[i * 3 + 2] = ((i + 2) >= vertices.Length) ? (UInt16)1 : (UInt16)(i + 2);
            }

            geoms.Add(new Geometry() { Vertices = vertices, Indices = indices, Color = color, Fill = shape.Fill, FillTransform = shape.FillTransform });

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellateShapeLibTess(Shape vectorShape, Color color, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            UnityEngine.Profiling.Profiler.BeginSample("LibTess");

            var tess = new Tess();

            var angle = 45.0f * Mathf.Deg2Rad;
            var mat = Matrix2D.RotateLH(angle);
            var invMat = Matrix2D.RotateLH(-angle);

            foreach (var c in vectorShape.Contours)
            {
                var contour = new List<Vector2>(100);
                foreach (var v in VectorUtils.TraceShape(c, vectorShape.PathProps.Stroke, tessellationOptions))
                    contour.Add(mat.MultiplyPoint(v));

                var contourArray = new ContourVertex[contour.Count];
                for (int i = 0; i < contour.Count; ++i)
                {
                    var v = contour[i];
                    contourArray[i] = new ContourVertex() { Position = new Vec3() { X = v.x, Y = v.y } };
                }

                tess.AddContour(contourArray, ContourOrientation.Original);
            }

            var windingRule = (vectorShape.Fill.Mode == FillMode.OddEven) ? WindingRule.EvenOdd : WindingRule.NonZero;
            try
            {
                tess.Tessellate(windingRule, ElementType.Polygons, 3);
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Shape tessellation failed, skipping...");
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var indices = new UInt16[tess.Elements.Length];
            for (int i = 0; i < tess.Elements.Length; ++i)
                indices[i] = (UInt16)tess.Elements[i];

            var vertices = new Vector2[tess.Vertices.Length];
            for (int i = 0; i < tess.Vertices.Length; ++i)
            {
                var cv = tess.Vertices[i];
                vertices[i] = invMat.MultiplyPoint(new Vector2(cv.Position.X, cv.Position.Y));
            }

            if (indices.Length > 0)
            {
                geoms.Add(new Geometry() { Vertices = vertices, Indices = indices, Color = color, Fill = vectorShape.Fill, FillTransform = vectorShape.FillTransform });
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        internal static Vector2[] GenerateShapeUVs(Vector2[] verts, Rect bounds, Matrix2D uvTransform)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateShapeUVs");

            uvTransform =
                Matrix2D.Translate(new Vector2(0, 1)) * Matrix2D.Scale(new Vector2(1.0f, -1.0f)) * // Do 1-uv.y
                uvTransform *
                Matrix2D.Scale(new Vector2(1.0f / bounds.width, 1.0f / bounds.height)) * Matrix2D.Translate(-bounds.position);
            var uvs = new Vector2[verts.Length];
            int vertCount = verts.Length;
            for (int i = 0; i < vertCount; i++)
                uvs[i] = uvTransform * verts[i];

            UnityEngine.Profiling.Profiler.EndSample();

            return uvs;
        }

        static void SwapXY(ref Vector2 v)
        {
            float t = v.x;
            v.x = v.y;
            v.y = t;
        }

        internal struct RawTexture
        {
            public Color32[] Rgba;
            public int Width;
            public int Height;
        }

        class AtlasEntry
        {
            public RawTexture Texture;
            public PackRectItem AtlasLocation;
        }

        /// <summary>A struct to hold packed atlas entries.</summary>
        public class TextureAtlas
        {
            /// <summary>The texture atlas.</summary>
            public Texture2D Texture { get; set; }

            /// <summary>The atlas entries.</summary>
            public List<PackRectItem> Entries { get; set; }
        };

        /// <summary>Generates a Texture2D atlas containing the textures and gradients for the vector geometry, and fill the UVs of the geometry.</summary>
        /// <param name="geoms">The list of Geometry objects, probably created with TessellateNodeHierarchy</param>
        /// <param name="rasterSize">Maximum size of the generated texture</param>
        /// <returns>The generated texture atlas</returns>
        public static TextureAtlas GenerateAtlasAndFillUVs(IEnumerable<Geometry> geoms, uint rasterSize)
        {
            var atlas = GenerateAtlas(geoms, rasterSize);
            if (atlas != null)
                FillUVs(geoms, atlas);
            return atlas;
        }

        private static int NextPOT(int v)
        {
            if (v <= 0)
                return 0;
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return ++v;
        }

        /// <summary>Generates a Texture2D atlas containing the textures and gradients for the vector geometry.</summary>
        /// <param name="geoms">The list of Geometry objects, probably created with TessellateNodeHierarchy</param>
        /// <param name="rasterSize">Maximum size of the generated texture</param>
        /// <param name="generatePOTTexture">Resize the texture to the next power-of-two</param>
        /// <param name="encodeSettings">Encode the gradient settings inside the texture</param>
        /// <param name="linear">If true, the texture will be created in linear colorspace</param>
        /// <returns>The generated texture atlas</returns>
        public static TextureAtlas GenerateAtlas(IEnumerable<Geometry> geoms, uint rasterSize, bool generatePOTTexture = true, bool encodeSettings = true, bool linear = true)
        {
            var fills = new Dictionary<IFill, AtlasEntry>();
            int texturedGeomCount = 0;
            foreach (var g in geoms)
            {
                RawTexture tex;
                if (g.Fill is GradientFill)
                {
                    tex = new RawTexture() { Width = (int)rasterSize, Height = 1, Rgba = RasterizeGradientStripe((GradientFill)g.Fill, (int)rasterSize) };
                    ++texturedGeomCount;
                }
                else if (g.Fill is TextureFill)
                {
                    var fillTex = ((TextureFill)g.Fill).Texture;
                    tex = new RawTexture() { Rgba = fillTex.GetPixels32(), Width = fillTex.width, Height = fillTex.height };
                    ++texturedGeomCount;
                }
                else
                {
                    continue;
                }
                fills[g.Fill] = new AtlasEntry() { Texture = tex };
            }

            if (fills.Count == 0)
                return null;

            UnityEngine.Profiling.Profiler.BeginSample("GenerateAtlas");

            Vector2 atlasSize;

            var rectsToPack = new List<KeyValuePair<IFill, Vector2>>(fills.Count);
            foreach (var fill in fills)
                rectsToPack.Add(new KeyValuePair<IFill, Vector2>(fill.Key, new Vector2(fill.Value.Texture.Width, fill.Value.Texture.Height)));

            rectsToPack.Add(new KeyValuePair<IFill, Vector2>(null, new Vector2(2, 2))); // White fill
            var pack = PackRects(rectsToPack, out atlasSize);
            
            if (encodeSettings)
            {
                // The first row/cols of the atlas is reserved for the gradient settings
                for (int packIndex = 0; packIndex < pack.Count; ++packIndex)
                {
                    var item = pack[packIndex];
                    item.Position.x += 3;
                    pack[packIndex] = item;
                }
                atlasSize.x += 3;
            }

            // Need enough space on first 3 columns for texture settings
            int maxSettingIndex = 0;
            foreach (var item in pack)
                maxSettingIndex = Math.Max(maxSettingIndex, item.SettingIndex);
            int minWidth = encodeSettings ? 3 : 0;
            int minHeight = encodeSettings ? (maxSettingIndex + 1) : maxSettingIndex;
            atlasSize.x = Math.Max(minWidth, (int)atlasSize.x);
            atlasSize.y = Math.Max(minHeight, (int)atlasSize.y);

            int atlasWidth = (int)atlasSize.x;
            int atlasHeight = (int)atlasSize.y;
            if (generatePOTTexture)
            {
                atlasWidth = NextPOT(atlasWidth);
                atlasHeight = NextPOT(atlasHeight);
            }

            var atlasColors = new Color32[atlasWidth * atlasHeight];
            for (int k = 0; k < atlasWidth * atlasHeight; ++k)
                atlasColors[k] = Color.black;
            Vector2 atlasInvSize = new Vector2(1.0f / (float)atlasWidth, 1.0f / (float)atlasHeight);
            Vector2 whiteTexelsScreenPos = pack[pack.Count - 1].Position;

            int i = 0;
            RawTexture rawAtlasTex = new RawTexture() { Rgba = atlasColors, Width = atlasWidth, Height = atlasHeight };
            foreach (var entry in fills.Values)
            {
                var packItem = pack[i++];
                entry.AtlasLocation = packItem;
                BlitRawTexture(entry.Texture, rawAtlasTex, (int)packItem.Position.x, (int)packItem.Position.y, packItem.Rotated);
            }

            RawTexture whiteTex = new RawTexture() { Width = 2, Height = 2, Rgba = new Color32[4] };
            for (i = 0; i < whiteTex.Rgba.Length; i++)
                whiteTex.Rgba[i] = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            BlitRawTexture(whiteTex, rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, false);

            if (encodeSettings)
                EncodeSettings(geoms, fills, rawAtlasTex, whiteTexelsScreenPos);

            var atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false, linear);
            atlasTex.wrapModeU = TextureWrapMode.Clamp;
            atlasTex.wrapModeV = TextureWrapMode.Clamp;
            atlasTex.wrapModeW = TextureWrapMode.Clamp;
            atlasTex.SetPixels32(atlasColors);
            atlasTex.Apply(false, true);

            UnityEngine.Profiling.Profiler.EndSample();

            return new TextureAtlas() { Texture = atlasTex, Entries = pack };
        }

        private static void EncodeSettings(IEnumerable<Geometry> geoms, Dictionary<IFill, AtlasEntry> fills, RawTexture rawAtlasTex, Vector2 whiteTexelsScreenPos)
        {
            // Setting 0 is reserved for the white texel
            WriteRawFloat4Packed(rawAtlasTex, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0);
            WriteRawInt2Packed(rawAtlasTex, (int)whiteTexelsScreenPos.x+1, (int)whiteTexelsScreenPos.y+1, 1, 0);
            WriteRawInt2Packed(rawAtlasTex, 0, 0, 2, 0);

            var writtenSettings = new HashSet<int>();
            writtenSettings.Add(0);

            foreach (var g in geoms)
            {
                AtlasEntry entry;
                int vertsCount = g.Vertices.Length;
                if ((g.Fill != null) && fills.TryGetValue(g.Fill, out entry))
                {
                    int setting = entry.AtlasLocation.SettingIndex;
                    if (writtenSettings.Contains(setting))
                        continue;

                    writtenSettings.Add(setting);

                    // There are 3 consecutive pixels to store the settings
                    int destX = 0;
                    int destY = setting;

                    var gradientFill = g.Fill as GradientFill;
                    if (gradientFill != null)
                    {
                        var focus = gradientFill.RadialFocus;
                        focus += Vector2.one;
                        focus /= 2.0f;
                        focus.y = 1.0f - focus.y;

                        WriteRawFloat4Packed(rawAtlasTex, ((float)gradientFill.Type)/255, ((float)gradientFill.Addressing)/255, focus.x, focus.y, destX++, destY);
                    }

                    var textureFill = g.Fill as TextureFill;
                    if (textureFill != null)
                    {
                        WriteRawFloat4Packed(rawAtlasTex, 0.0f, ((float)textureFill.Addressing)/255, 0.0f, 0.0f, destX++, destY);
                    }

                    var pos = entry.AtlasLocation.Position;
                    var size = new Vector2(entry.Texture.Width-1, entry.Texture.Height-1);
                    WriteRawInt2Packed(rawAtlasTex, (int)pos.x, (int)pos.y, destX++, destY);
                    WriteRawInt2Packed(rawAtlasTex, (int)size.x, (int)size.y, destX++, destY);
                }
            }
        }

        /// <summary>Fill the UVs of the geometry using the provided texture atlas.</summary>
        /// <param name="geoms">The geometry that will have its UVs filled</param>
        /// <param name="texAtlas">The texture atlas used for the UV generation</param>
        public static void FillUVs(IEnumerable<Geometry> geoms, TextureAtlas texAtlas)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FillUVs");

            var fills = new Dictionary<IFill, PackRectItem>();
            foreach (var entry in texAtlas.Entries)
            {
                if (entry.Fill != null)
                    fills[entry.Fill] = entry;
            }

            var item = new PackRectItem();
            foreach (var g in geoms)
            {
                int settingIndex = 0;
                if ((g.Fill != null) && fills.TryGetValue(g.Fill, out item))
                    settingIndex = item.SettingIndex;

                g.UVs = GenerateShapeUVs(g.Vertices, g.UnclippedBounds, g.FillTransform);
                g.SettingIndex = settingIndex;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
