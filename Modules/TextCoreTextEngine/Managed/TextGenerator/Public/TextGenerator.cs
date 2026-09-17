// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;

namespace UnityEngine.TextCore.Generation
{
    /// <summary>
    /// Generates renderable text geometry using the Advanced Text Generator (ATG).
    /// </summary>
    public static class TextGenerator
    {
        const int k_VertexPadding = 6;

        [NoAutoStaticsCleanup]
        static TextLib s_TextLib;
        [NoAutoStaticsCleanup]
        static TextSettings s_DefaultTextSettings;

        /// <summary>
        /// Generates the renderable geometry for the given settings.
        /// </summary>
        /// <param name="settings">The description of the text and layout to generate.</param>
        /// <returns>
        /// The generated geometry; fill a Mesh with <see cref="TextMesh.FillMesh"/> and render its
        /// sub-meshes with <see cref="TextMesh.materials"/>.
        /// </returns>
        public static TextMesh GenerateText(TextGenerationSettings settings)
        {
            IntPtr textGenerationInfo = TextGenerationInfo.Create(isPermanent: true);
            try
            {
                TryGenerate(settings, textGenerationInfo, out var mesh);
                return mesh;
            }
            finally
            {
                TextGenerationInfo.Destroy(textGenerationInfo);
            }
        }

        static bool TryGenerate(TextGenerationSettings settings, IntPtr textGenerationInfo, out TextMesh mesh)
        {
            mesh = default;

            if (!TryResolveGeneration(settings, out var textSettings, out var fontAsset, out var textLib))
                return false;

            var nativeSettings = BuildNativeSettings(settings, textSettings, fontAsset);
            var nativeTextInfo = GenerateNative(textLib, textSettings, ref nativeSettings, settings.text, textGenerationInfo, out _);
            mesh = BuildMesh(textLib, nativeTextInfo, nativeSettings);
            return true;
        }

        static bool TryResolveGeneration(in TextGenerationSettings settings, out TextSettings textSettings, out FontAsset fontAsset, out TextLib textLib)
        {
            fontAsset = null;
            textLib = null;

            textSettings = GetDefaultTextSettings();
            if (textSettings == null)
                return false;

            // The public API takes only a Font (keeping FontAsset off the surface ahead of the font
            // unification); the generator converts it to a cached font asset under the hood. Like the
            // legacy generator (TextGenerationError.NoFont), no font means no generation.
            if (settings.font == null)
                return false;
            fontAsset = textSettings.GetCachedFontAsset(settings.font, isIMGUI: false);
            if (fontAsset == null)
            {
                Debug.LogWarning("TextGenerator: could not resolve a font asset for the provided font.");
                return false;
            }

            textLib = GetTextLib();
            return true;
        }

        static NativeTextGenerationSettings BuildNativeSettings(TextGenerationSettings settings, TextSettings textSettings, FontAsset fontAsset)
        {
            var nativeSettings = NativeTextGenerationSettings.Default;
            nativeSettings.fontAsset = fontAsset.nativeFontAsset;
            nativeSettings.textSettings = textSettings.nativeTextSettings;

            float fontSize = settings.fontSize;
            if (fontSize <= 0f)
                fontSize = settings.font.fontSize > 0 ? settings.font.fontSize : fontAsset.faceInfo.pointSize;
            nativeSettings.fontSize = ToFixedPoint(fontSize);
            nativeSettings.color = settings.color;

            // Bold is expressed through the font weight only, never through the style flags.
            bool bold = (settings.fontStyle & FontStyles.Bold) != 0;
            nativeSettings.fontStyle = settings.fontStyle & ~FontStyles.Bold;
            nativeSettings.fontWeight = bold ? TextFontWeight.Bold : settings.fontWeight;

            nativeSettings.horizontalAlignment = settings.horizontalAlignment;

            nativeSettings.verticalAlignment = settings.verticalAlignment;

            // Mesh consumers are Y-up; the generator lays out Y-down and flips the output geometry.
            nativeSettings.flipYAxis = true;

            bool hasWidth = settings.extents.x > 0;
            nativeSettings.screenWidth = hasWidth ? ToFixedPoint(settings.extents.x) : -1;
            nativeSettings.screenHeight = settings.extents.y > 0 ? ToFixedPoint(settings.extents.y) : -1;

            nativeSettings.wordWrapEnabled = settings.wrapMode == TextWrapMode.Wrap && hasWidth;
            if (settings.whitespaceCollapse == WhitespaceCollapse.Collapse)
                nativeSettings.preProcessFlags |= PreProcessFlags.CollapseWhiteSpaces;

            nativeSettings.overflow = TextOverflow.Clip;

            nativeSettings.bestFit = settings.autoSize.enabled;
            nativeSettings.minFontSize = ToFixedPoint(settings.autoSize.minSize);
            nativeSettings.maxFontSize = ToFixedPoint(Mathf.Max(settings.autoSize.minSize, settings.autoSize.maxSize));
            nativeSettings.languageDirection = settings.languageDirection == TextDirection.RTL ? LanguageDirection.RTL : LanguageDirection.LTR;
            nativeSettings.richTextEnabled = settings.richText;

            nativeSettings.vertexPadding = ToFixedPoint(k_VertexPadding);

            nativeSettings.pixelsPerPointFixed64 = 64;

            return nativeSettings;
        }

