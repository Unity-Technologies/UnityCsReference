// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Tango
{
    internal enum AreaLearningMode
    {
        None = 0,
        LocalAreaDescriptionWithoutLearning,
        LocalAreaDescription,
        CloudAreaDescription
    }

    internal partial class TangoConfig
    {
        internal bool enableMotionTracking
        {
            set
            {
                AddConfigParameter("config_enable_motion_tracking", value);
                AddConfigParameter("config_enable_auto_recovery", value);
            }
        }

        internal bool enableDepth
        {
            set
            {
                AddConfigParameter("config_enable_depth", value);

                if (value)
                    AddConfigParameter("config_depth_mode", 0);
                else
                    RemoveConfigParameter("config_depth_mode");
            }
        }

        internal bool enableColorCamera
        {
            set { AddConfigParameter("config_enable_color_camera", value); }
        }

        internal AreaLearningMode areaLearningMode
        {
            set
            {
                switch (value)
                {
                    case AreaLearningMode.LocalAreaDescriptionWithoutLearning:
                        AddConfigParameter("config_enable_drift_correction", false);
                        AddConfigParameter("config_load_area_description_UUID", TangoDevice.areaDescriptionUUID);
                        AddConfigParameter("config_enable_learning_mode", false);
                        AddConfigParameter("config_experimental", false);
                        break;
                    case AreaLearningMode.LocalAreaDescription:
                        AddConfigParameter("config_enable_drift_correction", false);
                        AddConfigParameter("config_load_area_description_UUID", TangoDevice.areaDescriptionUUID);
                        AddConfigParameter("config_enable_learning_mode", true);
                        AddConfigParameter("config_experimental", false);
                        break;
                    case AreaLearningMode.CloudAreaDescription:
                        AddConfigParameter("config_enable_drift_correction", false);
                        RemoveConfigParameter("config_load_area_description_UUID");
                        AddConfigParameter("config_enable_learning_mode", false);
                        AddConfigParameter("config_experimental", true);
                        break;
                }
            }
        }

        internal Dictionary<string, bool> m_boolParams = new Dictionary<string, bool>();
        internal Dictionary<string, double> m_doubleParams = new Dictionary<string, double>();
        internal Dictionary<string, int> m_intParams = new Dictionary<string, int>();
        internal Dictionary<string, long> m_longParams = new Dictionary<string, long>();
        internal Dictionary<string, string> m_stringParams = new Dictionary<string, string>();

        internal void AddConfigParameter(string name, bool value)
        {
            m_boolParams[name] = value;
        }

        internal void AddConfigParameter(string name, double value)
        {
            m_doubleParams[name] = value;
        }

        internal void AddConfigParameter(string name, int value)
        {
            m_intParams[name] = value;
        }

        internal void AddConfigParameter(string name, long value)
        {
            m_longParams[name] = value;
        }

        internal void AddConfigParameter(string name, string value)
        {
            m_stringParams[name] = value;
        }

        internal void RemoveConfigParameter(string name)
        {
            m_stringParams.Remove(name);
            m_longParams.Remove(name);
            m_intParams.Remove(name);
            m_doubleParams.Remove(name);
            m_boolParams.Remove(name);
        }
    }
}
