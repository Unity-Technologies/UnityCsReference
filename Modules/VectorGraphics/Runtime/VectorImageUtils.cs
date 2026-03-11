// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.VectorGraphics
{
    [VisibleToOtherModules("UnityEditor.VectorGraphicsModule")]
    internal static class VectorImageUtils
    {
        [VisibleToOtherModules("UnityEditor.VectorGraphicsModule")]
        internal static void MakeVectorImageAsset(IEnumerable<VectorUtils.Geometry> geoms, Rect rect, uint rasterSize, out VectorImage outAsset, out Texture2D outTexAtlas)
        {
            var atlas = VectorUtils.GenerateAtlas(geoms, rasterSize, false, false, false);
            if (atlas != null)
                VectorUtils.FillUVs(geoms, atlas);

            bool hasTexture = atlas != null && atlas.Texture != null;
            outTexAtlas = hasTexture ? atlas.Texture : null;

            var vertices = new List<VectorImageVertex>(100);
            var indices = new List<UInt16>(300);
            var settings = new List<GradientSettings>();

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var geom in geoms)
            {
                if (geom.Vertices.Length == 0)
                    continue;

                var transformed = new Vector2[geom.Vertices.Length];
                for (int i = 0; i < geom.Vertices.Length; ++i)
                {
                    var v = geom.WorldTransform.MultiplyPoint(geom.Vertices[i]);
                    transformed[i] = v;
                }
                var b = VectorUtils.Bounds(transformed);
                min = Vector2.Min(min, b.min);
                max = Vector2.Max(max, b.max);
            }
            var bounds = Rect.zero;
            if (min.x != float.MaxValue)
                bounds = new Rect(min, max-min);

            // Save written settings to avoid duplicates
            var writtenSettings = new HashSet<int>();
            writtenSettings.Add(0);

            // Create a map of filling -> atlas entry
            var fillEntries = new Dictionary<IFill, VectorUtils.PackRectItem>();
            if (atlas != null && atlas.Entries != null)
            {
                foreach (var entry in atlas.Entries)
                {
                    if (entry.Fill != null)
                        fillEntries[entry.Fill] = entry;
                }
            }

            if (hasTexture && atlas != null && atlas.Entries != null && atlas.Entries.Count > 0)
            {
                // Write the 'white' texel info
                var entry = atlas.Entries[atlas.Entries.Count-1];
                settings.Add(new GradientSettings() {
                    gradientType = GradientType.Linear,
                    addressMode = UnityEngine.UIElements.AddressMode.Wrap,
                    radialFocus = Vector2.zero,
                    location = new RectInt((int)entry.Position.x, (int)entry.Position.y, (int)entry.Size.x, (int)entry.Size.y)
                });
            }

            foreach (var geom in geoms)
            {
                for (int i = 0; i < geom.Vertices.Length; ++i)
                {
                    var v = geom.WorldTransform.MultiplyPoint(geom.Vertices[i]);
                    v -= bounds.position;
                    geom.Vertices[i] = v;
                }

                VectorUtils.AdjustWinding(geom.Vertices, geom.Indices, VectorUtils.WindingDir.CCW);

                var count = vertices.Count;
                for (int i = 0; i < geom.Vertices.Length; ++i)
                {
                    Vector3 p = (Vector3)geom.Vertices[i];
                    p.z = Vertex.nearZ;
                    vertices.Add(new VectorImageVertex() {
                        position = p,
                        uv = hasTexture ? geom.UVs[i] : Vector2.zero,
                        tint = geom.Color,
                        settingIndex = (uint)geom.SettingIndex
                    });
                }

                var indicesPlusOffset = new UInt16[geom.Indices.Length];
                for (int i = 0; i < geom.Indices.Length; ++i)
                    indicesPlusOffset[i] = (UInt16)(geom.Indices[i] + count);

                indices.AddRange(indicesPlusOffset);

                if (atlas != null && atlas.Entries != null && atlas.Entries.Count > 0)
                {
                    VectorUtils.PackRectItem entry;
                    if (geom.Fill == null || !fillEntries.TryGetValue(geom.Fill, out entry) || writtenSettings.Contains(entry.SettingIndex))
                        continue;

                    writtenSettings.Add(entry.SettingIndex);

                    var gradientType = GradientFillType.Linear;
                    var radialFocus = Vector2.zero;
                    var addressMode = AddressMode.Wrap;

                    var gradientFill = geom.Fill as GradientFill;
                    if (gradientFill != null)
                    {
                        gradientType = gradientFill.Type;
                        radialFocus = gradientFill.RadialFocus;
                        addressMode = gradientFill.Addressing;
                    }

                    var textureFill= geom.Fill as TextureFill;
                    if (textureFill != null)
                        addressMode = textureFill.Addressing;

                    settings.Add(new GradientSettings() {
                        gradientType = (GradientType)gradientType,
                        addressMode = (UnityEngine.UIElements.AddressMode)addressMode,
                        radialFocus = radialFocus,
                        location = new RectInt((int)entry.Position.x, (int)entry.Position.y, (int)entry.Size.x, (int)entry.Size.y)
                    });
                }
            }

            if (rect == Rect.zero)
                rect = bounds;
            else
            {
                var offset = bounds.position - rect.position;

                for (int i = 0; i < vertices.Count; ++i)
                {
                    var v = vertices[i];
                    var p = (Vector2)v.position;

                    // Apply offset
                    p += offset;

                    // Clamp
                    p = Vector2.Max(rect.min, Vector2.Min(rect.max, p));

                    v.position = new Vector3(p.x, p.y, v.position.z);
                    vertices[i] = v;
                }
            }

            outAsset = MakeVectorImageAsset(vertices, indices, outTexAtlas, settings, rect);
        }

        [VisibleToOtherModules("UnityEditor.VectorGraphicsModule")]
        internal static Texture2D RenderVectorImageToTexture2D(VectorImage vi, int width, int height, int antiAliasing = 1)
        {
            if (vi == null)
                return null;

            if (width <= 0 || height <= 0)
                return null;

            RenderTexture rt = null;
            var oldActive = RenderTexture.active;

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0) {
                msaaSamples = antiAliasing,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };

            rt = RenderTexture.GetTemporary(desc);
            RenderTexture.active = rt;

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.clearColor = true;
            panelSettings.clearDepthStencil = true;
            panelSettings.targetTexture = rt;

            GL.PushMatrix();

            var panel = panelSettings.panel;
            var root = panel.visualTree;
            root.StretchToParentSize();
            root.style.backgroundImage = new StyleBackground(vi);
            panel.Repaint(Event.current);
            panel.Render();

            GL.PopMatrix();

            ScriptableObject.DestroyImmediate(panelSettings);

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();

            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(rt);

            return copy;
        }

        private static Texture2D BuildAtlasWithEncodedSettings(GradientSettings[] settings, Texture2D atlas)
        {
            var oldActive = RenderTexture.active;

            int width = atlas.width + 3;
            int height = Math.Max(settings.Length, atlas.height);

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0) {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };
            var rt = RenderTexture.GetTemporary(desc);
            GL.Clear(false, true, Color.black, 1.0f);
            Graphics.Blit(atlas, rt, Vector2.one, new Vector2(-3.0f/width, 0.0f));
            RenderTexture.active = rt;

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            // This encoding procedure is duplicated a few times, do something about it
            var rawSettingsTex = new VectorUtils.RawTexture() {
                Width = 3, Height = settings.Length,
                Rgba = new Color32[3*settings.Length]            };
            for (int i = 0; i < settings.Length; ++i)
            {
                var g = settings[i];

                // There are 3 consecutive pixels to store the settings
                int destX = 0;
                int destY = i;

                if (g.gradientType == GradientType.Radial)
                {
                    var focus = g.radialFocus;
                    focus += Vector2.one;
                    focus /= 2.0f;
                    focus.y = 1.0f - focus.y;

                    VectorUtils.WriteRawFloat4Packed(rawSettingsTex, ((float)g.gradientType)/255, ((float)g.addressMode)/255, focus.x, focus.y, destX++, destY);
                }
                else
                {
                    VectorUtils.WriteRawFloat4Packed(rawSettingsTex, 0.0f, ((float)g.addressMode)/255, 0.0f, 0.0f, destX++, destY);
                }

                var pos = g.location.position;
                var size = g.location.size;
                size.x -= 1;
                size.y -= 1;
                VectorUtils.WriteRawInt2Packed(rawSettingsTex, (int)pos.x+3, (int)pos.y, destX++, destY);
                VectorUtils.WriteRawInt2Packed(rawSettingsTex, (int)size.x, (int)size.y, destX++, destY);
            }

            copy.SetPixels32(0, 0, 3, settings.Length, rawSettingsTex.Rgba, 0);
            copy.Apply();

            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(rt);

            return copy;
        }

        [VisibleToOtherModules("UnityEditor.VectorGraphicsModule")]
        internal static VectorImage MakeVectorImageAsset(List<VectorImageVertex> vertices, List<UInt16> indices, Texture2D atlas, List<GradientSettings> settings, Rect rect)
        {
            var vectorImage = ScriptableObject.CreateInstance<VectorImage>();
            vectorImage.vertices = vertices.ToArray();
            vectorImage.indices = indices.ToArray();
            vectorImage.atlas = atlas;
            vectorImage.settings = settings.ToArray();
            vectorImage.size = rect.size;
            return vectorImage;
        }
    }

    public static partial class VectorUtils
    {
        /// <summary>Builds a sprite asset from a scene tessellation.</summary>
        /// <param name="geoms">The list of tessellated Geometry instances</param>
        /// <param name="gradientResolution">The maximum size of the texture holding gradient data</param>
        /// <returns>
        /// A new VectorImage containing the provided geometry.
        /// The VectorImage may have a texture if the geometry has any texture and/or gradients
        /// </returns>
        /// <remarks>
        /// It is the caller's responsibility to destroy the returned VectorImage using <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/>.
        /// </remarks>
        public static VectorImage BuildVectorImage(IEnumerable<Geometry> geoms, uint gradientResolution = 16)
        {
            return BuildVectorImage(geoms, Rect.zero, gradientResolution);
        }

        [VisibleToOtherModules("UnityEditor.VectorGraphicsModule")]
        internal static VectorImage BuildVectorImage(IEnumerable<Geometry> geoms, Rect rect, uint gradientResolution)
        {
            VectorImageUtils.MakeVectorImageAsset(geoms, rect, gradientResolution, out var asset, out _);
            return asset;
        }

        /// <summary>Builds an antialiased VectorImage from a vector scene definition.</summary>
        /// <param name="sceneInfo">The <see cref="SVGParser.SceneInfo"/> to build the VectorImage from.</param>
        /// <returns>A <see cref="VectorImage"/> constructed from the vector scene definition.</returns>
        public static VectorImage BuildVectorImage(SVGParser.SceneInfo sceneInfo)
        {
            using (var p = new Painter2D())
            {
                var root = sceneInfo.Scene.Root;
                DrawSceneWithPainter2D(root, root.Transform, p, 1.0f, sceneInfo.NodeOpacity);

                var result = ScriptableObject.CreateInstance<VectorImage>();
                p.SaveToVectorImage(result);

                return result;
            }
        }

        static void DrawSceneWithPainter2D(SceneNode node, Matrix2D matrix, Painter2D painter, float combinedOpacity, Dictionary<SceneNode, float> nodeOpacities)
        {
            if (node == null)
                return;

            if (node.Shapes != null)
            {
                foreach (var shape in node.Shapes)
                {
                    if (shape.Contours == null || shape.Contours.Length == 0)
                        break;

                    painter.opacity = combinedOpacity;

                    painter.BeginPath();

                    foreach (var contour in shape.Contours)
                    {
                        var segments = contour.Segments;
                        if (segments == null || segments.Length == 0)
                            break;

                        painter.MoveTo(matrix.MultiplyPoint(segments[0].P0));

                        for (int i = 0; i < (segments.Length - 1); ++i)
                        {
                            var seg0 = segments[i];
                            var seg1 = segments[i + 1];
                            painter.BezierCurveTo(matrix.MultiplyPoint(seg0.P1), matrix.MultiplyPoint(seg0.P2), matrix.MultiplyPoint(seg1.P0));
                        }

                        if (contour.Closed)
                        {
                            var seg0 = segments[segments.Length - 1];
                            var seg1 = segments[0];
                            painter.BezierCurveTo(matrix.MultiplyPoint(seg0.P1), matrix.MultiplyPoint(seg0.P2), matrix.MultiplyPoint(seg1.P0));
                            painter.ClosePath();
                        }
                    }

                    if (shape.Fill != null)
                    {
                        var solidFill = shape.Fill as SolidFill;
                        if (solidFill != null)
                        {
                            painter.fillColor = solidFill.Color;
                            painter.Fill(solidFill.Mode == FillMode.NonZero ? FillRule.NonZero : FillRule.OddEven);
                        }

                        var gradientFill = shape.Fill as GradientFill;
                        if (gradientFill != null)
                        {
                            ComputeFillGradientFromGradientFill(node, shape, gradientFill, out var fillGradient, out var fillTransform);
                            painter.fillGradient = fillGradient;
                            // Important to set after fillGradient, as setting fillGradient resets the transform
                            painter.fillTransform = fillTransform.ToMatrix4x4();
                            painter.Fill(gradientFill.Mode == FillMode.NonZero ? FillRule.NonZero : FillRule.OddEven);
                        }
                    }

                    if (shape.PathProps.Stroke != null)
                    {
                        var pathProps = shape.PathProps;
                        var stroke = pathProps.Stroke;

                        var gradientFill = stroke.Fill as GradientFill;
                        if (gradientFill != null)
                        {
                            ComputeFillGradientFromGradientFill(node, shape, gradientFill, out var fillGradient, out var fillTransform);
                            painter.strokeFillGradient = fillGradient;
                            painter.fillTransform = fillTransform.ToMatrix4x4();
                        }
                        else
                        {
                            painter.strokeColor = stroke.Color;
                        }

                        painter.lineWidth = stroke.HalfThickness * 2.0f;
                        painter.lineJoin = pathProps.Corners == PathCorner.Tipped ? LineJoin.Miter : (pathProps.Corners == PathCorner.Beveled ? LineJoin.Bevel : LineJoin.Round);
                        painter.lineCap = (pathProps.Head == PathEnding.Round || pathProps.Tail == PathEnding.Round) ? LineCap.Round : LineCap.Butt;
                        painter.SetDashPattern(stroke.Pattern);
                        painter.dashOffset = stroke.PatternOffset;

                        painter.Stroke();
                    }
                }
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    float childOpacity = 1.0f;
                    if (nodeOpacities == null || !nodeOpacities.TryGetValue(child, out childOpacity))
                        childOpacity = 1.0f;

                    float opacity = combinedOpacity * childOpacity;
                    var transform = matrix * child.Transform;

                    DrawSceneWithPainter2D(child, transform, painter, opacity, nodeOpacities);
                }
            }
        }

        static void ComputeFillGradientFromGradientFill(SceneNode node, Shape shape, GradientFill gradientFill, out FillGradient outFillGradient, out Matrix2D outFillTransform)
        {
            var bounds = VectorUtils.SceneNodeBounds(node);

            // Compute fill transform to match SVG importer's behavior
            outFillTransform =
                Matrix2D.Translate(new Vector2(0, 1)) * Matrix2D.Scale(new Vector2(1.0f, -1.0f)) * // Do 1-uv.y
                shape.FillTransform *
                Matrix2D.Scale(new Vector2(1.0f / bounds.width, 1.0f / bounds.height)) * Matrix2D.Translate(-bounds.position);

            // Convert gradients
            var addressMode = UnityEngine.UIElements.AddressMode.Mirror;
            if (gradientFill.Addressing == AddressMode.Wrap)
                addressMode = UnityEngine.UIElements.AddressMode.Wrap;
            else if (gradientFill.Addressing == AddressMode.Clamp)
                addressMode = UnityEngine.UIElements.AddressMode.Clamp;

            var g = new Gradient();
            var colorKeys = new GradientColorKey[gradientFill.Stops.Length];
            var alphaKeys = new GradientAlphaKey[gradientFill.Stops.Length];

            for (int i = 0; i < gradientFill.Stops.Length; ++i)
            {
                var stop = gradientFill.Stops[i];
                colorKeys[i] = new GradientColorKey() { color = stop.Color, time = stop.StopPercentage };
                alphaKeys[i] = new GradientAlphaKey() { alpha = stop.Color.a, time = stop.StopPercentage };
            }

            g.colorKeys = colorKeys;
            g.alphaKeys = alphaKeys;

            // Always construct a linear gradient, as the fill transform will take care of
            // properly apply both linear gradients direction and radial gradients.
            var start = bounds.position;
            var end = start + (Vector2.right * bounds.size);
            outFillGradient = FillGradient.MakeLinearGradient(g, start, end, addressMode);

            if (gradientFill.Type == GradientFillType.Radial)
            {
                outFillGradient.gradientType = GradientType.Radial;
                outFillGradient.focus = gradientFill.RadialFocus;
            }
        }
    }
}
