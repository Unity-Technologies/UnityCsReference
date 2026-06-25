// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Rendering;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// A scaler used by <see cref="AdaptivePerformanceIndexer"/> to adjust the resolution of all render targets that allow dynamic resolution.
    /// If a device or graphics API doesn't support a dynamic resolution, it will use the rendering pipeline's render scale multiplier as a fallback.
    /// </summary>
    public class AdaptiveResolution : AdaptivePerformanceScaler
    {
        [NoAutoStaticsCleanup] // instance counter maintained via Start/OnDestroy lifecycle
        static int instanceCount = 0;

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Settings == null)
                return;
            ApplyDefaultSetting(m_Settings.scalerSettings.AdaptiveResolution);
        }

        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected override void OnDisabled()
        {
            OnDestroy();
        }

        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected override void OnEnabled() {}

        void OnValidate()
        {
            if (MaxLevel < 1)
                MaxLevel = 1;
            MaxBound = Mathf.Clamp(MaxBound, 0.25f, 1.0f);
            MinBound = Mathf.Clamp(MinBound, 0.25f, MaxBound);
        }

        // TODO: expose dynamicResolution capability through SystemInfo
        private bool IsDynamicResolutionSupported()
        {
            return false;
        }

        private void Start()
        {
            ++instanceCount;
            if (instanceCount > 1)
                Debug.LogWarning("Multiple Adaptive Resolution scalers created. They will interfere with each other.");
            if (!IsDynamicResolutionSupported())
                Debug.Log(string.Format("Dynamic resolution is not supported. Will be using fallback to Render Scale Multiplier."));
        }

        private void OnDestroy()
        {
            --instanceCount;
            if (Scale == 1.0f)
                return;

            APLog.Debug("Restoring dynamic resolution scale factor to 1.0");
            if (IsDynamicResolutionSupported())
                ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
            else
                AdaptivePerformanceRenderSettings.RenderScaleMultiplier = 1.0f;
        }

        /// <summary>
        /// Callback for any level change
        /// </summary>
        protected override void OnLevel()
        {
            var scaleChange = ScaleChanged();

            // if Gfx API does not support Dynamic resolution, currentCamera.allowDynamicResolution will be false
            if (IsDynamicResolutionSupported())
            {
                if (scaleChange)
                    ScalableBufferManager.ResizeBuffers(Scale, Scale);
                int rezWidth = (int)Mathf.Ceil(ScalableBufferManager.widthScaleFactor * Screen.currentResolution.width);
                int rezHeight = (int)Mathf.Ceil(ScalableBufferManager.heightScaleFactor * Screen.currentResolution.height);
                APLog.Debug(
                    $"Adaptive Resolution Scale: {Scale:F3} Resolution: {rezWidth}x{rezHeight} ScaleFactor: {ScalableBufferManager.widthScaleFactor:F3}x{ScalableBufferManager.heightScaleFactor:F3} Level:{CurrentLevel}/{MaxLevel}");
            }
            else
            {
                AdaptivePerformanceRenderSettings.RenderScaleMultiplier = Scale;
                APLog.Debug($"Dynamic resolution is not supported. Using fallback to Render Scale Multiplier : {Scale:F3}");
                // TODO: warn if unsupported render pipeline is used
                //Debug.Log("You might not use a supported Render Pipeline. Currently only Universal Render Pipeline and Built-in are supported by Adaptive Resolution.");
            }
        }
    }
}
