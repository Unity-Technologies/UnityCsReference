// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

/// <summary>
/// Represents a reference to a VisualElement in a <see cref="PanelRenderer"/>.
/// </summary>
/// <example>
/// This example shows how to use VisualElementReference to reference an element in a UXML file that is loaded by a `PanelRenderer`.
/// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementReference_Example.cs"/>
/// </example>
/// <example>
/// The following example configures paths to reference elements in a nested UXML structure.
/// Root UXML:
/// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementReference_ExampleElement.uxml"/>
/// </example>
/// <example>
/// Template UXML:
/// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementReference_ExampleTemplate.uxml"/>
/// </example>
/// <example>
/// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/VisualElementReference_ExampleNested.cs"/>
/// </example>
[Serializable]
public class VisualElementReference : IVisualElementReferenceHandler, IEquatable<VisualElementReference>, IDisposable
{
    [SerializeField] PanelRenderer m_PanelRenderer;
    [SerializeField] AuthoringIdPath m_AuthoringPath;

    internal VisualElementAssetReferenceTable m_CurrentTable;
    Action<VisualElement> m_ReferenceResolved;
    Action<VisualElement> m_ReferenceUnloaded;

    VisualElement m_LoadedValue;
    bool m_Registered;

    /// <summary>
    /// The provider used to resolve the reference.
    /// <seealso cref="SetReference(PanelRenderer, AuthoringIdPath)"/>
    /// </summary>
    public PanelRenderer panelRenderer => m_PanelRenderer;

    /// <summary>
    /// The path to the referenced element.
    /// <seealso cref="SetReference(PanelRenderer, AuthoringIdPath)"/>
    /// </summary>
    public AuthoringIdPath authoringPath => m_AuthoringPath;

    internal virtual bool hasSubscribers => m_ReferenceResolved != null || m_ReferenceUnloaded != null;

    /// <summary>
    /// Creates a new empty VisualElementReference.
    /// </summary>
    public VisualElementReference()
    {
    }

    /// <summary>
    /// Creates a new VisualElementReference pointing to the given renderer and path.
    /// </summary>
    /// <param name="renderer">The PanelRenderer that contains the VisualTreeAsset.</param>
    /// <param name="path">The path of the element to reference.</param>
    public VisualElementReference(PanelRenderer renderer, AuthoringIdPath path)
    {
        SetReference(renderer, path);
    }

    ~VisualElementReference() => Dispose(false);

    /// <summary>
    /// Sets the reference to point to the given document and path.
    /// </summary>
    /// <param name="renderer">The <see cref="PanelRenderer"/> that contains the <see cref="VisualTreeAsset"/> with the element we want to reference.</param>
    /// <param name="path">The path of the element to reference.</param>
    public void SetReference(PanelRenderer renderer, AuthoringIdPath path)
    {
        if (m_PanelRenderer == renderer && m_AuthoringPath.Equals(path))
            return;

        m_AuthoringPath = path;

        OnReferenceUnloaded();

        if (m_PanelRenderer != renderer)
        {
            bool shouldRegister = m_Registered;
            UnregisterFromTableProvider();
            m_PanelRenderer = renderer;
            m_CurrentTable = null;
            if (shouldRegister)
                RegisterToTableProvider();
        }

        if (m_LoadedValue == null)
            TryResolveReference();
    }

    /// <summary>
    /// Registers a callback for when the reference is resolved from the document.
    /// When you register a callback, if the reference is already resolved, the callback is immediately invoked.
    /// </summary>
    /// <param name="callback">The callback to invoke when the reference is resolved.</param>
    public void RegisterReferenceResolvedCallback(Action<VisualElement> callback)
    {
        RegisterCallback(callback, ref m_ReferenceResolved, m_LoadedValue);
    }

    /// <summary>
    /// Unregisters a callback for when the reference is resolved from the document.
    /// </summary>
    /// <param name="callback">The callback to unregister.</param>
    public void UnregisterReferenceResolvedCallback(Action<VisualElement> callback)
    {
        UnregisterCallback(callback, ref m_ReferenceResolved);
    }

    /// <summary>
    /// Registers a callback for when the referenced object is unloaded. This occurs when the document is destroyed,
    /// such as when a live reload occurs after the VisualTreeAsset changes.
    /// At this point, all references are invalid and should be cleared.
    /// </summary>
    /// <param name="callback">The callback to invoke when the reference is unloaded.</param>
    public void RegisterReferenceUnloadedCallback(Action<VisualElement> callback)
    {
        RegisterCallback(callback, ref m_ReferenceUnloaded, null);
    }

    /// <summary>
    /// Unregisters a callback for when the referenced object is unloaded.
    /// </summary>
    /// <param name="callback">The callback to unregister.</param>
    public void UnregisterReferenceUnloadedCallback(Action<VisualElement> callback)
    {
        UnregisterCallback(callback, ref m_ReferenceUnloaded);
    }

    internal void RegisterCallback<T>(Action<T> callback, ref Action<T> callbackDelegate, T immediateValue)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callbackDelegate += callback;

        // If we have already resolved a value we dont want to miss the callback.
        if (immediateValue != null)
            callback(immediateValue);

