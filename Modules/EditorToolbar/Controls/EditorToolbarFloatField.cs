// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarFloatField : FloatField
    {
        const string k_FloatFieldUssClass = "unity-toolbar-float-field";

        public EditorToolbarFloatField() : base()
        {
            AddToClassList(k_FloatFieldUssClass);
        }
    }
}
