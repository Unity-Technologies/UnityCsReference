// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy.Editor.Tests
{
    /// <summary>
    /// This is a test hierarchy node type handler with a native counterpart.
    /// </summary>
    /// <remarks>
    /// This handler is part of Editor module, because we want to avoid it being included in runtime builds.
    /// </remarks>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/HierarchyEditor/TestNativeHierarchyNodeTypeHandler.h")]
    [NativeHeader("Modules/HierarchyEditor/TestNativeHierarchyNodeTypeHandlerBindings.h")]
    internal sealed partial class TestNativeHierarchyNodeTypeHandler : HierarchyNodeTypeHandler
    {
        internal new static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(TestNativeHierarchyNodeTypeHandler handler) => handler.m_Ptr;
        }

        [AutoStaticsCleanupOnCodeReload]
        static HierarchyNodeType s_NodeType;

        public new Hierarchy Hierarchy => base.Hierarchy;
        public new HierarchyCommandList CommandList => base.CommandList;
        public bool ConstructorCalled { get; private set; }
        public bool InitializeCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        TestNativeHierarchyNodeTypeHandler()
        {
            throw new NotSupportedException();
        }

        TestNativeHierarchyNodeTypeHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
            ConstructorCalled = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            InitializeCalled = true;
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalled = true;
            base.Dispose(disposing);
        }

        public new HierarchyNodeType GetNodeType()
        {
            if (s_NodeType == HierarchyNodeType.Null)
                s_NodeType = new HierarchyNodeType(GetStaticNodeType());

            return s_NodeType;
        }

        [FreeFunction("TestNativeHierarchyNodeTypeHandlerBindings::GetHierarchyScriptingObject", HasExplicitThis = true, IsThreadSafe = true)]
        public extern object GetHierarchyScriptingObject();

        [FreeFunction("TestNativeHierarchyNodeTypeHandlerBindings::GetHierarchyCommandListScriptingObject", HasExplicitThis = true, IsThreadSafe = true)]
        public extern object GetHierarchyCommandListScriptingObject();

        [FreeFunction("TestNativeHierarchyNodeTypeHandlerBindings::GetStaticNodeType", IsThreadSafe = true)]
        static extern int GetStaticNodeType();

        #region Called from native
        [RequiredByNativeCode(Optional = true)]
        static IntPtr CreateTestNativeNodeTypeHandler(IntPtr nativePtr, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var handler = new TestNativeHierarchyNodeTypeHandler(nativePtr,
                (Hierarchy)GCHandle.FromIntPtr(hierarchyPtr).Target,
                (HierarchyCommandList)GCHandle.FromIntPtr(cmdListPtr).Target);
            handler.Initialize();
            return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
        }
        #endregion
    }
}
