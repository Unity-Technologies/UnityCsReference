// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal class RunningAverage
    {
        public RunningAverage(int sampleWindowSize = 100)
        {
            m_Values = new float[sampleWindowSize];
        }

        public int GetNumValues()
        {
            return m_NumValues;
        }

        public int GetSampleWindowSize()
        {
            return m_Values.Length;
        }

        public float GetAverageOr(float defaultValue)
        {
            return (m_NumValues > 0) ? m_AverageValue : defaultValue;
        }

        public float GetMostRecentValueOr(float defaultValue)
        {
            return (m_NumValues > 0) ? m_Values[m_LastIndex] : defaultValue;
        }

        public void AddValue(float NewValue)
        {
            // Temporarily remember the oldest value, which will overwritten by the new value
            int oldestIndex = (m_LastIndex + 1) % m_Values.Length;
            float oldestValue = m_Values[oldestIndex];

            // Store the new value in the array, overwriting the 100th oldest value
            m_LastIndex = oldestIndex;
            m_Values[m_LastIndex] = NewValue;

            // Update average value over the past numValues (removing oldest and adding newest value)
            float totalValue = m_AverageValue * m_NumValues + NewValue - oldestValue;
            m_NumValues = Mathf.Min(m_NumValues + 1, m_Values.Length);
            m_AverageValue = totalValue / m_NumValues;
        }

        public void Reset()
        {
            m_NumValues = 0;
            m_LastIndex = -1;
            m_AverageValue = 0.0f;
            System.Array.Clear(m_Values, 0, m_Values.Length);
        }

        private float[] m_Values = null;
        private int m_NumValues = 0;
        private int m_LastIndex = -1;
        private float m_AverageValue = 0.0f;
    }
}
