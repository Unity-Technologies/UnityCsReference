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

        internal new class UxmlFactory : UxmlFactory<SelectableLabel, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var result = base.Create(bag, cc) as SelectableLabel;
                result.isReadOnly = true;
                return result;
            }
        }
    }
}
