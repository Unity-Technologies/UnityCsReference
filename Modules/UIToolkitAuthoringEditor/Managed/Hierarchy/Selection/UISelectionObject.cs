// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[HelpURL("ui-systems/in-scene-ui-authoring")]
internal abstract class UISelectionObject : ScriptableObject, INotifyBindablePropertyChanged
{
    public static readonly BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

    private bool m_IsReadOnly;

    /// <summary>
    /// The context that created this proxy, for proxies whose target does not live in a panel.
    /// </summary>
    /// <remarks>Use <see cref="AuthoringContext.For(UISelectionObject)"/> to resolve the context.</remarks>
    internal AuthoringContext OwnerContext { get; set; }

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            Notify(IsReadOnlyProperty);
        }
    }

    protected void Notify(BindingId property)
        => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
}
