// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    interface IDetailsElementBinder
    {
        void BindDetailsElement(VisualElement detailsElement, IDetailsProvider detailsProvider);
        void UnbindDetailsElement(VisualElement detailsElement);
    }
}
