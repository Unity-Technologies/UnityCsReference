// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Loading;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;


namespace UnityEngine.Loading
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/L1LoadingSystem.h")]
    [NativeClass("L1ResourceHandle")]
    internal struct L1LoadingHandle : IEquatable<L1LoadingHandle>
    {
        internal UInt64 value;

        public bool Equals(L1LoadingHandle other) => value == other.value;
        public override bool Equals(object obj) => obj is L1LoadingHandle other && Equals(other);
        public override int GetHashCode() => value.GetHashCode();
        public static bool operator ==(L1LoadingHandle lhs, L1LoadingHandle rhs) => lhs.value == rhs.value;
        public static bool operator !=(L1LoadingHandle lhs, L1LoadingHandle rhs) => lhs.value != rhs.value;
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/ContentLoad/Public/L1LoadingSystem.h")]
    internal sealed class L1LoadingOperation : AsyncOperation
    {
        public L1LoadingOperation() { }

        public extern Object asset { get; }

        public extern L1LoadingHandle Handle { get; }

        public extern bool WaitForCompletion(int timeout);

        private L1LoadingOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static L1LoadingOperation ConvertToManaged(IntPtr ptr) => new L1LoadingOperation(ptr);
            public static IntPtr ConvertToUnmanaged(L1LoadingOperation op) => op.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/ContentLoad/Public/L1LoadingSystem.h")]
    internal sealed class L1UnloadOperation : AsyncOperation
    {
        public L1UnloadOperation() { }

        public extern L1LoadingHandle Handle { get; }

        public extern bool WaitForCompletion(int timeout);

        private L1UnloadOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static L1UnloadOperation ConvertToManaged(IntPtr ptr) => new L1UnloadOperation(ptr);
            public static IntPtr ConvertToUnmanaged(L1UnloadOperation op) => op.m_Ptr;
        }
    }

    [NativeHeader("Modules/ContentLoad/Public/L1LoadingSystem.h")]
    [StaticAccessor("GetL1LoadingSystem()", StaticAccessorType.Dot)]
    internal sealed class LoadableManager
    {
        public extern static L1LoadingOperation LoadObjectAsync(LoadableReference weakRef);

        public extern static L1UnloadOperation ReleaseObjectAsync(L1LoadingHandle handle);
    }
}
