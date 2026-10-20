// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // Asserts that an editor panel already holds the values its GUIView sets up for IMGUI and rendering.
    sealed class HostViewChecks
    {
        [Flags]
        enum Mismatch
        {
            None = 0,
            PixelsPerPoint = 1 << 0,
            Size = 1 << 1,
            RenderTarget = 1 << 2,
        }

        // Absorbs float error only: every size compared here is a whole number of device pixels.
        const float k_PixelTolerance = 0.01f;

        readonly EditorPanel m_Panel;
        Mismatch m_Reported;

        public HostViewChecks(EditorPanel panel)
        {
            m_Panel = panel;
        }

        public bool PixelsPerPointMatches(float hostPixelsPerPoint, object host)
        {
            if (m_Panel.overrideScalingForTests || m_Panel.pixelsPerPoint == hostPixelsPerPoint)
                return true;

            Report(Mismatch.PixelsPerPoint, $"Panel pixels per point {m_Panel.pixelsPerPoint} differs from {hostPixelsPerPoint} set by {host}.");
            return false;
        }

        public bool SizeMatches(Vector2 appliedWindowSize, float appliedScale, Vector2 windowSize, object host)
        {
            if (m_Panel.overrideSizeForTests || (appliedWindowSize == windowSize && appliedScale == m_Panel.scale))
                return true;

            Report(Mismatch.Size, $"Panel was sized for {appliedWindowSize} points at scale {appliedScale} but {host} is {windowSize} points at scale {m_Panel.scale}.");
            return false;
        }

        public void CheckRenderTarget()
        {
            if (m_Panel.ownerObject is not GUIView)
                return;

            // Test overrides size or scale the panel apart from its window, so it need not cover whole pixels.
            if (m_Panel.overrideScalingForTests || m_Panel.overrideSizeForTests || m_Panel.skipRenderTargetCheckForTests)
                return;

            var paintingWindow = RenderTexture.active == null;
            var viewport = UnityEngine.UIElements.UIR.Utility.GetActiveViewport();
            var panelPixels = m_Panel.visualTree.layout.size * m_Panel.scaledPixelsPerPoint;
            if (Mathf.Abs(viewport.width - panelPixels.x) <= k_PixelTolerance && Mathf.Abs(viewport.height - panelPixels.y) <= k_PixelTolerance)
                return;

            var target = paintingWindow ? "the window" : $"render texture '{RenderTexture.active.name}'";
            Report(Mismatch.RenderTarget, $"Panel of {m_Panel.ownerObject} renders into a {viewport.width}x{viewport.height} pixel viewport of {target} but covers {panelPixels} pixels ({m_Panel.visualTree.layout.size} points at {m_Panel.scaledPixelsPerPoint} pixels per point).");
        }

        void Report(Mismatch mismatch, string message)
        {
            if ((m_Reported & mismatch) != 0)
                return;
            m_Reported |= mismatch;
            Debug.LogError(message);
        }
    }
}
