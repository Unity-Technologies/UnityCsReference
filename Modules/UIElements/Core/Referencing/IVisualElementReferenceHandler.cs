// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements;

/// <summary>
/// Interface for a reference to a VisualElement in a runtime document.
/// You can implement this interface to create custom reference types or use the built-in <see cref="VisualElementReference{T}"/>.
/// </summary>
interface IVisualElementReferenceHandler
{
    /// <summary>
    /// Tears down the UI document and clears its references.
    /// </summary>
    void ClearReferences();

    /// <summary>
    /// Sets up the reference table to resolve references.
    /// </summary>
    /// <param name="table">The table that can be used to resolve references.</param>
    void ResolveReferences(VisualElementAssetReferenceTable table);
}
