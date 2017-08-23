// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    class OutputNodeAnchorPresenter : NodeAnchorPresenter
    {
        public override Direction direction
        {
            get { return Direction.Output; }
        }
    }
}
