// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.AnimatedValues
{
    public abstract class BaseAnimValueNonAlloc<T> : BaseAnimValue<T> where T : IEquatable<T>
    {
        protected BaseAnimValueNonAlloc(T value) : base(value)
        {
        }

        protected BaseAnimValueNonAlloc(T value, UnityAction callback) : base(value, callback)
        {
        }

        protected override bool AreEqual(T a, T b)
        {
            return a.Equals(b);
        }
    }

    public abstract class BaseAnimValue<T> : ISerializationCallbackReceiver
    {
        T m_Start;

        [SerializeField]
        T m_Target;

        double m_LastTime;
        double m_LerpPosition = 1f;

        float m_Speed;
        public float speed = 2f;

        [NonSerialized]
        public UnityEvent valueChanged;

        // Don't have m_Animating survive script reloads, as it could cause the AnimValue to get stuck.
        // If m_Animating was true after reload but the Update callback registration had been lost,
        // The value would never change and also never re-register to the Update callback.
        [NonSerialized]
        bool m_Animating;

        protected BaseAnimValue(T value)
        {
            m_Start = value;
            m_Target = value;
            valueChanged = new UnityEvent();
        }

        protected BaseAnimValue(T value, UnityAction callback)
        {
            m_Start = value;
            m_Target = value;
            valueChanged = new UnityEvent();
            valueChanged.AddListener(callback);
        }

        protected virtual bool AreEqual(T a, T b)
        {
            return a.Equals(b);
        }

        static T2 Clamp<T2>(T2 val, T2 min, T2 max) where T2 : IComparable<T2>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        protected void BeginAnimating(T newTarget, T newStart)
        {
            BeginAnimating(newTarget, newStart, speed);
        }

        void BeginAnimating(T newTarget, T newStart, float animationSpeed)
        {
            m_Speed = animationSpeed;
            m_Start = newStart;
            m_Target = newTarget;
            if (!m_Animating)
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            }
            m_Animating = true;
            m_LastTime = EditorApplication.timeSinceStartup;
            m_LerpPosition = 0;
        }

        public bool isAnimating
        {
            get { return m_Animating; }
        }

        void Update()
        {
            if (!m_Animating)
                return;

            // update the lerpPosition
            UpdateLerpPosition();

            if (valueChanged != null)
                valueChanged.Invoke();

            if (lerpPosition >= 1f)
            {
                m_Animating = false;
                EditorApplication.update -= Update;
            }
        }

        protected float lerpPosition
        {
            get
            {
                var v = 1.0 - m_LerpPosition;
                var result = 1.0 - v * v * v * v;
                return (float)result;
            }
        }

        void UpdateLerpPosition()
        {
            double nowTime = EditorApplication.timeSinceStartup;
            double deltaTime = nowTime - m_LastTime;

            m_LerpPosition = Clamp(m_LerpPosition + (deltaTime * m_Speed), 0.0, 1.0);
            m_LastTime = nowTime;
        }

        protected void StopAnim(T newValue)
        {
            // If the new value is different, or we might be in the middle of a fade, we need to refresh.
            // Checking GetValue is not reliable on its own, since for e.g. bool it'll return the "closest" value,
            // but that doesn't mean the fade is done.
            bool invoke = (!AreEqual(newValue, GetValue()) || m_LerpPosition < 1) && valueChanged != null;

            m_Target = newValue;
            m_Start = newValue;
            m_LerpPosition = 1;
            m_Animating = false;
            // Only refresh *after* we set the correct new value.
            if (invoke)
                valueChanged.Invoke();
        }

        protected T start
        {
            get { return m_Start; }
        }

        public T target
        {
            get { return m_Target; }
            set
            {
                if (!AreEqual(m_Target, value))
                    BeginAnimating(value, this.value);
            }
        }

        internal void SetTarget(T newTarget, float animationSpeed)
        {
            if (!AreEqual(m_Target, value))
                BeginAnimating(newTarget, value, animationSpeed);
        }

        public T value
        {
            get { return lerpPosition >= 1f ? target : GetValue(); }
            set { StopAnim(value); }
        }

        internal void SkipFading()
        {
            StopAnim(target);
        }

        protected abstract T GetValue();

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Ensure we resume animating after script reload if value differs from target.
            if (!GetValue().Equals(target) && !m_Animating)
            {
                // Shouldn't be necessary to remove first since we use m_Animating
                // to keep track of it, but it doesn't hurt.
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
                m_Animating = true;
            }
        }
    }

    [Serializable]
    public class AnimFloat : BaseAnimValueNonAlloc<float>
    {
        [SerializeField]
        private float m_Value;

        public AnimFloat(float value)
            : base(value)
        {}

        public AnimFloat(float value, UnityAction callback) : base(value, callback)
        {}

        protected override float GetValue()
        {
            m_Value = Mathf.Lerp(start, target, lerpPosition);
            return m_Value;
        }
    }

    [Serializable]
    public class AnimVector3 : BaseAnimValueNonAlloc<Vector3>
    {
        [SerializeField]
        private Vector3 m_Value;

        public AnimVector3()
            : base(Vector3.zero)
        {}

        public AnimVector3(Vector3 value)
            : base(value)
        {}

        public AnimVector3(Vector3 value, UnityAction callback)
            : base(value, callback)
        {}

        protected override Vector3 GetValue()
        {
            m_Value = Vector3.Lerp(start, target, lerpPosition);
            return m_Value;
        }
    }

    [Serializable]
    public class AnimBool : BaseAnimValueNonAlloc<bool>
    {
        [SerializeField]
        private float m_Value;

        public AnimBool()
            : base(false)
        {}

        public AnimBool(bool value)
            : base(value)
        {}

        public AnimBool(UnityAction callback)
            : base(false, callback)
        {}

        public AnimBool(bool value, UnityAction callback)
            : base(value, callback)
        {}

        public float faded
        {
            get
            {
                GetValue();
                return m_Value;
            }
        }

        protected override bool GetValue()
        {
            float startVal = target ? 0f : 1f;
            float end = 1f - startVal;

            m_Value = Mathf.Lerp(startVal, end, lerpPosition);

            return m_Value > .5f;
        }

        public float Fade(float from, float to)
        {
            return Mathf.Lerp(from, to, faded);
        }
    }

    [Serializable]
    public class AnimQuaternion : BaseAnimValueNonAlloc<Quaternion>
    {
        [SerializeField]
        private Quaternion m_Value;


        public AnimQuaternion(Quaternion value)
            : base(value)
        {}

        public AnimQuaternion(Quaternion value, UnityAction callback)
            : base(value, callback)
        {}

        protected override Quaternion GetValue()
        {
            m_Value = Quaternion.Slerp(start, target, lerpPosition);
            return m_Value;
        }
    }
}
//namespace
