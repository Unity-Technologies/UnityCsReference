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
    public abstract class IAudioEffectPluginGUI
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Vendor { get; }
        public abstract bool OnGUI(IAudioEffectPlugin plugin);
    }
}
