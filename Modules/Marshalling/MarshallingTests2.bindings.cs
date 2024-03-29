// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// NOTE
// This file is STRICTLY for test purposes only. The point is to test the managed->native call through the
// BindingsGenerator. There is currently no alternative way to test this, than to have test classes lying around.


using System;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class MarshallingTests2
    {
        public static extern void ParameterNonBlittableStructReuse(StructCoreString param);
    }
}

