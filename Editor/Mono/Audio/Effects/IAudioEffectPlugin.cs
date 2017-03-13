// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public abstract class IAudioEffectPlugin
    {
        public abstract bool SetFloatParameter(string name, float value);
        public abstract bool GetFloatParameter(string name, out float value);
        public abstract bool GetFloatParameterInfo(string name, out float minRange, out float maxRange, out float defaultValue);
        public abstract bool GetFloatBuffer(string name, out float[] data, int numsamples);
        public abstract int GetSampleRate();
        public abstract bool IsPluginEditableAndEnabled();
    }
}
