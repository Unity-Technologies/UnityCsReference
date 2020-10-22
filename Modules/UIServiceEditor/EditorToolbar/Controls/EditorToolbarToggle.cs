// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class EditorToolbarToggle : ToolbarToggle
    {
        public new class UxmlFactory : UxmlFactory<EditorToolbarToggle, UxmlTraits> {}
        public new class UxmlTraits : ToolbarToggle.UxmlTraits {}

        public new const string ussClassName = "unity-editor-toolbar-toggle";
        public const string iconOnClassName = EditorToolbar.elementIconClassName + "-on";
        public const string iconOffClassName = EditorToolbar.elementIconClassName + "-off";

        GUIContent m_OnContent;
        GUIContent m_OffContent;

        public readonly TextElement textElement;
        public readonly VisualElement iconElement;

        public GUIContent onContent
        {
            get => m_OnContent;
            set
            {
                m_OnContent = value;
                UpdateContent();
            }
        }

        public GUIContent offContent
        {
            get => m_OffContent;
            set
            {
                m_OffContent = value;
                UpdateContent();
            }
        }

        public EditorToolbarToggle()
        {
            AddToClassList(ussClassName);
            var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);
            iconElement = EditorToolbarUtility.AddIconElement(input);
            textElement = EditorToolbarUtility.AddTextElement(input);
            UpdateContent();
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateContent();
        }

        void UpdateContent()
        {
            iconElement.EnableInClassList(iconOnClassName, value);
            iconElement.EnableInClassList(iconOffClassName, !value);

            var content = value ? onContent : offContent;
            if (content != null)
            {
                textElement.text = content.text;
                tooltip = content.tooltip;

                var image = content.image as Texture2D;
                if (image != null)
                    iconElement.style.backgroundImage = new StyleBackground(Background.FromTexture2D(image));
            }
        }
    }
}
