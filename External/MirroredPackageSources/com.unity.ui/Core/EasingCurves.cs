namespace UnityEngine.UIElements.Experimental
{
    /// <summary>
    /// A collection of easing curves to be used with ValueAnimations.
    /// </summary>
    /// <remarks>
    /// Easing curves control the rate of change of a value over time.
    /// </remarks>
    public static class Easing
    {
        private const float HalfPi = Mathf.PI / 2;

        /// <undoc/>
        public static float Step(float t)
        {
            return t < 0.5f ? 0 : 1;
        }

        /// <undoc/>
        public static float Linear(float t)
        {
            return t;
        }

        /// <undoc/>
        public static float InSine(float t)
        {
            return Mathf.Sin(HalfPi * (t - 1)) + 1;
        }

        /// <undoc/>
        public static float OutSine(float t)
        {
            return Mathf.Sin(t * HalfPi);
        }

        /// <undoc/>
        public static float InOutSine(float t)
        {
            return (Mathf.Sin(Mathf.PI * (t - 0.5f)) + 1) * 0.5f;
        }

        /// <undoc/>
        public static float InQuad(float t)
        {
            return t * t;
        }

        /// <undoc/>
        public static float OutQuad(float t)
        {
            return t * (2 - t);
        }

        /// <undoc/>
        public static float InOutQuad(float t)
        {
            t *= 2;
            if (t < 1.0f)
                return (t * t * 0.5f);
            return -0.5f * ((t - 1) * (t - 3) - 1);
        }

        /// <undoc/>
        public static float InCubic(float t)
        {
            return InPower(t, 3);
        }

        /// <undoc/>
        public static float OutCubic(float t)
        {
            return OutPower(t, 3);
        }

        /// <undoc/>
        public static float InOutCubic(float t)
        {
            return InOutPower(t, 3);
        }

        /// <undoc/>
        public static float InPower(float t, int power)
        {
            return Mathf.Pow(t, power);
        }

        /// <undoc/>
        public static float OutPower(float t, int power)
        {
            var sign = power % 2 == 0 ? -1 : 1;
            return (float)(sign * (Mathf.Pow(t - 1, power) + sign));
        }

        /// <undoc/>
        public static float InOutPower(float t, int power)
        {
            t *= 2;
            if (t < 1) return InPower(t, power) * 0.5f;
            var sign = power % 2 == 0 ? -1 : 1;
            return sign * 0.5f * (Mathf.Pow(t - 2, power) + sign * 2);
        }

        /// <undoc/>
        public static float InBounce(float t)
        {
            return 1 - OutBounce((1 - t));
        }

        /// <undoc/>
        public static float OutBounce(float t)
        {
            if (t < (1 / 2.75f))
            {
                return (7.5625f * t * t);
            }

            if (t < (2 / 2.75f))
            {
                float postFix = t -= (1.5f / 2.75f);
                return (7.5625f * (postFix) * t + .75f);
            }
            else if (t < (2.5f / 2.75f))
            {
                float postFix = t -= (2.25f / 2.75f);
                return (7.5625f * (postFix) * t + .9375f);
            }
            else
            {
                float postFix = t -= (2.625f / 2.75f);
                return (7.5625f * (postFix) * t + .984375f);
            }
        }

        /// <undoc/>
        public static float InOutBounce(float t)
        {
            if (t < 0.5f)
            {
                return InBounce(t * 2) * 0.5f;
            }
            else
            {
                return OutBounce((t - 0.5f) * 2) * 0.5f + 0.5f;
            }
        }

        /// <undoc/>
        public static float InElastic(float t)
        {
            if (t == 0) return 0;
            if ((t) == 1) return 1;
            float p = .3f;
            float s = p / 4;
            float power = Mathf.Pow(2, 10 * (t -= 1));
            return -(power * Mathf.Sin((t - s) * (2 * Mathf.PI) / p));
        }

        /// <undoc/>
        public static float OutElastic(float t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;

            float p = .3f;
            float s = p / 4;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - s) * (2 * Mathf.PI) / p) + 1;
        }

        /// <undoc/>
        public static float InOutElastic(float t)
        {
            if (t < 0.5f)
            {
                return InElastic(t * 2) * 0.5f;
            }
            else
            {
                return OutElastic((t - 0.5f) * 2) * 0.5f + 0.5f;
            }
        }

        /// <undoc/>
        public static float InBack(float t)
        {
            float s = 1.70158f;
            return (t) * t * ((s + 1) * t - s);
        }

        /// <undoc/>
        public static float OutBack(float t)
        {
            return 1 - (InBack(1 - t));
        }

        /// <undoc/>
        public static float InOutBack(float t)
        {
            if (t < 0.5f)
            {
                return InBack(t * 2) * 0.5f;
            }
            else
            {
                return OutBack((t - 0.5f) * 2) * 0.5f + 0.5f;
            }
        }

        /// <undoc/>
        public static float InBack(float t, float s)
        {
            return (t) * t * ((s + 1) * t - s);
        }

        /// <undoc/>
        public static float OutBack(float t, float s)
        {
            return 1 - (InBack(1 - t, s));
        }

        /// <undoc/>
        public static float InOutBack(float t, float s)
        {
            if (t < 0.5f)
            {
                return InBack(t * 2, s) * 0.5f;
            }
            else
            {
                return OutBack((t - 0.5f) * 2, s) * 0.5f + 0.5f;
            }
        }

        /// <undoc/>
        public static float InCirc(float t)
        {
            return -(Mathf.Sqrt(1 - (t * t)) - 1);
        }

        /// <undoc/>
        public static float OutCirc(float t)
        {
            t = t - 1;
            return Mathf.Sqrt(1 - (t * t));
        }

        /// <undoc/>
        public static float InOutCirc(float t)
        {
            t = t * 2;

            if (t < 1)
            {
                return -0.5f * (Mathf.Sqrt(1 - (t * t)) - 1);
            }
            else
            {
                t = t - 2;

                return 0.5f * (Mathf.Sqrt(1 - (t * t)) + 1);
            }
        }
    }
}
