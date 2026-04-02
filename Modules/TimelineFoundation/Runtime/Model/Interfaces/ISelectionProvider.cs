// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    /// <summary>
    /// Provides access to the Sequence selection when selection changes.
    /// </summary>
    interface ISelectionProvider
    {
        SequenceSelection selection { get; }
        event Action<SequenceSelection> selectionChanged;
        void SetSelection(IEnumerable<UniqueID> toSelect);

        void Select(IEnumerable<UniqueID> toSelect);
        void Deselect(IEnumerable<UniqueID> toSelect);

        void ToggleSelection(IEnumerable<UniqueID> toSelect);
    }
}
