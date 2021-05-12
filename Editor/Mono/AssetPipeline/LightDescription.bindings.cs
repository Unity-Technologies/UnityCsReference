// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.AssetImporters
{
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/AssetPipeline/ModelImporting/LightDescription.h")]
    public class LightDescription : IDisposable
    {
        internal IntPtr m_Ptr;

        public LightDescription()
        {
            m_Ptr = Internal_Create();
        }

        ~LightDescription()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public bool TryGetProperty(string propertyName, out float value) => TryGetFloatProperty(propertyName, out value);
        public bool TryGetProperty(string propertyName, out Vector4 value) => TryGetVector4Property(propertyName, out value);
        public bool TryGetProperty(string propertyName, out string value) => TryGetStringProperty(propertyName, out value);
        public bool TryGetProperty(string propertyName, out int value) => TryGetIntProperty(propertyName, out value);

        public extern void GetVector4PropertyNames(List<string> names);
        public extern void GetFloatPropertyNames(List<string> names);
        public extern void GetStringPropertyNames(List<string> names);
        public extern void GetIntPropertyNames(List<string> names);

        extern bool TryGetVector4Property(string propertyName, out Vector4 value);
        extern bool TryGetFloatProperty(string propertyName, out float value);
        extern bool TryGetStringProperty(string propertyName, out string value);
        extern bool TryGetIntProperty(string propertyName, out int value);

        public bool TryGetAnimationCurve(string clipName, string propertyName, out AnimationCurve value)
        {
            value = TryGetAnimationCurve(clipName, propertyName);
            return value != null;
        }

        public extern bool HasAnimationCurveInClip(string clipName, string propertyName);
        public extern bool HasAnimationCurve(string propertyName);
        extern AnimationCurve TryGetAnimationCurve(string clipName, string propertyName);

        static extern IntPtr Internal_Create();
        static extern void Internal_Destroy(IntPtr ptr);
    }
}
