// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IListItem
    {
        bool selected { get; }
        VisualState visualState { get; }
        VisualElement element { get; }
        void BindVisualState(VisualState newVisualState);
        void RefreshSelection();
    }
}
