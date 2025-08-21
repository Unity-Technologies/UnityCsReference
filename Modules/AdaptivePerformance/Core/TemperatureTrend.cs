// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.AdaptivePerformance.Editor.Tests")]
namespace UnityEngine.AdaptivePerformance
{
    internal class TemperatureTrend
    {
        bool m_UseProviderTrend;

        // sums for linear least-squares regression
        double m_SumX; // time
        double m_SumY; // temperature
        double m_SumXY; // time * temperature
        double m_SumXX; // time * time

        const int MeasurementTimeframeSeconds = 1 * 20;
        const int UpdateFrequency = 10;
        const int SamplesCapacity = UpdateFrequency * MeasurementTimeframeSeconds;

        // At this slope of a fitted line we report ThermalTrend of 1.0;
        const double SlopeAtMaxTrend = 0.1 / MeasurementTimeframeSeconds;

        float[] m_TimeStamps = new float[SamplesCapacity];
        float[] m_Temperature = new float[SamplesCapacity];
        int m_NumValues;
        int m_NextValueIndex;
        int m_OldestValueIndex;

        private void PopOldestValue()
        {
            double x = m_TimeStamps[m_OldestValueIndex];
            double y = m_Temperature[m_OldestValueIndex];
            m_SumX -= x;
            m_SumY -= y;
            m_SumXY -= x * y;
            m_SumXX -= x * x;

            m_OldestValueIndex = (m_OldestValueIndex + 1) % SamplesCapacity;
            --m_NumValues;
        }

        private void PushNewValue(float tempLevel, float timestamp)
        {
            m_TimeStamps[m_NextValueIndex] = timestamp;
            m_Temperature[m_NextValueIndex] = tempLevel;
            m_NextValueIndex = (m_NextValueIndex + 1) % SamplesCapacity;
            ++m_NumValues;

            double x = timestamp;
            double y = tempLevel;
            m_SumX += x;
            m_SumY += y;
            m_SumXY += x * y;
            m_SumXX += x * x;
        }

        public TemperatureTrend(bool useProviderTrend)
        {
            m_UseProviderTrend = useProviderTrend;
        }

        public void Reset()
        {
            m_NumValues = 0;
            m_OldestValueIndex = 0;
            m_NextValueIndex = 0;
            m_SumX = 0.0;
            m_SumY = 0.0;
            m_SumXY = 0.0;
            m_SumXX = 0.0;
            ThermalTrend = 0.0f;
        }

        public float ThermalTrend { get; private set; }

        private void UpdateTrend()
        {
            if (m_NumValues < 2)
            {
                ThermalTrend = 0.0f;
                return;
            }

            double p = m_NumValues * m_SumXY - m_SumX * m_SumY;
            double q = m_NumValues * m_SumXX - m_SumX * m_SumX;
            double m = p / q;

            m /= SlopeAtMaxTrend;

            if (m >= 1.0)
            {
                ThermalTrend = 1.0f;
            }
            else if (m >= -1.0)
            {
                if (Math.Abs(m) < 0.00001)
                    ThermalTrend = 0.0f;
                else
                    ThermalTrend = (float)m;
            }
            else if (m <= -1.0)
            {
                ThermalTrend = -1.0f;
            }
            else // NaN
            {
                ThermalTrend = 0.0f;
            }
        }

        public void Update(float temperatureTrendFromProvider, float newTemperatureLevel, bool changed, float newTemperatureTimestamp)
        {
            if (m_UseProviderTrend)
            {
                ThermalTrend = temperatureTrendFromProvider;
                return;
            }

            // The temperature level is not linear itself
            // To get higher trend values closer to 1.0 we use temp^3
            newTemperatureLevel = newTemperatureLevel * newTemperatureLevel * newTemperatureLevel;

            if (m_NumValues == 0)
            {
                PushNewValue(newTemperatureLevel, newTemperatureTimestamp);
                UpdateTrend();
                return;
            }

            bool updateTrend = false;

            float oldestTimeStamp = m_TimeStamps[m_OldestValueIndex];
            float timestampThresholdForNewValue = oldestTimeStamp + 1.0f / UpdateFrequency * m_NumValues;

            if (newTemperatureTimestamp - oldestTimeStamp > MeasurementTimeframeSeconds)
            {
                PopOldestValue();
                updateTrend = true;
            }

            if (changed || newTemperatureTimestamp >= timestampThresholdForNewValue)
            {
                if (m_NumValues == SamplesCapacity)
                    PopOldestValue();

                PushNewValue(newTemperatureLevel, newTemperatureTimestamp);
                updateTrend = true;
            }

            if (updateTrend)
                UpdateTrend();
        }

        public int NumValues
        {
            get => m_NumValues;
            set => m_NumValues = value;
        }
    }
}