        RegisterToTableProvider();
    }

    internal void UnregisterCallback<T>(Action<T> callback, ref Action<T> callbackDelegate)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callbackDelegate -= callback;

        if (!hasSubscribers)
            UnregisterFromTableProvider();
    }

    internal virtual void TryResolveReference()
    {
        if (m_CurrentTable?.TryGetReference(m_AuthoringPath, out VisualElement element) == true)
        {
            OnReferenceLoaded(element);
        }
    }

    internal virtual void OnReferenceLoaded(VisualElement element)
    {
        m_LoadedValue = element;
        m_ReferenceResolved?.Invoke(m_LoadedValue);
    }

    internal virtual void OnReferenceUnloaded()
    {
        m_ReferenceUnloaded?.Invoke(m_LoadedValue);
        m_LoadedValue = null;
    }

    /// <summary>
    /// Registers the reference to the table provider to resolve references.
    /// </summary>
    internal void RegisterToTableProvider()
    {
        if (!m_Registered)
        {
            m_PanelRenderer?.referenceProvider?.Add(this);

            // We want the m_Registered flag to be set so that when we update the provider later we register correctly.
            m_Registered = true;
        }
    }

    /// <summary>
    /// Unregisters the reference from the table provider.
    /// </summary>
    internal void UnregisterFromTableProvider()
    {
        if (m_Registered)
        {
            m_Registered = false;
            m_PanelRenderer?.referenceProvider?.Remove(this);
        }
    }

    /// <summary>
    /// Called when the reference table is setup and ready to resolve references.
    /// </summary>
    /// <param name="table">The table that can be used to find the referenced elements.</param>
    void IVisualElementReferenceHandler.ResolveReferences(VisualElementAssetReferenceTable table)
    {
        m_CurrentTable = table;
        TryResolveReference();
    }

    /// <summary>
    /// Called when the document is torn down and references should be cleared.
    /// </summary>
    void IVisualElementReferenceHandler.ClearReferences()
    {
        OnReferenceUnloaded();
        m_CurrentTable = null;
    }

    /// <summary>
    /// Disposes the reference and unregisters it from the table provider.
    /// This instance will not cause a memory leak, but calling Dispose will remove the weak reference from the list,
    /// which can improve performance. If Dispose is not called explicitly, the finalizer will trigger this cleanup.
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterFromTableProvider();
            OnReferenceUnloaded();
        }
    }

    /// <summary>
    /// Indicates whether the current object is equal to another element reference.
    /// </summary>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> has the same <see cref="PanelRenderer"/> and matching <see cref="AuthoringIdPath"/>.</returns>
    public bool Equals(VisualElementReference other)
    {
        if (ReferenceEquals(other, null))
            return false;
        return m_PanelRenderer == other.m_PanelRenderer && m_AuthoringPath.Equals(other.m_AuthoringPath);
    }
}

/// <summary>
/// Represents a strongly-typed reference to a VisualElement in a <see cref="PanelRenderer"/>.
/// </summary>
/// <typeparam name="T">The VisualElement type.</typeparam>
[Serializable]
public class VisualElementReference<T> : VisualElementReference where T : VisualElement
{
    Action<T> m_ReferenceResolvedTyped;
    Action<T> m_ReferenceUnloadedTyped;
    T m_LoadedValueTyped;

    internal override bool hasSubscribers => base.hasSubscribers || m_ReferenceResolvedTyped != null || m_ReferenceUnloadedTyped != null;

    /// <summary>
    /// Creates a new empty VisualElementReference.
    /// </summary>
    public VisualElementReference()
    {
    }

    /// <summary>
    /// Creates a new VisualElementReference pointing to the given renderer and path.
    /// </summary>
    /// <param name="renderer">The PanelRenderer that contains the VisualTreeAsset.</param>
    /// <param name="path">The path of the element to reference.</param>
    public VisualElementReference(PanelRenderer renderer, AuthoringIdPath path) : base(renderer, path)
    {
    }

    /// <summary>
    /// Registers a callback for when the reference is resolved from the document.
    /// When you register a callback, if the reference is already resolved, the callback is immediately invoked.
    /// </summary>
    /// <param name="callback">The callback to invoke when the reference is resolved.</param>
    public void RegisterReferenceResolvedCallback(Action<T> callback)
    {
        RegisterCallback(callback, ref m_ReferenceResolvedTyped, m_LoadedValueTyped);
    }

    /// <summary>
    /// Unregisters a callback for when the reference is resolved from the document.
    /// </summary>
    /// <param name="callback">The callback to unregister.</param>
    public void UnregisterReferenceResolvedCallback(Action<T> callback)
    {
        UnregisterCallback(callback, ref m_ReferenceResolvedTyped);
    }

    /// <summary>
    /// Registers a callback for when the referenced object is unloaded. This occurs when the document is destroyed,
    /// such as when a live reload occurs after the VisualTreeAsset changes.
    /// At this point, all references are invalid and should be cleared.
    /// </summary>
    /// <param name="callback">The callback to invoke when the reference is unloaded.</param>
    public void RegisterReferenceUnloadedCallback(Action<T> callback)
    {
        RegisterCallback(callback, ref m_ReferenceUnloadedTyped, default);
    }

    /// <summary>
    /// Unregisters a callback for when the referenced object is unloaded.
    /// </summary>
    /// <param name="callback">The callback to unregister.</param>
    public void UnregisterReferenceUnloadedCallback(Action<T> callback)
    {
        UnregisterCallback(callback, ref m_ReferenceUnloadedTyped);
    }

    internal override void OnReferenceLoaded(VisualElement element)
    {
        m_LoadedValueTyped = (T)element;
        m_ReferenceResolvedTyped?.Invoke(m_LoadedValueTyped);
        
        // Also invoke base class callbacks
        base.OnReferenceLoaded(element);
    }

    internal override void OnReferenceUnloaded()
    {
        m_ReferenceUnloadedTyped?.Invoke(m_LoadedValueTyped);
        m_LoadedValueTyped = null;
        
        // Also invoke base class callbacks
        base.OnReferenceUnloaded();
    }
}
