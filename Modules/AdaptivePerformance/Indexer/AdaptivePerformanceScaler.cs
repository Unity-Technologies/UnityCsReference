// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;


namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Scaler impact on visuals.
    /// </summary>
    public enum ScalerVisualImpact
    {
        /// <summary>
        /// Low impact on visual quality. Changes might not be very noticeable to the user.
        /// </summary>
        Low,
        /// <summary>
        /// Medium impact on visual quality. Mildly affects the visuals in the scene and might be noticeable to the user.
        /// </summary>
        Medium,
        /// <summary>
        /// High impact on visual quality. The scaler will have an easily visible effect on quality.
        /// </summary>
        High
    }

    /// <summary>
    /// Bottleneck flags that the scaler targets.
    /// </summary>
    [System.Flags]
    public enum ScalerTarget
    {
        /// <summary>
        /// The scaler targets the CPU and attempts to reduce the CPU load.
        /// </summary>
        CPU = 0x1,
        /// <summary>
        /// The scaler targets the GPU and attempts to reduce the GPU load.
        /// </summary>
        GPU = 0x2,
        /// <summary>
        /// The scaler targets fillrate, often at the expense of visual quality.
        /// </summary>
        FillRate = 0x4
    }

    /// <summary>
    /// Abstract class representing single feature that is controlled by <see cref="AdaptivePerformanceIndexer"/>.
    /// You control the quality through changing the levels, where 0 represents the controller not being applied and 1,2... as applied.
    /// As a result, a higher level represents lower visuals, but better performance.
    /// </summary>
    [RequireDerived]
    public abstract class AdaptivePerformanceScaler : ScriptableObject
    {
        private AdaptivePerformanceIndexer m_Indexer;
        /// <summary>
        /// Returns a string with the name of the scaler.
        /// </summary>
        public virtual string Name
        {
            get => m_defaultSetting.name;
            set
            {
                if (m_defaultSetting.name == value)
                    return;

                m_defaultSetting.name = value;
            }
        }

        /// <summary>
        /// Returns `true` if this scaler is active, otherwise returns `false`.
        /// </summary>
        public virtual bool Enabled
        {
            get => m_defaultSetting.enabled;
            set
            {
                if (m_defaultSetting.enabled == value)
                    return;

                m_defaultSetting.enabled = value;
                AdaptivePerformanceAnalytics.SendAdaptiveFeatureUpdateEvent(Name, m_defaultSetting.enabled);
            }
        }
        /// <summary>
        /// Returns a generic scale of this scaler in range [0,1] which is applied to the quality.
        /// </summary>
        public virtual float Scale
        {
            get => m_defaultSetting.scale;
            set
            {
                if (m_defaultSetting.scale == value)
                    return;

                m_defaultSetting.scale = value;
            }
        }
        /// <summary>
        /// Returns the visual impact of scaler when applied.
        /// </summary>
        public virtual ScalerVisualImpact VisualImpact
        {
            get => m_defaultSetting.visualImpact;
            set
            {
                if (m_defaultSetting.visualImpact == value)
                    return;

                m_defaultSetting.visualImpact = value;
            }
        }
        /// <summary>
        /// Returns the bottlenecks that this scaler targets.
        /// </summary>
        public virtual ScalerTarget Target
        {
            get => m_defaultSetting.target;
            set
            {
                if (m_defaultSetting.target == value)
                    return;

                m_defaultSetting.target = value;
            }
        }
        /// <summary>
        /// Returns the maximum level of this scaler.
        /// </summary>
        public virtual int MaxLevel
        {
            get => m_defaultSetting.maxLevel;
            set
            {
                if (m_defaultSetting.maxLevel == value)
                    return;

                m_defaultSetting.maxLevel = value;
            }
        }
        /// <summary>
        /// Returns the minimum boundary of this scaler.
        /// </summary>
        public virtual float MinBound
        {
            get => m_defaultSetting.minBound;
            set
            {
                if (m_defaultSetting.minBound == value)
                    return;

                m_defaultSetting.minBound = value;
            }
        }
        /// <summary>
        /// Returns the maximum boundary of this scaler.
        /// </summary>
        public virtual float MaxBound
        {
            get => m_defaultSetting.maxBound;
            set
            {
                if (m_defaultSetting.maxBound == value)
                    return;

                m_defaultSetting.maxBound = value;
            }
        }
        /// <summary>
        /// Returns the current level of scale.
        /// </summary>
        public int CurrentLevel { get; private set; }
        /// <summary>
        /// Returns `true` if the scaler can no longer be applied, otherwise returns `false`.
        /// </summary>
        public bool IsMaxLevel { get => CurrentLevel == MaxLevel; }
        /// <summary>
        /// Returns `true` if the scaler is not applied, otherwise returns `false`.
        /// </summary>
        public bool NotLeveled { get => CurrentLevel == 0; }
        /// <summary>
        /// Scaler impact on GPU so far in milliseconds.
        /// </summary>
        public int GpuImpact { get; internal set; }
        /// <summary>
        /// Scaler impact on CPU so far in milliseconds.
        /// </summary>
        public int CpuImpact { get; internal set; }

        int m_OverrideLevel = -1;
        /// <summary>
        /// Default settings for this scaler.
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase DefaultSetting
        {
            get => m_defaultSetting;
            set => m_defaultSetting = value;
        }

        [SerializeField]
        AdaptivePerformanceScalerSettingsBase m_defaultSetting = new AdaptivePerformanceScalerSettingsBase();

        /// <summary>
        /// Settings reference for all scalers.
        /// </summary>
        protected IAdaptivePerformanceSettings m_Settings;

        /// <summary>
        /// Allows you to manually override the scaler level.
        /// If the value is -1, <see cref="AdaptivePerformanceIndexer"/> will handle levels, otherwise scaler will be forced to the value you specify.
        /// </summary>
        public int OverrideLevel
        {
            get => m_OverrideLevel;
            set
            {
                m_OverrideLevel = value;
                m_Indexer.UpdateOverrideLevel(this);
            }
        }

        /// <summary>
        /// Calculate the cost of applying this particular scaler.
        /// </summary>
        /// <returns>Cost of scaler ranges from 0 to infinity.</returns>
        public int CalculateCost()
        {
            var bottleneck = Holder.Instance.PerformanceStatus.PerformanceMetrics.PerformanceBottleneck;

            var cost = 0;

            switch (VisualImpact)
            {
                case ScalerVisualImpact.Low:
                    cost += CurrentLevel * 1;
                    break;
                case ScalerVisualImpact.Medium:
                    cost += CurrentLevel * 2;
                    break;
                case ScalerVisualImpact.High:
                    cost += CurrentLevel * 3;
                    break;
            }

            // Bottleneck should always be best priority
            if (bottleneck == PerformanceBottleneck.CPU && (Target & ScalerTarget.CPU) == 0)
                cost = 6;
            if (bottleneck == PerformanceBottleneck.GPU && (Target & ScalerTarget.GPU) == 0)
                cost = 6;
            if (bottleneck == PerformanceBottleneck.TargetFrameRate && (Target & ScalerTarget.FillRate) == 0)
                cost = 6;

            return cost;
        }

        /// <summary>
        /// Ensures settings are applied during startup.
        /// </summary>
        protected virtual void Awake()
        {
            if (Holder.Instance == null)
                return;

            m_Settings =  Holder.Instance.Settings;
            m_Indexer = Holder.Instance.Indexer;
        }

        internal void InitializeScaler()
        {
            if (Holder.Instance == null)
                return;

            m_Settings =  Holder.Instance.Settings;
            m_Indexer = Holder.Instance.Indexer;
            EnableScaler();
        }

        private void OnEnable()
        {
            EnableScaler();
        }

        internal void EnableScaler()
        {
            if (m_Indexer == null)
                return;
            m_Indexer.AddScaler(this);
            AdaptivePerformanceAnalytics.RegisterFeature(Name, Enabled);
            AdaptivePerformanceAnalytics.SendAdaptiveFeatureUpdateEvent(Name, Enabled);
            OnEnabled();
        }

        internal void RemoveScaler()
        {
            if (m_Indexer == null)
                return;

            if (m_Indexer.RemoveScaler(this))
            {
                OnDisabled();
            }
        }

        private void OnDisable()
        {
            RemoveScaler();
        }

        internal void IncreaseLevel()
        {
            if (IsMaxLevel)
            {
                Debug.LogError("Cannot increase scaler level as it is already max.");
                return;
            }
            CurrentLevel++;
            OnLevelIncrease();
            OnLevel();
        }

        internal void DecreaseLevel()
        {
            if (NotLeveled)
            {
                Debug.LogError("Cannot decrease scaler level as it is already 0.");
                return;
            }
            CurrentLevel--;
            OnLevelDecrease();
            OnLevel();
        }

        internal void Activate()
        {
            OnEnabled();
        }

        internal void Deactivate()
        {
            OnDisabled();
        }

        /// <summary>
        /// Any scaler with settings in <see cref="IAdaptivePerformanceSettings"/> needs to call this method and provide the scaler specific setting. Unity uses the setting arguments in the base-scaler as the default settings.
        /// This is also used by Scaler Profiles to apply their Settings.
        /// </summary>
        /// <param name="defaultSetting">The settings to apply to the scaler.</param>
        public void ApplyDefaultSetting(AdaptivePerformanceScalerSettingsBase defaultSetting)
        {
            m_defaultSetting = defaultSetting;
        }

        /// <summary>
        /// Checks if scale changed based on the current level and returns true if scale changed false otherwise.
        /// </summary>
        /// <returns>Returns true if scale changed false otherwise.</returns>
        protected bool ScaleChanged()
        {
            float oldScaleFactor = Scale;
            float scaleIncrement = (MaxBound - MinBound) / MaxLevel;

            Scale = scaleIncrement * (MaxLevel - CurrentLevel) + MinBound;

            return Scale != oldScaleFactor;
        }

        /// <summary>
        /// Callback for when the performance level is increased.
        /// </summary>
        protected virtual void OnLevelIncrease() {}
        /// <summary>
        /// Callback for when the performance level is decreased.
        /// </summary>
        protected virtual void OnLevelDecrease() {}
        /// <summary>
        /// Callback for any level change
        /// </summary>
        protected virtual void OnLevel() {}
        /// <summary>
        /// Callback when scaler gets enabled and added to the indexer
        /// </summary>
        protected virtual void OnEnabled() {}
        /// <summary>
        /// Callback when scaler gets disabled and removed from indexer
        /// </summary>
        protected virtual void OnDisabled() {}
    }
}