        static unsafe NativeTextInfo GenerateNative(TextLib textLib, TextSettings textSettings, ref NativeTextGenerationSettings nativeSettings, string text, IntPtr textGenerationInfo, out bool wasCached)
        {
            text ??= string.Empty;

            bool cached = false;
            NativeTextInfo nativeTextInfo;
            fixed (char* textPtr = text)
            {
                nativeSettings.textBufferPtr = (IntPtr)textPtr;
                nativeSettings.textBufferLength = text.Length;

                if (nativeSettings.richTextEnabled && text.Length > 0)
                    PreloadRichTextAssets(textSettings, (IntPtr)textPtr, text.Length, nativeSettings.textSettings);

                FontAsset.CreateHbFaceIfNeeded();
                nativeTextInfo = textLib.GenerateText(nativeSettings, textGenerationInfo, ref cached);
            }

            wasCached = cached;
            return nativeTextInfo;
        }

        static void PreloadRichTextAssets(TextSettings textSettings, IntPtr textPtr, int textLength, IntPtr nativeTextSettings)
        {
            var defaultSpriteAsset = textSettings.defaultSpriteAsset != null ? textSettings.defaultSpriteAsset : TextSettings.s_GlobalSpriteAsset;
            if (defaultSpriteAsset != null)
            {
                defaultSpriteAsset.UpdateLookupTables();
                _ = defaultSpriteAsset.entityId;
            }

            NativeRichTextAssetRegistry.PreloadAssetsFromText(textPtr, textLength, nativeTextSettings);
        }

        static TextMesh BuildMesh(TextLib textLib, NativeTextInfo nativeTextInfo, NativeTextGenerationSettings nativeSettings)
        {
            var meshInfos = BuildMeshInfos(textLib, nativeTextInfo, nativeSettings, out var materials);
            return new TextMesh
            {
                meshInfos = meshInfos,
                materials = materials,
                size = new Vector2(nativeTextInfo.totalWidth / 64.0f, nativeTextInfo.totalHeight / 64.0f),
                isElided = nativeTextInfo.isElided,
            };
        }

        static TextMeshInfo[] BuildMeshInfos(TextLib textLib, NativeTextInfo nativeTextInfo, NativeTextGenerationSettings nativeSettings, out Material[] materials)
        {
            var textElementIndicesByMesh = new List<List<List<int>>>();
            var missingGlyphsPerFontAsset = new Dictionary<EntityId, HashSet<uint>>();

            OSFontFallbackResolver.Resolve(nativeTextInfo, missingGlyphsPerFontAsset);

            if (textLib.HasMissingGlyphs(nativeTextInfo, ref missingGlyphsPerFontAsset))
                PopulateGlyphs(missingGlyphsPerFontAsset);

            // Rasterizes any newly added glyphs and fills the UV0 coordinates in the mesh info.
            textLib.ProcessMeshInfos(nativeTextInfo, nativeSettings, ref textElementIndicesByMesh, uvsAreGenerated: false);

            var meshInfos = new List<TextMeshInfo>();
            var subMeshMaterials = new List<Material>();
            int processedMeshIndex = 0;
            for (int i = 0; i < nativeTextInfo.meshInfoCount; i++)
            {
                var meshInfo = nativeTextInfo.meshInfos[i];

                var textAsset = UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as UnityEngine.TextCore.Text.TextAsset;

                if (textAsset == null)
                    continue;

                var textElementInfos = meshInfo.textElementInfos;

                if (textAsset is FontAsset fontAsset)
                {
                    int sdfScale = fontAsset.IsBitmap() ? 0 : fontAsset.atlasPadding + 1;
                    int atlasCount = fontAsset.atlasTextures.Length;

                    for (int atlasIndex = 0; atlasIndex < atlasCount; atlasIndex++)
                    {
                        var elementIndices = FilterInkedElements(fontAsset, textElementInfos, textElementIndicesByMesh[processedMeshIndex][atlasIndex]);
                        if (elementIndices.Count == 0)
                            continue;

                        subMeshMaterials.Add(atlasIndex == 0
                            ? fontAsset.material
                            : MaterialManager.GetFallbackMaterial(fontAsset, fontAsset.material, atlasIndex));
                        meshInfos.Add(BuildSubMesh(sdfScale, textElementInfos, elementIndices));
                    }
                }
                else if (textAsset is SpriteAsset spriteAsset)
                {
                    var elementIndices = textElementIndicesByMesh[processedMeshIndex][0];
                    if (elementIndices.Count > 0)
                    {
                        subMeshMaterials.Add(spriteAsset.material);
                        meshInfos.Add(BuildSubMesh(sdfScale: 0, textElementInfos, elementIndices));
                    }
                }

                processedMeshIndex++;
            }

            materials = subMeshMaterials.ToArray();
            return meshInfos.ToArray();
        }

