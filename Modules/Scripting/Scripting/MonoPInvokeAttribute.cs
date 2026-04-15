// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace AOTInternal
{
    // This is a version private to Scripting because the Burst ILPP is not ready for the public version to move
    // down to Scripting. When Burst is a module, this should become the public version. 

    // Mono AOT compiler detects this attribute by name and generates required wrappers for
    // native->managed callbacks. Works only for static methods.
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    internal class MonoPInvokeCallbackAttribute : Attribute
    {
        internal MonoPInvokeCallbackAttribute(Type type) {}
    }
}
