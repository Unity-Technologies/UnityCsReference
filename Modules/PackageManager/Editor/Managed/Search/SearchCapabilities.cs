// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [RequiredByNativeCode]
    [Flags]
    internal enum SearchCapabilities
    {
        None           = 0,
        Classification = 1 << 0,
        Ordering       = 1 << 1 ,
        Pagination     = 1 << 2,
    }
}
