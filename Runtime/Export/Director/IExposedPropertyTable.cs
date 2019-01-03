// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEngine
{
    public interface IExposedPropertyTable
    {
        void SetReferenceValue(PropertyName id, UnityEngine.Object value);
        UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid);
        void ClearReferenceValue(PropertyName id);
    }
}
