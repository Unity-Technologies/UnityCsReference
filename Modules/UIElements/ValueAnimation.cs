// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements.Experimental
{
    internal interface IValueAnimationUpdate
    {
        void Tick(long currentTimeMs);
    }

    public interface IValueAnimation
    {
        void Start();
        void Stop();
        void Recycle();
        bool isRunning { get; }
        int durationMs { get; set; }
    }

    public sealed class ValueAnimation<T> : IValueAnimationUpdate, IValueAnimation
    {
        const int k_DefaultDurationMs = 400;
        const int k_DefaultMaxPoolSize = 100;

        private long m_StartTimeMs;
        private int m_DurationMs;
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

        public Func<float, float> easingCurve {get; set;}
        public bool isRunning {get; private set;}

        public Action onAnimationCompleted {get; set;}

        public bool autoRecycle {get; set;}
        private bool recycled { get; set; }
        static ObjectPool<ValueAnimation<T>> sObjectPool = new ObjectPool<ValueAnimation<T>>(k_DefaultMaxPoolSize);

        private VisualElement owner { get; set; }

        public Action<VisualElement, T> valueUpdated {get; set;}
        public Func<VisualElement, T> initialValue {get; set;}
        public Func<T, T, float, T> interpolator {get; set;}

        private T _from;
        private bool fromValueSet = false;

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
        public T to { get; set; }

        public ValueAnimation()
        {
            SetDefaultValues();
        }

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

        public static ValueAnimation<T> Create(VisualElement e, Func<T, T, float, T> interpolator)
        {
            var result = sObjectPool.Get();
            result.recycled = false;
            result.SetOwner(e);
            result.interpolator = interpolator;
            return result;
        }

        public ValueAnimation<T> Ease(Func<float, float> easing)
        {
            easingCurve = easing;
            return this;
        }

        public ValueAnimation<T> OnCompleted(Action callback)
        {
            onAnimationCompleted = callback;
            return this;
        }

        public ValueAnimation<T> KeepAlive()
        {
            autoRecycle = false;
            return this;
        }
    }
}
