// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Rendering;

namespace UnityEngine.UIElements.UIR
{
    // Guards against shader-info upload-staging exhaustion in update-without-render loops
    // (e.g. editor tests spinning Panel.Repaint without ever rendering): unrendered updates never
    // reach the frame bookkeeping that recycles the device's upload staging memory; on D3D12 each
    // shader-info texture upload then permanently consumes fresh scratch (committed resource +
    // synchronous MakeResident), which degrades and can eventually OOM. Every k_KickInterval
    // upload-carrying unrendered updates, GL.Flush submits the pending work and runs the
    // bookkeeping that recycles the staging pool. This stays a backstop even though the storage
    // skips identical texel writes (see ShaderInfoStorage.SetTexel): genuinely changing records
    // still upload every update.
    class ShaderInfoUpdateGuard
    {
        const int k_KickInterval = 32; // ~2MB of 64KB uploads between kicks, within the primary scratch pool

        bool m_RenderedSinceLastUpdate = true; // start true so the first update never kicks
        int m_UnrenderedUploadCount; // upload-carrying updates since the last kick

        // Call at the end of an update pass, once the storage changes have been issued.
        public void IssuedPendingStorageChanges(bool uploaded)
        {
            if (uploaded && !m_RenderedSinceLastUpdate &&
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 &&
                ++m_UnrenderedUploadCount >= k_KickInterval)
            {
                m_UnrenderedUploadCount = 0;
                GL.Flush(); // amortized: submits pending uploads + recycles staging, ~1/32 updates
            }

            m_RenderedSinceLastUpdate = false;
        }

        // Call when the panel content is drawn: either directly, or serialized for a camera to draw.
        public void OnRender()
        {
            m_RenderedSinceLastUpdate = true;
            m_UnrenderedUploadCount = 0; // uploads are counted since the last render
        }
    }
}
