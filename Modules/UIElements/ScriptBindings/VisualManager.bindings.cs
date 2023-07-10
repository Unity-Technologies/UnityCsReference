// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements;

/// <summary>
/// Callback invoked when a hierarchy change occurs.
/// </summary>
delegate void HierarchyChangedDelegate(VisualManager manager, in VisualNodeHandle handle, HierarchyChangeType type);

/// <summary>
/// Callback invoked when a version change occurs.
/// </summary>
delegate void VersionChangedDelegate(VisualManager manager, in VisualNodeHandle handle, VersionChangeType type);

/// <summary>
/// Callback type for <see cref="VisualNode"/> events.
/// </summary>
delegate void VisualNodeDelegate(VisualManager manager, in VisualNodeHandle handle);

/// <summary>
/// Callback type for <see cref="VisualNode"/> events with a child.
/// </summary>
delegate void VisualNodeChildDelegate(VisualManager manager, in VisualNodeHandle handle, in VisualNodeHandle child);

/// <summary>
/// The <see cref="VisualManager"/> represents the the native data storage for visual nodes.
///
/// The native bindings work in a functional manner. All operations are performed on a <see cref="VisualNodeHandle"/>.
/// </summary>
[NativeType(Header = "Modules/UIElements/VisualManager.h")]
sealed class VisualManager : IDisposable
{
    [UsedImplicitly]
    internal static class BindingsMarshaller
    {
        public static IntPtr ConvertToNative(VisualManager store) => store.m_Ptr;
        public static VisualManager ConvertToManaged(IntPtr ptr) => new(ptr, isWrapper: true);
    }

    /// <summary>
    /// The shared singleton instance.
    /// </summary>
    public static VisualManager SharedManager { get; private set; }

    static bool s_Initialized;
    static bool s_AppDomainUnloadRegistered;

    static VisualManager()
    {
        Initialize();
    }

    static void Initialize()
    {
        if (s_Initialized)
            return;

        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodeData>();
        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodePseudoStateData>();
        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodeClassData>();
        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodeRenderData>();
        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodeTextData>();
        VisualNodePropertyRegistry.RegisterInternalProperty<VisualNodeImguiData>();

        s_Initialized = true;

        if (!s_AppDomainUnloadRegistered)
        {
            // important: this will always be called from a special unload thread (main thread will be blocking on this)
            AppDomain.CurrentDomain.DomainUnload += (_, __) =>
            {
                if (s_Initialized)
                    Shutdown();
            };

            s_AppDomainUnloadRegistered = true;
        }

        SharedManager = new VisualManager();
    }

    static void Shutdown()
    {
        if (!s_Initialized)
            return;

        s_Initialized = false;

        SharedManager.Dispose();
    }

    /// <summary>
    /// Weak reference to all <see cref="VisualManager"/> instances.
    /// </summary>
    static readonly List<WeakReference<VisualManager>> s_CallbackInstances = new();

    /// <summary>
    /// Register an instance for static lookup.
    /// </summary>
    /// <param name="instance">The instance to register.</param>
    /// <returns>The handle id.</returns>
    int RegisterCallbackInstance(VisualManager instance)
    {
        for (var i = 0; i < s_CallbackInstances.Count; i++)
        {
            if (!s_CallbackInstances[i].TryGetTarget(out _))
            {
                s_CallbackInstances[i] = new WeakReference<VisualManager>(instance);
                return i + 1;
            }
        }

        s_CallbackInstances.Add(new WeakReference<VisualManager>(this));
        return s_CallbackInstances.Count;
    }

