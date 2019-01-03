// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEngine
{
    [System.Serializable]
    [UsedByNativeCode(Name = "ExposedReference")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ExposedReference<T> where T : UnityEngine.Object
    {
        [SerializeField]
        public PropertyName exposedName;

        [SerializeField]
        public UnityEngine.Object defaultValue;

        public T Resolve(IExposedPropertyTable resolver)
        {
            if (resolver != null)
            {
                bool isValid;
                Object result = resolver.GetReferenceValue(exposedName, out isValid);
                if (isValid)
                    return result as T;
            }

            return defaultValue as T;
        }
    }
}
