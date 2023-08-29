// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Licensing.UI.Events.Buttons
{
interface IEventsButtonFactory
{
    public VisualElement Create(EventsButtonType buttonType, Action additionalCustomClickAction = null);
}
}
