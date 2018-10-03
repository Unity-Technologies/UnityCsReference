// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    [InitializeOnLoad]
    internal class UXMLEditorFactories
    {
        private static bool s_Registered;

        static UXMLEditorFactories()
        {
            if (s_Registered)
                return;

            s_Registered = true;

            IUxmlFactory[] factories =
            {
                // Primitives
                new TextElement.UxmlFactory(),
                new FloatField.UxmlFactory(),
                new DoubleField.UxmlFactory(),
                new IntegerField.UxmlFactory(),
                new LongField.UxmlFactory(),
                new CurveField.UxmlFactory(),
                new ObjectField.UxmlFactory(),
                new ColorField.UxmlFactory(),
                new EnumField.UxmlFactory(),
                new MaskField.UxmlFactory(),
                new LayerMaskField.UxmlFactory(),
                new LayerField.UxmlFactory(),
                new TagField.UxmlFactory(),
                new GradientField.UxmlFactory(),

                // Compounds
                new RectField.UxmlFactory(),
                new Vector2Field.UxmlFactory(),
                new Vector3Field.UxmlFactory(),
                new Vector4Field.UxmlFactory(),
                new BoundsField.UxmlFactory(),
                new PropertyControl<int>.UxmlFactory(),
                new PropertyControl<long>.UxmlFactory(),
                new PropertyControl<float>.UxmlFactory(),
                new PropertyControl<double>.UxmlFactory(),
                new PropertyControl<string>.UxmlFactory(),

                new RectIntField.UxmlFactory(),
                new Vector2IntField.UxmlFactory(),
                new Vector3IntField.UxmlFactory(),
                new BoundsIntField.UxmlFactory(),
                new VisualSplitter.UxmlFactory(),
                // Toolbar
                new Toolbar.UxmlFactory(),
                new ToolbarButton.UxmlFactory(),
                new ToolbarToggle.UxmlFactory(),
                new ToolbarSpacer.UxmlFactory(),
                new ToolbarFlexSpacer.UxmlFactory(),
                new ToolbarMenu.UxmlFactory(),
                new ToolbarPopup.UxmlFactory(),
                new ToolbarSearchField.UxmlFactory(),
                new ToolbarPopupSearchField.UxmlFactory(),
                // Bound
                new PropertyField.UxmlFactory(),
                new InspectorElement.UxmlFactory(),
            };

            foreach (IUxmlFactory factory in factories)
            {
                VisualElementFactoryRegistry.RegisterFactory(factory);
            }
        }
    }
}
