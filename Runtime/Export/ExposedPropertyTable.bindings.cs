// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Director/Core/ExposedPropertyTable.bindings.h")]
    [NativeHeader("Runtime/Utilities/PropertyName.h")]
    public struct ExposedPropertyResolver
    {
        internal IntPtr table;

        internal static Object ResolveReferenceInternal(IntPtr ptr, PropertyName name, out bool isValid)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("Argument \"ptr\" can't be null.");

            return ResolveReferenceBindingsInternal(ptr, name, out isValid);
        }

        [FreeFunction("ExposedPropertyTableBindings::ResolveReferenceInternal")]
        extern private static Object ResolveReferenceBindingsInternal(IntPtr ptr, PropertyName name, out bool isValid);
    }
}
