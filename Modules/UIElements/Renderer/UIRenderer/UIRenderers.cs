// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

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
        Immediate,
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

        public void Reset(Rect _viewport, Matrix4x4 _projection)
        {
            viewport = _viewport;
            projection = _projection;
            view.Clear();
            view.Push(new ViewTransform { transform = Matrix4x4.identity, clipRect = k_UnlimitedRect.ToVector4() });
            scissor.Clear();
            scissor.Push(k_UnlimitedRect);
        }

        internal Rect viewport; // In points, not in pixels
        internal Matrix4x4 projection;
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

        internal void ExecuteNonDrawMesh(DrawParams drawParams, bool straightY, float pixelsPerPoint, ref Exception immediateException)
        {
            switch (type)
            {
                case CommandType.Immediate:
                {
                    if (immediateException != null)
                        break;

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
                    GL.LoadProjectionMatrix(drawParams.projection);
                    Utility.ProfileImmediateRendererEnd();

                    if (hasScissor)
                        Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(drawParams.scissor.Peek(), drawParams.viewport, pixelsPerPoint));
                    break;
                }
                case CommandType.PushView:
                    var parent = owner.hierarchy.parent;
                    Vector4 clipRect;
                    if (parent != null)
                        clipRect = RectToScreenSpace(parent.worldClip, drawParams.projection, straightY);
                    else
                        clipRect = UIRUtility.ToVector4(DrawParams.k_UnlimitedRect);
                    var vt = new ViewTransform() { transform = owner.worldTransform, clipRect = clipRect };
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
                    Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(elemRect, drawParams.viewport, pixelsPerPoint));
                    break;
                case CommandType.PopScissor:
                    drawParams.scissor.Pop();
                    Rect prevRect = drawParams.scissor.Peek();
                    if (prevRect.x == DrawParams.k_UnlimitedRect.x)
                        Utility.DisableScissor();
                    else Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(prevRect, drawParams.viewport, pixelsPerPoint));
                    break;
            }
        }

        static Vector4 RectToScreenSpace(Rect rc, Matrix4x4 projection, bool straightY)
        {
            var viewport = Utility.GetActiveViewport();
            var minClipSpace = projection.MultiplyPoint(new Vector3(rc.xMin, rc.yMin, UIRUtility.k_MeshPosZ));
            var maxClipSpace = projection.MultiplyPoint(new Vector3(rc.xMax, rc.yMax, UIRUtility.k_MeshPosZ));
            float yScale = straightY ? 0.5f : -0.5f;
            var x1 = (minClipSpace.x * 0.5f + 0.5f) * viewport.width;
            var x2 = (maxClipSpace.x * 0.5f + 0.5f) * viewport.width;
            var y1 = (minClipSpace.y * yScale + 0.5f) * viewport.height;
            var y2 = (maxClipSpace.y * yScale + 0.5f) * viewport.height;
            return new Vector4(Mathf.Min(x1, x2), Mathf.Min(y1, y2), Mathf.Max(x1, x2), Mathf.Max(y1, y2));
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

        static RectInt RectPointsToPixelsAndFlipYAxis(Rect rect, Rect viewport, float pixelsPerPoint)
        {
            var r = new RectInt(0, 0, 0, 0);
            r.x = Mathf.RoundToInt(rect.x * pixelsPerPoint);
            r.y = Mathf.RoundToInt((viewport.height - rect.yMax) * pixelsPerPoint);
            r.width = Mathf.RoundToInt(rect.width * pixelsPerPoint);
            r.height = Mathf.RoundToInt(rect.height * pixelsPerPoint);
            return r;
        }
    }
}
