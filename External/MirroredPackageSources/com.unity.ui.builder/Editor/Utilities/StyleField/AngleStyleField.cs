using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class AngleStyleField : DimensionStyleField
    {
        public new class UxmlFactory : UxmlFactory<AngleStyleField, UxmlTraits> {}

        public new class UxmlTraits : DimensionStyleField.UxmlTraits {}

        public AngleStyleField() : this(string.Empty) { }

        public AngleStyleField(string label) : base(label)
        {
            option = StyleFieldConstants.UnitDegree;
            units.Clear();
            units.Add(StyleFieldConstants.UnitDegree);
            units.Add(StyleFieldConstants.UnitGrad);
            units.Add(StyleFieldConstants.UnitRad);
            units.Add(StyleFieldConstants.UnitTurn);
            populatesOptionsMenuFromParentRow = false;
            valueConverter = ConvertAngle;

            UpdateOptionsMenu();
        }

        static float GetFactorToDegree(Dimension.Unit unit)
        {
            switch (unit)
            {
                case Dimension.Unit.Gradian:
                    return 360f / 400;
                case Dimension.Unit.Radian:
                    return Mathf.Rad2Deg;
                case Dimension.Unit.Turn:
                    return 360f;
                default:
                    return 1;
            }
        }

        static float GetFactor(Dimension.Unit unit1, Dimension.Unit unit2)
        {
            return GetFactorToDegree(unit1) / GetFactorToDegree(unit2);
        }

        static float ConvertAngle(float angle, Dimension.Unit fromUnit, Dimension.Unit toUnit)
        {
            return BuilderEditorUtility.FixRoundOff(angle * GetFactor(fromUnit, toUnit));
        }
    }
}
