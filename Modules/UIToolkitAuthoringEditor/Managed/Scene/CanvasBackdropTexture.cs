// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.UIToolkit.Editor;

sealed class CanvasBackdropTexture
{
    // Everything a composite depends on; value-compared to decide whether a redraw is needed.
    public struct Settings : IEquatable<Settings>
    {
        public Rect CanvasRect; // canvas rect in render-target pixels: offset and size at the current zoom
        public float PixelsPerPoint;
        public Texture2D CheckerTexture; // borrowed from UICanvas's CheckerboardBackground element; never destroyed here
        public int CheckerCellSize; // in points
        public CanvasBackgroundType BackgroundType;
        public Color BackgroundColor;
        public float BackgroundColorOpacity;
        public Texture2D BackgroundImage;
        public Hash128 BackgroundImageContentsHash; // an in-place reimport keeps the instance but changes the pixels
        public float BackgroundImageOpacity;
        public ScaleMode BackgroundImageScaleMode;

        public bool Equals(Settings other)
        {
            // ReferenceEquals on textures: replacing a destroyed (fake-null) asset with real null must still
            // count as a change, or the stale pixels stay baked into the composite.
            return CanvasRect == other.CanvasRect &&
                PixelsPerPoint == other.PixelsPerPoint &&
                ReferenceEquals(CheckerTexture, other.CheckerTexture) &&
                CheckerCellSize == other.CheckerCellSize &&
                BackgroundType == other.BackgroundType &&
                BackgroundColor == other.BackgroundColor &&
                BackgroundColorOpacity == other.BackgroundColorOpacity &&
                ReferenceEquals(BackgroundImage, other.BackgroundImage) &&
                BackgroundImageContentsHash == other.BackgroundImageContentsHash &&
                BackgroundImageOpacity == other.BackgroundImageOpacity &&
                BackgroundImageScaleMode == other.BackgroundImageScaleMode;
        }
    }

    // Graphics.DrawTexture modulates as 2 * color * texel, so 0.5 is the neutral value.
    const float k_DrawTextureNeutral = 0.5f;

    RenderTexture m_Texture;
    Settings m_Settings;
    bool m_Dirty = true;

