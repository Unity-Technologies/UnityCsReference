// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum CustomizedDependencyType
    {
        None                = 0,
        Resettable          = 1 << 0,
        NonResettable       = 1 << 1,
        All                 = Resettable | NonResettable
    }
}
