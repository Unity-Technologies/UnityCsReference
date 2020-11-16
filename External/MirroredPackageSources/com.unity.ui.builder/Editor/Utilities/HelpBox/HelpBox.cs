using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class HelpBox : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((HelpBox)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        public string text { get; set; }

        public HelpBox()
        {
            Add(new IMGUIContainer(() =>
            {
                EditorGUILayout.HelpBox(text, MessageType.Info, true);
            }));
        }
    }
}
