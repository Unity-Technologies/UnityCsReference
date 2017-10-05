// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.XR.Tango
{
    internal static partial class TangoDevice
    {
        static private XR.ARBackgroundRenderer m_BackgroundRenderer = null;
        static private string m_AreaDescriptionUUID = "";

        static internal string areaDescriptionUUID
        {
            get { return m_AreaDescriptionUUID; }
            set { m_AreaDescriptionUUID = value; }
        }

        static internal XR.ARBackgroundRenderer backgroundRenderer
        {
            get
            {
                return m_BackgroundRenderer;
            }
            set
            {
                if (value == null)
                    return;

                if (m_BackgroundRenderer != null)
                {
                    m_BackgroundRenderer.backgroundRendererChanged -= OnBackgroundRendererChanged;
                }

                m_BackgroundRenderer = value;

                // Set event handlers
                m_BackgroundRenderer.backgroundRendererChanged += OnBackgroundRendererChanged;

                // Set current state of ARBackgroundRenderer in Tango device
                OnBackgroundRendererChanged();
            }
        }

        static private void OnBackgroundRendererChanged()
        {
            SetBackgroundMaterial(m_BackgroundRenderer.backgroundMaterial);
            SetRenderMode(m_BackgroundRenderer.mode);
        }

        static internal bool Connect(TangoConfig config)
        {
            string[] boolKeys, intKeys, longKeys, doubleKeys, stringKeys;
            bool[] boolValues;
            int[] intValues;
            long[] longValues;
            double[] doubleValues;
            string[] stringValues;

            CopyDictionaryToArrays(config.m_boolParams, out boolKeys, out boolValues);
            CopyDictionaryToArrays(config.m_intParams, out intKeys, out intValues);
            CopyDictionaryToArrays(config.m_longParams, out longKeys, out longValues);
            CopyDictionaryToArrays(config.m_doubleParams, out doubleKeys, out doubleValues);
            CopyDictionaryToArrays(config.m_stringParams, out stringKeys, out stringValues);

            return Connect(boolKeys, boolValues,
                intKeys, intValues,
                longKeys, longValues,
                doubleKeys, doubleValues,
                stringKeys, stringValues);
        }

        static private void CopyDictionaryToArrays<T>(Dictionary<string, T> dictionary, out string[] keys, out T[] values)
        {
            if (dictionary.Count == 0)
            {
                keys = null;
                values = null;
            }
            else
            {
                keys = new string[dictionary.Count];
                values = new T[dictionary.Count];

                int i = 0;
                foreach (KeyValuePair<string, T> entry in dictionary)
                {
                    keys[i] = entry.Key;
                    values[i++] = entry.Value;
                }
            }
        }
    }
}
