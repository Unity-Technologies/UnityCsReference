// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [RequiredByNativeCode]
    internal enum RegistryComplianceStatus
    {
        Compliant           = 0,
        PartiallyNonCompliant = 1,
        NonCompliant  = 2,
    }
}
