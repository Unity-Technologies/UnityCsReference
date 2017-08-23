// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    interface ISelection
    {
        List<ISelectable> selection { get; }

        void AddToSelection(ISelectable selectable);
        void RemoveFromSelection(ISelectable selectable);
        void ClearSelection();
    }
}
