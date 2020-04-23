using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    internal enum VertexFlags
    {
        // Vertex Type
        // These values are like enum values, they are mutually exclusive. Only one may be specified on a vertex.
        IsSolid = 0,
        IsText = 1,
        IsAtlasTexturedPoint = 2,
        IsAtlasTexturedBilinear = 3,
        IsCustomTextured = 4,
        IsEdge = 5, // X and Y can grow/shrink according to displacement
        IsEdgeNoShrinkX = 6,
        IsEdgeNoShrinkY = 7,
        IsSVGGradients = 8,
        IsCustomSVGGradients = 9,

        LastType = 10,
    }

    internal struct State
    {
        public Material material;
        public Texture custom, font;
    }

    internal enum CommandType
    {
        Draw,
        ImmediateCull, Immediate,
        PushView, PopView,
        PushScissor, PopScissor
    }

    internal struct ViewTransform
    {
        internal Matrix4x4 transform;
        internal Vector4 clipRect;
    }

    internal class DrawParams
    {
        internal static readonly Rect k_UnlimitedRect = new Rect(-100000, -100000, 200000, 200000);
        internal static readonly Rect k_FullNormalizedRect = new Rect(-1, -1, 2, 2);


        public void Reset()
        {
            view.Clear();
            view.Push(new ViewTransform { transform = Matrix4x4.identity, clipRect = UIRUtility.ToVector4(k_FullNormalizedRect) });
            scissor.Clear();
            scissor.Push(k_UnlimitedRect);
        }

        internal readonly Stack<ViewTransform> view = new Stack<ViewTransform>(8);
        internal readonly Stack<Rect> scissor = new Stack<Rect>(8);
    }

    internal class RenderChainCommand : PoolItem
    {
        internal VisualElement owner;
        internal RenderChainCommand prev, next;
        internal bool closing; // Is this a closing command

        internal CommandType type;

        internal State state;
        internal MeshHandle mesh;
        internal int indexOffset, indexCount;
        internal Action callback; // Immediate render command only

        internal void Reset()
        {
            owner = null;
            prev = next = null;
            closing = false;
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
                    RectInt worldRect = RectPointsToPixelsAndFlipYAxis(owner.worldBound, pixelsPerPoint);
                    if (!worldRect.Overlaps(Utility.GetActiveViewport()))
                        break;

                    // Element isn't culled, follow through the normal immediate callback procedure
                    goto case CommandType.Immediate;
                }
                case CommandType.Immediate:
                {
                    if (immediateException != null)
                        break;

                    Matrix4x4 oldProjection = Utility.GetUnityProjectionMatrix();
                    bool hasScissor = drawParams.scissor.Count > 1; // We always expect the "unbound" scissor rectangle to exists
                    if (hasScissor)
                        Utility.DisableScissor(); // Disable scissor since most IMGUI code assume it's inactive

                    Utility.ProfileImmediateRendererBegin();
                    try
                    {
                        using (new GUIClip.ParentClipScope(owner.worldTransform, owner.worldClip))
                            callback();
                    }
                    catch (Exception e)
                    {
                        immediateException = e;
                    }

                    GL.modelview = drawParams.view.Peek().transform;
                    GL.LoadProjectionMatrix(oldProjection);
                    Utility.ProfileImmediateRendererEnd();

                    if (hasScissor)
                        Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(drawParams.scissor.Peek(), pixelsPerPoint));
                    break;
                }
                case CommandType.PushView:
                    var vt = new ViewTransform() { transform = owner.worldTransform, clipRect = RectToClipSpace(owner.worldClip) };
                    drawParams.view.Push(vt);
                    GL.modelview = vt.transform;
                    break;
                case CommandType.PopView:
                    drawParams.view.Pop();
                    GL.modelview = drawParams.view.Peek().transform;
                    break;
                case CommandType.PushScissor:
                    Rect elemRect = CombineScissorRects(owner.worldClip, drawParams.scissor.Peek());
                    drawParams.scissor.Push(elemRect);
                    Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(elemRect, pixelsPerPoint));
                    break;
                case CommandType.PopScissor:
                    drawParams.scissor.Pop();
                    Rect prevRect = drawParams.scissor.Peek();
                    if (prevRect.x == DrawParams.k_UnlimitedRect.x)
                        Utility.DisableScissor();
                    else Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(prevRect, pixelsPerPoint));
                    break;
            }
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
