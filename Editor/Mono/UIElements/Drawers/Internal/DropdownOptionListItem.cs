// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;
using System;

namespace UnityEditor.UIElements
{
    internal class DropdownOptionListItem : BindableElement
    {
        [Serializable]
        internal new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            public override object CreateInstance() => new DropdownOptionListItem();
        }

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
