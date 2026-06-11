// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // Per-glyph TextCoreSettings: scoped state used by TextElement.Glyph.SetTints.
    //
    // The text job system (UITKTextJobSystem / ATGTextJobSystem) opens a scope with
    // Begin before invoking the user's PostProcessTextVertices callback, and closes it
    // in a finally with End. While the scope is open, Glyph.SetTints reaches in via
    // GetOrAlloc to dedup-allocate per-glyph shader-info atlas entries. The dict
    // instance is stashed on ExtraRenderData (and freed via FreeAllocs), so cross-frame
    // leftovers are bounded by the element's lifetime.
    class PerGlyphTextCoreSettings
    {
        readonly RenderTreeManager m_Owner;

        RenderData m_Rd;
        List<NativeSlice<Vertex>> m_Verts;
        TextCoreSettings m_Baseline;
        bool m_OomWarned;

        public TextCoreSettings baseline => m_Baseline;

        public PerGlyphTextCoreSettings(RenderTreeManager owner)
        {
            m_Owner = owner;
        }

        public void Begin(RenderData rd, List<NativeSlice<Vertex>> verts, in TextCoreSettings baseline)
        {
            Reset(rd);
            m_Rd = rd;
            m_Verts = verts;
            m_Baseline = baseline;
            m_OomWarned = false;
        }

        // True when SetTints was called; callers pass it to DrawText so ConvertMeshJob preserves per-vertex bytes.
        public bool End()
        {
            bool anyAlloc = false;
            if (m_Rd != null && m_Rd.hasExtraData)
            {
                var dict = m_Owner.GetExtraData(m_Rd).textCoreSettingsAllocs;
                anyAlloc = dict != null && dict.Count > 0;
            }
            m_Rd = null;
            m_Verts = null;
            return anyAlloc;
        }

        // Safe when textCoreSettingsAllocs is null or the element never used the API.
        public void Reset(RenderData rd)
        {
            if (rd == null || !rd.hasExtraData)
                return;
            var dict = m_Owner.GetExtraData(rd).textCoreSettingsAllocs;
            if (dict == null || dict.Count == 0)
                return;
            foreach (var alloc in dict.Values)
                m_Owner.shaderInfoAllocator.FreeTextCoreSettings(alloc);
            dict.Clear();
        }

        // Frees atlas entries owned by the given ExtraRenderData (called from RenderTreeManager.FreeExtraData).
        public void FreeAllocs(ExtraRenderData extraData)
        {
            if (extraData.textCoreSettingsAllocs == null)
                return;
            foreach (var alloc in extraData.textCoreSettingsAllocs.Values)
                m_Owner.shaderInfoAllocator.FreeTextCoreSettings(alloc);
            extraData.textCoreSettingsAllocs.Clear();
        }

        // Returns BMPAlloc.Invalid on atlas OOM; vertexData is undefined in that case.
        public BMPAlloc GetOrAlloc(in TextCoreSettings settings, out ushort vertexData)
        {
            vertexData = 0;
            if (m_Rd == null)
                return BMPAlloc.Invalid;

            var extra = m_Owner.GetOrAddExtraData(m_Rd);
            if (extra.textCoreSettingsAllocs == null)
                extra.textCoreSettingsAllocs = new Dictionary<TextCoreSettings, BMPAlloc>();

            if (extra.textCoreSettingsAllocs.Count == 0)
            {
                ushort baselineId = ShaderInfoAllocator.BMPAllocToId(m_Rd.textCoreSettingsID);
                PrestampVertices(m_Verts, baselineId);
            }

            if (extra.textCoreSettingsAllocs.TryGetValue(settings, out var existing))
            {
                vertexData = ShaderInfoAllocator.BMPAllocToId(existing);
                return existing;
            }

            var alloc = m_Owner.shaderInfoAllocator.AllocTextCoreSettings(settings);
            if (!alloc.IsValid())
            {
                if (!m_OomWarned)
                {
                    m_OomWarned = true;
                    Debug.LogWarning("Per-glyph TextCoreSettings atlas is full; falling back to baseline outline/shadow tints for this element.");
                }
                return alloc;
            }
            m_Owner.shaderInfoAllocator.SetTextCoreSettingValue(alloc, settings);
            extra.textCoreSettingsAllocs[settings] = alloc;
            vertexData = ShaderInfoAllocator.BMPAllocToId(alloc);
            return alloc;
        }

        internal static bool InvokePostProcessVertices(
            PerGlyphTextCoreSettings perGlyphTcs,
            TextElement textElement,
            RenderData rd,
            List<NativeSlice<Vertex>> vertices,
            in TextElement.GlyphsEnumerable glyphs)
        {
            if (perGlyphTcs == null)
            {
                textElement.PostProcessTextVertices.Invoke(glyphs);
                return false;
            }

            var baseline = TextUtilities.GetTextCoreSettingsForElement(textElement, false);
            perGlyphTcs.Begin(rd, vertices, baseline);
            bool usesPerGlyphTcs;
            try
            {
                textElement.PostProcessTextVertices.Invoke(glyphs);
            }
            finally
            {
                usesPerGlyphTcs = perGlyphTcs.End();
            }
            return usesPerGlyphTcs;
        }

        static void PrestampVertices(List<NativeSlice<Vertex>> vertices, ushort baseline)
        {
            if (vertices == null)
                return;
            for (int s = 0; s < vertices.Count; s++)
            {
                var slice = vertices[s];
                int len = slice.Length;
                for (int v = 0; v < len; v++)
                {
                    var vert = slice[v];
                    vert.dynamicColorOrTextCoreId = baseline;
                    slice[v] = vert;
                }
            }
        }
    }
}
