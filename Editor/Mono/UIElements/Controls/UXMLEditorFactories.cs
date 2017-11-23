// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

            // Primitives
            Factories.RegisterFactory<DoubleField>((bag, __) => new DoubleField());
            Factories.RegisterFactory<IntegerField>((bag, __) => new IntegerField());
            Factories.RegisterFactory<CurveField>((bag, __) => new CurveField());
            Factories.RegisterFactory<ObjectField>((bag, __) => new ObjectField());
            Factories.RegisterFactory<ColorField>((bag, __) => new ColorField());

            // Compounds
            Factories.RegisterFactory<RectField>((bag, __) => new RectField());
            Factories.RegisterFactory<Vector2Field>((bag, __) => new Vector2Field());
            Factories.RegisterFactory<Vector3Field>((bag, __) => new Vector3Field());
            Factories.RegisterFactory<Vector4Field>((bag, __) => new Vector4Field());
            Factories.RegisterFactory<BoundsField>((bag, __) => new BoundsField());
        }
    }
}
