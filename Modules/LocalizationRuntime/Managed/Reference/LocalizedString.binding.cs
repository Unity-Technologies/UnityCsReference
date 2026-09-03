// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Localization;

[UxmlObject]
public partial class LocalizedString
{
    string m_BoundValue;

    [UxmlObjectReference("variables")]
    internal List<NamedVariable> Variables
    {
        get => LocalVariables.Variables;
        set => LocalVariables.Variables = value;
    }

    private protected override void StartBinding()
    {
        ApplyEditorPreviewLocale();
        // Subscribing resolves and delivers, so this also primes the first value.
        StringChanged += OnBoundValueChanged;
    }

    private protected override void StopBinding()
    {
        StringChanged -= OnBoundValueChanged;
        m_BoundValue = null;
    }

    private protected override BindingResult WriteBoundValue(in BindingContext context)
        => WriteToTarget(context, m_BoundValue);

    void OnBoundValueChanged(string value)
    {
        m_BoundValue = value;
        MarkDirty();
    }
}
