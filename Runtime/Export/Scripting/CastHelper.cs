// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    //generic casts in our mono carry a uncomfortable performance penalty. in some cases, we are sure the cast will succeed. we use the
    //trick with the struct below to get a cast done without any checks in that case, which gets us a performance increase.
    internal struct CastHelper<T>
    {
        public T t;
        public System.IntPtr onePointerFurtherThanT;
    }
}
