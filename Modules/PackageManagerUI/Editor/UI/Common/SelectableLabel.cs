// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SelectableLabel : TextField
    {
        internal new class UxmlFactory : UxmlFactory<SelectableLabel, UxmlTraits> {}

        public SelectableLabel()
        {
            isReadOnly = true;
        }
    }
}
