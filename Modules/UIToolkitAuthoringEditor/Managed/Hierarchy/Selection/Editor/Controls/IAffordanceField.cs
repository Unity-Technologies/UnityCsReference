// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal interface IAffordanceField
{
    /// <summary>
    /// The affordance element.
    /// </summary>
    public FieldAffordanceElement affordanceElement { get; set; }

    /// <summary>
    /// The value field's input element, where binding-state tint classes (e.g. animation-driven) are applied.
    /// </summary>
    public VisualElement valueInputElement { get; }
}
