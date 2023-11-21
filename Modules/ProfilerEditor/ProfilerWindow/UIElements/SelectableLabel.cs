// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class SelectableLabel : TextField
    {
        public SelectableLabel()
        {
            isReadOnly = true;
        }

        public new class UxmlSerializedData : TextField.UxmlSerializedData
        {
            public override object CreateInstance() => new SelectableLabel();
        }
    }
}
