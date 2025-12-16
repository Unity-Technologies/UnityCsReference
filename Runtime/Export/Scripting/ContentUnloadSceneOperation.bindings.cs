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
    [NativeHeader("Runtime/ContentLoad/ContentUnloadSceneOperation.h")]
    internal sealed class ContentUnloadSceneOperation : AsyncOperation
    {
        public ContentUnloadSceneOperation() { }

        private ContentUnloadSceneOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static ContentUnloadSceneOperation ConvertToManaged(IntPtr ptr) => new ContentUnloadSceneOperation(ptr);
            public static IntPtr ConvertToUnmanaged(ContentUnloadSceneOperation op) => op.m_Ptr;
        }
    }
}
