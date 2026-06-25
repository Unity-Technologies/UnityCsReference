// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace AOT
{
    // Mono AOT compiler detects this attribute by name and generates required wrappers for
    // native->managed callbacks. Works only for static methods.
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type) {}
    }
}
