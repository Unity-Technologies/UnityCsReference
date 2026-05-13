// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

internal interface IStyleDataGroup<T>
{
    T GetDefault();
    T Copy();
    void CopyFrom(ref T other);
    void Dispose();
}
