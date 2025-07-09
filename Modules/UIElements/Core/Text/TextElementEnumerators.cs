// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    public partial class TextElement
    {
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

            internal Glyph(NativeSlice<Vertex> vertices)
            {
                this.vertices = vertices;
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

            internal GlyphsEnumerable(TextElement te, List<NativeSlice<Vertex>> vertices, ATGMeshInfo[] meshInfos)
            {
                m_TextElement = te;
                m_Vertices = vertices;
                Count = ComputeCount(vertices);

                foreach (var meshInfo in meshInfos)
                {
                    if (meshInfo.textElementInfoIndicesByAtlas.Count > 1)
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
                var textInfo = m_TextElement.uitkTextHandle.textInfo;
                int count = textInfo.characterCount;

                 while (m_NextIndex < count)
                 {
                     ref var ei = ref textInfo.textElementInfo[m_NextIndex++];
                     if (!ei.isVisible)
                        continue;

                     Current = new Glyph(m_Vertices[ei.materialReferenceIndex].Slice(ei.vertexIndex, 4));
                     return true;
                 }

                return false;
            }

            bool MoveNextAdvanced()
            {
                var tgi = m_TextElement.uitkTextHandle.textGenerationInfo;
                var count = TextGenerationInfo.GetGlyphCount(tgi);

                while (m_NextIndex < count)
                {
                    var textRenderingIndices = TextGenerationInfo.GetTextRenderingIndices(tgi, m_NextIndex++);

                    // Skip invisible glyphs such as \n
                    if (textRenderingIndices.textElementInfoIndex < 0 || textRenderingIndices.meshIndex < 0)
                        continue;

                    Current = new Glyph(m_Vertices[textRenderingIndices.meshIndex].Slice(textRenderingIndices.textElementInfoIndex * 4, 4));
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Resets the enumerator to its initial position.
            /// </summary>
            public void Reset() => m_NextIndex = 0;
        }
    }
}
