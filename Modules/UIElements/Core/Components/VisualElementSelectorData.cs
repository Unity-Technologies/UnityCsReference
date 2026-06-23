// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

/// <summary>
/// Unmanaged component containing all data needed for CSS selector matching.
/// Must remain in sync with C++ definition in VisualElementSelectorData.h
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VisualElementSelectorData
{
    // Element identity for selector matching
    public int typeNameId;           // UniqueStyleString.id for element type (e.g., "Button", "Label")
    public int nameId;               // UniqueStyleString.id of the element's name
    public PseudoStates pseudoStates; // Current pseudo-state flags (:hover, :active, etc.)

    // Direct pointer to the logical parent's component. Distinct from the main LayoutData
    // component's parent reference, which tracks the physical (visual tree) parent.
    public VisualElementSelectorData* logicalParent; // null at the root of the logical hierarchy

    // CSS class list (pointer to shared immutable data)
    public int* classIds;            // Pointer to sorted int[] of UniqueStyleString.ids
    public int classCount;           // Number of classes

    // Pseudo-state dependency tracking (for style recalculation optimization)
    public PseudoStates triggerPseudoMask;    // States that would trigger match if set
    public PseudoStates dependencyPseudoMask; // States that contributed to match

    public static readonly VisualElementSelectorData Default = new VisualElementSelectorData
    {
        typeNameId = -1,
        // Matches VisualElement initial name value and behaviour when unset.
        nameId = UniqueStyleString.Empty.id,
        pseudoStates = PseudoStates.None,
        logicalParent = null,
        classIds = null,
        classCount = 0,
        triggerPseudoMask = PseudoStates.None,
        dependencyPseudoMask = PseudoStates.None
    };
}
