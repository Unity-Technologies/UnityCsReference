// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SelectableLabel : Label
    {
        [Serializable]
        internal new class UxmlSerializedData : Label.UxmlSerializedData
        {
            public override object CreateInstance() => new SelectableLabel();
        }

        public SelectableLabel()
        {
            SetAsSelectableAndElided();
        }

        public void SetAsSelectableAndElided()
        {
            selection.isSelectable = true;
            focusable = true;
            displayTooltipWhenElided = true;
        }

        public void SetValueWithoutNotify(string value)
        {
            text = value;
        }

        public bool multiline { get; set; }

        public string value
        {
            get => text;
            set => text = value;
        }
    }
}
