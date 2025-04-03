// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal enum VertexFlags
    {
        // Vertex Type
        // These values are like enum values, they are mutually exclusive. Only one may be specified on a vertex.
        IsSolid = 0,
        IsText = 1,
        IsTextured = 2,
        IsDynamic = 3,
        IsSvgGradients = 4, // Gradient/Texture-less SVG do NOT use this flag
    }

    internal struct State
    {
        public Material material;
        public TextureId texture;
        public int stencilRef;
        public float sdfScale;
        public float sharpness;
        public bool isPremultiplied;
    }

    internal enum CommandType
    {
        Draw,
        ImmediateCull, Immediate,
        PushView, PopView,
        PushScissor, PopScissor,
        PushDefaultMaterial, PopDefaultMaterial,
        BeginDisable, EndDisable,
        CutRenderChain
    }

    internal class DrawParams
    {
        internal static readonly Rect k_UnlimitedRect = new Rect(-100000, -100000, 200000, 200000);
        internal static readonly Rect k_FullNormalizedRect = new Rect(-1, -1, 2, 2);


        public void Reset()
        {
            view.Clear();
            view.Push(Matrix4x4.identity);
            scissor.Clear();
            scissor.Push(k_UnlimitedRect);
            defaultMaterial.Clear();
        }

        internal readonly Stack<Matrix4x4> view = new Stack<Matrix4x4>(8);
        internal readonly Stack<Rect> scissor = new Stack<Rect>(8);
        internal readonly List<Material> defaultMaterial = new List<Material>(8);
    }

    internal class RenderChainCommand : LinkedPoolItem<RenderChainCommand>
    {
        internal RenderData owner;
        internal RenderChainCommand prev, next;
        internal bool isTail; // Is this a tail command

        internal CommandType type;

        internal State state;
        internal MeshHandle mesh;
        internal int indexOffset; // Offset within the mesh (remember: there might be multiple commands per mesh e.g. one sub-range for background, another for border, etc)
        internal int indexCount;
        internal Action callback; // Immediate render command only
        private static readonly int k_ID_MainTex = Shader.PropertyToID("_MainTex");

        static ProfilerMarker s_ImmediateOverheadMarker = new ProfilerMarker("UIR.ImmediateOverhead");

        internal void Reset()
        {
            owner = null;
            prev = next = null;
            isTail = false;
            type = CommandType.Draw;
            state = new State();
            mesh = null;
            indexOffset = indexCount = 0;
            callback = null;
        }

        internal void ExecuteNonDrawMesh(DrawParams drawParams, float pixelsPerPoint, ref Exception immediateException)
        {
            switch (type)
            {
                case CommandType.ImmediateCull:
                {
                    // TODO: Validate VisualElement access for RenderTrees
                    RectInt worldRect = RectPointsToPixelsAndFlipYAxis(owner.owner.worldBound, pixelsPerPoint);
                    if (!worldRect.Overlaps(Utility.GetActiveViewport()))
                        break;

                    // Element isn't culled, follow through the normal immediate callback procedure
                    goto case CommandType.Immediate;
                }
                case CommandType.Immediate:
                {
                    if (immediateException != null)
                        break;

                    s_ImmediateOverheadMarker.Begin();
                    Matrix4x4 oldProjection = Utility.GetUnityProjectionMatrix();
                    Camera oldCamera = Camera.current;
                    RenderTexture oldRT = RenderTexture.active;
                    bool hasScissor = drawParams.scissor.Count > 1; // We always expect the "unbound" scissor rectangle to exists
                    if (hasScissor)
                        Utility.DisableScissor(); // Disable scissor since most IMGUI code assume it's inactive

                    // TODO: Validate VisualElement access for RenderTrees
                    using (new GUIClip.ParentClipScope(owner.owner.worldTransform, owner.owner.worldClip))
                    {
                        s_ImmediateOverheadMarker.End();
                        try
                        {
                            callback();
                        }
                        catch (Exception e)
                        {
                            immediateException = e;
                        }
                        s_ImmediateOverheadMarker.Begin();
                    }

                    Camera.SetupCurrent(oldCamera);
                    RenderTexture.active = oldRT;
                    GL.modelview = drawParams.view.Peek();
                    GL.LoadProjectionMatrix(oldProjection);

                    if (hasScissor)
                        Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(drawParams.scissor.Peek(), pixelsPerPoint));

                    s_ImmediateOverheadMarker.End();
                    break;
                }
                case CommandType.PushView:
                {
                    // TODO: Offset the clipping rect by the offset within the RT and the post-effect margin

                    // Transform
                    UIRUtility.ComputeMatrixRelativeToRenderTree(owner, out var matrix);
                    drawParams.view.Push(matrix);
                    GL.modelview = matrix;
                    // Scissors
                    Rect clipRect;
                    var parent = owner.parent;
                    if (parent != null)
                        clipRect = parent.clippingRect;
                    else
                        clipRect = DrawParams.k_FullNormalizedRect;
                    PushScissor(drawParams, clipRect, pixelsPerPoint);
                    break;
                }
                case CommandType.PopView:
                {
                    // Transform
                    drawParams.view.Pop();
                    GL.modelview = drawParams.view.Peek();
                    // Scissors
                    PopScissor(drawParams, pixelsPerPoint);
                    break;
                }
                case CommandType.PushScissor:
                {
                    PushScissor(drawParams, owner.clippingRect, pixelsPerPoint);
                    break;
                }
                case CommandType.PopScissor:
                {
                    PopScissor(drawParams, pixelsPerPoint);
                    break;
                }

                // Logic of both command is entirely in UIRenderDevice as it need access to the local variable defaultMat
                case CommandType.PushDefaultMaterial:
                case CommandType.PopDefaultMaterial:
                    break;
            }
        }

        internal static void PushScissor(DrawParams drawParams, Rect scissor, float pixelsPerPoint)
        {
            // TODO: Offset the clipping rect by the offset within the RT and the post-effect margin
            Rect elemRect = CombineScissorRects(scissor, drawParams.scissor.Peek());
            drawParams.scissor.Push(elemRect);
            Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(elemRect, pixelsPerPoint));
        }

        internal static void PopScissor(DrawParams drawParams, float pixelsPerPoint)
        {
            drawParams.scissor.Pop();
            Rect prevRect = drawParams.scissor.Peek();
            if (prevRect.x == DrawParams.k_UnlimitedRect.x)
                Utility.DisableScissor();
            else
                Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(prevRect, pixelsPerPoint));
        }

        void Blit(Texture source, RenderTexture destination, float depth)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            RenderTexture.active = destination;
            state.material.SetTexture(k_ID_MainTex, source);
            state.material.SetPass(0);

            // Clockwise winding: we don't support blit under a mask
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, depth);
            GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, depth);
            GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, depth);
            GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, depth);
            GL.End();
            GL.PopMatrix();
        }

        static Vector4 RectToClipSpace(Rect rc)
        {
            // Since the shader compares positions multiplied by the MVP matrix, then we must ensure to use
            // the same MVP matrices the shader uses.. namely, the GPU projection matrix
            Matrix4x4 projection = Utility.GetDeviceProjectionMatrix();
            var minClipSpace = projection.MultiplyPoint(new Vector3(rc.xMin, rc.yMin, UIRUtility.k_MeshPosZ));
            var maxClipSpace = projection.MultiplyPoint(new Vector3(rc.xMax, rc.yMax, UIRUtility.k_MeshPosZ));
            return new Vector4(
                Mathf.Min(minClipSpace.x, maxClipSpace.x), Mathf.Min(minClipSpace.y, maxClipSpace.y),
                Mathf.Max(minClipSpace.x, maxClipSpace.x), Mathf.Max(minClipSpace.y, maxClipSpace.y));
        }

        static Rect CombineScissorRects(Rect r0, Rect r1)
        {
            var r = new Rect(0, 0, 0, 0);
            r.x = Math.Max(r0.x, r1.x);
            r.y = Math.Max(r0.y, r1.y);
            r.xMax = Math.Max(r.x, Math.Min(r0.xMax, r1.xMax));
            r.yMax = Math.Max(r.y, Math.Min(r0.yMax, r1.yMax));
            return r;
        }

        static RectInt RectPointsToPixelsAndFlipYAxis(Rect rect, float pixelsPerPoint)
        {
            float viewportHeight = Utility.GetActiveViewport().height;
            var r = new RectInt(0, 0, 0, 0);
            r.x = Mathf.RoundToInt(rect.x * pixelsPerPoint);
            r.y = Mathf.RoundToInt(viewportHeight - rect.yMax * pixelsPerPoint);
            r.width = Mathf.RoundToInt(rect.width * pixelsPerPoint);
            r.height = Mathf.RoundToInt(rect.height * pixelsPerPoint);
            return r;
        }
    }
}
