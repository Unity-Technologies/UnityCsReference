// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal abstract class UISelectionObject : ScriptableObject, INotifyBindablePropertyChanged
{
    public static readonly BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

    private bool m_IsReadOnly;

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
