// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    [Flags]
    enum CommandType
    {
        Draw                          = 1 << 0,
        Immediate                     = 1 << 1,
        ImmediateCull                 = 1 << 2,
        PushView                      = 1 << 3,
        PopView                       = 1 << 4,
        PushScissor                   = 1 << 5,
        PopScissor                    = 1 << 6,
        PushDefaultMaterial           = 1 << 7,
        PopDefaultMaterial            = 1 << 8,
        BeginDisable                  = 1 << 9,
        EndDisable                    = 1 << 10,
        CutRenderChain                = 1 << 11,
        BeginPanelComponent           = 1 << 12,  // Profiler-only marker
        EndPanelComponent             = 1 << 13,  // Profiler-only marker
        GenerateBackdropFilterTexture = 1 << 14,

        // Pair masks for hot-path "is any variant of X" checks.
        AnyImmediate        = Immediate | ImmediateCull | GenerateBackdropFilterTexture,
        AnyView             = PushView | PopView,
        AnyScissor          = PushScissor | PopScissor,
        AnyDefaultMaterial  = PushDefaultMaterial | PopDefaultMaterial,
        AnyDisable          = BeginDisable | EndDisable,
        AnyPanelComponent   = BeginPanelComponent | EndPanelComponent,
    }

    [Flags]
    enum CommandFlags
    {
        None = 0,

        IsPremultiplied = 1 << 0, // Used for textures that are premultiplied (e.g. filter output)

        // 3 bits store mutually exclusive render types. When any bit is set, the other render types are excluded from the shader.
        ForceRenderTypeBitOffset = 1,
        ForceRenderTypeSolid = 1 << ForceRenderTypeBitOffset,
        ForceRenderTypeTextured = 2 << ForceRenderTypeBitOffset,
        ForceRenderTypeText = 3 << ForceRenderTypeBitOffset,
        ForceRenderTypeSvgGradient = 4 << ForceRenderTypeBitOffset,
        ForceRenderTypeBits = 7 << ForceRenderTypeBitOffset,

        ForceSingleTextureSlot = 1 << 4,

        SkipForceGamma = 1 << 5,
    }

    class DrawParams
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
            drawBounds = Rect.zero;
        }

        internal readonly Stack<Matrix4x4> view = new Stack<Matrix4x4>(8);
        internal readonly Stack<Rect> scissor = new Stack<Rect>(8);
        internal readonly List<Material> defaultMaterial = new(8);
        internal readonly List<MaterialPropertyBlock> props = new(8);

        // Always equal to drawBounds.min; derived rather than stored so the two can't drift out of sync.
        internal Vector2 boundsMin => drawBounds.min;
        internal Rect drawBounds;
    }

    class RenderChainCommand : LinkedPoolItem<RenderChainCommand>
    {
        public RenderData owner;
        public RenderChainCommand prev, next;
        public CommandType type;
        public CommandFlags flags;
        public Material material;
        public MaterialPropertyBlock userProps;
        public TextureId texture;
        public int stencilRef;
        public float sdfScale;
        public float sharpness;
        public MeshHandle mesh;
        public int indexOffset; // Offset within the mesh (remember: there might be multiple commands per mesh e.g. one sub-range for background, another for border, etc)
        public int indexCount;
        public Action callback; // Immediate render command only
        // Set on BeginPanelComponent commands only.
        public EntityId panelComponentId;

        static ProfilerMarker s_ImmediateOverheadMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.ImmediateOverhead");

        public RenderChainCommand()
        {
            Reset();
        }

        public void Reset()
        {
            owner = null;
            prev = next = null;
            type = CommandType.Draw;
            flags = 0;
            material = null;
            userProps = null;
            texture = TextureId.invalid;
            stencilRef = 0;
            sdfScale = 0;
            sharpness = 0;
            mesh = null;
            indexOffset = indexCount = 0;
            callback = null;
            panelComponentId = EntityId.None;
        }

        public void ExecuteNonDrawMesh(DrawParams drawParams, float pixelsPerPoint, ref Exception immediateException)
        {
            switch (type)
            {
                case CommandType.ImmediateCull:
                {
                    // TODO: Validate VisualElement access for RenderTrees
                    RectInt worldRect = RectPointsToPixelsAndFlipYAxis(owner.owner.worldBound, drawParams.boundsMin, pixelsPerPoint);
                    if (!worldRect.Overlaps(Utility.GetActiveViewport()))
                        break;

                    // Element isn't culled, follow through the normal immediate callback procedure
                    goto case CommandType.Immediate;
                }
                case CommandType.Immediate:
                {
                    if (immediateException != null)
                        break;

                    Matrix4x4 oldProjection;
                    Camera oldCamera;
                    RenderTexture oldRT;
                    using (s_ImmediateOverheadMarker.Auto())
                    {
                        if (owner.compositeOpacity < 0.001f)
                            break;

                        oldProjection = Utility.GetUnityProjectionMatrix();
                        oldCamera = Camera.current;
                        oldRT = RenderTexture.active;

                        UIRUtility.ComputeMatrixRelativeToRenderTree(owner, out var matrix);
                        GL.modelview = matrix;

                        PushScissor(drawParams, owner.clippingRect, pixelsPerPoint);
                    }
                    try
                    {
                        callback();
                    }
                    catch (Exception e)
                    {
                        immediateException = e;
                    }
                    using (s_ImmediateOverheadMarker.Auto())
                    {
                        PopScissor(drawParams, pixelsPerPoint);

                        Camera.SetupCurrent(oldCamera);
                        RenderTexture.active = oldRT;
                        GL.modelview = drawParams.view.Peek();
                        GL.LoadProjectionMatrix(oldProjection);
                        GL.invertCulling = false;
                    }
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

                case CommandType.GenerateBackdropFilterTexture:
                {
                    VisualElement ve = owner.owner;

                    // GenerateBackdropFilterTexture restores only RenderTexture.active: rebind depth/stencil too
                    // or stencil clipping breaks for UI drawn after this element (impossible when targeting the
                    // backbuffer — SetRenderTarget can't mix a screen color with an RT depth — but the internal
                    // save/restore re-binds it correctly there). The rebind resets the viewport: restore it, or
                    // filtered sub-tree quads shift. A stale scissor would clip the filter temp draws.
                    bool restoreTargetState = RenderTexture.active != null;

                    RenderBuffer oldColor = default, oldDepth = default;
                    if (restoreTargetState)
                    {
                        oldColor = Graphics.activeColorBuffer;
                        oldDepth = Graphics.activeDepthBuffer;
                    }

                    RectInt oldViewport = Utility.GetActiveViewport();
                    Utility.DisableScissor();

                    BackdropFilterHelper.GenerateBackdropFilterTexture(drawParams, ve, pixelsPerPoint, owner);

                    if (restoreTargetState)
                        Graphics.SetRenderTarget(oldColor, oldDepth);
                    GL.Viewport(UIRUtility.CastToRect(oldViewport));

                    // Restore the entry scissor (mirrors PopScissor).
                    Rect scissor = drawParams.scissor.Peek();
                    if (scissor.x == DrawParams.k_UnlimitedRect.x)
                        Utility.DisableScissor();
                    else
                        Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(scissor, drawParams.boundsMin, pixelsPerPoint));
                    break;
                }
            }
        }

        public static void PushScissor(DrawParams drawParams, Rect scissor, float pixelsPerPoint)
        {
            Rect elemRect = CombineScissorRects(scissor, drawParams.scissor.Peek());
            drawParams.scissor.Push(elemRect);
            Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(elemRect, drawParams.boundsMin, pixelsPerPoint));
        }

        public static void PopScissor(DrawParams drawParams, float pixelsPerPoint)
        {
            drawParams.scissor.Pop();
            Rect prevRect = drawParams.scissor.Peek();
            if (prevRect.x == DrawParams.k_UnlimitedRect.x)
                Utility.DisableScissor();
            else
                Utility.SetScissorRect(RectPointsToPixelsAndFlipYAxis(prevRect, drawParams.boundsMin, pixelsPerPoint));
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

        internal static RectInt RectPointsToPixelsAndFlipYAxis(Rect rect, Vector2 boundsMin, float pixelsPerPoint)
        {
            // UUM-142586: Offset the scissor rect by boundsMin. This matters for nested render trees whose
            // bounds are inflated by filters or contain negatively-positioned descendants.
            return RectPointsToPixels(rect, boundsMin, pixelsPerPoint, pixelsPerPoint, Utility.GetActiveViewport());
        }

        // Maps a points-space rect into viewport pixels: translate by origin, scale per-axis, flip Y within the viewport.
        internal static RectInt RectPointsToPixels(Rect rect, Vector2 origin, float scaleX, float scaleY, RectInt viewport)
        {
            var r = new RectInt(0, 0, 0, 0);
            r.x = viewport.x + Mathf.RoundToInt((rect.x - origin.x) * scaleX);
            r.y = viewport.y + viewport.height - Mathf.RoundToInt((rect.yMax - origin.y) * scaleY);
            r.width = Mathf.RoundToInt(rect.width * scaleX);
            r.height = Mathf.RoundToInt(rect.height * scaleY);
            return r;
        }
    }
}
