// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal class MeshNode : PoolItem
    {
        public MeshHandle mesh;
        public MeshNode next;

        public void Reset()
        {
            mesh = null;
            next = null;
        }
    }

    [Flags]
    internal enum VertexFlags
    {
        // Vertex Type
        // These values are like enum values, they are mutually exclusive. Only one may be specified on a vertex.
        IsSolid = 0,
        IsText = 1,
        IsTextured = 2,
        IsCustom = 3,
        IsEdge = 4,
        IsSVGGradients = 5,
        LastType = 6,
        AllTypeBits = 7, // Excluding LastType, determines which bits are used for type storage.
        AllUsedBits = 7, // Excluding meta-flags like LastType, corresponds to all used bits.
    }

    internal static class VertexFlagsUtil
    {
        public static bool TypeIsEqual(VertexFlags a, VertexFlags b)
        {
            VertexFlags typeA = a & VertexFlags.AllTypeBits;
            VertexFlags typeB = b & VertexFlags.AllTypeBits;
            Assert.IsTrue(a < VertexFlags.LastType);
            Assert.IsTrue(b < VertexFlags.LastType);
            return typeA == typeB;
        }
    }

    internal struct Vertex
    {
        public Vector3 position;
        public Color32 tint;
        public Vector2 uv;
        public float transformID;   // Allocator gives an int, but we only take floats, so set to ((float)transformID)
        public float clippingID;
        public float flags;         // Solid,Font,AtlasTextured,CustomTextured,Edge,SVG with gradients,...
        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    [Flags]
    internal enum StateFields
    {
        None = 0,
        Material = 1 << 0,
        Atlas = 1 << 1,
        Font = 1 << 2
    }

    internal sealed class State : PoolItem
    {
        public Material material; // Responsible for enabling immediate clipping
        public Texture custom, font;

        public State()
        {}

        public State(State other)
        {
            Assert.IsNotNull(other);
            CopyFrom(other);
        }

        public void Reset()
        {
            material = null;
            custom = null;
            font = null;
            key = 0;
        }

        public UInt64 key { get; private set; }
        public void UpdateKey()
        {
            if (material != null)
                key += (UInt64)material.GetHashCode();
            if (custom != null)
                key += (UInt64)custom.GetHashCode();
            if (font != null)
                key += (UInt64)font.GetHashCode();
        }

        public void CopyFrom(State other)
        {
            Assert.IsNotNull(other);
            material = other.material;
            custom = other.custom;
            font = other.font;
            key = other.key;
        }

        /// <returns>The fields that have been overridden.</returns>
        public StateFields OverrideWith(State other)
        {
            if (other == this)
                return StateFields.None;

            Assert.IsNotNull(other);
            var overrides = StateFields.None;
            if (material != other.material)
            {
                material = other.material;
                overrides |= StateFields.Material;
            }

            if (custom != other.custom && other.custom != null)
            {
                custom = other.custom;
                overrides |= StateFields.Atlas;
            }

            if (font != other.font && other.font != null)
            {
                font = other.font;
                overrides |= StateFields.Font;
            }

            if (overrides != StateFields.None)
                UpdateKey();

            return overrides;
        }
    }

    [Flags]
    internal enum RendererTypes
    {
        Unspecified = 0,
        EmptyRenderer = 1 << 0,
        MeshRenderer = 1 << 1,
        ImmediateRenderer = 1 << 2,

        ScissorClipRenderer = 1 << 3,
        MaskRenderer = 1 << 4,
        ZoomPanRenderer = 1 << 5,
        ContentRenderer = ScissorClipRenderer | MaskRenderer | ZoomPanRenderer
    }

    internal abstract class RendererBase : PoolItem
    {
        internal RendererTypes type;
        protected bool m_ChainsWithMeshRenderer;
        public RendererBase next;
        public RendererBase contents; // Used only for content renderers.
        internal abstract void Draw(DrawChainState dcs);
        internal RendererTypes rendererType { get { return type; } }
        internal bool chainsWithMeshRenderer { get { return m_ChainsWithMeshRenderer; } }

        protected void Reset()
        {
            next = null;
            contents = null;
        }
    }

    internal abstract class ContentRendererBase : RendererBase
    {
    }

    /// <summary>
    /// The role of this renderer is to maintain the insertion point of a VisualElement within the DrawChain.
    /// </summary>
    internal sealed class EmptyRenderer : RendererBase
    {
        public EmptyRenderer()
        {
            type = RendererTypes.EmptyRenderer;
            m_ChainsWithMeshRenderer = true;
        }

        internal override void Draw(DrawChainState dcs)
        {
            if (!dcs.nextInChainIsMeshRenderer)
                dcs.KickRanges();
        }
    }

    internal sealed class MeshRenderer : RendererBase
    {
        public const float k_PosZ = -1.0f; // The correct z value to use to draw a shape regularly (no clipping)

        public State state;
        public MeshNode meshChain;

        public MeshRenderer()
        {
            type = RendererTypes.MeshRenderer;
            m_ChainsWithMeshRenderer = true;
        }

        public new void Reset()
        {
            state = null;
            meshChain = null;
            base.Reset();
        }

        internal override void Draw(DrawChainState dcs) { DrawMeshChain(dcs, state, meshChain); }

        static internal void DrawMeshChain(DrawChainState dcs, State state, MeshNode meshChain)
        {
            if (meshChain == null)
                return; // Not much to draw

            dcs.SetState(state);

            MeshNode node = meshChain;
            int maxIndexUsed = dcs.currentDrawRange.firstIndex + dcs.currentDrawRange.indexCount;
            int maxVertexReferenced = dcs.currentDrawRange.vertsReferenced + dcs.currentDrawRange.minIndexVal;
            while (node != null)
            {
                MeshHandle mesh = node.mesh;
                bool drawRangeRestart = maxIndexUsed != mesh.allocIndices.start;
                if (dcs.pageObj != mesh.allocPage)
                {
                    dcs.KickRanges();
                    dcs.pageObj = mesh.allocPage;
                    drawRangeRestart = true;
                }

                if (drawRangeRestart)
                {
                    dcs.StashCurrentAndOpenNewDrawRange();
                    dcs.currentDrawRange.firstIndex = (int)mesh.allocIndices.start;
                    dcs.currentDrawRange.indexCount = (int)mesh.allocIndices.size;
                    dcs.currentDrawRange.minIndexVal = (int)mesh.allocVerts.start;
                    dcs.currentDrawRange.vertsReferenced = (int)mesh.allocVerts.size;
                    maxIndexUsed = (int)(mesh.allocIndices.start + mesh.allocIndices.size);
                    maxVertexReferenced = (int)(mesh.allocVerts.size + mesh.allocVerts.start);
                }
                else
                {
                    // We can chain
                    maxVertexReferenced = Math.Max(maxVertexReferenced, (int)(mesh.allocVerts.size + mesh.allocVerts.start));
                    dcs.currentDrawRange.indexCount += (int)mesh.allocIndices.size;
                    dcs.currentDrawRange.minIndexVal = Math.Min(dcs.currentDrawRange.minIndexVal, (int)mesh.allocVerts.start);
                    dcs.currentDrawRange.vertsReferenced = maxVertexReferenced - dcs.currentDrawRange.minIndexVal;
                    maxIndexUsed += (int)mesh.allocIndices.size;
                }

                node = node.next;
            }

            if (!dcs.nextInChainIsMeshRenderer)
                dcs.KickRanges();
        }
    }

    internal sealed class ScissorClipRenderer : ContentRendererBase
    {
        public ScissorClipRenderer() { type = RendererTypes.ScissorClipRenderer; }

        public Rect scissorArea;
        public uint transformID;

        internal override void Draw(DrawChainState dcs)
        {
            var prevScissorRect = dcs.scissorRect;
            Matrix4x4 transform = dcs.view * dcs.GetTransform(transformID);

            ++dcs.scissorCount;
            if (dcs.scissorCount == 1)
                dcs.scissorRect = scissorArea.Transform(transform);
            else
                dcs.scissorRect = UIRUtility.IntersectRects(dcs.scissorRect, scissorArea.Transform(transform));

            var tScissorRect = FlipRectYAxis(dcs.scissorRect, dcs.viewport);
            // Scissor test is performed in fragment coordinates,
            // it is independent of the viewport,
            // so we have to offset our scissor rects according to the current viewport
            var currentViewport = Utility.GetViewport();
            tScissorRect.x += currentViewport.x;
            tScissorRect.y += currentViewport.y;

            Utility.SetScissorRect(tScissorRect);

            try
            {
                UIRenderDevice.ContinueChain(contents, dcs, false);
            }
            finally
            {
                dcs.scissorRect = prevScissorRect;

                --dcs.scissorCount;
                if (dcs.scissorCount == 0)
                    Utility.DisableScissor();
                else
                    Utility.SetScissorRect(FlipRectYAxis(dcs.scissorRect, dcs.viewport));
            }
        }

        RectInt FlipRectYAxis(Rect rect, Rect viewport)
        {
            var r = new RectInt(0, 0, 0, 0);
            float pixelsPerPoint = GUIUtility.pixelsPerPoint;
            r.x = Mathf.RoundToInt(rect.x * pixelsPerPoint);
            r.y = Mathf.RoundToInt((viewport.height - rect.yMax) * pixelsPerPoint);
            r.width = Mathf.RoundToInt(rect.width * pixelsPerPoint);
            r.height = Mathf.RoundToInt(rect.height * pixelsPerPoint);
            return r;
        }
    }

    internal sealed class MaskRenderer : ContentRendererBase
    {
        public MaskRenderer() { type = RendererTypes.MaskRenderer; m_ChainsWithMeshRenderer = true; } // Perfectly compatible with MeshRenderer

        public const float k_MaskPosZ = 0.0f; // The correct z value to use to mark a shape to be clipped

        // Utilities
        public static void MakeMeshMasked(NativeSlice<UInt16> indices) { FlipWindingOrder(indices); }
        public static void MakeMeshMaskRegister(NativeSlice<Vertex> vertices) { UseMaskPosZ(vertices); }
        public static void MakeMeshMaskUnregister(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices) { UseMaskPosZ(vertices); FlipWindingOrder(indices); }

        // The mesh handle passed down to those two functions must be a fresh one that was never draw with,
        // or else the changed data will not be sent to the GPU as expected
        public static void MakeMeshMasked(MeshHandle newMeshHandle)
        {
            MakeMeshMasked(newMeshHandle.allocPage.indices.cpuData.Slice((int)newMeshHandle.allocIndices.start, (int)newMeshHandle.allocIndices.size));
        }

        public static void MakeMeshRegisterUnregister(IUIRenderDevice device, MeshHandle meshRegister, out MeshHandle meshUnregister)
        {
            var meshVerts = meshRegister.allocPage.vertices.cpuData.Slice((int)meshRegister.allocVerts.start, (int)meshRegister.allocVerts.size);
            var meshIndices = meshRegister.allocPage.indices.cpuData.Slice((int)meshRegister.allocIndices.start, (int)meshRegister.allocIndices.size);
            MakeMeshMaskRegister(meshVerts);

            NativeSlice<Vertex> verts;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            meshUnregister = device.Allocate((uint)meshVerts.Length, (uint)meshIndices.Length, out verts, out indices, out indexOffset);
            verts.CopyFrom(meshVerts);
            int indexCount = indices.Length;

            int actualIndexOffset = indexOffset - (int)meshRegister.allocVerts.start;
            for (int i = 0; i < indexCount; i += 3)
            {
                indices[i + 0] = (UInt16)(meshIndices[i + 0] + actualIndexOffset);
                indices[i + 1] = (UInt16)(meshIndices[i + 2] + actualIndexOffset);
                indices[i + 2] = (UInt16)(meshIndices[i + 1] + actualIndexOffset);
            }
        }

        public State state;
        public MeshNode maskRegister, maskUnregister;
        internal override void Draw(DrawChainState dcs)
        {
            bool nextWasMeshRenderer = dcs.nextInChainIsMeshRenderer;
            dcs.nextInChainIsMeshRenderer = true;
            MeshRenderer.DrawMeshChain(dcs, state, maskRegister);
            UIRenderDevice.ContinueChain(contents, dcs, true);
            dcs.nextInChainIsMeshRenderer = nextWasMeshRenderer;
            MeshRenderer.DrawMeshChain(dcs, state, maskUnregister);
        }

        #region Internals
        static void FlipWindingOrder(NativeSlice<UInt16> indices)
        {
            int indexCount = indices.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                UInt16 t = indices[i];
                indices[i] = indices[i + 1];
                indices[i + 1] = t;
            }
        }

        static void UseMaskPosZ(NativeSlice<Vertex> vertices)
        {
            int vertexCount = vertices.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = vertices[i];
                v.position.z = k_MaskPosZ;
                vertices[i] = v;
            }
        }

        #endregion
    }

    internal sealed class ZoomPanRenderer : ContentRendererBase
    {
        public ZoomPanRenderer() { type = RendererTypes.ZoomPanRenderer; }

        public Matrix4x4 viewMatrix; // Only translation and positive scaling, dont include rotation

        internal override void Draw(DrawChainState dcs)
        {
            var prevViewMatrix = dcs.view;
            dcs.view = viewMatrix * prevViewMatrix;
            GL.modelview = dcs.view;
            dcs.InvalidateState(); // Due to changing the state's view
            UIRenderDevice.ContinueChain(contents, dcs, false);
            dcs.view = prevViewMatrix;
            GL.modelview = prevViewMatrix;
            dcs.InvalidateState();
        }
    }

    internal sealed class ImmediateRenderer : RendererBase
    {
        public ImmediateRenderer() { type = RendererTypes.ImmediateRenderer; }

        internal delegate void DrawImmediateDelegate();

        internal DrawImmediateDelegate immediateHandler;
        internal Matrix4x4 worldTransform;
        internal Rect worldClip;

        static readonly CustomSampler k_Sampler = CustomSampler.Create("UIR.ImmediateRenderer");

        internal override void Draw(DrawChainState dcs)
        {
            // The immediate callback may do anything, including changing the current material.
            // So, we invalidate the state here to make sure the next MeshRenderer material is properly set.
            dcs.InvalidateState();

            // Start a GUIClip scope since most GL/Handles use cases make use of the _GUIClipTexture
            k_Sampler.Begin();
            using (new GUIClip.ParentClipScope(worldTransform, worldClip))
                immediateHandler();
            k_Sampler.End();
        }
    }

    internal static class RectUtil
    {
        internal static Rect Transform(this Rect rect, Matrix4x4 matrix)
        {
            Vector3 v = new Vector3(rect.position.x, rect.position.y, 0);
            Vector3 pos = matrix.MultiplyPoint(v);
            v.x += rect.width;
            v.y += rect.height;
            Vector3 pos2 = matrix.MultiplyPoint(v);
            return new Rect(pos, pos2 - pos);
        }
    }
}
