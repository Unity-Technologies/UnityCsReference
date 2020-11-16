using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class FoldoutWithCheckbox : PersistedFoldout
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<FoldoutWithCheckbox, UxmlTraits> { }

        public new class UxmlTraits : PersistedFoldout.UxmlTraits { }

        const string k_UssPath = BuilderConstants.UtilitiesPath + "/FoldoutWithCheckbox/FoldoutWithCheckbox.uss";
        const string k_CheckboxClassName = "unity-foldout__checkbox";
        const string k_LabelClassName = "unity-foldout-with-checkbox__label";

        readonly Toggle m_Checkbox;
        readonly Label m_Label;

        public FoldoutWithCheckbox()
        {
            m_Toggle.text = string.Empty;
            m_Toggle.style.flexGrow = 0;

            m_Checkbox = new Toggle();
            m_Checkbox.style.flexGrow = 0;

            m_Checkbox.AddToClassList(k_CheckboxClassName);
            m_Checkbox.RegisterValueChangedCallback(e
                => SetCheckboxValueWithoutNotify(e.newValue));
            m_Header.hierarchy.Add(m_Checkbox);

            m_Label = new Label();
            m_Label.AddToClassList(k_LabelClassName);
            m_Label.AddManipulator(new Clickable(evt =>
            {
                if ((evt as MouseUpEvent)?.button == (int)MouseButton.LeftMouse)
                {
                    m_Toggle.value = !m_Toggle.value;
                }
            }));

            m_Header.hierarchy.Add(m_Label);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));
        }

        public override string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public void SetCheckboxValueWithoutNotify(bool newValue)
        {
            m_Checkbox.SetValueWithoutNotify(newValue);
            contentContainer.SetEnabled(newValue);
        }

        public void RegisterCheckboxValueChangedCallback(EventCallback<ChangeEvent<bool>> callback)
        {
            m_Checkbox.RegisterCallback(callback);
        }
    }
}
