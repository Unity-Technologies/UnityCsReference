// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomEditor(typeof(FilterFunctionDefinition))]
    internal class FilterFunctionDefinitionInspector : Editor
    {
        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}
