// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// What a <see cref="UIViewportWindow"/> shows: a document, the panel it is cloned into, and the operations
/// the viewport performs on it.
/// </summary>
/// <remarks>
/// The viewport renders a <see cref="PanelElement"/>, and the UI Stage is the only place one already exists;
/// every other source injects one for as long as the context is acquired. A context is a passive view over an
/// already-resolved document, replaced rather than mutated when its source resolves to something else.
/// </remarks>
interface IUIViewportContext
{
    /// <summary>
    /// What the context was resolved from. Two contexts with the same source and document are interchangeable,
    /// which is what lets a re-resolve keep the live panel instead of rebuilding it.
    /// </summary>
    object Source { get; }

    /// <summary>The panel the canvas renders; only valid between <see cref="Acquire"/> and <see cref="Release"/>.</summary>
    PanelElement PanelElement { get; }

    /// <summary>The document the panel shows. The theme override is stored per root document.</summary>
    VisualTreeAsset RootVisualTreeAsset { get; }

    /// <summary>The document drops are authored into; the root one unless a sub-document is being edited.</summary>
    VisualTreeAsset EditedVisualTreeAsset { get; }

    PanelSettings PanelSettings { get; }

    string HeaderTitle { get; }

    /// <summary>Derived from the document, so a document keeps its framing wherever it is opened from.</summary>
    string CanvasStorageKey { get; }

    /// <summary>Whether what the context was resolved from still exists; a stale one empties the window.</summary>
    bool IsValid { get; }

    /// <summary>
    /// Whether the viewport may author into the document. Which setting governs it differs per source, and the
    /// viewport must not accept a drop the Hierarchy showing the same document would refuse.
    /// </summary>
    bool AllowsAuthoring { get; }

    /// <summary>Creates the panel the context owns, if it owns one.</summary>
    void Acquire();

    void Release();

    /// <summary>Re-clones the document into the panel, for the changes only a re-clone can show.</summary>
    void RequestRefresh();

    /// <summary>
    /// Whether instantiating <paramref name="visualTreeAsset"/> here would make a document (transitively)
    /// instantiate itself. On the context because only it knows which documents the preview nests.
    /// </summary>
    bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset);

    void PopulateBreadcrumbs(UIViewport viewport);
}