    public Texture Update(int width, int height, in Settings settings)
    {
        if (width <= 0 || height <= 0)
        {
            Release();
            return null;
        }

        var graphicsFormat = PanelElement.GetRenderTextureGraphicsFormat();
        if (m_Texture == null || m_Texture.width != width || m_Texture.height != height ||
            m_Texture.graphicsFormat != graphicsFormat)
        {
            var descriptor = new RenderTextureDescriptor(width, height, graphicsFormat, GraphicsFormat.None);
            if (m_Texture == null)
            {
                m_Texture = new RenderTexture(descriptor)
                {
                    name = "CanvasBackdrop",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                m_Texture.Release();
                m_Texture.descriptor = descriptor;
                m_Texture.Create();
            }
            m_Dirty = true;
        }

        if (!m_Settings.Equals(settings))
        {
            m_Settings = settings;
            m_Dirty = true;
        }

        // IsCreated: a GPU device reset invalidates the native texture while the managed size still matches.
        if (m_Dirty || !m_Texture.IsCreated())
            Redraw();

        return m_Texture;
    }

    public void Release()
    {
        if (m_Texture != null)
        {
            m_Texture.Release();
            UnityEngine.Object.DestroyImmediate(m_Texture);
            m_Texture = null;
        }

        m_Dirty = true;
    }

    void Redraw()
    {
        // The finallys keep a draw failure from corrupting the ambient render state for the rest of the frame:
        // the only enclosing handler swallows the exception without restoring anything.
        var previous = RenderTexture.active;
        try
        {
            RenderTexture.active = m_Texture;
            GL.Clear(false, true, Color.clear);

            var rect = m_Settings.CanvasRect;
            if (rect.width > 0 && rect.height > 0)
            {
                GL.PushMatrix();
                try
                {
                    GL.LoadPixelMatrix(0, m_Texture.width, m_Texture.height, 0);

                    DrawCheckerboard();

                    // The GUI blend accumulates alpha additively, so the overlays assume the checkerboard base is opaque.
                    switch (m_Settings.BackgroundType)
                    {
                        case CanvasBackgroundType.Color:
                            DrawColorOverlay();
                            break;
                        case CanvasBackgroundType.Image when m_Settings.BackgroundImage:
                            DrawImageOverlay();
                            break;
                    }
                }
                finally
                {
                    GL.PopMatrix();
                }
            }
        }
        finally
        {
            RenderTexture.active = previous;
        }

        m_Dirty = false;
    }

    void DrawCheckerboard()
    {
        var texture = m_Settings.CheckerTexture;
        if (texture == null || texture.width <= 0 || m_Settings.CheckerCellSize <= 0 || m_Settings.PixelsPerPoint <= 0)
            return;

        // Cell size is in points and the rect is in render-target pixels; one repeat of the repeat-wrapped
        // texture covers texture.width cells, so the cells keep a fixed on-screen size at any zoom.
        var tile = m_Settings.CheckerCellSize * m_Settings.PixelsPerPoint * texture.width;
        var rect = m_Settings.CanvasRect;
        // DrawTexture maps sourceRect.y to the rect's BOTTOM edge, which moves when the canvas resizes; pin the
        // top edge at a fixed one-texel v instead so the grid and its parity stay anchored to the canvas
        // top-left, like CheckerboardBackground's own quad grid.
        Graphics.DrawTexture(rect, texture,
            new Rect(0, 1f / texture.height - rect.height / tile, rect.width / tile, rect.height / tile), 0, 0, 0, 0,
            new Color(k_DrawTextureNeutral, k_DrawTextureNeutral, k_DrawTextureNeutral, k_DrawTextureNeutral));
    }

    void DrawColorOverlay()
    {
        var color = m_Settings.BackgroundColor;
        var modulate = new Color(
            color.r * k_DrawTextureNeutral,
            color.g * k_DrawTextureNeutral,
            color.b * k_DrawTextureNeutral,
            color.a * m_Settings.BackgroundColorOpacity * k_DrawTextureNeutral);
        Graphics.DrawTexture(m_Settings.CanvasRect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, modulate);
    }

    void DrawImageOverlay()
    {
        var image = m_Settings.BackgroundImage;
        float imageWidth = image.width;
        float imageHeight = image.height;
        if (imageWidth <= 0 || imageHeight <= 0)
            return;

        var screenRect = Rect.zero;
        var sourceRect = Rect.zero;
        CalculateScaledTextureRects(m_Settings.CanvasRect, m_Settings.BackgroundImageScaleMode,
            imageWidth / imageHeight, ref screenRect, ref sourceRect);

        var modulate = new Color(k_DrawTextureNeutral, k_DrawTextureNeutral, k_DrawTextureNeutral,
            m_Settings.BackgroundImageOpacity * k_DrawTextureNeutral);
        Graphics.DrawTexture(screenRect, image, sourceRect, 0, 0, 0, 0, modulate);
    }

    // Local copy of GUI.CalculateScaledTextureRects: a member-level internals grant on it does not
    // survive Release module compilation (the member is absent from the reference assembly).
    static void CalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
    {
        float destAspect = position.width / position.height;

        switch (scaleMode)
        {
            case ScaleMode.StretchToFill:
                outScreenRect = position;
                outSourceRect = new Rect(0, 0, 1, 1);
                break;
            case ScaleMode.ScaleAndCrop:
                if (destAspect > imageAspect)
                {
                    float stretch = imageAspect / destAspect;
                    outScreenRect = position;
                    outSourceRect = new Rect(0, (1 - stretch) * .5f, 1, stretch);
                }
                else
                {
                    float stretch = destAspect / imageAspect;
                    outScreenRect = position;
                    outSourceRect = new Rect(.5f - stretch * .5f, 0, stretch, 1);
                }
                break;
            case ScaleMode.ScaleToFit:
                if (destAspect > imageAspect)
                {
                    float stretch = imageAspect / destAspect;
                    outScreenRect = new Rect(position.xMin + position.width * (1.0f - stretch) * .5f, position.yMin, stretch * position.width, position.height);
                    outSourceRect = new Rect(0, 0, 1, 1);
                }
                else
                {
                    float stretch = destAspect / imageAspect;
                    outScreenRect = new Rect(position.xMin, position.yMin + position.height * (1.0f - stretch) * .5f, position.width, stretch * position.height);
                    outSourceRect = new Rect(0, 0, 1, 1);
                }
                break;
        }
    }
}