        static List<int> FilterInkedElements(FontAsset fontAsset, Span<NativeTextElementInfo> textElementInfos, List<int> elementIndices)
        {
            var result = new List<int>(elementIndices.Count);
            foreach (var index in elementIndices)
            {
                int glyphID = textElementInfos[index].glyphID;

                var glyph = fontAsset.GetGlyphInCache((uint)glyphID);
                if (glyph == null || (glyph.metrics.width > 0f && glyph.metrics.height > 0f))
                    result.Add(index);
            }
            return result;
        }

        static TextMeshInfo BuildSubMesh(int sdfScale, Span<NativeTextElementInfo> textElementInfos, List<int> elementIndices)
        {
            int quadCount = elementIndices.Count;
            int vertexCount = quadCount * 4;

            var vertices = new Vector3[vertexCount];
            var uvs0 = new Vector2[vertexCount];
            var uvs2 = new Vector2[vertexCount];
            var colors = new Color32[vertexCount];
            var triangles = new int[quadCount * 6];

            int v = 0;
            int t = 0;
            for (int q = 0; q < quadCount; q++)
            {
                ref var element = ref textElementInfos[elementIndices[q]];

                // Legacy TextGenerator corner order: top-left, top-right, bottom-right, bottom-left.
                vertices[v + 0] = element.topLeft.position;
                vertices[v + 1] = element.topRight.position;
                vertices[v + 2] = element.bottomRight.position;
                vertices[v + 3] = element.bottomLeft.position;

                uvs0[v + 0] = element.topLeft.uv0;
                uvs0[v + 1] = element.topRight.uv0;
                uvs0[v + 2] = element.bottomRight.uv0;
                uvs0[v + 3] = element.bottomLeft.uv0;

                // A non-zero uv2.y from the generator denotes synthesized bold, encoded as a negative
                // SDF scale. The x component is unused: the shader reads only y
                bool isBold = element.bottomLeft.uv2.y != 0;
                var uv2 = new Vector2(0, isBold ? -sdfScale : sdfScale);
                uvs2[v + 0] = uv2;
                uvs2[v + 1] = uv2;
                uvs2[v + 2] = uv2;
                uvs2[v + 3] = uv2;

                colors[v + 0] = element.topLeft.color;
                colors[v + 1] = element.topRight.color;
                colors[v + 2] = element.bottomRight.color;
                colors[v + 3] = element.bottomLeft.color;

                // Two triangles per quad.
                triangles[t + 0] = v + 0;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 0;

                v += 4;
                t += 6;
            }

            return new TextMeshInfo
            {
                vertices = vertices,
                uvs0 = uvs0,
                uvs2 = uvs2,
                colors = colors,
                triangles = triangles,
            };
        }

        static void PopulateGlyphs(Dictionary<EntityId, HashSet<uint>> missingGlyphsPerFontAsset)
        {
            if (missingGlyphsPerFontAsset.Count == 0)
                return;

            var missingGlyphs = new List<uint>();
            foreach (var entry in missingGlyphsPerFontAsset)
            {
                if (UnityEngine.Object.FindObjectFromInstanceIDThreadSafe(entry.Key) is not FontAsset fontAsset || entry.Value.Count == 0)
                    continue;

                missingGlyphs.Clear();
                missingGlyphs.AddRange(entry.Value);
                fontAsset.TryAddGlyphs(missingGlyphs, populateFontFeatures: false);
            }

            FontAsset.UpdateFontAssetsInUpdateQueue();
        }

        static TextLib GetTextLib()
        {
            if (s_TextLib != null)
                return s_TextLib;

            var icuAsset = TextHandle.GetICUAssetStaticFalback();
            s_TextLib = new TextLib(icuAsset != null ? icuAsset.bytes : Array.Empty<byte>());
            return s_TextLib;
        }

        static TextSettings GetDefaultTextSettings()
        {
            if (s_DefaultTextSettings == null)
            {
                s_DefaultTextSettings = ScriptableObject.CreateInstance<TextSettings>();
                s_DefaultTextSettings.hideFlags = HideFlags.HideAndDontSave;
            }
            return s_DefaultTextSettings;
        }

        static int ToFixedPoint(float value)
        {
            return (int)(value * 64.0f);
        }
    }
}
