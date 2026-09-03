// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using RootBend = UnityEngine.UIElements.UIR.UIRCurvatureGeometry.RootBend;

namespace UnityEngine.UIElements.UIR
{
    // Curved UI.
    //
    // Engine-owned mesh modifier that bends a curved element AND its whole subtree onto a single curved
    // surface, according to the curved element's computedStyle.unityCurvature. It is auto-armed by SyncState
    // (mirroring RenderEvents.SyncBackdropFilterState): when an element's resolved curvature becomes non-flat
    // a shared registration is added to its modifier list; when it returns to flat it is removed.
    //
    // SyncState runs from DepthFirstOnVisualsChanged immediately before RebuildEffectiveModifiers in the SAME
    // visuals pass, so it mutates owner.m_MeshModifiers directly rather than going through the public
    // AddMeshModifier/RemoveMeshModifier API — those dirty the render tree (UIEOnVisualsChanged), which is
    // illegal while the visual tree is being processed. The same-pass rebuild folds the change into the
    // effective chain without a re-dirty.
    //
    // The registration is RECURSIVE, so the callback runs on the curved element and each descendant; each
    // composes the bends of its whole chain of curved ancestors (see UIRCurvatureGeometry.Bend/BuildConformChain),
    // so children conform to the parent's curve (and a curve nested in a curve bends onto both) instead of curving
    // about their own centers. A single static registration serves every curved subtree (stable callback ref + id),
    // so the chain cache dedups it; int.MinValue priority establishes the base 3D shape before any user modifiers run.
    //
    // The conform/bend geometry (chain construction + per-point bend) lives in UIRCurvatureGeometry, shared with
    // input picking so clicks land exactly where the mesh is drawn. This class owns only the per-draw MESH work:
    // a sparse sheet — the element's solid/textured background fill OR its border — is RE-TESSELLATED (midpoint
    // subdivision of its own source triangles) so it curves smoothly instead of faceting, and a border stays flush
    // with its (also-tessellated) fill instead of opening a gap. Already-dense geometry (text glyphs, gradients)
    // and Mask stencil geometry are bent in place. Subdivision count is derived from the bend angle (see
    // UIRCurvatureGeometry.SegmentsFor) and capped to the per-entry vertex budget (BudgetCappedLevel).
    // Curvature is only visually meaningful on WorldSpace panels.
    static class UIRCurvatureMeshModifier
    {
        // Shared recursive registration reused for every curved subtree. Stable callback ref + id let the
        // chain cache treat all curved subtrees' chains as content-identical and dedup them.
        static readonly long s_Id = UIRUtility.GetNextMeshModifierId();
        [NoAutoStaticsCleanup]
        static readonly MeshModifierRegistration s_Registration =
            new MeshModifierRegistration(Modify, recursive: true, priority: int.MinValue, id: s_Id, appliesToSubTreeQuad: true);

        // Pre-build sync: arm/disarm the curvature modifier on a flat<->non-flat transition.
        // Cost is per-element and only acts on the transition, piggybacking the existing visuals pass.
        internal static void SyncState(RenderData renderData)
        {
            // A filtered element owns TWO renderData (the composite quad in the outer tree + the nested
            // tree root) but ONE m_MeshModifiers list: arm only via the outer one, or the registration
            // is added twice.
            if (renderData.isNestedRenderTreeRoot)
                return;

            VisualElement ve = renderData.owner;
            bool wasEnabled = renderData.hasCurvatureModifier;
            bool isEnabled = ve.hasCurvature;

            if (wasEnabled == isEnabled)
                return;

            // Mutate the owner's modifier list directly (no dirty): RebuildEffectiveModifiers runs next,
            // in this same pass, and reads it fresh.
            if (isEnabled)
                (ve.m_MeshModifiers ??= new List<MeshModifierRegistration>()).Add(s_Registration);
            else
                RemoveRegistration(ve.m_MeshModifiers);

            renderData.hasCurvatureModifier = isEnabled;
        }