    /// <summary>
    /// Unregister an instance from the static lookup.
    /// </summary>
    /// <param name="id">The handle id.</param>
    void UnregisterCallbackInstance(int id)
    {
        s_CallbackInstances[id - 1] = default;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void NativeHierarchyChangedDelegate(IntPtr instance, in VisualNodeHandle handle, HierarchyChangeType type);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void NativeVersionChangedDelegate(IntPtr instance, in VisualNodeHandle handle, VersionChangeType type);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void NativeVisualNodeDelegate(IntPtr instance, in VisualNodeHandle handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void NativeVisualNodeChildDelegate(IntPtr instance, in VisualNodeHandle handle, in VisualNodeHandle child);

    static readonly NativeHierarchyChangedDelegate s_HierarchyChanged = InvokeHierarchyChanged;
    static readonly NativeVersionChangedDelegate s_VersionChanged = InvokeVersionChanged;
    static readonly NativeVisualNodeDelegate s_Blur = InvokeBlur;
    static readonly NativeVisualNodeChildDelegate s_ChildAdded = InvokeChildAdded;
    static readonly NativeVisualNodeChildDelegate s_ChildRemoved = InvokeChildRemoved;

    static readonly IntPtr s_HierarchyChangedPtr = Marshal.GetFunctionPointerForDelegate(s_HierarchyChanged);
    static readonly IntPtr s_VersionChangedPtr = Marshal.GetFunctionPointerForDelegate(s_VersionChanged);
    static readonly IntPtr s_BlurPtr = Marshal.GetFunctionPointerForDelegate(s_Blur);
    static readonly IntPtr s_ChildAddedPtr = Marshal.GetFunctionPointerForDelegate(s_ChildAdded);
    static readonly IntPtr s_ChildRemovedPtr = Marshal.GetFunctionPointerForDelegate(s_ChildRemoved);

    [AOT.MonoPInvokeCallback(typeof(NativeHierarchyChangedDelegate))]
    static void InvokeHierarchyChanged(IntPtr instance, in VisualNodeHandle handle, HierarchyChangeType type)
    {
        for (var i=0; i<s_CallbackInstances.Count; i++)
            if (s_CallbackInstances[i].TryGetTarget(out var target) && target.m_Ptr == instance)
                target.OnHierarchyChanged.Invoke(target, in handle, type);
    }

    [AOT.MonoPInvokeCallback(typeof(NativeVersionChangedDelegate))]
    static void InvokeVersionChanged(IntPtr instance, in VisualNodeHandle handle, VersionChangeType type)
    {
        for (var i=0; i<s_CallbackInstances.Count; i++)
            if (s_CallbackInstances[i].TryGetTarget(out var target) && target.m_Ptr == instance)
                target.OnVersionChanged?.Invoke(target, in handle, type);
    }

    [AOT.MonoPInvokeCallback(typeof(NativeVisualNodeDelegate))]
    static void InvokeBlur(IntPtr instance, in VisualNodeHandle handle)
    {
        for (var i = 0; i < s_CallbackInstances.Count; i++)
            if (s_CallbackInstances[i].TryGetTarget(out var target) && target.m_Ptr == instance)
                target.OnBlur?.Invoke(target, in handle);
    }

    [AOT.MonoPInvokeCallback(typeof(NativeVisualNodeChildDelegate))]
    static void InvokeChildAdded(IntPtr instance, in VisualNodeHandle handle, in VisualNodeHandle child)
    {
        for (var i = 0; i < s_CallbackInstances.Count; i++)
            if (s_CallbackInstances[i].TryGetTarget(out var target) && target.m_Ptr == instance)
                target.OnChildAdded?.Invoke(target, in handle, in child);
    }

    [AOT.MonoPInvokeCallback(typeof(NativeVisualNodeChildDelegate))]
    static void InvokeChildRemoved(IntPtr instance, in VisualNodeHandle handle, in VisualNodeHandle child)
    {
        for (var i = 0; i < s_CallbackInstances.Count; i++)
            if (s_CallbackInstances[i].TryGetTarget(out var target) && target.m_Ptr == instance)
                target.OnChildRemoved?.Invoke(target, in handle, in child);
    }

    /// <summary>
    /// Handle to the native allocated manager.
    /// </summary>
    [RequiredByNativeCode] IntPtr m_Ptr;

    /// <summary>
    /// Flag indicating if this instance owns the managed memory.
    /// </summary>
    [RequiredByNativeCode] bool m_IsWrapper;

    /// <summary>
    /// The unique instance id for this store.
    /// </summary>
    readonly int m_InstanceId;

    /// <summary>
    /// Reference to the native property storage. This is used for fast access without bindings.
    /// </summary>
    readonly VisualNodePropertyRegistry m_Registry;

    /// <summary>
    /// Weak reference to all visual element instances owned by this manager.
    /// </summary>
    readonly ChunkAllocatingArray<WeakReference<VisualElement>> m_Elements = new();

    /// <summary>
    /// Weak reference to all panels instances owned by this manager.
    /// </summary>
    readonly ChunkAllocatingArray<WeakReference<BaseVisualElementPanel>> m_Panels = new();

    /// <summary>
    /// Returns true if the native memory for this object is allocated.
    /// </summary>
    public bool IsCreated => m_Ptr != IntPtr.Zero;

    /// <summary>
    /// Lock object used when accessing the <see cref="m_NodesToRemove"/> stack.
    /// </summary>
    readonly object m_NodeLock = new();

    /// <summary>
    /// The set of handles to free.
    /// </summary>
    readonly Stack<VisualNodeHandle> m_NodesToRemove = new();

    /// <summary>
    /// Lock object used when accessing the <see cref="m_NodesToRemove"/> stack.
    /// </summary>
    readonly object m_PanelLock = new();

    /// <summary>
    /// The set of handles to free.
    /// </summary>
    readonly Stack<VisualPanelHandle> m_PanelsToRemove = new();

    /// <summary>
    /// The reference to the native <see cref="ClassNameStore"/>.
    /// </summary>
    internal VisualNodeClassNameStore ClassNameStore { get; }

    /// <summary>
    /// The root node.
    /// </summary>
    [NativeProperty("Root", TargetType.Field)]
    public extern VisualNodeHandle Root { get; }

    /// <summary>
    /// Callback invoked when a hierarchy change occurs.
    /// </summary>
    public event HierarchyChangedDelegate OnHierarchyChanged;

    /// <summary>
    /// Callback invoked when a version change occurs.
    /// </summary>
    public event VersionChangedDelegate OnVersionChanged;

    /// <summary>
    /// Callback invoked when native is requesting an element be blurred.
    /// </summary>
    public event VisualNodeDelegate OnBlur;

    /// <summary>
    /// Callback invoked when a child is added to a node.
    /// </summary>
    public event VisualNodeChildDelegate OnChildAdded;

    /// <summary>
    /// Callback invoked when a child is added to a node.
    /// </summary>
    public event VisualNodeChildDelegate OnChildRemoved;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualManager"/> class.
    /// </summary>
    public VisualManager() : this(Internal_Create(), false)
    {
    }

    VisualManager(IntPtr ptr, bool isWrapper)
    {
        m_InstanceId = RegisterCallbackInstance(this);

        m_Ptr = ptr;
        m_IsWrapper = isWrapper;

        // Gather managed bindings.
        ClassNameStore = GetClassNameStore();

        // Register for native callbacks.
        SetHierarchyChangedCallback(s_HierarchyChangedPtr);
        SetVersionChangedCallback(s_VersionChangedPtr);
        SetBlurCallback(s_BlurPtr);
        SetChildAddedCallback(s_ChildAddedPtr);
        SetChildRemovedCallback(s_ChildRemovedPtr);

        m_Registry = new VisualNodePropertyRegistry(this);
    }

    ~VisualManager()
    {
        UnregisterCallbackInstance(m_InstanceId);
        Dispose(false);
    }

    /// <summary>
    /// Dispose this object, releasing its memory.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (m_Ptr != IntPtr.Zero)
        {
            if (!m_IsWrapper)
                Internal_Destroy(m_Ptr);

            m_Ptr = IntPtr.Zero;
        }
    }

    public VisualPanel CreatePanel()
    {
        TryFreePanels();
        var handle = AddPanel();
        return new VisualPanel(this, handle);
    }

    public void DestroyPanelThreaded(ref VisualPanel panel)
    {
        if (panel.Handle == VisualPanelHandle.Null)
            return;

        lock (m_PanelLock)
            m_PanelsToRemove.Push(panel.Handle);

        panel = VisualPanel.Null;
    }

    public VisualNode CreateNode()
    {
        TryFreeNodes();
        var handle = AddNode();
        return new VisualNode(this, handle);
    }

    public void DestroyNodeThreaded(ref VisualNode node)
    {
        if (node.Handle == VisualNodeHandle.Null)
            return;

        lock (m_NodeLock)
            m_NodesToRemove.Push(node.Handle);

        node = VisualNode.Null;
    }

    void TryFreePanels()
    {
        var lockTaken = false;

        try
        {
            Monitor.TryEnter(m_PanelLock, ref lockTaken);

            if (lockTaken)
            {
                while (m_PanelsToRemove.Count > 0)
                {
                    RemovePanel(m_PanelsToRemove.Pop());
                }
            }
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(m_PanelLock);
        }
    }

    void TryFreeNodes()
    {
        var lockTaken = false;

        try
        {
            Monitor.TryEnter(m_NodeLock, ref lockTaken);

            if (lockTaken)
            {
                while (m_NodesToRemove.Count > 0)
                {
                    RemoveNode(m_NodesToRemove.Pop());
                }
            }
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(m_NodeLock);
        }
    }

    /// <summary>
    /// Sets the managed binding for this handle.
    /// </summary>
    /// <param name="handle">The handle to bind to.</param>
    /// <param name="element">The managed instance to bind.</param>
    public void SetOwner(in VisualNodeHandle handle, VisualElement element)
    {
        m_Elements[handle.Id] = null != element ? new WeakReference<VisualElement>(element) : default;
    }

    /// <summary>
    /// Gets the managed binding for this handle.
    /// </summary>
    /// <param name="handle">The handle to get the binding for.</param>
    /// <returns>The managed binding.</returns>
    public VisualElement GetOwner(in VisualNodeHandle handle)
    {
        m_Elements[handle.Id].TryGetTarget(out var element);
        return element;
    }

    /// <summary>
    /// Sets the managed binding for this handle.
    /// </summary>
    /// <param name="handle">The handle to bind to.</param>
    /// <param name="panel">The managed instance to bind.</param>
    public void SetOwner(in VisualPanelHandle handle, BaseVisualElementPanel panel)
    {
        m_Panels[handle.Id] = null != panel ? new WeakReference<BaseVisualElementPanel>(panel) : default;
    }

    /// <summary>
    /// Gets the managed binding for this handle.
    /// </summary>
    /// <param name="handle">The handle to get the binding for.</param>
    /// <returns>The managed binding.</returns>
    public BaseVisualElementPanel GetOwner(in VisualPanelHandle handle)
    {
        m_Panels[handle.Id].TryGetTarget(out var panel);
        return panel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref T GetProperty<T>(VisualNodeHandle handle) where T : unmanaged
        => ref m_Registry.GetPropertyRef<T>(handle);

    [FreeFunction("VisualManager::Create")]
    static extern IntPtr Internal_Create();

    [FreeFunction("VisualManager::Destroy")]
    static extern void Internal_Destroy(IntPtr ptr);

    [NativeThrows]
    extern void SetHierarchyChangedCallback(IntPtr callback);

    [NativeThrows]
    extern void SetVersionChangedCallback(IntPtr callback);

    [NativeThrows]
    extern void SetBlurCallback(IntPtr callback);

    [NativeThrows]
    extern void SetChildAddedCallback(IntPtr callback);

    [NativeThrows]
    extern void SetChildRemovedCallback(IntPtr callback);

    [NativeThrows]
    internal extern IntPtr GetPropertyPtr(int index);

    internal extern int PanelCount();

    [NativeThrows]
    internal extern VisualPanelHandle AddPanel();

    [NativeThrows]
    internal extern bool RemovePanel(in VisualPanelHandle handle);

    [NativeThrows]
    internal extern bool ContainsPanel(in VisualPanelHandle handle);

    [NativeThrows]
    internal extern void ClearPanels();

    [NativeThrows]
    internal extern unsafe void* GetPanelDataPtr(in VisualPanelHandle handle);

    [NativeThrows]
    internal extern VisualNodeHandle GetRootContainer(in VisualPanelHandle handle);

    [NativeThrows]
    internal extern bool SetRootContainer(in VisualPanelHandle handle, in VisualNodeHandle container);

    internal extern int NodeCount();

    [NativeThrows]
    internal extern VisualNodeHandle AddNode();

    [NativeThrows]
    internal extern bool RemoveNode(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern bool ContainsNode(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void ClearNodes();

    [NativeThrows]
    internal extern void SetName(in VisualNodeHandle handle, string name);

    [NativeThrows]
    internal extern string GetName(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern int GetChildrenCount(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern IntPtr GetChildrenPtr(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern int IndexOfChild(in VisualNodeHandle handle, in VisualNodeHandle child);

    [NativeThrows]
    internal extern bool AddChild(in VisualNodeHandle handle, in VisualNodeHandle child);

    [NativeThrows]
    internal extern bool RemoveChild(in VisualNodeHandle handle, in VisualNodeHandle child);

    [NativeThrows]
    internal extern bool InsertChildAtIndex(in VisualNodeHandle handle, int index, in VisualNodeHandle child);

    [NativeThrows]
    internal extern bool RemoveChildAtIndex(in VisualNodeHandle handle, int index);

    [NativeThrows]
    internal extern bool ClearChildren(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern VisualNodeHandle GetParent(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern bool RemoveFromParent(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern bool AddToClassList(in VisualNodeHandle handle, string className);

    [NativeThrows]
    internal extern bool RemoveFromClassList(in VisualNodeHandle handle, string className);

    [NativeThrows]
    internal extern bool ClassListContains(in VisualNodeHandle handle, string className);

    [NativeThrows]
    internal extern bool ClearClassList(in VisualNodeHandle handle);

    [NativeThrows]
    extern VisualNodeClassNameStore GetClassNameStore();

    [NativeThrows]
    internal extern bool IsEnabled(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void SetEnabled(in VisualNodeHandle handle, bool enabled);

    [NativeThrows]
    internal extern bool IsEnabledInHierarchy(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern VisualPanelHandle GetPanel(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void SetPanel(in VisualNodeHandle handle, in VisualPanelHandle panel);

    [NativeThrows]
    [return: Unmarshalled]
    internal extern PseudoStates GetPseudoStates(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void SetPseudoStates(in VisualNodeHandle handle, PseudoStates states);

    [NativeThrows]
    [return: Unmarshalled]
    internal extern RenderHints GetRenderHints(in VisualNodeHandle handles);

    [NativeThrows]
    internal extern void SetRenderHints(in VisualNodeHandle handle, RenderHints hints);

    [NativeThrows]
    [return: Unmarshalled]
    internal extern LanguageDirection GetLanguageDirection(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void SetLanguageDirection(in VisualNodeHandle handle, LanguageDirection direction);

    [NativeThrows]
    [return: Unmarshalled]
    internal extern LanguageDirection GetLocalLanguageDirection(in VisualNodeHandle handle);

    [NativeThrows]
    internal extern void SetLocalLanguageDirection(in VisualNodeHandle handle, LanguageDirection direction);
}
