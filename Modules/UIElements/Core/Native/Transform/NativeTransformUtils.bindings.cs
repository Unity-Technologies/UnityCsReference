// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements;

[NativeHeader("Modules/UIElements/Core/Native/Transform/NativeTransformUtils.h")]
[StructLayout(LayoutKind.Sequential)]
internal static class NativeTransformUtils
{
    static unsafe NativeTransformUtils()
    {
        var manager = new Manager();
        InitSharedManager((IntPtr)(&manager));
    }

    public static extern void InitSharedManager(IntPtr managerPtr);

    public static extern void UpdateWorldTransform(UnmanagedDataHandle handle);
    public static extern void UpdateWorldTransformHierarchy(UnmanagedDataHandle handle);

    // For performance tests only
    public static extern int CountHierarchy(UnmanagedDataHandle handle);

    // Assumes flat panel.
    public static extern void UpdateBoundingBox(UnmanagedDataHandle handle);

    // IMPORTANT: this is the world-space version of PerformPick, which uses a localPoint argument.
    public static extern unsafe UnmanagedDataHandle PerformPick(UnmanagedDataHandle root, Vector3 localPoint,
        bool includeIgnoredElement, UnmanagedHandleBuffer* results);

    // Ideally this struct would replace NativeTransformUtils and be bound properly
    // between native and managed, but it's currently not doable because of the
    // UnmanagedDataStore fields, which contain a MemoryLabel.
    // In the meantime, we keep a static class for the API and an instance class for the data,
    // and we have a managed copy of the instance data as well as a native one.
    // This works because the data only consists of fixed pointers and immutable values.
    private unsafe struct Manager
    {
        readonly UnmanagedDataStore m_Nodes;
        readonly UnmanagedDataStore m_Panels;
        private readonly UnmanagedDataHandle m_DefaultPanelHandle;
        readonly IntPtr m_ContainsPoint;
        readonly TransformData* m_InitialStyleTransform;

        public Manager()
        {
            m_Nodes = LayoutManager.SharedManager.Nodes;
            m_Panels = LayoutManager.SharedManager.Configs;
            m_DefaultPanelHandle = LayoutManager.SharedManager.GetDefaultConfig().Handle;
            m_ContainsPoint = Marshal.GetFunctionPointerForDelegate(k_ContainsPointDelegate);
            m_InitialStyleTransform = InitialStyle.Get().transformData.GetValuePtr();
        }

        private delegate bool ContainsPointDelegate(UnmanagedDataHandle handle, float x, float y);
        private static readonly ContainsPointDelegate k_ContainsPointDelegate = ContainsPoint;

        static readonly ProfilerMarker k_InvokeContainsPointMarker = new("InvokeContainsPoint");

        [AOT.MonoPInvokeCallback(typeof(ContainsPointDelegate))]
        // Passing a Vector2 as an argument gets translated wrongly on the native side.
        static bool ContainsPoint(UnmanagedDataHandle handle, float x, float y)
        {
            using (k_InvokeContainsPointMarker.Auto())
            {
                try
                {
                    var ve = BaseVisualElementPanel.GetPanelElementFromHandle(handle);
                    return ve.ContainsPoint(new Vector2(x, y));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return false;
            }
        }
    }
}
