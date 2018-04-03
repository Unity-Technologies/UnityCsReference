// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class UXMLEditorFactories
    {
        private static bool s_Registered;

        internal static void RegisterAll()
        {
            if (s_Registered)
                return;

            s_Registered = true;

            IUxmlFactory[] factories =
            {
                // Primitives
                new FloatField.FloatFieldFactory(),
                new DoubleField.DoubleFieldFactory(),
                new IntegerField.IntegerFieldFactory(),
                new LongField.LongFieldFactory(),
                new CurveField.CurveFieldFactory(),
                new ObjectField.ObjectFieldFactory(),
                new ColorField.ColorFieldFactory(),
                new EnumField.EnumFieldFactory(),
                new GradientField.GradientFieldFactory(),

                // Compounds
                new RectField.RectFieldFactory(),
                new Vector2Field.Vector2FieldFactory(),
                new Vector3Field.Vector3FieldFactory(),
                new Vector4Field.Vector4FieldFactory(),
                new BoundsField.BoundsFieldFactory(),
                new PropertyControl<int>.PropertyControlFactory(),
                new PropertyControl<long>.PropertyControlFactory(),
                new PropertyControl<float>.PropertyControlFactory(),
                new PropertyControl<double>.PropertyControlFactory(),
                new PropertyControl<string>.PropertyControlFactory(),
            };
            foreach (IUxmlFactory factory in factories)
            {
                VisualElementFactoryRegistry.RegisterFactory(factory);
            }
        }
    }
}
