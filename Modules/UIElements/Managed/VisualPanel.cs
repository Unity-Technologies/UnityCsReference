// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements;

readonly struct VisualPanel
{
    public static VisualPanel Null => new(default, VisualPanelHandle.Null);

    /// <summary>
    /// The manager storing the actual node data.
    /// </summary>
    readonly VisualManager m_Manager;

    /// <summary>
    /// The handle to the underlying data.
    /// </summary>
    readonly VisualPanelHandle m_Handle;

    /// <summary>
    /// Gets the handle for this panel.
    /// </summary>
    public VisualPanelHandle Handle => m_Handle;

    /// <summary>
    /// Returns <see langword="true"/> if the underlying memory is allocated for this panel.
    /// </summary>
    public bool IsCreated => !m_Handle.Equals(VisualPanelHandle.Null) && m_Manager.ContainsPanel(m_Handle);

    /// <summary>
    /// Reference access to the internal data struct.
    /// </summary>
    internal unsafe ref VisualPanelData Data => ref UnsafeUtility.AsRef<VisualPanelData>(m_Manager.GetPanelDataPtr(in m_Handle));

    /// <summary>
    /// The node for this panel.
    /// </summary>
    public ref VisualNodeHandle RootContainer => ref Data.RootContainer;

    /// <summary>
    /// Flag indicating if the layout pass is taking place.
    /// </summary>
    public ref bool DuringLayoutPhase => ref Data.DuringLayoutPhase;

    /// <summary>
    /// Gets the root container for this panel.
    /// </summary>
    /// <returns></returns>
    public VisualNode GetRootContainer() => new(m_Manager, Data.RootContainer);

    /// <summary>
    /// Gets the root container for this panel.
    /// </summary>
    public void SetRootContainer(VisualNode node) => Data.RootContainer = node.Handle;

    /// <summary>
    /// Initializes a new <see cref="VisualPanel"/> object.
    /// </summary>
    /// <param name="manager">The manager storing the node data.</param>
    /// <param name="handle">The handle to the panel data.</param>
    internal VisualPanel(VisualManager manager, VisualPanelHandle handle)
    {
        m_Manager = manager;
        m_Handle = handle;
    }

    /// <summary>
    /// Destroys the panel instance and releases the underlying resources.
    /// </summary>
    public void Destroy()
    {
        m_Manager.RemovePanel(m_Handle);
    }

    /// <summary>
    /// Gets the managed owner for this panel.
    /// </summary>
    public BaseVisualElementPanel GetOwner() => m_Manager.GetOwner(in m_Handle);

    /// <summary>
    /// Sets the managed owner for this panel.
    /// </summary>
    /// <param name="owner">The managed owner to set.</param>
    public void SetOwner(BaseVisualElementPanel owner) => m_Manager.SetOwner(in m_Handle, owner);
}
