// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class SerializedMinMaxColor
    {
        public SerializedProperty maxColor;
        public SerializedProperty minColor;
        public SerializedProperty minMax;

        public SerializedMinMaxColor(SerializedModule m)
        {
            Init(m, "curve");
        }

        public SerializedMinMaxColor(SerializedModule m, string name)
        {
            Init(m, name);
        }

        void Init(SerializedModule m, string name)
        {
            maxColor = m.GetProperty(name, "maxColor");
            minColor = m.GetProperty(name, "minColor");
            minMax = m.GetProperty(name, "minMax");
        }
    }
} // namespace UnityEditor
