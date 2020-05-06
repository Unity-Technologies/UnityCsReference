using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements.Experimental
{
    internal interface IValueAnimationUpdate
    {
        void Tick(long currentTimeMs);
    }

    /// <summary>
    /// Base interface for transition animations.
    /// </summary>
    public interface IValueAnimation
    {
        /// <summary>
        /// Starts the animation using this object's values.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops this animation.
        /// </summary>
        void Stop();
        /// <summary>
        /// Returns this animation object into its object pool.
        /// </summary>
        /// <remarks>
        /// Keeping a reference to the animation object afterwards could lead to unspecified behaviour.
        /// </remarks>
        void Recycle();
        /// <summary>
        /// Tells if the animation is currently active.
        /// </summary>
        bool isRunning { get; }
        /// <summary>
        /// Duration of the transition in milliseconds.
        /// </summary>
        int durationMs { get; set; }
    }

    /// <summary>
    /// Implementation object for transition animations.
    /// </summary>
    public sealed class ValueAnimation<T> : IValueAnimationUpdate, IValueAnimation
    {
        const int k_DefaultDurationMs = 400;
        const int k_DefaultMaxPoolSize = 100;

        private long m_StartTimeMs;
        private int m_DurationMs;
        /// <summary>
        /// Duration of the animation in milliseconds.
        /// </summary>
        public int durationMs
        {
            get { return m_DurationMs; }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                m_DurationMs = value;
            }
        }

        /// <summary>
        /// Smoothing function related to this animation. Default value is <see cref="Easing.OutQuad"/>.
        /// </summary>
        public Func<float, float> easingCurve {get; set;}
        /// <summary>
        /// Tells if the animation is currently active.
        /// </summary>
        public bool isRunning {get; private set;}

        /// <summary>
        /// Callback invoked when this animation has completed.
        /// </summary>
        public Action onAnimationCompleted {get; set;}

        /// <summary>
        /// Returns true if this animation object will be automatically returned to the instance pool after the animation has completed.
        /// </summary>
        public bool autoRecycle {get; set;}
        private bool recycled { get; set; }
        static ObjectPool<ValueAnimation<T>> sObjectPool = new ObjectPool<ValueAnimation<T>>(k_DefaultMaxPoolSize);

        private VisualElement owner { get; set; }

        /// <summary>
        /// Callback invoked after every value interpolation.
        /// </summary>
        public Action<VisualElement, T> valueUpdated {get; set;}
        /// <summary>
        /// Callback invoked when the from field has not been set, in order to retrieve the starting state of this animation.
        /// </summary>
        public Func<VisualElement, T> initialValue {get; set;}
        /// <summary>
        /// Value interpolation method.
        /// </summary>
        public Func<T, T, float, T> interpolator {get; set;}

        private T _from;
        private bool fromValueSet = false;

        /// <summary>
        /// The animation start value.
        /// </summary>
        public T from
        {
            get
            {
                if (!fromValueSet)
                {
                    if (initialValue != null)
                    {
                        from = initialValue(owner);
                    }
                }
                return _from;
            }
            set
            {
                fromValueSet = true;
                _from = value;
            }
        }
        /// <summary>
        /// The animation end value.
        /// </summary>
        public T to { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// To properly use object pooling, use the Create static function.
        /// </remarks>
        public ValueAnimation()
        {
            SetDefaultValues();
        }

        /// <summary>
        /// Starts the animation using this object's values.
        /// </summary>
        public void Start()
        {
            CheckNotRecycled();

            if (owner != null)
            {
                m_StartTimeMs = Panel.TimeSinceStartupMs();
                Register();
                isRunning = true;
            }
        }

        /// <summary>
        /// Stops this animation.
        /// </summary>
        /// <remarks>
        /// If set, the onAnimationCompleted callback will be called.
        /// </remarks>
        public void Stop()
        {
            CheckNotRecycled();

            if (isRunning)
            {
                Unregister();
                isRunning = false;
                onAnimationCompleted?.Invoke();
                if (autoRecycle)
                {
                    if (!recycled)
                    {
                        Recycle();
                    }
                }
            }
        }

        /// <summary>
        /// Returns this animation object into its object pool.
        /// </summary>
        /// <remarks>
        /// Keeping a reference to the animation object afterwards could lead to unspecified behaviour.
        /// </remarks>
        public void Recycle()
        {
            CheckNotRecycled();

            //we clear all references:
            if (isRunning)
            {
                if (!autoRecycle)
                {
                    Stop();
                }
                else
                {
                    Stop();
                    return;
                }
            }

            // We reset all fields
            SetDefaultValues();
            recycled = true;

            sObjectPool.Release(this);
        }

        void IValueAnimationUpdate.Tick(long currentTimeMs)
        {
            CheckNotRecycled();

            long interval = currentTimeMs - m_StartTimeMs;

            float progress = interval / (float)durationMs;

            bool done = false;
            if (progress >= 1.0f)
            {
                progress = 1.0f;
                done = true;
            }

            progress = easingCurve?.Invoke(progress) ?? progress;

            if (interpolator != null)
            {
                T value = interpolator(from, to, progress);

                valueUpdated?.Invoke(owner, value);
            }

            if (done)
            {
                Stop();
            }
        }

        private void SetDefaultValues()
        {
            m_DurationMs = k_DefaultDurationMs;
            autoRecycle = true;
            owner = null;
            m_StartTimeMs = 0;

            onAnimationCompleted = null;
            valueUpdated = null;
            initialValue = null;
            interpolator = null;

            to = default(T);
            from = default(T);
            fromValueSet = false;
            easingCurve = Easing.OutQuad;
        }

        private void Unregister()
        {
            if (owner != null)
            {
                owner.UnregisterAnimation(this);
            }
        }

        private void Register()
        {
            if (owner != null)
            {
                owner.RegisterAnimation(this);
            }
        }

        internal void SetOwner(VisualElement e)
        {
            if (isRunning)
            {
                Unregister();
            }

            owner = e;

            if (isRunning)
            {
                Register();
            }
        }

        void CheckNotRecycled()
        {
            if (recycled)
            {
                throw new InvalidOperationException("Animation object has been recycled. Use KeepAlive() to keep a reference to an animation after it has been stopped.");
            }
        }

        /// <summary>
        /// Creates a new ValueAnimation object or returns an available one from it's instance pool.
        /// </summary>
        public static ValueAnimation<T> Create(VisualElement e, Func<T, T, float, T> interpolator)
        {
            var result = sObjectPool.Get();
            result.recycled = false;
            result.SetOwner(e);
            result.interpolator = interpolator;
            return result;
        }

        /// <summary>
        /// Sets the easing function.
        /// </summary>
        public ValueAnimation<T> Ease(Func<float, float> easing)
        {
            easingCurve = easing;
            return this;
        }

        /// <summary>
        /// Sets a callback invoked when this animation has completed.
        /// </summary>
        public ValueAnimation<T> OnCompleted(Action callback)
        {
            onAnimationCompleted = callback;
            return this;
        }

        /// <summary>
        /// Will not return the object to the instance pool when the animation has completed.
        /// </summary>
        public ValueAnimation<T> KeepAlive()
        {
            autoRecycle = false;
            return this;
        }
    }
}