        static void RemoveRegistration(List<MeshModifierRegistration> list)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].id == s_Id)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        static void Modify(MeshModificationContext ctx)
        {
            VisualElement ve = ctx.element;
            RenderData rd = ctx.renderData;

            // A nested render tree is a flat 2D capture (a filter's input texture): bending its content
            // would warp the capture. The filtered element's composite quad — bent and tessellated below —
            // carries the curvature into the outer tree instead. This rule composes for filters inside
            // filters: an inner quad captured by an outer filter stays flat too.
            if (rd?.renderTree?.rootRenderData?.isNestedRenderTreeRoot == true)
                return;
            bool isSubTreeQuad = rd is { isSubTreeQuad: true };

            // The modifier is recursive: it runs on the curved element AND every descendant. Each conforms to the
            // CHAIN of curved ancestors (innermost -> outermost), composing their bends onto one shared surface —
            // so a whole panel lies on one curved surface, and a curved element nested inside another bends onto
            // BOTH. See UIRCurvatureGeometry.CountChain / BuildConformChain / Bend.

            // Curvature is only meaningful on a WorldSpace panel: it displaces geometry along Z, and only a
            // WorldSpace panel consumes that as depth. A flat panel — a runtime overlay, an editor window, or the
            // UI Builder canvas (all rendered in 2D) — ignores the Z and renders the X/Y arc as an in-plane SQUASH.
            // So bend only on a non-flat WorldSpace runtime panel; everywhere else the geometry renders normally
            // (the UI Builder shows curvature via its authoring overlay instead). WorldSpace consumes the written
            // Vertex.z as depth WITHOUT the X/Y 1/pixelsPerUnit shrink, so pre-scale the sag by 1/pixelsPerUnit so
            // world-Z matches the world X/Y scale.
            if (!(ve.elementPanel is BaseRuntimePanel rp) || rp.isFlat || rp.pixelsPerUnit <= UIRUtility.k_Epsilon)
                return;
            float zScale = 1f / rp.pixelsPerUnit;

            // Collect the chain of curved ancestors (innermost first): the affine map into each one's flat box plus
            // its extent/angles, and the map back to element-local. Empty when no ancestor curves, or when the
            // nearest declared curvature is an explicit 0deg flat barrier (its subtree opts out of outer curves).
            // BuildConformChain also excludes this element's OWN out-of-plane translate (translate.z) from the
            // conform, so the mesh carries only the surface (arc + sag); the GPU transform re-applies translate.z
            // via its m23 as an off-surface lift (m22 is forced to 1 in GetTransformIDTransformInfo, so Vertex.z is
            // never scaled; only m23 lifts). A single-element chain is the per-surface conform; longer chains compose.
            int count = UIRCurvatureGeometry.CountChain(ve);
            if (count == 0)
                return;

            Span<RootBend> chain = stackalloc RootBend[count];
            UIRCurvatureGeometry.BuildConformChain(ve, chain, out Matrix4x4 toElement, out int segX, out int segY);

            foreach (var draw in ctx.draws)
            {
                // Idempotence guard. The modifier pipeline hands each run the element's PERSISTENT
                // entries: BendInPlace mutates their vertices through the raw slice and SetMesh replaces their
                // mesh, so when a later visuals pass re-processes an element whose entries were NOT regenerated
                // (e.g. the first-frame font-atlas settle re-dirties the panel), Modify receives its own previous
                // OUTPUT and bends it AGAIN. That double bend is invisible in the math (each pass is locally
                // correct) but doubles the rendered arc + sag relative to the declared -unity-curvature, and it
                // desynchronizes rendering from picking (which reconstructs a single bend). UITK mesh generation
                // never writes Vertex.z for regular content (the conform also excludes translate.z), so a non-zero
                // Z is a reliable "already bent" marker: skip the draw instead of stacking another bend. The real
                // fix is pipeline-level (regenerate entries for modifier-bearing elements before re-running
                // modifiers); this guard keeps the modifier correct until then.
                bool alreadyBent = false;
                {
                    var vv = draw.vertices;
                    for (int i = 0; i < vv.Length; i++)
                    {
                        if (Mathf.Abs(vv[i].position.z) > 1e-5f)
                        {
                            alreadyBent = true;
                            break;
                        }
                    }
                }
                if (alreadyBent)
                    continue;

                // A sparse sheet that facets when displaced — a background fill (solid colour OR image) or a
                // border — is re-meshed (midpoint subdivision of its OWN source triangles) to follow the arc.
                // Subdividing only the existing triangles keeps a border hollow (it just adds vertices along the
                // ring); bending a border in place instead leaves its long straight edges as chords, which both
                // facet AND pull away from the tessellated fill, opening a gap that shows the panel behind.
                // Already-dense geometry (text, gradients) and Mask stencil geometry are bent in place. Textured
                // fills subdivide too: Mid lerps uv, so the image mapping carries through the resample.
                // A subtree quad (a filtered element's composite) is ALWAYS a sparse textured sheet
                // spanning the whole (padded) element, so it re-meshes regardless of its phase/type.
                bool tess = (segX > 1 || segY > 1)
                    && draw.indices.Length >= 3
                    && (isSubTreeQuad
                        || ((draw.renderType == RenderType.Solid || draw.renderType == RenderType.Texture)
                            && (draw.phase == DrawPhase.Background || draw.phase == DrawPhase.Border)));
                if (tess)
                    TessellateAndBend(ctx, draw, chain, toElement, zScale, segX, segY);
                else
                    BendInPlace(draw, chain, toElement, zScale);
            }
        }

        static unsafe void BendInPlace(DrawData draw, ReadOnlySpan<RootBend> chain, in Matrix4x4 toElement, float zScale)
        {
            // Mutate position in place through the raw pointer: avoids copying the whole (large) Vertex struct
            // twice per vertex, and skips the per-element NativeSlice safety checks — both dominate the bend
            // math for the many small fills that are bent in place rather than tessellated.
            NativeSlice<Vertex> verts = draw.vertices;
            int n = verts.Length;
            Vertex* p = (Vertex*)NativeSliceUnsafeUtility.GetUnsafePtr(verts);
            for (int i = 0; i < n; ++i)
                p[i].position = UIRCurvatureGeometry.Bend(p[i].position, chain, toElement, zScale);
        }

        // Re-meshes the element's background fill by recursively subdividing its SOURCE triangles (midpoint
        // subdivision) and bending each resulting vertex onto the arc. Subdividing the existing geometry — rather
        // than resampling onto a coarse uniform grid — preserves the fill's structure: both the opaque interior
        // and the thin anti-aliasing bands at the edges. (A coarse grid can miss both: a single-axis bend's only
        // two rows land on the transparent AA edges, leaving the whole fill invisible.) New vertices average every
        // interpolated channel of their edge endpoints, and the bend is applied at the leaves. Allocates only a
        // temporary native mesh — no managed per-frame allocations; the recursion runs on the call stack.
        static void TessellateAndBend(MeshModificationContext ctx, DrawData draw, ReadOnlySpan<RootBend> chain, in Matrix4x4 toElement, float zScale, int segX, int segY)
        {
            NativeSlice<Vertex> src = draw.vertices;
            NativeSlice<ushort> srcIdx = draw.indices;
            int srcTris = srcIdx.Length / 3;
            if (srcTris == 0)
            {
                BendInPlace(draw, chain, toElement, zScale);
                return;
            }

            // Output is srcTris * 4^level leaf triangles, each carrying its own 3 vertices (no sharing). Cap the
            // level so that count never exceeds the per-entry vertex limit: a fill that already has many source
            // triangles (e.g. a rounded-corner rect's arc fans) needs little subdivision, and one so dense that
            // even a single level would overflow is bent in place rather than re-meshed.
            int level = BudgetCappedLevel(srcTris, SubdivisionLevel(segX, segY));
            if (level < 1)
            {
                BendInPlace(draw, chain, toElement, zScale);
                return;
            }

            int leavesPerTri = 1 << (2 * level);           // 4^level
            int outTriCount = srcTris * leavesPerTri;
            int outVertCount = outTriCount * 3;            // no vertex sharing: each leaf carries its own 3 verts

            ctx.AllocateUIMesh(ExtraVertexChannels.None, outVertCount, outTriCount * 3, out UIMesh mesh);
            NativeSlice<Vertex> verts = mesh.vertices;
            NativeSlice<ushort> idx = mesh.indices;

            int vc = 0, ic = 0;
            for (int t = 0; t + 2 < srcIdx.Length; t += 3)
                Subdivide(src[srcIdx[t]], src[srcIdx[t + 1]], src[srcIdx[t + 2]], level,
                    verts, idx, ref vc, ref ic, chain, toElement, zScale);

            draw.SetMesh(mesh);
        }

        // 2^level segments per original edge, from the per-axis bend density, capped so a steep bend can't
        // explode the vertex count (4^level leaf triangles per source triangle).
        static int SubdivisionLevel(int segX, int segY)
        {
            int level = Mathf.CeilToInt(Mathf.Log(Mathf.Max(2, Mathf.Max(segX, segY)), 2f));
            return Mathf.Clamp(level, 1, 4);
        }

        // Largest subdivision level in [0, desired] whose re-meshed output (srcTris * 4^level * 3 vertices) stays
        // within the per-entry vertex limit. Returns 0 when even a single level would overflow, signalling the
        // caller to bend the fill in place. Guards a high-triangle fill (rounded-corner arc fans) from exceeding
        // UIRenderDevice.maxVerticesPerPage when re-tessellated.
        static int BudgetCappedLevel(int srcTris, int desiredLevel)
        {
            int level = desiredLevel;
            while (level >= 1 && (long)srcTris * (1L << (2 * level)) * 3 > UIRenderDevice.maxVerticesPerPage)
                --level;
            return level;
        }

        // Recursively splits a triangle into four via edge midpoints (in flat space), emitting bent leaf triangles.
        // Vertices are passed by `in` (readonly ref) throughout so the 64-byte struct is not copied down the
        // recursion — only field reads happen, so no defensive copy is emitted.
        static void Subdivide(in Vertex a, in Vertex b, in Vertex c, int level,
            NativeSlice<Vertex> verts, NativeSlice<ushort> idx, ref int vc, ref int ic,
            ReadOnlySpan<RootBend> chain, in Matrix4x4 toElement, float zScale)
        {
            if (level == 0)
            {
                idx[ic++] = Emit(verts, ref vc, in a, chain, toElement, zScale);
                idx[ic++] = Emit(verts, ref vc, in b, chain, toElement, zScale);
                idx[ic++] = Emit(verts, ref vc, in c, chain, toElement, zScale);
                return;
            }

            Vertex ab = Mid(in a, in b), bc = Mid(in b, in c), ca = Mid(in c, in a);
            Subdivide(in a, in ab, in ca, level - 1, verts, idx, ref vc, ref ic, chain, toElement, zScale);
            Subdivide(in ab, in b, in bc, level - 1, verts, idx, ref vc, ref ic, chain, toElement, zScale);
            Subdivide(in ca, in bc, in c, level - 1, verts, idx, ref vc, ref ic, chain, toElement, zScale);
            Subdivide(in ab, in bc, in ca, level - 1, verts, idx, ref vc, ref ic, chain, toElement, zScale);
        }

        static ushort Emit(NativeSlice<Vertex> verts, ref int vc, in Vertex v,
            ReadOnlySpan<RootBend> chain, in Matrix4x4 toElement, float zScale)
        {
            Vertex r = v; // mutable copy: the leaf vertex is bent then written to the mesh
            r.position = UIRCurvatureGeometry.Bend(r.position, chain, toElement, zScale);
            verts[vc] = r;
            return (ushort)vc++;
        }

        // Midpoint of an edge: the public vertex interpolation blends every continuous channel (position/tint/uv/
        // layoutUV/AA-SDF circle) and copies the discrete per-fill data (ids/flags) from the first endpoint.
        static Vertex Mid(in Vertex a, in Vertex b)
        {
            Vertex.Lerp(in a, in b, 0.5f, out Vertex r);
            return r;
        }

        internal static class Testing
        {
            public static float CombineSag(float sagX, float sagY) => UIRCurvatureGeometry.Testing.CombineSag(sagX, sagY);
            public static int BudgetCappedLevel(int srcTris, int desiredLevel) => UIRCurvatureMeshModifier.BudgetCappedLevel(srcTris, desiredLevel);
        }
    }
}
