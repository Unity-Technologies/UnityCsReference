// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal interface IEasingCurve
    {
        float GetEffectiveProgress(float progress);
    }

    internal abstract class ValueAnimationBase
    {
        const int k_DefaultDuration = 200;
        const int k_StepDuration = 5;
        IVisualElementScheduler m_Schedule;
        static Dictionary<Type, Func<object, object, float, object>> s_InterpolateFuncs;

        public event Action finished;

        protected abstract object startValue { get; }
        protected abstract object endValue { get; }

        public int duration { get; set; }

        public IEasingCurve easingCurve { get; set; }

        public float progress { get; private set; }

        private float progressIncrement
        {
            get
            {
                if (duration > 0)
                    return (float)k_StepDuration / duration;

                return float.MaxValue;
            }
        }

        public bool running { get; private set; }

        public ValueAnimationBase(IVisualElementScheduler schedule)
        {
            m_Schedule = schedule;
            duration = k_DefaultDuration;
        }

        static ValueAnimationBase()
        {
            s_InterpolateFuncs = new Dictionary<Type, Func<object, object, float, object>>();
            s_InterpolateFuncs[typeof(float)] = (a, b, interp) => Mathf.Lerp((float)a, (float)b, interp);
            s_InterpolateFuncs[typeof(Vector2)] = (a, b, interp) => Vector2.Lerp((Vector2)a, (Vector2)b, interp);
            s_InterpolateFuncs[typeof(Vector3)] = (a, b, interp) => Vector3.Lerp((Vector3)a, (Vector3)b, interp);
            s_InterpolateFuncs[typeof(Rect)] = (a, b, interp) =>
            {
                Rect r1 = (Rect)a;
                Rect r2 = (Rect)b;

                return new Rect(Mathf.Lerp(r1.x, r2.x, interp)
                    , Mathf.Lerp(r1.y, r2.y, interp)
                    , Mathf.Lerp(r1.width, r2.width, interp)
                    , Mathf.Lerp(r1.height, r2.height, interp));
            };
            s_InterpolateFuncs[typeof(Color)] = (a, b, interp) => Color.Lerp((Color)a, (Color)b, interp);
        }

        public void Start()
        {
            if (running || m_Schedule == null)
                return;

            running = true;

            UpdateValue();
            m_Schedule.Execute(Step).StartingIn(0).Every(k_StepDuration).Until(() => progress >= 1f || !running);
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            progress = 0;

            if (finished != null)
                finished();
        }

        void Step()
        {
            progress += progressIncrement;

            UpdateValue();

            if (progress >= 1.0f)
            {
                Stop();
            }
        }

        void UpdateValue()
        {
            float effectiveProgress = easingCurve != null ? easingCurve.GetEffectiveProgress(progress) : progress;
            object currentValue = Interpolate(startValue, endValue, effectiveProgress);

            NotifyValueUpdated(currentValue);
        }

        protected abstract void NotifyValueUpdated(object value);

        static object Interpolate(object a, object b, float interp)
        {
            Func<object, object, float, object> interpolateFunc = null;

            if (s_InterpolateFuncs.TryGetValue(a.GetType(), out interpolateFunc))
            {
                return interpolateFunc(a, b, interp);
            }
            return a;
        }
    }

    internal class ValueAnimation<T> : ValueAnimationBase
    {
        public event Action<T> valueUpdated;

        public T from { get; set; }
        public T to { get; set; }

        protected override object startValue => from;
        protected override object endValue => to;

        public ValueAnimation(IVisualElementScheduler schedule) : base(schedule)
        {
        }

        protected override void NotifyValueUpdated(object value)
        {
            if (valueUpdated != null)
                valueUpdated((T)value);
        }
    }
}
