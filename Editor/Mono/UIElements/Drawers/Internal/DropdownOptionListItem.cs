// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Diagnostics;

namespace UnityEditor.UIElements
{
    [UxmlElement]
    internal partial class DropdownOptionListItem : BindableElement
    {
        ObjectField imageProperty { get; set; }
        TextField textProperty { get; set; }

        public DropdownOptionListItem(string textPath, string imagePath)
        {
            SetItem(textPath, imagePath);
        }

        public DropdownOptionListItem()
        {
            SetItem("", "");
        }

        void SetItem(string textPath, string imagePath)
        {
            textProperty = new TextField()
            {
                label = "",
                bindingPath = textPath
            };
            Add(textProperty);

            imageProperty = new ObjectField()
            {
                label = "",
                bindingPath = imagePath,
                objectType = typeof(Sprite)
            };
            Add(imageProperty);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
