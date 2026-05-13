// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    [UxmlElement]
    internal partial class SelectableLabel : TextField
    {
        public SelectableLabel()
        {
            isReadOnly = true;
        }
    }
}
