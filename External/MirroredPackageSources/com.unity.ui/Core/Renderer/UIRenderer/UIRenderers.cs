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
        IsTextured = 2,
        IsDynamic = 3,
        IsSvgGradients = 4, // Gradient/Texture-less SVG do NOT use this flag
        IsEdge = 5, // X and Y can grow/shrink according to displacement
        IsEdgeNoShrinkX = 6,
        IsEdgeNoShrinkY = 7,
        // The only use case outside the package is for GraphView edges. In order to preserve binary compatibility,
        // the value MUST not change, so calling it "Last" does not make sense if we cannot increase the value.
        // For this reason, this is being deprecated.
        [Obsolete("Enum member VertexFlags.LastType has been deprecated. Use VertexFlags.IsGraphViewEdge instead.")]
        LastType = 10,
        IsGraphViewEdge = 10,
    }

    internal struct State
    {
        public Material material;
        public Texture custom, font;
        public float fontTexSDFScale;
        public TextureId texture;
    }

    internal enum CommandType
    {
        Draw,
        ImmediateCull, Immediate,
        PushView, PopView,
        PushScissor, PopScissor,
        PushRenderTexture, PopRenderTexture,
        BlitToPreviousRT, //From Active target to previous on RT stack
        PushDefaultMaterial, PopDefaultMaterial,
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
            renderTexture.Clear();
            defaultMaterial.Clear();
        }

        internal readonly Stack<ViewTransform> view = new Stack<ViewTransform>(8);
        internal readonly Stack<Rect> scissor = new Stack<Rect>(8);

        // Using list instead of stack to allow access to all elements in previson for the blit of any RT into any RT.
        // Right now, because blit change the active RT, pop use this to release the temporary RT before removing it from the stack.
        // The workaround would be that blit restore the active RT=> may have a performance impact.
        internal readonly List<RenderTexture> renderTexture = new List<RenderTexture>(8);


        internal readonly List<Material> defaultMaterial = new List<Material>(8);
    }

    internal class RenderChainCommand : LinkedPoolItem<RenderChainCommand>
    {
        internal VisualElement owner;
        internal RenderChainCommand prev, next;
        internal bool closing; // Is this a closing command

        internal CommandType type;

        internal State state;
        internal MeshHandle mesh;
        internal int indexOffset, indexCount;
        internal Action callback; // Immediate render command only
        private static readonly int k_ID_MainTex = Shader.PropertyToID("_MainTex");

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

                case CommandType.PushRenderTexture:
                {
                    RectInt viewport = Utility.GetActiveViewport();
                    RenderTexture rt = RenderTexture.GetTemporary(viewport.width, viewport.height, 24, RenderTextureFormat.ARGBHalf);
                    RenderTexture.active = rt;
                    GL.Clear(true, true, new Color(0, 0, 0, 0), UIRUtility.k_ClearZ);
                    drawParams.renderTexture.Add(RenderTexture.active);
                    break;
                }

                case CommandType.PopRenderTexture:
                {
                    int index = drawParams.renderTexture.Count - 1;
                    Debug.Assert(index > 0);    //Check that we have something to pop, We should never pop the last(original)one

                    // Only supported use case for now is to do a blit befor the pop.
                    // This should set the active RenderTexture to index-1 and we should not have this warning.
                    Debug.Assert(drawParams.renderTexture[index - 1] == RenderTexture.active, "Content of previous render texture was probably not blitted");

                    var rt = drawParams.renderTexture[index];
                    if (rt != null)
                        RenderTexture.ReleaseTemporary(rt);
                    drawParams.renderTexture.RemoveAt(index);
                }
                break;

                case CommandType.BlitToPreviousRT:
                {
                    // Currently the command only blit to the previous RT, but this could be expanded if we use
                    // indexCount and indexOffset to point to specific indices in the renderTextureBuffer.
                    // The main difficulty is to memorize the current renderTexture depth to get the indices in the RenderChain
                    // as we can edit the stack in the middle and that would requires rewriting previous/ subsequent commands

                    //Also, there is currently no way to have a permanently assigned rt to be used as cache.

                    var source = drawParams.renderTexture[drawParams.renderTexture.Count - 1];
                    var destination = drawParams.renderTexture[drawParams.renderTexture.Count - 2];


                    // Note: Graphics.Blit set the arctive RT => RT is not restored and it is expected to be chaged before PopRenderTexture
                    //TODO check blit code for other side effect
                    Debug.Assert(source == RenderTexture.active, "Unexpected render target change: Current renderTarget is not the one on the top of the stack");

                    //The following lines are equivalent to
                    //Graphics.Blit(source, destination, state.material);
                    //except the vertex are at the specified depth
                    var flipped = (indexOffset != 0);
                    Blit(source, destination, UIRUtility.k_MeshPosZ, flipped);
                }
                break;

//Logic of both command is entirely in UIRenderDevice as it need access to the local variable defaultMat
                case CommandType.PushDefaultMaterial:
                    break;

                case CommandType.PopDefaultMaterial:
                    break;
            }
        }

        private void Blit(Texture source, RenderTexture destination, float depth, bool flip = false)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            RenderTexture.active = destination;
            state.material.SetTexture(k_ID_MainTex, source);
            state.material.SetPass(0);
            GL.Begin(GL.QUADS);
            if (!flip)
            {
                GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, depth);
                GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, depth);
                GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, depth);
                GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, depth);
            }
            else
            {
                GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, depth);
                GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, depth);
                GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, depth);
                GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, depth);
            }
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
