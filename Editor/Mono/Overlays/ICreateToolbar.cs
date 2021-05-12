// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public interface ICreateHorizontalToolbar
    {
        VisualElement CreateHorizontalToolbarContent();
    }

    public interface ICreateVerticalToolbar
    {
        VisualElement CreateVerticalToolbarContent();
    }
}
