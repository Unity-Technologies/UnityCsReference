// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    public partial class TextElement
    {
        /// <summary>
        /// Classifies what a <see cref="Glyph"/> represents. ATG-only; the
        /// legacy TextCore generator always reports <see cref="Character"/>.
        /// </summary>
        public enum GlyphKind
        {
            /// <summary>A regular text character.</summary>
            Character = 0,
            /// <summary>A glyph emitted by an inline <c>&lt;sprite&gt;</c> tag.</summary>
            Sprite = 1,
            // Reserved for upcoming decoration-quad support. Kept commented
            // out until the generator emits them so we don't ship public
            // values that always return Character. When uncommenting,
            // preserve these ordinals — do not renumber.
            // Underline = 2,
            // Strikethrough = 3,
            // Mark = 4,
        }

        /// <summary>
        /// Encapsulates a single glyph rendered inside a <see cref="TextElement"/> mesh.
        /// A glyph is a quad made of four vertices, laid out clockwise:
        /// bottom‑left → top‑left → top‑right → bottom‑right
        /// </summary>
        /// <remarks>
        /// Each <see cref="Vertex"/> stores:
        ///\\
        ///- Position – 3‑D coordinates
        ///- Color – per‑vertex tint
        ///- UV0 (x,y) – location of the glyph in the atlas texture
        ///- UV2.x – index of the texture slice inside a texture‑atlas array
        ///- UV2.y – SDF scale (negative values indicate bold weight)
        /// </remarks>
        public readonly struct Glyph
        {
            /// <summary>
            /// Four vertices that describe the glyph’s quad in BL‑TL‑TR‑BR order.
            /// </summary>
            public readonly NativeSlice<Vertex> vertices;

            /// <summary>
            /// Zero-based visual line number on which this glyph is laid out.
            /// </summary>
            public readonly int line;

            /// <summary>
            /// Value of the enclosing <c>&lt;link=...&gt;</c> tag, matching
            /// <see cref="Experimental.PointerDownLinkTagEvent.linkID"/>, or
            /// <c>null</c> when the glyph is not inside a <c>&lt;link&gt;</c> tag.
            /// </summary>
            public readonly string linkID;

            /// <summary>
            /// What this glyph represents. See <see cref="GlyphKind"/>.
            /// </summary>
            public readonly GlyphKind kind;

            internal readonly TextElement m_TextElement;

            internal Glyph(NativeSlice<Vertex> vertices, int line, string linkID, GlyphKind kind, TextElement textElement)
            {
                this.vertices = vertices;
                this.line = line;
                this.linkID = linkID;
                this.kind = kind;
                m_TextElement = textElement;
            }

            /// <summary>
            /// Overrides the outline and/or shadow tint for this glyph. Pass <c>null</c> to leave
            /// a field at the element baseline (<c>unityTextOutlineColor</c> / <c>textShadow.color</c>).
            /// </summary>
            /// <remarks>
            /// Not additive: each call rebuilds from the element baseline, so pass both
            /// <paramref name="outline"/> and <paramref name="shadow"/> together if you need both.
            /// Only valid inside a <see cref="PostProcessTextVertices"/> callback.
            /// </remarks>
            public void SetTints(Color? outline = null, Color? shadow = null)
            {
                if (outline == null && shadow == null)
                    return;
                if (m_TextElement == null)
                    return;

                var rd = m_TextElement.renderData;
                var perGlyphTcs = rd?.renderTree?.renderTreeManager?.perGlyphTcs;
                if (perGlyphTcs == null)
                    return;

                var settings = perGlyphTcs.baseline;
                if (outline.HasValue)
                {
                    // Match per-element premul-by-outline-alpha in TextUtilities.GetTextCoreSettingsForElement.
                    var c = outline.Value;
                    c.r *= c.a; c.g *= c.a; c.b *= c.a;
                    settings.outlineColor = c;
                }
                if (shadow.HasValue)
                    settings.underlayColor = shadow.Value;

                var alloc = perGlyphTcs.GetOrAlloc(settings, out var data);
                if (!alloc.IsValid())
                    return;

                // Local copy: NativeSlice is a struct over a pointer, so writes still hit the same
                // memory. Avoids CS1648 from the set-indexer on the readonly `vertices` field.
                var verts = vertices;
                int len = verts.Length;
                for (int i = 0; i < len; i++)
                {
                    var v = verts[i];
                    v.dynamicColorOrTextCoreId = data;
                    verts[i] = v;
                }
            }
        }

        /// <summary>
        /// Enumerates all visible glyphs in the order they appear on screen.
        /// </summary>
        public readonly struct GlyphsEnumerable
        {
            /// <summary>
            /// Total number of glyphs in this enumeration.
            /// </summary>
            public readonly int Count;
            readonly List<NativeSlice<Vertex>> m_Vertices;
            readonly TextElement m_TextElement;

            internal GlyphsEnumerable(TextElement te, List<NativeSlice<Vertex>> vertices)
            {
                m_TextElement = te;
                m_Vertices = vertices;
                Count = ComputeCount(vertices);
            }

            internal GlyphsEnumerable(TextElement te, List<NativeSlice<Vertex>> vertices, Span<ATGMeshInfo> meshInfos)
            {
                m_TextElement = te;
                m_Vertices = vertices;
                Count = ComputeCount(vertices);

                foreach (var meshInfo in meshInfos)
                {
                    var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextCore.Text.TextAsset;
                    if (textAsset is FontAsset fa && fa.atlasTextureCount > 1)
                        Debug.LogWarning("PostProcessTextVertices with ATG does not support this Multi-Atlas.");
                }
            }

            static int ComputeCount(List<NativeSlice<Vertex>> verts)
            {
                int totalVerts = 0;
                for (int i = 0; i < verts.Count; i++)
                    totalVerts += verts[i].Length;
                return totalVerts / 4;
            }

            /// <summary>
            /// Returns an enumerator that iterates glyphs in visual order.
            /// </summary>
            public GlyphsEnumerator GetEnumerator()
            {
                return new GlyphsEnumerator(m_TextElement, m_Vertices);
            }
        }


        /// <summary>
        /// Iterates over glyphs in visual order.
        /// </summary>
        public struct GlyphsEnumerator
        {
            /// <summary>
            /// Gets the current glyph’s vertex location.
            /// </summary>
            public Glyph Current { get; private set; }

            readonly TextElement m_TextElement;
            readonly List<NativeSlice<Vertex>> m_Vertices;
            int m_NextIndex;

            internal GlyphsEnumerator(TextElement textElement, List<NativeSlice<Vertex>> vertices)
            {
                m_TextElement = textElement;
                m_Vertices = vertices;
                m_NextIndex = 0;
                Current = default;
            }

            /// <summary>
            /// Advances the enumerator to the next visible glyph.
            /// </summary>
            /// <returns>
            /// True if a glyph is found; false if the end is reached.
            /// </returns>
            public bool MoveNext()
            {
                if (m_TextElement.computedStyle.unityTextGenerator == TextGeneratorType.Advanced)
                {
                    return MoveNextAdvanced();
                }
                else
                {
                    return MoveNextStandard();
                }
            }

            private bool MoveNextStandard()
            {
                var handle = m_TextElement.uitkTextHandle;
                var textInfo = handle.textInfo;
                int count = textInfo.characterCount;

                while (m_NextIndex < count)
                {
                    int idx = m_NextIndex++;
                    ref var ei = ref textInfo.textElementInfo[idx];
                    if (!ei.isVisible)
                        continue;

                    var slice = m_Vertices[ei.materialReferenceIndex].Slice(ei.vertexIndex, 4);
                    Current = new Glyph(slice, ei.lineNumber, null, GlyphKind.Character, m_TextElement);
                    return true;
                }

                return false;
            }

            bool MoveNextAdvanced()
            {
                var handle = m_TextElement.uitkTextHandle;
                var tgi = handle.textGenerationInfo;
                var count = TextGenerationInfo.GetGlyphCount(tgi);

                while (m_NextIndex < count)
                {
                    int glyphIndex = m_NextIndex++;
                    var info = TextGenerationInfo.GetGlyphRenderInfo(tgi, glyphIndex);

                    // Skip invisible glyphs such as \n
                    if (info.textElementInfoIndex < 0 || info.meshIndex < 0)
                        continue;

                    string linkID = ResolveLinkID(handle, info.linkID);

                    var slice = m_Vertices[info.meshIndex].Slice(info.textElementInfoIndex * 4, 4);
                    Current = new Glyph(slice, info.lineIndex, linkID, (GlyphKind)info.kind, m_TextElement);
                    return true;
                }

                return false;
            }

            // Returns null outside <link>, or for <a href> hyperlinks (filtered to match pointer-event semantics).
            static string ResolveLinkID(UITKTextHandle handle, int linkID)
            {
                var links = handle.m_Links;
                if (links == null || (uint)linkID >= (uint)links.Length)
                    return null;
                var entry = links[linkID];
                return entry.isHyperlink ? null : entry.value;
            }

            /// <summary>
            /// Resets the enumerator to its initial position.
            /// </summary>
            public void Reset() => m_NextIndex = 0;
        }
    }
}
