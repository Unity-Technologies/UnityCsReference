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

        public T Resolve(ExposedPropertyResolver resolver)
        {
            bool isValid;
            Object result = ExposedPropertyResolver.ResolveReferenceInternal(resolver.table, exposedName, out isValid);
            if (isValid)
                return result as T;
            else
                return defaultValue as T;
        }
    }
}
