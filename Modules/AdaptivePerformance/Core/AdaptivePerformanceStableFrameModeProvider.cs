// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Mode provider that prioritizes consistent frame time over the highest achievable frame rate.
    /// </summary>
    /// <remarks>
    /// On mode start the provider reads the AdaptiveFramerate scaler's bounds and
    /// pins <see cref="Time.maximumDeltaTime"/> to <c>1 / MinBound</c> — a
    /// physics-stability clamp so a hitch can never stretch a single physics step
    /// past the budget.
    ///
    /// The provider then forces vsync off (<see cref="QualitySettings.vSyncCount"/>
    /// to 0) and takes ownership of framerate: vsync's snap-to-refresh-divisor
    /// behavior turns a missed deadline into a large frame-time spike, exactly the
    /// variance this mode fights. It force-enables the AdaptiveFramerate scaler,
    /// starts <see cref="Application.targetFrameRate"/> at MaxBound, and lets the
    /// scaler participate in the indexer's cost-based adjustment so the framerate
    /// seeks the highest stably-reachable rate (MinBound is the floor). The original
    /// vsync count, framerate, and maximum delta time are all restored on mode end.
    ///
    /// Each frame it produces a performance action and adopts whichever signal asks
    /// for the most aggressive change. A headroom guard runs on every frame regardless
    /// of vsync: it asks for a decrease when CPU/GPU work climbs toward the presented
    /// frame interval, which is one load bump from a dropped frame. Alongside it a
    /// variance signal runs, whose source depends on whether presentation is
    /// vsync-locked. When it is (mobile always is — its compositor paces frames at the
    /// hardware level regardless of <see cref="QualitySettings.vSyncCount"/>),
    /// wall-clock frame time quantizes to the display cadence and its variance goes
    /// blind until a frame drops, so the provider measures variance on CPU/GPU work
    /// time. When it isn't, wall-clock frame time is authoritative and its variance is
    /// measured directly. Both paths also honor the MinBound floor via
    /// <see cref="TargetFrameTimeVarianceMs"/>'s companion lower-bound check.
    /// </remarks>
    [System.Serializable]
    public class AdaptivePerformanceStableFrameModeProvider : IAdaptivePerformanceModeProvider
    {
        internal const string kModeName = nameof(OperationMode.StableFrameMode);
        /// <summary>
        /// The name of the operation mode.
        /// </summary>
        public string ModeName => kModeName;
        /// <summary>
        /// The action the mode applies in response to the device's thermal state.
        /// </summary>
        public StateAction ThermalAction { get; private set; }
        /// <summary>
        /// The action the mode applies in response to the device's performance state.
        /// </summary>
        public StateAction PerformanceAction { get; private set; }
        // StableFrameMode drives scalers from frame-time variance, not CPU/GPU
        // utilization, so these stay Stale.
        /// <summary>
        /// The action the mode applies in response to the device's CPU utilization data. Always <see cref="StateAction.Stale"/> because this mode drives scalers from frame-time variance rather than CPU utilization.
        /// </summary>
        public StateAction CpuUtilizationAction => StateAction.Stale;
        /// <summary>
        /// The action the mode applies in response to the device's GPU utilization data. Always <see cref="StateAction.Stale"/> because this mode drives scalers from frame-time variance rather than GPU utilization.
        /// </summary>
        public StateAction GpuUtilizationAction => StateAction.Stale;
        /// <summary>
        /// The Indexer that this mode adjusts scalers through.
        /// </summary>
        public AdaptivePerformanceIndexer Indexer { get; internal set; }
        /// <summary>
        /// The active Adaptive Performance settings.
        /// </summary>
        public IAdaptivePerformanceSettings Settings => Holder.Instance.Settings;

        private ThermalStateTracker m_ThermalStateTracker = new ThermalStateTracker();

        // Rolling buffer of recent frame times (ms). Sized to roughly one second at 60 Hz —
        // large enough to smooth out single-frame spikes, small enough to react quickly when
        // the engine genuinely stops being stable.
        const int k_VarianceWindow = 60;
        readonly float[] m_FrameTimesMs = new float[k_VarianceWindow];
        int m_WindowIndex;
        int m_WindowFilled;

        // Hard ceiling applied to samples in SampleFrameTime.
        const float k_FrameTimeSampleCeilingMs = 100f;

        // Fraction of the presented frame interval the mean work time may reach
        // before the headroom guard calls for a Decrease. At 0.9 a frame spending
        // more than 90% of its present-interval budget on real work is one small
        // load bump from a dropped frame. NOTE: work time under-reports true frame
        // cost (it misses present/driver overhead and pipeline bubbles), so this
        // guard is optimistic — the fraction likely wants to drop below 0.9 once
        // real device traces are available.
        const float k_HeadroomDangerFraction = 0.9f;

        // Which side of the vsync gate the current window was sampled under. The
        // buffer holds wall-clock frame time when unlocked and gating work time when
        // locked — not comparable — so a runtime flip must reset the window.
        bool m_SampledVsyncLocked;

        [SerializeField]
        float m_TargetFrameTimeVarianceMs = 16f;

        /// <summary>
        /// Maximum tolerated variance in the mode's frame-cost signal, in ms².
        /// </summary>
        /// <remarks>
        /// When the rolling variance rises above this value the provider asks the
        /// indexer to decrease scaler quality. When it falls below a quarter of this
        /// value (equivalent to half the corresponding standard deviation) the
        /// provider asks for an increase. The band in between provides hysteresis so
        /// the system does not oscillate around the target.
        ///
        /// The signal the variance is measured on depends on presentation timing.
        /// When frames are presented on a fixed display cadence — vsync, or any
        /// mobile compositor — wall-clock frame time quantizes to that cadence and
        /// masks instability, so the provider measures variance on CPU and GPU work
        /// time instead. Otherwise it measures variance on wall-clock frame time
        /// directly. For the full behavior of this mode, refer to
        /// <see cref="AdaptivePerformanceStableFrameModeProvider"/>.
        ///
        /// As a rule of thumb the value is roughly the square of the tolerated
        /// standard deviation in ms: the default of 16 corresponds to about 4 ms of
        /// jitter.
        /// </remarks>
        public float TargetFrameTimeVarianceMs
        {
            get => m_TargetFrameTimeVarianceMs;
            set => m_TargetFrameTimeVarianceMs = Mathf.Max(0.5f, value);
        }

        // Time.maximumDeltaTime is a physics-stability clamp, applied whenever a
        // valid MinBound exists (regardless of vsync). m_MaximumDeltaTimeOverridden
        // gates the restore so a mode-end that runs before mode-start (e.g. swapped
        // providers during teardown) can't clobber the value with a default 0.
        bool m_MaximumDeltaTimeOverridden;
        float m_OriginalMaximumDeltaTime;

        // Framerate-scaler force-on state. This mode turns vsync off (see below) and
        // takes ownership of framerate, so this is populated on every mode start.
        // Restored whenever this mode get switched or inactivated.
        bool m_FramerateScalerForcedOn;
        bool m_OriginalAdaptiveFramerateEnabled;
        int m_SavedTargetFrameRate;
        AdaptiveFramerate m_AdaptiveFramerateScaler;

        // Vsync override state. This mode forces QualitySettings.vSyncCount to 0 so
        // targetFrameRate/AdaptiveFramerate — not the display refresh — controls the
        // rate; vsync's snap-to-refresh-divisor cliff is a major variance source under
        // load, exactly what this mode fights. m_VSyncOverridden gates the restore of
        // the user's original count.
        bool m_VSyncOverridden;
        int m_OriginalVSyncCount;

        // Cached at mode-start from AdaptiveFramerate.MinBound — the slowest
        // framerate the user tolerates
        float m_MaxAcceptableFrameTimeSeconds;

        /// <summary>
        /// Called when stable frame mode becomes the active operation mode. Resets the variance window and applies the framerate bounds read from the AdaptiveFramerate scaler.
        /// </summary>
        public void OnOperationModeStart()
        {
            if (Settings == null)
                return;
            Settings.IndexerOperationMode = OperationMode.StableFrameMode;
            ResetWindow();
            ApplyFramerateBoundsFromScaler(saveTargetFrameRate: true);
        }

        /// <summary>
        /// Called when stable frame mode is replaced by another operation mode. Resets the variance window and restores the framerate bounds it applied.
        /// </summary>
        public void OnOperationModeEnd()
        {
            ResetWindow();
            RestoreFramerateBounds();
        }

        // A profile switch re-hydrates the AdaptiveFramerate scaler with the new profile's
        // bounds and Enabled state but leaves targetFrameRate at the old MaxBound, which may
        // fall outside the new bounds and block every level change. Re-apply the bounds.
        internal void OnScalerProfileChanged()
        {
            if (Settings == null || Indexer == null)
                return;

            ResetWindow();
            ApplyFramerateBoundsFromScaler(saveTargetFrameRate: false);
        }

        // Read the framerate window from the AdaptiveFramerate scaler and apply it.
        // MinBound is the slowest framerate the user tolerates, so 1 / MinBound is
        // the largest deltaTime physics should ever see — exactly what
        // Time.maximumDeltaTime caps. After the physics clamp, force vsync off and
        // force the AdaptiveFramerate scaler on, starting at MaxBound, so the
        // variance-driven adjustment can seek the highest stably-reachable rate.
        // saveTargetFrameRate is false when re-applying mid-session (profile switch), where
        // targetFrameRate is this mode's own value, not the one to restore on mode end.
        void ApplyFramerateBoundsFromScaler(bool saveTargetFrameRate)
        {
            m_MaxAcceptableFrameTimeSeconds = 0f;

            if (Indexer == null)
                return;
            var framerateScaler = Indexer.GetScalerByType<AdaptiveFramerate>();
            if (framerateScaler == null)
                return;

            // MinBound must be positive — 1 / 0 would yield Infinity and Unity
            // silently rejects that, leaving the user wondering why nothing happened.
            var minBoundFps = Mathf.Max(1f, framerateScaler.MinBound);
            var maxBoundFps = framerateScaler.MaxBound;

            // Physics-stability clamp — orthogonal to vsync/framerate control. Apply
            // whenever a valid MinBound exists so a hitch can lower frame rate but
            // cannot stretch a single physics step past the budget, which is the
            // stability guarantee this mode is named for.
            if (!m_MaximumDeltaTimeOverridden)
            {
                m_OriginalMaximumDeltaTime = Time.maximumDeltaTime;
                m_MaximumDeltaTimeOverridden = true;
            }
            Time.maximumDeltaTime = 1f / minBoundFps;
            m_MaxAcceptableFrameTimeSeconds = 1f / minBoundFps;

            // With vsync off, targetFrameRate/AdaptiveFramerate seeks the highest steadily
            // holdable rate. Save the user's count so mode-end can restore it. On mobile
            // the compositor still paces presentation regardless of this setting; the
            // override only lets targetFrameRate apply there.
            if (!m_VSyncOverridden)
            {
                m_OriginalVSyncCount = QualitySettings.vSyncCount;
                m_VSyncOverridden = true;
            }
            QualitySettings.vSyncCount = 0;

            m_AdaptiveFramerateScaler = framerateScaler;
            m_OriginalAdaptiveFramerateEnabled = framerateScaler.Enabled;
            // Deferred: the indexer's next Update fires OnEnabled via ActivateEnabledScalers.
            framerateScaler.Enabled = true;
            if (saveTargetFrameRate)
                m_SavedTargetFrameRate = Application.targetFrameRate;
            // Start at the ceiling and let the variance/lower-bound Decrease signal
            // pull it down to the highest stably-reachable rate; MinBound is the floor.
            Application.targetFrameRate = (int)maxBoundFps;
            m_FramerateScalerForcedOn = true;
        }

        void RestoreFramerateBounds()
        {
            m_MaxAcceptableFrameTimeSeconds = 0f;

            if (m_FramerateScalerForcedOn && m_AdaptiveFramerateScaler != null)
            {
                // RemoveScaler fires OnDisabled once (restoring the scaler's polluted
                // m_DefaultFPS), then park it disabled; restore the user's Enabled
                // state so the next mode's ActivateEnabledScalers re-enables it if it
                // was on.
                m_AdaptiveFramerateScaler.RemoveScaler();
                m_AdaptiveFramerateScaler.Enabled = m_OriginalAdaptiveFramerateEnabled;
                if (Indexer != null)
                    Indexer.AddDisabledScaler(m_AdaptiveFramerateScaler);
                // Must run AFTER RemoveScaler/OnDisabled, which restored the polluted
                // default FPS.
                Application.targetFrameRate = m_SavedTargetFrameRate;
                m_AdaptiveFramerateScaler = null;
                m_FramerateScalerForcedOn = false;
            }

            if (m_MaximumDeltaTimeOverridden)
            {
                Time.maximumDeltaTime = m_OriginalMaximumDeltaTime;
                m_MaximumDeltaTimeOverridden = false;
            }

            if (m_VSyncOverridden)
            {
                QualitySettings.vSyncCount = m_OriginalVSyncCount;
                m_VSyncOverridden = false;
            }
        }

        /// <summary>
        /// Applies stable frame mode's variance, lower-bound, and headroom actions to the active scalers. Adaptive Performance calls this method every frame while stable frame mode is active.
        /// </summary>
        public void ApplyModeActions()
        {
            if (Settings == null || Indexer == null)
                return;

            // Choose the variance source by whether presentation is vsync-locked.
            var vsyncLocked = IsPresentationVsyncLocked();
            if (vsyncLocked != m_SampledVsyncLocked)
            {
                ResetWindow();
                m_SampledVsyncLocked = vsyncLocked;
            }

            if (vsyncLocked)
            {
                var workMs = TryGetCurrentWorkTimeMs();
                // Fall back to wall-clock only if the platform reports no timing.
                SampleFrameTime(workMs >= 0f ? workMs : Time.unscaledDeltaTime * 1000f);
            }
            else
            {
                SampleFrameTime(Time.unscaledDeltaTime * 1000f);
            }

            // Three signals, most aggressive wins.
            var performanceAction = SelectMostAggressive(
                ComputePerformanceActionFromVariance(),
                ComputePerformanceActionFromLowerBound());
            performanceAction = SelectMostAggressive(
                performanceAction, ComputePerformanceActionFromHeadroom());
            PerformanceAction = performanceAction;

            ThermalAction = m_ThermalStateTracker.Update();
            Indexer.AdjustScalersBasedOnStateAction(ThermalAction, PerformanceAction, true, null);
        }

        // True when frames are presented on a fixed display cadence we don't control.
        // Mobile compositors always are, regardless of QualitySettings.vSyncCount
        // (which reads 0 there); on desktop this tracks the actual vsync setting.
        // Distinct from the vsync check in ApplyFramerateBoundsFromScaler, which asks
        // a different question — whether to drive Application.targetFrameRate.
        static bool IsPresentationVsyncLocked() =>
            Application.isMobilePlatform || QualitySettings.vSyncCount != 0;

        // Gating work time this frame (ms): the larger of CPU and GPU, since the frame
        // is paced by whichever ran longer. Both read -1 when the platform reports no
        // timing at all; returns -1 then so the caller can fall back to wall-clock.
        // Mathf.Max(-1, valid) keeps the valid one, so a platform exposing only CPU
        // (or only GPU) timing still works. This is a proxy for true frame cost — it
        // can miss present/driver overhead and pipeline bubbles — but the variance
        // signal only cares about its spread, which an additive offset leaves intact.
        static float TryGetCurrentWorkTimeMs()
        {
            var frameTiming = Holder.Instance.PerformanceStatus.FrameTiming;
            var gatingSeconds = Mathf.Max(frameTiming.CurrentCpuFrameTime, frameTiming.CurrentGpuFrameTime);
            return gatingSeconds > 0f ? gatingSeconds * 1000f : -1f;
        }

        void ResetWindow()
        {
            m_WindowIndex = 0;
            m_WindowFilled = 0;
        }

        void SampleFrameTime(float frameTimeMs)
        {
            // Clip catastrophic outliers (scene loads, GC pauses) so a single hitch
            // doesn't poison the variance signal for the full window's worth of
            // frames. The ceiling is loose enough that any frame-time the engine
            // could reasonably hit in steady-state still goes in unchanged.
            if (frameTimeMs > k_FrameTimeSampleCeilingMs)
                frameTimeMs = k_FrameTimeSampleCeilingMs;
            m_FrameTimesMs[m_WindowIndex] = frameTimeMs;
            m_WindowIndex = (m_WindowIndex + 1) % k_VarianceWindow;
            if (m_WindowFilled < k_VarianceWindow)
                m_WindowFilled++;
        }

        StateAction ComputePerformanceActionFromVariance()
        {
            // Wait until the window is full before acting — measuring variance over a
            // half-empty window produces noisy decisions in the first second of the mode.
            if (m_WindowFilled < k_VarianceWindow)
                return StateAction.Stale;

            // Two-pass mean/variance over the buffer. Cheaper than maintaining a
            // running sum (which drifts in float precision across long sessions
            // because every sample contributes to the same accumulator forever)
            // and the constant factor is trivial at N=60.
            float sum = 0f;
            for (int i = 0; i < k_VarianceWindow; i++)
                sum += m_FrameTimesMs[i];
            var mean = sum / k_VarianceWindow;

            float sumSquaredDeviations = 0f;
            for (int i = 0; i < k_VarianceWindow; i++)
            {
                var diff = m_FrameTimesMs[i] - mean;
                sumSquaredDeviations += diff * diff;
            }
            var varianceMs2 = sumSquaredDeviations / k_VarianceWindow;

            // Compare variance directly against the target instead of taking sqrt
            // and comparing stddev. Mathematically identical for ordering (both sides
            // are non-negative) and saves a Mathf.Sqrt per frame. The 0.25 hysteresis
            // factor is the squared form of the 0.5 stddev hysteresis (0.5² = 0.25).
            if (varianceMs2 > m_TargetFrameTimeVarianceMs)
                return StateAction.Decrease;
            if (varianceMs2 < m_TargetFrameTimeVarianceMs * 0.25f)
                return StateAction.Increase;
            return StateAction.Stale;
        }

        // The lower bound IS the threshold — no margin, no hysteresis. If smoothed
        // frame time exceeds 1 / MinBound the user's "slowest acceptable framerate"
        // is being violated and we ask for a Decrease. This signal never asks to
        // Increase: it only knows about the floor, not about whether there is
        // headroom above. Increase decisions are left to the variance signal.
        StateAction ComputePerformanceActionFromLowerBound()
        {
            if (m_MaxAcceptableFrameTimeSeconds <= 0f)
                return StateAction.Stale;

            // FrameTiming.AverageFrameTime is in seconds, matching the units of
            // m_MaxAcceptableFrameTimeSeconds (1 / MinBoundFps). Don't conflate
            // with the variance buffer above, which works in milliseconds.
            var frameSeconds = Holder.Instance.PerformanceStatus.FrameTiming.AverageFrameTime;
            if (frameSeconds <= 0f)
                return StateAction.Stale;

            if (frameSeconds > m_MaxAcceptableFrameTimeSeconds)
                return StateAction.Decrease;
            return StateAction.Stale;
        }

       // Decrease scaler qualiity when workload approaches the frame budget;
       // Currently using the average active CPU/GPU time as an indicator.
        StateAction ComputePerformanceActionFromHeadroom()
        {
            var frameTiming = Holder.Instance.PerformanceStatus.FrameTiming;
            var workSeconds = Mathf.Max(frameTiming.AverageCpuFrameTime, frameTiming.AverageGpuFrameTime);
            var presentedSeconds = frameTiming.AverageFrameTime;
            if (workSeconds <= 0f || presentedSeconds <= 0f)
                return StateAction.Stale;

            return workSeconds > presentedSeconds * k_HeadroomDangerFraction
                ? StateAction.Decrease
                : StateAction.Stale;
        }

        // Ranking: FastDecrease > Decrease > Increase > Stale. A Decrease from
        // one signal wins over an Increase from the other — when in doubt we'd
        // rather lose quality than gain it and risk stutter. Increase carries
        // whenever any signal asks for it, since the lower-bound signal only
        // raises Decrease/Stale and would otherwise veto every Increase.
        static StateAction SelectMostAggressive(StateAction a, StateAction b)
        {
            if (a == StateAction.FastDecrease || b == StateAction.FastDecrease)
                return StateAction.FastDecrease;
            if (a == StateAction.Decrease || b == StateAction.Decrease)
                return StateAction.Decrease;
            if (a == StateAction.Increase || b == StateAction.Increase)
                return StateAction.Increase;
            return StateAction.Stale;
        }
    }
}
