// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Loading
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/ContentLoad/ContentLoadOperation.h")]
    internal sealed class ContentLoadOperation : ResourceRequest
    {
        [NativeMethod("GetLoadedObject")]
        protected override extern Object GetResult();

        public ContentLoadOperation() { }

        private ContentLoadOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static ContentLoadOperation ConvertToManaged(IntPtr ptr) => new ContentLoadOperation(ptr);
            public static IntPtr ConvertToUnmanaged(ContentLoadOperation op) => op.m_Ptr;
        }
    }
}
