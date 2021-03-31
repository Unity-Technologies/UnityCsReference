using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for selecting a color.
    /// </summary>
    public class ColorField : BaseField<Color>
    {
        /// <summary>
        /// Instantiates a <see cref="ColorField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ColorField"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<Color, UxmlColorAttributeDescription>
        {
            UxmlBoolAttributeDescription m_ShowEyeDropper = new UxmlBoolAttributeDescription { name = "show-eye-dropper", defaultValue = true };
            UxmlBoolAttributeDescription m_ShowAlpha = new UxmlBoolAttributeDescription { name = "show-alpha", defaultValue = true };
            UxmlBoolAttributeDescription m_Hdr = new UxmlBoolAttributeDescription { name = "hdr" };

            /// <summary>
            /// Initialize <see cref="ColorField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ColorField)ve).showEyeDropper = m_ShowEyeDropper.GetValueFromBag(bag, cc);
                ((ColorField)ve).showAlpha = m_ShowAlpha.GetValueFromBag(bag, cc);
                ((ColorField)ve).hdr = m_Hdr.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// If true, the color picker will show the eyedropper control. If false, the color picker won't show the eyedropper control.
        /// </summary>
        public bool showEyeDropper { get; set; }
        /// <summary>
        /// If true, allows the user to set an alpha value for the color. If false, hides the alpha component.
        /// </summary>
        public bool showAlpha { get; set; }
        /// <summary>
        /// If true, treats the color as an HDR value. If false, treats the color as a standard LDR value.
        /// </summary>
        public bool hdr { get; set; }

        private IMGUIContainer m_ColorField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-color-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of ColorField.
        /// </summary>
        public ColorField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of ColorField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public ColorField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            showEyeDropper = true;
            showAlpha = true;

            // The focus on a color field is implemented like a BaseCompoundField : the ColorField and its inner child
            // are both put in the focus ring. When the ColorField is receiving the Focus, it is "delegating" it to the inner child,
            // which is, in this case, the IMGUIContainer.
            m_ColorField = new IMGUIContainer(OnGUIHandler) { name = "unity-internal-color-field", focusOnlyIfHasFocusableControls = false };
            visualInput = m_ColorField;
            visualInput.AddToClassList(inputUssClassName);

            labelElement.focusable = false;
        }

        private void OnGUIHandler()
        {
            // Dirty repaint on eye dropper update to preview the color under the cursor
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                IncrementVersion(VersionChangeType.Repaint);
            }

            var editorGUIShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = showMixedValue;

            Color newColor = EditorGUILayout.ColorField(GUIContent.none, value, showEyeDropper, showAlpha, hdr);
            if (value != newColor)
                value = newColor;

            EditorGUI.showMixedValue = editorGUIShowMixedValue;
        }

        protected override void UpdateMixedValueContent()
        {
        }
    }
}
