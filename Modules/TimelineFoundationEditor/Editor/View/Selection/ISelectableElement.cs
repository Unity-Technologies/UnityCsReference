// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// Visual elements that participate in selection must inherit from ISelectableElement.
    /// </summary>
    interface ISelectableElement : IUniqueElement
    {
        bool selected { get; }
        bool supportsMultiSelect { get; }
        void OnSelectionStateChanged(bool isSelected);
    }
}
