// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlainPropertyField : PropertyField
    {
        public PlainPropertyField(SerializedProperty property) : base(property)
        {
            RegisterCallback<GeometryChangedEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(GeometryChangedEvent evt)
        {
            var toggle = this.Q<Toggle>();
            if (toggle == null || toggle.parent.parent != this)
                return;

            toggle.value = true;
            toggle.style.display = DisplayStyle.None;

            toggle.parent.Q<VisualElement>("unity-content").style.marginLeft = 0;
        }
    }
}
